using ModuKeymapStudio.Core.Models;

namespace ModuKeymapStudio.Core.Editing;

public sealed class LayerDeletionException(string message, IReadOnlyList<LayerReference> references) : Exception(message)
{
    public IReadOnlyList<LayerReference> References { get; } = references;
}

