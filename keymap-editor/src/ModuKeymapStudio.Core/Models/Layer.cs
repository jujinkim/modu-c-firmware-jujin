namespace ModuKeymapStudio.Core.Models;

public sealed record Layer(
    int Index,
    string NodeName,
    string DisplayName,
    int NodeStart,
    int BlockEnd,
    int BindingsStart,
    int BindingsEnd,
    IReadOnlyList<Binding> Bindings,
    int? DisplayNameStart,
    int? DisplayNameEnd);
