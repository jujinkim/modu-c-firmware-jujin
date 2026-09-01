using System.Windows;
using ModuKeymapStudio.Core.Editing;

namespace ModuKeymapStudio;

public partial class AddLayerWindow : Window
{
    private readonly HashSet<string> _existingNames;

    public AddLayerWindow(IEnumerable<string> existingNames)
    {
        InitializeComponent();
        _existingNames = existingNames.ToHashSet(StringComparer.Ordinal);
        var index = _existingNames.Count;
        while (_existingNames.Contains($"layer_{index}")) index++;
        NodeNameBox.Text = $"layer_{index}";
        DisplayNameBox.Text = $"Layer {index}";
        NodeNameBox.Focus();
        NodeNameBox.SelectAll();
    }

    public string NodeName => NodeNameBox.Text.Trim();
    public string DisplayName => DisplayNameBox.Text.Trim();
    public bool CloneCurrent => CloneRadio.IsChecked == true;

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        if (!KeymapEditor.IsValidNodeName(NodeName))
        {
            ValidationText.Text = "노드 이름은 영문 또는 밑줄로 시작하고 영문, 숫자, 밑줄, 하이픈만 사용할 수 있습니다.";
            return;
        }
        if (_existingNames.Contains(NodeName))
        {
            ValidationText.Text = "같은 노드 이름의 레이어가 이미 있습니다.";
            return;
        }
        if (string.IsNullOrWhiteSpace(DisplayName))
        {
            ValidationText.Text = "표시 이름을 입력하세요.";
            return;
        }
        DialogResult = true;
    }
}

