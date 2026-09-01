using System.Globalization;
using System.Text.RegularExpressions;
using ModuKeymapStudio.Core.Models;
using ModuKeymapStudio.Core.Parsing;

namespace ModuKeymapStudio.Core.Editing;

public static partial class KeymapEditor
{
    private static readonly HashSet<string> LayerBehaviors = ["&mo", "&to", "&tog", "&sl", "&lt"];

    public static bool IsValidNodeName(string value) => NodeNameRegex().IsMatch(value);

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
        var escapedDisplayName = displayName.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
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

    private sealed record Replacement(int Start, int End, string Value);

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex NodeNameRegex();

    [GeneratedRegex(@"^(?<behavior>&[A-Za-z_][A-Za-z0-9_]*)(?:\s+(?<target>\S+))?", RegexOptions.CultureInvariant)]
    private static partial Regex ReferenceRegex();
}
