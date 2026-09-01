namespace ModuKeymapStudio.Core.IO;

public static class RepositoryLocator
{
    public static readonly string KeymapRelativePath = System.IO.Path.Combine(
        "modu-module", "boards", "shields", "modu", "modu.keymap");

    public static string? FindKeymap(params string?[] startingPaths)
    {
        foreach (var startingPath in startingPaths.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            var root = File.Exists(startingPath) ? System.IO.Path.GetDirectoryName(startingPath)! : startingPath!;
            var directory = new DirectoryInfo(System.IO.Path.GetFullPath(root));
            while (directory is not null)
            {
                var candidate = System.IO.Path.Combine(directory.FullName, KeymapRelativePath);
                if (File.Exists(candidate)) return candidate;
                directory = directory.Parent;
            }
        }
        return null;
    }

    public static string? FindRepositoryRoot(string path)
    {
        var directory = new DirectoryInfo(File.Exists(path) ? System.IO.Path.GetDirectoryName(path)! : path);
        while (directory is not null)
        {
            if (File.Exists(System.IO.Path.Combine(directory.FullName, "build.ps1")) &&
                File.Exists(System.IO.Path.Combine(directory.FullName, KeymapRelativePath)))
                return directory.FullName;
            directory = directory.Parent;
        }
        return null;
    }
}

