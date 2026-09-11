using System.Globalization;
using System.Text.RegularExpressions;
using ModuKeymapStudio.Core.Models;
using ModuKeymapStudio.Core.Parsing;

namespace ModuKeymapStudio.Core.Editing;

public static partial class KeymapEditor
{
    public static IReadOnlyList<int> DelayOptions { get; } =
        Array.AsReadOnly(Enumerable.Range(1, 20).Select(value => value * 50).ToArray());

    // The first argument stays in the keymap binding, including layer references.
    // A one-parameter macro forwards it when the original behavior needs two arguments.
    public static KeymapDocument SetDelayedBinding(
        KeymapDocument document, int layerIndex, int keyIndex, string rawBinding, int? delayMs)
    {
        var raw = rawBinding.Trim();
        var existing = GetDelayedBinding(document, raw);
        raw = existing?.RawBinding ?? raw;
        if (delayMs is null) return ReplaceBinding(document, layerIndex, keyIndex, raw);
        if (!DelayOptions.Contains(delayMs.Value))
            throw new ArgumentOutOfRangeException(nameof(delayMs), "지연은 50~1000ms 사이에서 50ms 단위로 선택하세요.");

        var parts = SplitDelayBinding(raw);
        if (parts[0] == "&trans")
            throw new ArgumentException("투명 키에는 실행할 동작이 없습니다. 먼저 지연할 키나 동작을 선택하세요.", nameof(rawBinding));
        if (existing?.DelayMs == delayMs) return ReplaceBinding(document, layerIndex, keyIndex, rawBinding);
        var argument = parts.Length > 1 && !UsesFixedDelayMacro(parts) ? parts[1] : "0";

        var mask = KeymapParser.CreateCodeMask(document.Source);
        foreach (Match match in Regex.Matches(mask, @"\b(?<label>mks_delay_\d+)\s*:"))
        {
            var candidate = $"&{match.Groups["label"].Value} {argument} 0";
            var setting = GetDelayedBinding(document, candidate);
            if (setting?.DelayMs == delayMs && setting.RawBinding == string.Join(' ', parts))
                return ReplaceBinding(document, layerIndex, keyIndex, candidate);
        }

        var id = 1;
        while (Regex.IsMatch(mask, $@"\bmks_delay_{id}(?:_action)?\b")) id++;
        var label = $"mks_delay_{id}";
        var definition = CreateDelayDefinition(label, parts, delayMs.Value, document.NewLine);
        var updated = InsertDelayDefinition(document, definition);
        return ReplaceBinding(updated, layerIndex, keyIndex,
            $"&{label} {argument} 0");
    }

    public static DelayedBinding? GetDelayedBinding(KeymapDocument document, string rawBinding)
    {
        string[] parts;
        try { parts = SplitDelayBinding(rawBinding.Trim()); }
        catch (ArgumentException) { return null; }
        if (parts.Length != 3 || parts[2] != "0") return null;
        var label = parts[0][1..];
        var legacy = label is "mks_boot_hold" or "mks_reset_hold";
        if (!legacy && !Regex.IsMatch(label, @"^mks_delay_\d+$")) return null;
        var body = FindDelayNodeBody(document.Source, label);
        if (body is null) return null;
        var match = Regex.Match(body,
            "^\\s*compatible\\s*=\\s*\"zmk,behavior-hold-tap\"\\s*;\\s*" +
            "#binding-cells\\s*=\\s*<2>\\s*;\\s*flavor\\s*=\\s*\"tap-preferred\"\\s*;\\s*" +
            @"tapping-term-ms\s*=\s*<(?<ms>\d+)>\s*;\s*bindings\s*=\s*<(?<hold>&\w+)>\s*,\s*<&none>\s*;\s*$");
        if (!match.Success || !int.TryParse(match.Groups["ms"].Value, out var delay)) return null;
        var hold = match.Groups["hold"].Value;
        if (legacy)
        {
            if (parts[1] != "0" || hold != (label == "mks_boot_hold" ? "&bootloader" : "&sys_reset")) return null;
            return new DelayedBinding(hold, delay);
        }

        // Generated nodes carry the original arity in a comment; verify the complete
        // node shape before interpreting it, so hand-edited behaviors stay opaque.
        var nodeSource = FindDelayNodeBody(document.Source, label, stripComments: false)!;
        var arityMatch = Regex.Match(nodeSource, @"/\* mks-delay-args: (?<arity>[0123]) \*/");
        if (!arityMatch.Success) return null;
        var arity = int.Parse(arityMatch.Groups["arity"].Value, CultureInfo.InvariantCulture);
        if (arity == 0) return parts[1] == "0" ? new DelayedBinding(hold, delay) : null;
        if (arity == 1) return new DelayedBinding($"{hold} {parts[1]}", delay);
        if (hold != $"&{label}_action") return null;
        var macro = FindDelayNodeBody(document.Source, label + "_action");
        if (macro is null) return null;
        if (arity == 3)
        {
            var fixedMatch = Regex.Match(macro,
                "^\\s*compatible\\s*=\\s*\"zmk,behavior-macro\"\\s*;\\s*" +
                @"#binding-cells\s*=\s*<0>\s*;\s*wait-ms\s*=\s*<0>\s*;\s*tap-ms\s*=\s*<0>\s*;\s*" +
                @"bindings\s*=\s*<&macro_press\s+(?<raw>&[^<>]+)>\s*,\s*<&macro_pause_for_release>\s*,\s*" +
                @"<&macro_release\s+\k<raw>>\s*;\s*$");
            return fixedMatch.Success && parts[1] == "0"
                ? new DelayedBinding(fixedMatch.Groups["raw"].Value.Trim(), delay) : null;
        }
        var macroMatch = Regex.Match(macro,
            "^\\s*compatible\\s*=\\s*\"zmk,behavior-macro-one-param\"\\s*;\\s*" +
            @"#binding-cells\s*=\s*<1>\s*;\s*wait-ms\s*=\s*<0>\s*;\s*tap-ms\s*=\s*<0>\s*;\s*" +
            @"bindings\s*=\s*<&macro_param_1to1>\s*,\s*<&macro_press\s+(?<action>&\w+)\s+0\s+(?<second>[^<>]+)>\s*,\s*" +
            @"<&macro_pause_for_release>\s*,\s*<&macro_param_1to1>\s*,\s*<&macro_release\s+\k<action>\s+0\s+\k<second>>\s*;\s*$");
        return macroMatch.Success
            ? new DelayedBinding($"{macroMatch.Groups["action"].Value} {parts[1]} {macroMatch.Groups["second"].Value.Trim()}", delay)
            : null;
    }

    private static string[] SplitDelayBinding(string raw)
    {
        if (!BindingSyntax.IsValid(raw) || raw.Contains("/*", StringComparison.Ordinal) || raw.Contains("//", StringComparison.Ordinal))
            throw new ArgumentException("지연할 ZMK 바인딩 한 개를 입력하세요.");
        var tokens = new List<string>();
        var start = 0;
        var depth = 0;
        for (var index = 0; index <= raw.Length; index++)
        {
            if (index < raw.Length)
            {
                if (raw[index] == '(') depth++;
                if (raw[index] == ')' && --depth < 0) throw new ArgumentException("바인딩 괄호가 올바르지 않습니다.");
                if (!char.IsWhiteSpace(raw[index]) || depth > 0) continue;
            }
            if (index > start) tokens.Add(raw[start..index]);
            start = index + 1;
        }
        if (depth != 0 || tokens.Count is < 1 or > 3 || tokens.Skip(1).Any(token => token.Contains('&') || token.IndexOfAny(['{', '}', '"']) >= 0))
            throw new ArgumentException("지연할 동작 한 개와 최대 두 개의 매개변수를 입력하세요.");
        return tokens.ToArray();
    }

    private static string? FindDelayNodeBody(string source, string label, bool stripComments = true)
    {
        var mask = KeymapParser.CreateCodeMask(source);
        var match = Regex.Match(mask, $@"\b{Regex.Escape(label)}\s*:\s*[\w-]+\s*\{{");
        if (!match.Success) return null;
        var open = mask.IndexOf('{', match.Index);
        var close = KeymapParser.FindMatching(mask, open, '{', '}');
        if (close < 0) return null;
        var body = source[(open + 1)..close];
        return stripComments ? Regex.Replace(body, @"/\*.*?\*/|//[^\r\n]*", " ", RegexOptions.Singleline) : body;
    }

    private static string CreateDelayDefinition(string label, string[] parts, int delayMs, string nl)
    {
        var lines = new List<string>();
        var fixedMacro = UsesFixedDelayMacro(parts);
        if (fixedMacro)
        {
            var raw = string.Join(' ', parts);
            lines.AddRange([
                $"{label}_action: {label}_action {{",
                "    compatible = \"zmk,behavior-macro\";",
                "    #binding-cells = <0>;",
                "    wait-ms = <0>;",
                "    tap-ms = <0>;",
                $"    bindings = <&macro_press {raw}>, <&macro_pause_for_release>, <&macro_release {raw}>;",
                "};"
            ]);
        }
        else if (parts.Length == 3)
        {
            lines.AddRange([
                $"{label}_action: {label}_action {{",
                "    compatible = \"zmk,behavior-macro-one-param\";",
                "    #binding-cells = <1>;",
                "    wait-ms = <0>;",
                "    tap-ms = <0>;",
                $"    bindings = <&macro_param_1to1>, <&macro_press {parts[0]} 0 {parts[2]}>,",
                $"               <&macro_pause_for_release>, <&macro_param_1to1>, <&macro_release {parts[0]} 0 {parts[2]}>;",
                "};"
            ]);
        }
        lines.AddRange([
            $"{label}: {label} {{",
            $"    /* mks-delay-args: {(fixedMacro ? 3 : parts.Length - 1)} */",
            "    compatible = \"zmk,behavior-hold-tap\";",
            "    #binding-cells = <2>;",
            "    flavor = \"tap-preferred\";",
            $"    tapping-term-ms = <{delayMs}>;",
            $"    bindings = <{(fixedMacro || parts.Length == 3 ? "&" + label + "_action" : parts[0])}>, <&none>;",
            "};"
        ]);
        return string.Join(nl, lines);
    }

    // Commands such as BT_CLR expand to TWO cells despite being one text token.
    // Keep unknown/macro-based arguments intact inside a zero-parameter macro.
    private static bool UsesFixedDelayMacro(string[] parts) => parts.Length > 1 &&
        parts[0] is not ("&kp" or "&mo" or "&to" or "&tog" or "&sl" or "&sk" or "&kt" or "&mkp" or "&lt" or "&mt");

    private static KeymapDocument InsertDelayDefinition(KeymapDocument document, string definition)
    {
        var mask = KeymapParser.CreateCodeMask(document.Source);
        var match = BehaviorsNodeRegex().Match(mask);
        var nl = document.NewLine;
        if (match.Success)
        {
            var close = KeymapParser.FindMatching(mask, mask.IndexOf('{', match.Index), '{', '}');
            if (close < 0) throw new ArgumentException("behaviors 노드가 닫히지 않았습니다.");
            var indent = GetLineIndent(document.Source, match.Index);
            var lineStart = GetLineStart(document.Source, close);
            var onOwnLine = string.IsNullOrWhiteSpace(document.Source[lineStart..close]);
            var block = string.Join(nl, definition.Split(nl).Select(line => indent + "    " + line)) + nl;
            return KeymapParser.Parse(document.Source.Insert(onOwnLine ? lineStart : close,
                onOwnLine ? block : nl + block + indent));
        }
        var keymapStart = Regex.Match(mask, @"\bkeymap\s*\{").Index;
        var rootIndent = GetLineIndent(document.Source, keymapStart);
        var node = $"behaviors {{{nl}" +
            string.Join(nl, definition.Split(nl).Select(line => rootIndent + "    " + line)) +
            $"{nl}{rootIndent}}};{nl}{nl}{rootIndent}";
        return KeymapParser.Parse(document.Source.Insert(keymapStart, node));
    }
}
