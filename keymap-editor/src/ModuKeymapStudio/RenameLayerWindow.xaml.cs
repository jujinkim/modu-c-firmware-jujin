using System.Windows;
using ModuKeymapStudio.Core.Editing;
using ModuKeymapStudio.Core.Models;

namespace ModuKeymapStudio;

public partial class RenameLayerWindow : Window
{
    private readonly HashSet<string> _otherNodeNames;
    private readonly bool _isDefaultLayer;

    public RenameLayerWindow(Layer layer, IEnumerable<string> otherNodeNames)
    {
        InitializeComponent();
        _otherNodeNames = otherNodeNames.ToHashSet(StringComparer.Ordinal);
        _isDefaultLayer = layer.Index == 0;
        NodeNameBox.Text = layer.NodeName;
        NodeNameBox.IsReadOnly = _isDefaultLayer;
        DefaultNodeHint.Visibility = _isDefaultLayer ? Visibility.Visible : Visibility.Collapsed;
        DisplayNameBox.Text = layer.DisplayName;
        DisplayNameBox.Focus();
        DisplayNameBox.SelectAll();
    }

    public string NodeName => NodeNameBox.Text.Trim();
    public string DisplayName => DisplayNameBox.Text.Trim();

    private void Rename_Click(object sender, RoutedEventArgs e)
    {
        if (!KeymapEditor.IsValidNodeName(NodeName))
        {
            ValidationText.Text = "노드 이름은 영문 또는 밑줄로 시작하고 영문, 숫자, 밑줄, 하이픈만 사용할 수 있습니다.";
            return;
        }
        if (_isDefaultLayer && NodeName != "default_layer")
        {
            ValidationText.Text = "기본 레이어의 default_layer 노드 이름은 변경할 수 없습니다.";
            return;
        }
        if (_otherNodeNames.Contains(NodeName))
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
