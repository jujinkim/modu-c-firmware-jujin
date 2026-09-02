using System.Globalization;
using System.Text.RegularExpressions;
using ModuKeymapStudio.Core.Models;
using ModuKeymapStudio.Core.Parsing;

namespace ModuKeymapStudio.Core.Editing;

public static partial class KeymapEditor
{
    private static readonly HashSet<string> LayerBehaviors = ["&mo", "&to", "&tog", "&sl", "&lt"];

    public static bool IsValidNodeName(string value) => NodeNameRegex().IsMatch(value);

    public static bool IsEmptyBinding(Binding binding) =>
        binding.Raw is "&trans" or "&none";

    public static KeymapDocument ReplaceBinding(KeymapDocument document, int layerIndex, int keyIndex, string rawBinding)
    {
        if (layerIndex < 0 || layerIndex >= document.Layers.Count)
            throw new ArgumentOutOfRangeException(nameof(layerIndex));
        var layer = document.Layers[layerIndex];
        if (keyIndex < 0 || keyIndex >= layer.Bindings.Count)
            throw new ArgumentOutOfRangeException(nameof(keyIndex));

        var trimmed = rawBinding.Trim();
        if (!BindingSyntax.IsValid(trimmed))
            throw new ArgumentException("바인딩은 &behavior 형식의 한 줄 ZMK 표현이어야 합니다.", nameof(rawBinding));

        var binding = layer.Bindings[keyIndex];
        if (binding.Raw == trimmed) return document;
        return KeymapParser.Parse(ReplaceRange(document.Source, binding.Start, binding.End, trimmed));
    }

    public static KeymapDocument RenameLayer(
        KeymapDocument document,
        int layerIndex,
        string nodeName,
        string displayName)
    {
        if (layerIndex < 0 || layerIndex >= document.Layers.Count)
            throw new ArgumentOutOfRangeException(nameof(layerIndex));

        nodeName = nodeName.Trim();
        displayName = displayName.Trim();
        var layer = document.Layers[layerIndex];
        if (!IsValidNodeName(nodeName))
            throw new ArgumentException("노드 이름은 영문 또는 밑줄로 시작하고 영문, 숫자, 밑줄, 하이픈만 사용할 수 있습니다.", nameof(nodeName));
        if (layerIndex == 0 && !string.Equals(nodeName, "default_layer", StringComparison.Ordinal))
            throw new ArgumentException("기본 레이어의 노드 이름 default_layer는 변경할 수 없습니다.", nameof(nodeName));
        if (document.Layers.Any(item => item.Index != layerIndex && string.Equals(item.NodeName, nodeName, StringComparison.Ordinal)))
            throw new ArgumentException("같은 노드 이름의 레이어가 이미 있습니다.", nameof(nodeName));
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("표시 이름을 입력하세요.", nameof(displayName));
        if (displayName.IndexOfAny(['\r', '\n']) >= 0)
            throw new ArgumentException("표시 이름은 한 줄이어야 합니다.", nameof(displayName));

        var escapedDisplayName = EscapeDisplayName(displayName);
        var replacements = new List<Replacement>();
        if (!string.Equals(layer.NodeName, nodeName, StringComparison.Ordinal))
            replacements.Add(new Replacement(layer.NodeStart, layer.NodeStart + layer.NodeName.Length, nodeName));

        if (layer.DisplayNameStart is int displayStart && layer.DisplayNameEnd is int displayEnd)
        {
            if (!string.Equals(document.Source[displayStart..displayEnd], escapedDisplayName, StringComparison.Ordinal))
                replacements.Add(new Replacement(displayStart, displayEnd, escapedDisplayName));
        }
        else
        {
            var openBrace = document.Source.IndexOf('{', layer.NodeStart + layer.NodeName.Length);
            if (openBrace < 0 || openBrace >= layer.BindingsStart)
                throw new InvalidOperationException("레이어의 여는 중괄호를 찾을 수 없습니다.");
            var insertAt = GetLineEndIncludingNewLine(document.Source, openBrace + 1);
            var indent = GetLineIndent(document.Source, layer.NodeStart) + "    ";
            replacements.Add(new Replacement(insertAt, insertAt, $"{indent}display-name = \"{escapedDisplayName}\";{document.NewLine}"));
        }

        if (replacements.Count == 0) return document;
        return KeymapParser.Parse(ApplyReplacements(document.Source, replacements));
    }

    public static KeymapDocument MoveBinding(
        KeymapDocument document,
        int layerIndex,
        int sourceKeyIndex,
        int targetKeyIndex,
        KeyMoveOperation operation)
    {
        if (layerIndex < 0 || layerIndex >= document.Layers.Count)
            throw new ArgumentOutOfRangeException(nameof(layerIndex));
        if (!KeymapDocument.IsEditableKeyIndex(sourceKeyIndex))
            throw new ArgumentOutOfRangeException(nameof(sourceKeyIndex), "실제 키만 이동하거나 복사할 수 있습니다.");
        if (!KeymapDocument.IsEditableKeyIndex(targetKeyIndex))
            throw new ArgumentOutOfRangeException(nameof(targetKeyIndex), "실제 키만 대상으로 사용할 수 있습니다.");
        if (sourceKeyIndex == targetKeyIndex)
            throw new ArgumentException("같은 키에는 드롭할 수 없습니다.", nameof(targetKeyIndex));

        var layer = document.Layers[layerIndex];
        if (sourceKeyIndex >= layer.Bindings.Count || targetKeyIndex >= layer.Bindings.Count)
            throw new ArgumentOutOfRangeException(nameof(targetKeyIndex));

        var source = layer.Bindings[sourceKeyIndex];
        var target = layer.Bindings[targetKeyIndex];
        if (IsEmptyBinding(source))
            throw new InvalidOperationException("&trans와 &none 바인딩은 드래그할 수 없습니다.");

        var targetIsEmpty = IsEmptyBinding(target);
        var requiresEmptyTarget = operation is KeyMoveOperation.Move or KeyMoveOperation.Copy;
        if (requiresEmptyTarget && !targetIsEmpty)
            throw new InvalidOperationException("이 작업은 빈 대상 키에서만 사용할 수 있습니다.");
        if (!requiresEmptyTarget && targetIsEmpty)
            throw new InvalidOperationException("이 작업은 할당된 대상 키에서만 사용할 수 있습니다.");

        var replacements = new List<Replacement>
        {
            new(target.Start, target.End, source.Raw)
        };
        if (operation is KeyMoveOperation.Move or KeyMoveOperation.OverwriteMove)
            replacements.Add(new Replacement(source.Start, source.End, layerIndex == 0 ? "&none" : "&trans"));
        else if (operation == KeyMoveOperation.Swap)
            replacements.Add(new Replacement(source.Start, source.End, target.Raw));

        return KeymapParser.Parse(ApplyReplacements(document.Source, replacements));
    }

    public static KeymapDocument SetSafetyHoldBinding(
        KeymapDocument document,
        int layerIndex,
        int keyIndex,
        SafetyHoldAction action)
    {
        var (label, nodeName, holdBehavior) = action switch
        {
            SafetyHoldAction.Bootloader => ("mks_boot_hold", "mks_bootloader_hold", "&bootloader"),
            SafetyHoldAction.SystemReset => ("mks_reset_hold", "mks_system_reset_hold", "&sys_reset"),
            _ => throw new ArgumentOutOfRangeException(nameof(action))
        };

        var updated = EnsureSafetyHoldDefinition(document, label, nodeName, holdBehavior);
        return ReplaceBinding(updated, layerIndex, keyIndex, $"&{label} 0 0");
    }

    private static KeymapDocument EnsureSafetyHoldDefinition(
        KeymapDocument document,
        string label,
        string nodeName,
        string holdBehavior)
    {
        var mask = KeymapParser.CreateCodeMask(document.Source);
        if (Regex.IsMatch(mask, $@"\b{Regex.Escape(label)}\s*:", RegexOptions.CultureInvariant))
            return document;

        var newLine = document.NewLine;
        var behaviorsMatch = BehaviorsNodeRegex().Match(mask);
        string insertion;
        int insertAt;
        if (behaviorsMatch.Success)
        {
            var openBrace = mask.IndexOf('{', behaviorsMatch.Index);
            var closeBrace = KeymapParser.FindMatching(mask, openBrace, '{', '}');
            if (closeBrace < 0)
                throw new InvalidOperationException("behaviors 노드가 닫히지 않았습니다.");
            insertAt = GetLineStart(document.Source, closeBrace);
            var nodeIndent = GetLineIndent(document.Source, behaviorsMatch.Index) + "    ";
            insertion = CreateSafetyHoldDefinition(nodeIndent, label, nodeName, holdBehavior, newLine) + newLine;
        }
        else
        {
            var keymapLineStart = GetLineStart(document.Source, document.KeymapOpenBrace);
            var rootChildIndent = GetLineIndent(document.Source, keymapLineStart);
            var nodeIndent = rootChildIndent + "    ";
            insertAt = keymapLineStart;
            insertion =
                $"{rootChildIndent}behaviors {{{newLine}" +
                CreateSafetyHoldDefinition(nodeIndent, label, nodeName, holdBehavior, newLine) +
                $"{rootChildIndent}}};{newLine}{newLine}";
        }

        return KeymapParser.Parse(document.Source.Insert(insertAt, insertion));
    }

    private static string CreateSafetyHoldDefinition(
        string indent,
        string label,
        string nodeName,
        string holdBehavior,
        string newLine)
    {
        var propertyIndent = indent + "    ";
        return
            $"{indent}{label}: {nodeName} {{{newLine}" +
            $"{propertyIndent}compatible = \"zmk,behavior-hold-tap\";{newLine}" +
            $"{propertyIndent}#binding-cells = <2>;{newLine}" +
            $"{propertyIndent}flavor = \"tap-preferred\";{newLine}" +
            $"{propertyIndent}tapping-term-ms = <500>;{newLine}" +
            $"{propertyIndent}bindings = <{holdBehavior}>, <&none>;{newLine}" +
            $"{indent}}};{newLine}";
    }

    public static KeymapDocument AddLayer(
        KeymapDocument document,
        string nodeName,
        string displayName,
        bool cloneCurrent,
        int currentLayerIndex)
    {
        nodeName = nodeName.Trim();
        displayName = displayName.Trim();
        if (!IsValidNodeName(nodeName))
            throw new ArgumentException("노드 이름은 영문 또는 밑줄로 시작하고 영문, 숫자, 밑줄, 하이픈만 사용할 수 있습니다.");
        if (document.Layers.Any(layer => string.Equals(layer.NodeName, nodeName, StringComparison.Ordinal)))
            throw new ArgumentException("같은 노드 이름의 레이어가 이미 있습니다.");
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("표시 이름을 입력하세요.");
        if (currentLayerIndex < 0 || currentLayerIndex >= document.Layers.Count)
            throw new ArgumentOutOfRangeException(nameof(currentLayerIndex));

        var sourceBindings = cloneCurrent
            ? document.Layers[currentLayerIndex].Bindings.Select(binding => binding.Raw).ToArray()
            : Enumerable.Repeat("&trans", KeymapDocument.ModuBindingCount).ToArray();

        if (sourceBindings.Length != KeymapDocument.ModuBindingCount)
            throw new InvalidOperationException($"복제할 레이어의 바인딩 수가 {KeymapDocument.ModuBindingCount}개가 아닙니다.");

        var indent = GetLineIndent(document.Source, document.Layers[0].NodeStart);
        var propertyIndent = indent + "    ";
        var bindingIndent = propertyIndent + "    ";
        var escapedDisplayName = EscapeDisplayName(displayName);
        var lines = new List<string>();
        for (var row = 0; row < 5; row++)
            lines.Add(bindingIndent + string.Join("  ", sourceBindings.Skip(row * 12).Take(12)));
        lines.Add(bindingIndent + string.Join("  ", sourceBindings.Skip(60).Take(7)));

        var nl = document.NewLine;
        var block =
            $"{indent}{nodeName} {{{nl}" +
            $"{propertyIndent}display-name = \"{escapedDisplayName}\";{nl}" +
            $"{propertyIndent}bindings = <{nl}" +
            string.Join(nl, lines) + nl +
            $"{propertyIndent}>;{nl}" +
            $"{indent}}};{nl}{nl}";

        var insertAt = GetLineStart(document.Source, document.KeymapCloseBrace);
        var prefix = document.Source[..insertAt];
        if (!prefix.EndsWith(nl + nl, StringComparison.Ordinal)) block = nl + block;
        return KeymapParser.Parse(document.Source.Insert(insertAt, block));
    }

    public static IReadOnlyList<LayerReference> GetLayerReferences(KeymapDocument document)
    {
        var references = new List<LayerReference>();
        foreach (var layer in document.Layers)
        {
            foreach (var binding in layer.Bindings)
            {
                var match = ReferenceRegex().Match(binding.Raw);
                if (!match.Success || !LayerBehaviors.Contains(match.Groups["behavior"].Value)) continue;

                var targetGroup = match.Groups["target"];
                var target = 0;
                var isResolved = targetGroup.Success && int.TryParse(targetGroup.Value, NumberStyles.None, CultureInfo.InvariantCulture, out target);
                references.Add(new LayerReference(
                    layer.Index,
                    layer.NodeName,
                    binding.Index,
                    match.Groups["behavior"].Value,
                    isResolved ? target : null,
                    isResolved,
                    targetGroup.Success ? binding.Start + targetGroup.Index : binding.End,
                    targetGroup.Success ? binding.Start + targetGroup.Index + targetGroup.Length : binding.End));
            }
        }
        return references;
    }

    public static KeymapDocument DeleteLayer(KeymapDocument document, int layerIndex)
    {
        if (layerIndex == 0)
            throw new LayerDeletionException("기본 레이어 0은 삭제할 수 없습니다.", []);
        if (layerIndex < 0 || layerIndex >= document.Layers.Count)
            throw new ArgumentOutOfRangeException(nameof(layerIndex));

        var references = GetLayerReferences(document)
            .Where(reference => reference.SourceLayerIndex != layerIndex)
            .ToArray();
        var blockers = references
            .Where(reference => !reference.IsResolved || reference.TargetLayerIndex == layerIndex)
            .ToArray();

        if (blockers.Length > 0)
        {
            var reason = blockers.Any(reference => !reference.IsResolved)
                ? "심볼 또는 해석할 수 없는 레이어 참조가 있어 안전하게 삭제할 수 없습니다."
                : "다른 키가 이 레이어를 참조하고 있어 삭제할 수 없습니다.";
            throw new LayerDeletionException(reason, blockers);
        }

        var replacements = references
            .Where(reference => reference.TargetLayerIndex > layerIndex)
            .Select(reference => new Replacement(
                reference.ArgumentStart,
                reference.ArgumentEnd,
                (reference.TargetLayerIndex!.Value - 1).ToString(CultureInfo.InvariantCulture)))
            .ToList();

        var layer = document.Layers[layerIndex];
        var deleteStart = GetLineStart(document.Source, layer.NodeStart);
        var deleteEnd = GetLineEndIncludingNewLine(document.Source, layer.BlockEnd);
        replacements.Add(new Replacement(deleteStart, deleteEnd, string.Empty));

        return KeymapParser.Parse(ApplyReplacements(document.Source, replacements));
    }

    public static string ApplyReplacements(string source, IEnumerable<(int Start, int End, string Value)> replacements) =>
        ApplyReplacements(source, replacements.Select(item => new Replacement(item.Start, item.End, item.Value)));

    private static string ApplyReplacements(string source, IEnumerable<Replacement> replacements)
    {
        foreach (var replacement in replacements.OrderByDescending(item => item.Start))
            source = ReplaceRange(source, replacement.Start, replacement.End, replacement.Value);
        return source;
    }

    private static string ReplaceRange(string source, int start, int end, string value) =>
        string.Concat(source.AsSpan(0, start), value, source.AsSpan(end));

    private static int GetLineStart(string source, int position)
    {
        while (position > 0 && source[position - 1] is not '\r' and not '\n') position--;
        return position;
    }

    private static int GetLineEndIncludingNewLine(string source, int position)
    {
        while (position < source.Length && source[position] is not '\r' and not '\n') position++;
        if (position < source.Length && source[position] == '\r') position++;
        if (position < source.Length && source[position] == '\n') position++;
        return position;
    }

    private static string GetLineIndent(string source, int position)
    {
        var lineStart = GetLineStart(source, position);
        var end = lineStart;
        while (end < source.Length && source[end] is ' ' or '\t') end++;
        return source[lineStart..end];
    }

    private static string EscapeDisplayName(string displayName) =>
        displayName.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

    private sealed record Replacement(int Start, int End, string Value);

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex NodeNameRegex();

    [GeneratedRegex(@"^(?<behavior>&[A-Za-z_][A-Za-z0-9_]*)(?:\s+(?<target>\S+))?", RegexOptions.CultureInvariant)]
    private static partial Regex ReferenceRegex();

    [GeneratedRegex(@"\bbehaviors\s*\{", RegexOptions.CultureInvariant)]
    private static partial Regex BehaviorsNodeRegex();
}
