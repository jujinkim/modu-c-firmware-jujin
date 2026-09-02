using System.Text.RegularExpressions;
using ModuKeymapStudio.Core.Models;

namespace ModuKeymapStudio.Core.Parsing;

public static partial class KeymapParser
{
    public static KeymapDocument Parse(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new KeymapParseException("키맵 파일이 비어 있습니다.");

        var mask = CreateCodeMask(source);
        var keymapMatch = KeymapNodeRegex().Match(mask);
        if (!keymapMatch.Success)
            throw new KeymapParseException("keymap 노드를 찾을 수 없습니다.");

        var openBrace = mask.IndexOf('{', keymapMatch.Index);
        var closeBrace = FindMatching(mask, openBrace, '{', '}');
        if (closeBrace < 0)
            throw new KeymapParseException("keymap 노드가 닫히지 않았습니다.");

        var layers = ParseLayers(source, mask, openBrace, closeBrace);
        if (layers.Count == 0)
            throw new KeymapParseException("bindings 속성이 있는 레이어를 찾을 수 없습니다.");

        var newLine = source.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        return new KeymapDocument(source, layers, openBrace, closeBrace, newLine);
    }

    private static List<Layer> ParseLayers(string source, string mask, int keymapOpen, int keymapClose)
    {
        var layers = new List<Layer>();
        var cursor = keymapOpen + 1;

        while (cursor < keymapClose)
        {
            if (!IsNodeNameStart(mask[cursor]))
            {
                cursor++;
                continue;
            }

            var nameStart = cursor;
            while (cursor < keymapClose && IsNodeNamePart(mask[cursor])) cursor++;
            var nodeName = source[nameStart..cursor];
            var afterName = cursor;
            while (afterName < keymapClose && char.IsWhiteSpace(mask[afterName])) afterName++;

            if (afterName >= keymapClose || mask[afterName] != '{')
            {
                cursor = afterName + 1;
                continue;
            }

            var blockClose = FindMatching(mask, afterName, '{', '}');
            if (blockClose < 0 || blockClose > keymapClose)
                throw new KeymapParseException($"{nodeName} 레이어 노드가 닫히지 않았습니다.");

            var blockEnd = blockClose + 1;
            while (blockEnd < source.Length && char.IsWhiteSpace(source[blockEnd]) && source[blockEnd] is not '\r' and not '\n') blockEnd++;
            if (blockEnd < source.Length && source[blockEnd] == ';') blockEnd++;

            var bindingProperty = FindBindingsProperty(mask, afterName + 1, blockClose);
            if (bindingProperty is not null)
            {
                var (bodyStart, bodyEnd) = bindingProperty.Value;
                var bindings = ParseBindings(source, mask, bodyStart, bodyEnd);
                var displayName = FindDisplayName(source, afterName + 1, blockClose);
                layers.Add(new Layer(
                    layers.Count,
                    nodeName,
                    displayName?.Value ?? nodeName,
                    nameStart,
                    blockEnd,
                    bodyStart,
                    bodyEnd,
                    bindings,
                    displayName?.Start,
                    displayName?.End));
            }

            cursor = blockEnd;
        }

        return layers;
    }

    private static (int Start, int End)? FindBindingsProperty(string mask, int start, int end)
    {
        var match = BindingsRegex().Match(mask, start);
        while (match.Success && match.Index < end)
        {
            var openAngle = mask.IndexOf('<', match.Index, end - match.Index);
            if (openAngle < 0) return null;
            var closeAngle = FindMatching(mask, openAngle, '<', '>');
            if (closeAngle > openAngle && closeAngle <= end)
                return (openAngle + 1, closeAngle);
            match = match.NextMatch();
        }

        return null;
    }

    private static IReadOnlyList<Binding> ParseBindings(string source, string mask, int start, int end)
    {
        var ampersands = new List<int>();
        for (var i = start; i < end; i++)
        {
            if (mask[i] == '&' && i + 1 < end && (char.IsLetter(mask[i + 1]) || mask[i + 1] == '_'))
                ampersands.Add(i);
        }

        var bindings = new List<Binding>(ampersands.Count);
        for (var index = 0; index < ampersands.Count; index++)
        {
            var bindingStart = ampersands[index];
            var segmentEnd = index + 1 < ampersands.Count ? ampersands[index + 1] : end;
            var bindingEnd = segmentEnd;
            while (bindingEnd > bindingStart && char.IsWhiteSpace(mask[bindingEnd - 1])) bindingEnd--;
            if (bindingEnd <= bindingStart) continue;
            bindings.Add(new Binding(index, source[bindingStart..bindingEnd], bindingStart, bindingEnd));
        }

        return bindings;
    }

    private static DisplayNameLocation? FindDisplayName(string source, int start, int end)
    {
        var block = source[start..end];
        var match = DisplayNameRegex().Match(block);
        if (!match.Success) return null;
        var value = match.Groups["value"];
        return new DisplayNameLocation(Regex.Unescape(value.Value), start + value.Index, start + value.Index + value.Length);
    }

    private sealed record DisplayNameLocation(string Value, int Start, int End);

    internal static int FindMatching(string mask, int openingIndex, char opening, char closing)
    {
        var depth = 0;
        for (var i = openingIndex; i < mask.Length; i++)
        {
            if (mask[i] == opening) depth++;
            else if (mask[i] == closing && --depth == 0) return i;
        }
        return -1;
    }

    internal static string CreateCodeMask(string source)
    {
        var chars = source.ToCharArray();
        var state = ScanState.Code;

        for (var i = 0; i < chars.Length; i++)
        {
            var current = source[i];
            var next = i + 1 < source.Length ? source[i + 1] : '\0';

            switch (state)
            {
                case ScanState.Code when current == '/' && next == '/':
                    chars[i] = chars[i + 1] = ' ';
                    i++;
                    state = ScanState.LineComment;
                    break;
                case ScanState.Code when current == '/' && next == '*':
                    chars[i] = chars[i + 1] = ' ';
                    i++;
                    state = ScanState.BlockComment;
                    break;
                case ScanState.Code when current == '"':
                    chars[i] = ' ';
                    state = ScanState.String;
                    break;
                case ScanState.LineComment:
                    if (current is '\r' or '\n') state = ScanState.Code;
                    else chars[i] = ' ';
                    break;
                case ScanState.BlockComment:
                    if (current == '*' && next == '/')
                    {
                        chars[i] = chars[i + 1] = ' ';
                        i++;
                        state = ScanState.Code;
                    }
                    else if (current is not '\r' and not '\n') chars[i] = ' ';
                    break;
                case ScanState.String:
                    if (current == '\\' && next != '\0')
                    {
                        chars[i] = chars[i + 1] = ' ';
                        i++;
                    }
                    else if (current == '"')
                    {
                        chars[i] = ' ';
                        state = ScanState.Code;
                    }
                    else if (current is not '\r' and not '\n') chars[i] = ' ';
                    break;
            }
        }

        return new string(chars);
    }

    private static bool IsNodeNameStart(char value) => char.IsLetter(value) || value == '_';
    private static bool IsNodeNamePart(char value) => char.IsLetterOrDigit(value) || "_,.@+*#?-".Contains(value);

    private enum ScanState { Code, LineComment, BlockComment, String }

    [GeneratedRegex(@"\bkeymap\s*\{", RegexOptions.CultureInvariant)]
    private static partial Regex KeymapNodeRegex();

    [GeneratedRegex(@"\bbindings\s*=\s*<", RegexOptions.CultureInvariant)]
    private static partial Regex BindingsRegex();

    [GeneratedRegex("\\bdisplay-name\\s*=\\s*\"(?<value>(?:\\\\.|[^\"\\\\])*)\"\\s*;", RegexOptions.CultureInvariant)]
    private static partial Regex DisplayNameRegex();
}
