using System.Text;
using ModuKeymapStudio.Core.Models;
using ModuKeymapStudio.Core.Parsing;

namespace ModuKeymapStudio.Core.IO;

public sealed class KeymapFile
{
    private KeymapFile(string path, byte[] originalBytes, string originalText, Encoding encoding, bool hasPreamble)
    {
        Path = path;
        OriginalBytes = originalBytes;
        OriginalText = originalText;
        Encoding = encoding;
        HasPreamble = hasPreamble;
        Document = KeymapParser.Parse(originalText);
    }

    public string Path { get; private set; }
    public byte[] OriginalBytes { get; private set; }
    public string OriginalText { get; private set; }
    public Encoding Encoding { get; }
    public bool HasPreamble { get; }
    public KeymapDocument Document { get; private set; }

    public static KeymapFile Load(string path)
    {
        var fullPath = System.IO.Path.GetFullPath(path);
        var bytes = File.ReadAllBytes(fullPath);
        var (encoding, preambleLength) = DetectEncoding(bytes);
        var text = encoding.GetString(bytes, preambleLength, bytes.Length - preambleLength);
        return new KeymapFile(fullPath, bytes, text, encoding, preambleLength > 0);
    }

    public void Save(string source, string? destinationPath = null)
    {
        var target = System.IO.Path.GetFullPath(destinationPath ?? Path);
        var bytes = source == OriginalText
            ? OriginalBytes
            : Encode(source, Encoding, HasPreamble);

        File.WriteAllBytes(target, bytes);
        Path = target;
        OriginalBytes = bytes;
        OriginalText = source;
        Document = KeymapParser.Parse(source);
    }

    private static byte[] Encode(string source, Encoding encoding, bool includePreamble)
    {
        var content = encoding.GetBytes(source);
        if (!includePreamble) return content;
        var preamble = encoding.GetPreamble();
        var result = new byte[preamble.Length + content.Length];
        Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
        Buffer.BlockCopy(content, 0, result, preamble.Length, content.Length);
        return result;
    }

    private static (Encoding Encoding, int PreambleLength) DetectEncoding(byte[] bytes)
    {
        if (bytes.AsSpan().StartsWith(Encoding.UTF8.GetPreamble()))
            return (new UTF8Encoding(true, true), Encoding.UTF8.GetPreamble().Length);
        if (bytes.AsSpan().StartsWith(Encoding.Unicode.GetPreamble()))
            return (Encoding.Unicode, Encoding.Unicode.GetPreamble().Length);
        if (bytes.AsSpan().StartsWith(Encoding.BigEndianUnicode.GetPreamble()))
            return (Encoding.BigEndianUnicode, Encoding.BigEndianUnicode.GetPreamble().Length);
        return (new UTF8Encoding(false, true), 0);
    }
}
