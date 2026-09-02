using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using Microsoft.Win32;
using ModuKeymapStudio.Core.Editing;
using ModuKeymapStudio.Core.IO;
using ModuKeymapStudio.Core.Keycodes;
using ModuKeymapStudio.Core.Models;
using ModuKeymapStudio.Core.Parsing;
using ModuKeymapStudio.Services;

namespace ModuKeymapStudio;

public partial class MainWindow : Window
{
    private const double MiddleGapWidth = 140;
    private const double StandardKeyWidth = 68;
    private const double ThumbKeyWidth = 100;
    private readonly IReadOnlyList<KeyOption> _baseOptions = CreateKeyOptions();
    private KeymapFile? _file;
    private KeymapDocument? _document;
    private DocumentHistory? _history;
    private int _selectedLayer;
    private int? _selectedKey;
    private bool _refreshing;
    private bool _startupComplete;
    private Point? _dragStart;
    private int? _dragSourceKey;
    private Button? _dragSourceButton;
    private Button? _dragTargetButton;
    private Border? _dragSourceOverlay;
    private Border? _dragGhost;
    private bool _isDraggingKey;
    private bool _dragCanStart;
    private bool _changingTheme;

    public MainWindow()
    {
        InitializeComponent();
        CategoryBox.ItemsSource = new[] { "전체" }
            .Concat(ZmkKeycodeCatalog.Categories)
            .Concat(["Bluetooth", "마우스", "레이어"])
            .ToArray();
        CategoryBox.SelectedIndex = 0;
        BehaviorBox.ItemsSource = new[]
        {
            new BehaviorChoice("일반 키 (&kp)", "&kp"),
            new BehaviorChoice("투명 (&trans)", "&trans"),
            new BehaviorChoice("없음 (&none)", "&none"),
            new BehaviorChoice("누르는 동안 레이어 (&mo)", "&mo"),
            new BehaviorChoice("레이어로 이동 (&to)", "&to"),
            new BehaviorChoice("레이어 토글 (&tog)", "&tog"),
            new BehaviorChoice("고정 레이어 (&sl)", "&sl"),
            new BehaviorChoice("레이어 탭 (&lt)", "&lt"),
            new BehaviorChoice("고급 원문", "raw")
        };
        BehaviorBox.DisplayMemberPath = nameof(BehaviorChoice.Label);
        BehaviorBox.SelectedValuePath = nameof(BehaviorChoice.Code);
        BehaviorBox.SelectedIndex = 0;
        ThemeBox.ItemsSource = new[]
        {
            new ThemeChoice("시스템", ThemePreference.System),
            new ThemeChoice("라이트", ThemePreference.Light),
            new ThemeChoice("다크", ThemePreference.Dark)
        };
        ThemeBox.DisplayMemberPath = nameof(ThemeChoice.Label);
        ThemeBox.SelectedValuePath = nameof(ThemeChoice.Preference);
        _changingTheme = true;
        ThemeBox.SelectedValue = ThemeManager.Preference;
        _changingTheme = false;
        SetDocumentControls(false);
        RefreshPresetList();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (_startupComplete) return;
        _startupComplete = true;

        new AboutWindow { Owner = this }.ShowDialog();

        var path = RepositoryLocator.FindKeymap(AppContext.BaseDirectory, Environment.CurrentDirectory);
        if (path is not null) LoadKeymap(path);
        else
        {
            StatusText.Text = "저장소에서 modu.keymap을 찾지 못했습니다. 파일을 선택하세요.";
            OpenKeymapDialog();
        }
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        if (!ConfirmDiscardChanges()) return;
        OpenKeymapDialog();
    }

    private void OpenKeymapDialog()
    {
        var dialog = new OpenFileDialog
        {
            Title = "ZMK 키맵 열기",
            Filter = "ZMK keymap (*.keymap)|*.keymap|모든 파일 (*.*)|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) == true) LoadKeymap(dialog.FileName);
    }

    private void LoadKeymap(string path)
    {
        try
        {
            _file = KeymapFile.Load(path);
            _document = _file.Document;
            _history = new DocumentHistory(_document.Source);
            _selectedLayer = 0;
            _selectedKey = null;
            SetDocumentControls(true);
            RefreshAll();
            var errors = _document.Validate();
            StatusText.Text = errors.Count == 0
                ? $"{_document.Layers.Count}개 레이어 · 실제 키 {KeymapDocument.ModuEditableKeyCount}개 · 예약 슬롯 6개 숨김"
                : string.Join(" ", errors);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or KeymapParseException or DecoderFallbackException)
        {
            MessageBox.Show(this, exception.Message, "키맵을 열 수 없음", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "키맵을 열지 못했습니다.";
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e) => SaveDocument(saveAs: false);
    private void SaveAs_Click(object sender, RoutedEventArgs e) => SaveDocument(saveAs: true);

    private void About_Click(object sender, RoutedEventArgs e)
    {
        new AboutWindow { Owner = this }.ShowDialog();
    }

    private void KeySelectionHelp_Click(object sender, RoutedEventArgs e)
    {
        new KeySelectionHelpWindow { Owner = this }.ShowDialog();
    }

    private bool SaveDocument(bool saveAs)
    {
        if (_document is null || _file is null) return false;
        var errors = _document.Validate();
        if (errors.Count > 0)
        {
            MessageBox.Show(this, string.Join(Environment.NewLine, errors), "저장 전 검증 실패", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        string? destination = null;
        if (saveAs)
        {
            var dialog = new SaveFileDialog
            {
                Title = "키맵 다른 이름으로 저장",
                Filter = "ZMK keymap (*.keymap)|*.keymap|모든 파일 (*.*)|*.*",
                FileName = Path.GetFileName(_file.Path),
                InitialDirectory = Path.GetDirectoryName(_file.Path),
                AddExtension = true,
                DefaultExt = ".keymap"
            };
            if (dialog.ShowDialog(this) != true) return false;
            destination = dialog.FileName;
        }

        try
        {
            _file.Save(_document.Source, destination);
            _document = _file.Document;
            StatusText.Text = $"저장했습니다: {_file.Path}";
            RefreshDocumentState();
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or KeymapParseException)
        {
            MessageBox.Show(this, exception.Message, "저장 실패", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private void Undo_Click(object sender, RoutedEventArgs e)
    {
        if (_history is null || !_history.CanUndo) return;
        _document = KeymapParser.Parse(_history.Undo());
        ClampSelection();
        RefreshAll();
        StatusText.Text = "마지막 변경을 취소했습니다.";
    }

    private void Redo_Click(object sender, RoutedEventArgs e)
    {
        if (_history is null || !_history.CanRedo) return;
        _document = KeymapParser.Parse(_history.Redo());
        ClampSelection();
        RefreshAll();
        StatusText.Text = "변경을 다시 적용했습니다.";
    }

    private void AddLayer_Click(object sender, RoutedEventArgs e)
    {
        if (_document is null) return;
        var dialog = new AddLayerWindow(_document.Layers.Select(layer => layer.NodeName)) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var updated = KeymapEditor.AddLayer(_document, dialog.NodeName, dialog.DisplayName, dialog.CloneCurrent, _selectedLayer);
            Commit(updated, updated.Layers.Count - 1, null);
            StatusText.Text = $"{dialog.DisplayName} 레이어를 마지막 번호로 추가했습니다.";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            MessageBox.Show(this, exception.Message, "레이어 추가 실패", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void RenameLayer_Click(object sender, RoutedEventArgs e)
    {
        if (_document is null) return;
        var layer = _document.Layers[_selectedLayer];
        var dialog = new RenameLayerWindow(
            layer,
            _document.Layers.Where(item => item.Index != layer.Index).Select(item => item.NodeName)) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var updated = KeymapEditor.RenameLayer(_document, _selectedLayer, dialog.NodeName, dialog.DisplayName);
            Commit(updated, _selectedLayer, _selectedKey);
            StatusText.Text = $"레이어 이름을 {dialog.DisplayName}(으)로 변경했습니다.";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            MessageBox.Show(this, exception.Message, "레이어 이름 변경 실패", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ThemeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_changingTheme || ThemeBox.SelectedValue is not ThemePreference preference) return;
        try
        {
            _changingTheme = true;
            ThemeManager.SetPreference(preference, save: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"테마 설정을 저장하지 못했습니다: {exception.Message}", "설정", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            _changingTheme = false;
        }
    }

    private void DeleteLayer_Click(object sender, RoutedEventArgs e)
    {
        if (_document is null) return;
        var layer = _document.Layers[_selectedLayer];
        if (MessageBox.Show(this, $"레이어 {_selectedLayer}: {layer.DisplayName}을(를) 삭제할까요?", "레이어 삭제",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try
        {
            var updated = KeymapEditor.DeleteLayer(_document, _selectedLayer);
            Commit(updated, Math.Min(_selectedLayer, updated.Layers.Count - 1), null);
            StatusText.Text = "레이어를 삭제하고 더 높은 숫자 참조를 자동으로 조정했습니다.";
        }
        catch (LayerDeletionException exception)
        {
            var locations = exception.References.Count == 0
                ? string.Empty
                : Environment.NewLine + Environment.NewLine + string.Join(Environment.NewLine,
                    exception.References.Select(reference => $"• {reference.Location} · {reference.Behavior} {(reference.TargetLayerIndex?.ToString() ?? "심볼/누락")}"));
            MessageBox.Show(this, exception.Message + locations, "레이어 삭제 차단", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Build_Click(object sender, RoutedEventArgs e)
    {
        if (_file is null || _document is null) return;
        if (IsDirty && !SaveDocument(saveAs: false)) return;
        var repositoryRoot = RepositoryLocator.FindRepositoryRoot(_file.Path);
        if (repositoryRoot is null)
        {
            MessageBox.Show(this, "현재 키맵에서 build.ps1이 있는 저장소 루트를 찾지 못했습니다.", "빌드할 수 없음", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        new BuildWindow(repositoryRoot) { Owner = this }.ShowDialog();
    }

    private void LayerTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_refreshing || _document is null || LayerTabs.SelectedIndex < 0) return;
        _selectedLayer = LayerTabs.SelectedIndex;
        _selectedKey = null;
        RefreshKeyboard();
        RefreshSelectionEditor();
        RefreshPresetList();
        RefreshDocumentState();
    }

    private void PresetFilter_Changed(object sender, EventArgs e)
    {
        if (PresetList is not null) RefreshPresetList();
    }

    private void PresetList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PresetList.SelectedItem is not KeyOption option) return;
        RawBindingBox.Text = option.Binding;
        PopulateBehaviorFields(option.Binding);
    }

    private void PresetList_MouseDoubleClick(object sender, MouseButtonEventArgs e) => ApplySelectedPreset();
    private void ApplyPreset_Click(object sender, RoutedEventArgs e) => ApplySelectedPreset();

    private void ApplySelectedPreset()
    {
        if (PresetList.SelectedItem is KeyOption option) ApplyBinding(option.Binding);
    }

    private void ApplyRaw_Click(object sender, RoutedEventArgs e) => ApplyBinding(RawBindingBox.Text);

    private void ApplyBehavior_Click(object sender, RoutedEventArgs e)
    {
        if (BehaviorBox.SelectedValue is not string behavior) return;
        string raw;
        switch (behavior)
        {
            case "&trans":
            case "&none":
                raw = behavior;
                break;
            case "&mo":
            case "&to":
            case "&tog":
            case "&sl":
                if (TargetLayerBox.SelectedValue is not int target) { ShowBindingWarning("대상 레이어를 선택하세요."); return; }
                raw = $"{behavior} {target}";
                break;
            case "&lt":
                if (TargetLayerBox.SelectedValue is not int layer) { ShowBindingWarning("대상 레이어를 선택하세요."); return; }
                if (string.IsNullOrWhiteSpace(ArgumentBox.Text)) { ShowBindingWarning("탭할 때 보낼 키 코드를 입력하세요."); return; }
                raw = $"&lt {layer} {ArgumentBox.Text.Trim()}";
                break;
            case "&kp":
                if (string.IsNullOrWhiteSpace(ArgumentBox.Text)) { ShowBindingWarning("ZMK 키 코드를 입력하세요."); return; }
                raw = $"&kp {ArgumentBox.Text.Trim()}";
                break;
            default:
                raw = RawBindingBox.Text;
                break;
        }
        ApplyBinding(raw);
    }

    private void ApplyBinding(string raw)
    {
        if (_document is null || _selectedKey is null)
        {
            ShowBindingWarning("먼저 키를 선택하세요.");
            return;
        }
        try
        {
            var updated = raw.Trim() switch
            {
                ZmkBehaviorCatalog.HeldBootloaderBinding => KeymapEditor.SetSafetyHoldBinding(
                    _document, _selectedLayer, _selectedKey.Value, SafetyHoldAction.Bootloader),
                ZmkBehaviorCatalog.HeldSystemResetBinding => KeymapEditor.SetSafetyHoldBinding(
                    _document, _selectedLayer, _selectedKey.Value, SafetyHoldAction.SystemReset),
                _ => KeymapEditor.ReplaceBinding(_document, _selectedLayer, _selectedKey.Value, raw)
            };
            Commit(updated, _selectedLayer, _selectedKey);
            StatusText.Text = $"레이어 {_selectedLayer}, 키 {_selectedKey.Value + 1}을(를) {raw.Trim()}로 변경했습니다.";
        }
        catch (ArgumentException exception)
        {
            ShowBindingWarning(exception.Message);
        }
    }

    private void BehaviorBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateBehaviorFieldsVisibility();

    private void RefreshAll()
    {
        if (_document is null) return;
        _refreshing = true;
        LayerTabs.Items.Clear();
        foreach (var layer in _document.Layers)
            LayerTabs.Items.Add(new TabItem { Header = new TextBlock { Text = $"{layer.Index}: {layer.DisplayName}" } });
        LayerTabs.SelectedIndex = _selectedLayer;
        _refreshing = false;
        RefreshKeyboard();
        RefreshSelectionEditor();
        RefreshPresetList();
        RefreshDocumentState();
    }

    private void RefreshKeyboard()
    {
        KeyboardPanel.Children.Clear();
        if (_document is null) return;
        var bindings = _document.Layers[_selectedLayer].Bindings;
        if (bindings.Count != KeymapDocument.ModuBindingCount)
        {
            KeyboardPanel.Children.Add(new TextBlock
            {
                Text = $"이 레이어에는 {bindings.Count}개 바인딩이 있습니다. MODU 키맵 형식에는 {KeymapDocument.ModuBindingCount}개가 필요합니다.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(20)
            });
            ((TextBlock)KeyboardPanel.Children[^1]).SetResourceReference(TextBlock.ForegroundProperty, "WarningBrush");
            return;
        }

        for (var row = 0; row < 5; row++)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            for (var column = 0; column < 13; column++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = column == 6 ? new GridLength(MiddleGapWidth) : new GridLength(1, GridUnitType.Star) });
            for (var column = 0; column < 12; column++)
            {
                var index = row * 12 + column;
                if (!KeymapDocument.IsEditableKeyIndex(index)) continue;
                var button = CreateKeyButton(bindings[index], index, StandardKeyWidth);
                Grid.SetColumn(button, column < 6 ? column : column + 1);
                grid.Children.Add(button);
            }

            // The 67th switch belongs to the optional module on the right half.
            // It sits immediately to the left of N instead of in the thumb row.
            if (row == 3)
            {
                var moduleButton = CreateKeyButton(bindings[66], 66, StandardKeyWidth, isAdditionalModule: true);
                moduleButton.Width = StandardKeyWidth;
                moduleButton.HorizontalAlignment = HorizontalAlignment.Right;
                Grid.SetColumn(moduleButton, 6);
                grid.Children.Add(moduleButton);
            }
            KeyboardPanel.Children.Add(grid);
        }

        var thumbs = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 18, 0, 0) };
        var leftThumbs = new StackPanel { Orientation = Orientation.Horizontal };
        var rightThumbs = new StackPanel { Orientation = Orientation.Horizontal };
        for (var index = 60; index < 63; index++) leftThumbs.Children.Add(CreateKeyButton(bindings[index], index, ThumbKeyWidth));
        for (var index = 63; index < 66; index++) rightThumbs.Children.Add(CreateKeyButton(bindings[index], index, ThumbKeyWidth));
        thumbs.Children.Add(leftThumbs);
        thumbs.Children.Add(new Border { Width = MiddleGapWidth });
        thumbs.Children.Add(rightThumbs);
        KeyboardPanel.Children.Add(thumbs);
    }

    private Button CreateKeyButton(Binding binding, int index, double minWidth, bool isAdditionalModule = false)
    {
        var selected = _selectedKey == index;
        var isTransparent = _selectedLayer > 0 && binding.Raw == "&trans";
        var visibleBinding = isTransparent ? _document!.Layers[0].Bindings[index].Raw : binding.Raw;
        var bindingDescription = DescribeBinding(visibleBinding);
        var tooltipText = isTransparent
            ? $"{binding.Raw}\n기본 레이어: {visibleBinding}\n{bindingDescription}"
            : $"{binding.Raw}\n{bindingDescription}";
        if (isAdditionalModule) tooltipText = $"추가모듈\n{tooltipText}";

        var label = CreateKeycapContent(visibleBinding, index, minWidth, isTransparent);
        var button = new Button
        {
            Content = label,
            MinWidth = minWidth,
            Height = 78,
            Margin = new Thickness(3),
            Padding = new Thickness(4),
            Tag = index,
            ToolTip = new TextBlock
            {
                Text = tooltipText,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12
            },
            BorderThickness = new Thickness(selected ? 2 : 1)
        };
        ApplyKeyButtonVisual(button, index);
        ToolTipService.SetInitialShowDelay(button, 250);
        ToolTipService.SetShowDuration(button, 30000);
        button.PreviewMouseLeftButtonDown += KeyButton_MouseLeftButtonDown;
        button.PreviewMouseMove += KeyButton_MouseMove;
        button.PreviewMouseLeftButtonUp += KeyButton_MouseLeftButtonUp;
        return button;
    }

    private void KeyButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_document is null || sender is not Button button || button.Tag is not int index) return;
        _dragStart = e.GetPosition(KeyboardPanel);
        _dragSourceKey = index;
        _dragSourceButton = button;
        _dragCanStart = !KeymapEditor.IsEmptyBinding(_document.Layers[_selectedLayer].Bindings[index]);
        _isDraggingKey = false;
        button.CaptureMouse();
        e.Handled = true;
    }

    private void KeyButton_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragStart is null || _dragSourceKey is null) return;
        var current = e.GetPosition(KeyboardPanel);
        if (!_isDraggingKey && _dragCanStart &&
            (Math.Abs(current.X - _dragStart.Value.X) >= SystemParameters.MinimumHorizontalDragDistance ||
             Math.Abs(current.Y - _dragStart.Value.Y) >= SystemParameters.MinimumVerticalDragDistance))
        {
            _isDraggingKey = true;
            SetDragSourceVisual();
            ShowDragGhost();
            Mouse.OverrideCursor = Cursors.SizeAll;
        }
        if (!_isDraggingKey) return;

        UpdateDragGhost(e.GetPosition(RootGrid));
        var target = FindKeyButton(current);
        if (target?.Tag is not int targetIndex || targetIndex == _dragSourceKey)
            target = null;
        SetDragTarget(target);
        e.Handled = true;
    }

    private void KeyButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        var sourceIndex = _dragSourceKey;
        var wasDragging = _isDraggingKey;
        var position = e.GetPosition(KeyboardPanel);
        var target = FindKeyButton(position);
        var targetIndex = target?.Tag as int?;
        _dragSourceButton?.ReleaseMouseCapture();
        e.Handled = true;

        if (sourceIndex is null)
        {
            ResetDragState();
            return;
        }
        if (!wasDragging)
        {
            ResetDragState();
            _selectedKey = sourceIndex;
            RefreshKeyboard();
            RefreshSelectionEditor();
            return;
        }
        if (target is null || targetIndex is null || targetIndex == sourceIndex)
        {
            ResetDragState();
            return;
        }
        SetDragTarget(target);
        StopDragPointerFeedback();
        ShowDropMenu(target, sourceIndex.Value, targetIndex.Value);
    }

    private Button? FindKeyButton(Point position)
    {
        DependencyObject? current = KeyboardPanel.InputHitTest(position) as DependencyObject;
        while (current is not null)
        {
            if (current is Button button && button.Tag is int index && KeymapDocument.IsEditableKeyIndex(index))
                return button;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private void SetDragTarget(Button? target)
    {
        if (ReferenceEquals(target, _dragTargetButton)) return;
        if (_dragTargetButton?.Tag is int oldIndex) ApplyKeyButtonVisual(_dragTargetButton, oldIndex);
        _dragTargetButton = target;
        if (target is null) return;
        target.SetResourceReference(Control.BackgroundProperty, "DragTargetBrush");
        target.SetResourceReference(Control.BorderBrushProperty, "DragTargetBorderBrush");
        target.BorderThickness = new Thickness(3);
    }

    private void SetDragSourceVisual()
    {
        if (_dragSourceButton is null) return;
        _dragSourceButton.SetResourceReference(Control.BackgroundProperty, "DragSourceBrush");
        _dragSourceButton.SetResourceReference(Control.BorderBrushProperty, "DragSourceBorderBrush");
        _dragSourceButton.BorderThickness = new Thickness(3);
        if (_dragSourceButton.Content is Grid content)
        {
            _dragSourceOverlay = new Border
            {
                CornerRadius = new CornerRadius(4),
                BorderThickness = new Thickness(3),
                Opacity = 0.68,
                IsHitTestVisible = false
            };
            _dragSourceOverlay.SetResourceReference(Border.BackgroundProperty, "DragSourceBrush");
            _dragSourceOverlay.SetResourceReference(Border.BorderBrushProperty, "DragSourceBorderBrush");
            Panel.SetZIndex(_dragSourceOverlay, 100);
            content.Children.Add(_dragSourceOverlay);
        }
    }

    private void ShowDragGhost()
    {
        if (_dragSourceButton is null) return;
        var scale = 0.68;
        _dragGhost = new Border
        {
            Width = Math.Max(46, _dragSourceButton.ActualWidth * scale),
            Height = Math.Max(50, _dragSourceButton.ActualHeight * scale),
            CornerRadius = new CornerRadius(6),
            BorderThickness = new Thickness(1),
            Background = new VisualBrush(_dragSourceButton) { Stretch = Stretch.Fill },
            Opacity = 0.76,
            IsHitTestVisible = false,
            SnapsToDevicePixels = true
        };
        _dragGhost.SetResourceReference(Border.BorderBrushProperty, "DragSourceBorderBrush");
        DragGhostLayer.Children.Add(_dragGhost);
    }

    private void UpdateDragGhost(Point cursorPosition)
    {
        if (_dragGhost is null) return;
        const double offset = 16;
        var left = cursorPosition.X + offset;
        var top = cursorPosition.Y + offset;
        if (left + _dragGhost.Width > RootGrid.ActualWidth)
            left = cursorPosition.X - _dragGhost.Width - offset;
        if (top + _dragGhost.Height > RootGrid.ActualHeight)
            top = cursorPosition.Y - _dragGhost.Height - offset;
        Canvas.SetLeft(_dragGhost, Math.Max(0, left));
        Canvas.SetTop(_dragGhost, Math.Max(0, top));
    }

    private void ResetDragState()
    {
        if (_dragSourceOverlay?.Parent is Panel overlayParent)
            overlayParent.Children.Remove(_dragSourceOverlay);
        _dragSourceOverlay = null;
        if (_dragSourceButton?.Tag is int sourceIndex) ApplyKeyButtonVisual(_dragSourceButton, sourceIndex);
        if (_dragTargetButton?.Tag is int oldIndex) ApplyKeyButtonVisual(_dragTargetButton, oldIndex);
        StopDragPointerFeedback();
        _dragSourceKey = null;
        _dragSourceButton = null;
        _dragTargetButton = null;
    }

    private void StopDragPointerFeedback()
    {
        if (_dragGhost is not null)
        {
            DragGhostLayer.Children.Remove(_dragGhost);
            _dragGhost = null;
        }
        _dragStart = null;
        _dragCanStart = false;
        _isDraggingKey = false;
        Mouse.OverrideCursor = null;
    }

    private void ApplyKeyButtonVisual(Button button, int index)
    {
        if (_document is null) return;
        var binding = _document.Layers[_selectedLayer].Bindings[index];
        var selected = _selectedKey == index;
        var isTransparent = _selectedLayer > 0 && binding.Raw == "&trans";
        button.SetResourceReference(Control.BackgroundProperty,
            selected ? "SelectedKeyBrush" : isTransparent ? "TransparentKeyBrush" : "KeyBrush");
        button.SetResourceReference(Control.BorderBrushProperty,
            selected ? "AccentBrush" : isTransparent ? "TransparentKeyBorderBrush" : "KeyBorderBrush");
        button.BorderThickness = new Thickness(selected ? 2 : 1);
    }

    private void ShowDropMenu(Button target, int sourceIndex, int targetIndex)
    {
        if (_document is null) return;
        var targetIsEmpty = KeymapEditor.IsEmptyBinding(_document.Layers[_selectedLayer].Bindings[targetIndex]);
        var choices = targetIsEmpty
            ? new[] { ("이동", KeyMoveOperation.Move), ("복사", KeyMoveOperation.Copy) }
            : new[]
            {
                ("덮어쓰기 이동", KeyMoveOperation.OverwriteMove),
                ("덮어쓰기 복사", KeyMoveOperation.OverwriteCopy),
                ("교체", KeyMoveOperation.Swap)
            };
        var menu = new ContextMenu
        {
            PlacementTarget = target,
            Placement = PlacementMode.Bottom,
            StaysOpen = false
        };
        foreach (var (label, operation) in choices)
        {
            var item = new MenuItem { Header = label };
            item.Click += (_, _) => ApplyKeyMove(sourceIndex, targetIndex, operation);
            menu.Items.Add(item);
        }
        menu.Closed += (_, _) => ResetDragState();
        menu.IsOpen = true;
    }

    private void ApplyKeyMove(int sourceIndex, int targetIndex, KeyMoveOperation operation)
    {
        if (_document is null) return;
        try
        {
            var updated = KeymapEditor.MoveBinding(_document, _selectedLayer, sourceIndex, targetIndex, operation);
            Commit(updated, _selectedLayer, targetIndex);
            StatusText.Text = $"키 {sourceIndex + 1} → {targetIndex + 1}: {MoveOperationLabel(operation)}을(를) 적용했습니다.";
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ShowBindingWarning(exception.Message);
        }
    }

    private static string MoveOperationLabel(KeyMoveOperation operation) => operation switch
    {
        KeyMoveOperation.Move => "이동",
        KeyMoveOperation.Copy => "복사",
        KeyMoveOperation.OverwriteMove => "덮어쓰기 이동",
        KeyMoveOperation.OverwriteCopy => "덮어쓰기 복사",
        KeyMoveOperation.Swap => "교체",
        _ => operation.ToString()
    };

    private static Grid CreateKeycapContent(string raw, int index, double minWidth, bool isTransparent)
    {
        var presentation = GetKeycapPresentation(raw);
        var grid = new Grid { Width = minWidth - 8, Height = 62 };

        if (presentation.BaseCharacter is not null)
        {
            var characters = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };
            if (presentation.ShiftCharacter is not null)
            {
                characters.Children.Add(new TextBlock
                {
                    Text = presentation.ShiftCharacter,
                    FontFamily = new FontFamily("Segoe UI Symbol"),
                    FontSize = minWidth >= ThumbKeyWidth ? 15 : 13,
                    FontWeight = FontWeights.SemiBold,
                    LineHeight = minWidth >= ThumbKeyWidth ? 17 : 15,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
            }
            var hasShiftCharacter = presentation.ShiftCharacter is not null;
            characters.Children.Add(new TextBlock
            {
                Text = presentation.BaseCharacter,
                FontFamily = new FontFamily("Segoe UI Symbol"),
                FontSize = hasShiftCharacter
                    ? minWidth >= ThumbKeyWidth ? 20 : 18
                    : minWidth >= ThumbKeyWidth ? 25 : 22,
                FontWeight = FontWeights.SemiBold,
                LineHeight = hasShiftCharacter
                    ? minWidth >= ThumbKeyWidth ? 22 : 20
                    : minWidth >= ThumbKeyWidth ? 27 : 24,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            });
            grid.Children.Add(characters);

            var code = new Viewbox
            {
                MaxWidth = minWidth - 27,
                Height = 10,
                Stretch = Stretch.Uniform,
                StretchDirection = StretchDirection.DownOnly,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom
            };
            code.Child = new TextBlock
            {
                Text = presentation.ZmkCode,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 9,
                FontWeight = FontWeights.SemiBold,
            };
            ((TextBlock)code.Child).SetResourceReference(TextBlock.ForegroundProperty,
                isTransparent ? "TransparentKeyDetailBrush" : "KeyDetailBrush");
            grid.Children.Add(code);
        }
        else
        {
            var centerFontSize = presentation.CenterText.Length switch
            {
                <= 3 => minWidth >= ThumbKeyWidth ? 19 : 17,
                <= 5 => minWidth >= ThumbKeyWidth ? 17 : 15,
                _ => minWidth >= ThumbKeyWidth ? 15 : 13
            };
            var centerText = new TextBlock
            {
                Text = presentation.CenterText,
                FontFamily = string.IsNullOrEmpty(presentation.ZmkCode) ? new FontFamily("Consolas") : new FontFamily("Segoe UI"),
                FontWeight = FontWeights.SemiBold,
                FontSize = centerFontSize,
                TextWrapping = string.IsNullOrEmpty(presentation.ZmkCode) ? TextWrapping.Wrap : TextWrapping.NoWrap,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            };
            centerText.SetResourceReference(TextBlock.ForegroundProperty,
                isTransparent ? "TransparentKeyTextBrush" : "KeyTextBrush");
            if (string.IsNullOrEmpty(presentation.ZmkCode))
            {
                centerText.Width = minWidth - 10;
                grid.Children.Add(centerText);
            }
            else
            {
                grid.Children.Add(new Viewbox
                {
                    Width = minWidth - 13,
                    Height = 25,
                    Stretch = Stretch.Uniform,
                    StretchDirection = StretchDirection.DownOnly,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 8),
                    Child = centerText
                });
            }
            if (!string.IsNullOrEmpty(presentation.ZmkCode))
            {
                var code = new TextBlock
                {
                    Text = presentation.ZmkCode,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 9,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Bottom
                };
                code.SetResourceReference(TextBlock.ForegroundProperty,
                    isTransparent ? "TransparentKeyDetailBrush" : "KeyDetailBrush");
                grid.Children.Add(code);
            }
        }

        grid.Children.Add(new TextBlock
        {
            Text = (index + 1).ToString(),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 9,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom
        });
        ((TextBlock)grid.Children[^1]).SetResourceReference(TextBlock.ForegroundProperty,
            isTransparent ? "TransparentKeyDetailBrush" : "KeyDetailBrush");

        foreach (var text in FindVisualChildren<TextBlock>(grid).Where(item => item.ReadLocalValue(TextBlock.ForegroundProperty) == DependencyProperty.UnsetValue))
            text.SetResourceReference(TextBlock.ForegroundProperty, isTransparent ? "TransparentKeyTextBrush" : "KeyTextBrush");
        return grid;
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match) yield return match;
            foreach (var descendant in FindVisualChildren<T>(child)) yield return descendant;
        }
    }

    private static KeycapPresentation GetKeycapPresentation(string raw)
    {
        if (raw.StartsWith("&kp ", StringComparison.Ordinal))
        {
            var code = raw[4..].Trim();
            var option = ZmkKeycodeCatalog.All.FirstOrDefault(item =>
                item.Aliases.Any(alias => alias.Equals(code, StringComparison.OrdinalIgnoreCase)));
            if (option?.BaseCharacter is not null)
                return new KeycapPresentation(string.Empty, option.BaseCharacter, option.ShiftCharacter, code);
            if (option?.Code is "LANG3" or "LANG4" or "LANG5")
                return new KeycapPresentation(option.KeycapLabel, null, null, code);
        }
        return new KeycapPresentation(FriendlyBinding(raw), null, null, string.Empty);
    }

    private void RefreshSelectionEditor()
    {
        if (_document is null || _selectedKey is null)
        {
            SelectedKeyText.Text = "키를 클릭해 바인딩을 편집하세요.";
            RawBindingBox.Text = string.Empty;
            return;
        }
        var binding = _document.Layers[_selectedLayer].Bindings[_selectedKey.Value];
        SelectedKeyText.Text = $"레이어 {_selectedLayer} · 키 {_selectedKey.Value + 1} · {binding.Raw}";
        RawBindingBox.Text = binding.Raw;
        PopulateBehaviorFields(binding.Raw);
    }

    private void PopulateBehaviorFields(string raw)
    {
        var parts = raw.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var behavior = parts.Length > 0 ? parts[0] : "raw";
        if (BehaviorBox.Items.Cast<BehaviorChoice>().All(choice => choice.Code != behavior)) behavior = "raw";
        BehaviorBox.SelectedValue = behavior;
        if (behavior is "&mo" or "&to" or "&tog" or "&sl" or "&lt")
        {
            if (parts.Length > 1 && int.TryParse(parts[1], out var layer)) TargetLayerBox.SelectedValue = layer;
            ArgumentBox.Text = behavior == "&lt" && parts.Length > 2 ? string.Join(" ", parts.Skip(2)) : string.Empty;
        }
        else if (behavior == "&kp") ArgumentBox.Text = parts.Length > 1 ? string.Join(" ", parts.Skip(1)) : string.Empty;
        UpdateBehaviorFieldsVisibility();
    }

    private void UpdateBehaviorFieldsVisibility()
    {
        if (BehaviorBox.SelectedValue is not string behavior) return;
        var hasLayer = behavior is "&mo" or "&to" or "&tog" or "&sl" or "&lt";
        var hasArgument = behavior is "&kp" or "&lt";
        TargetLayerLabel.Visibility = TargetLayerBox.Visibility = hasLayer ? Visibility.Visible : Visibility.Collapsed;
        ArgumentLabel.Visibility = ArgumentBox.Visibility = hasArgument ? Visibility.Visible : Visibility.Collapsed;
        ArgumentLabel.Text = behavior == "&lt" ? "탭 키 코드" : "ZMK 키 코드";
    }

    private void RefreshPresetList()
    {
        if (CategoryBox is null || PresetList is null) return;
        var category = CategoryBox.SelectedItem as string ?? "전체";
        var query = SearchBox?.Text.Trim() ?? string.Empty;
        var options = _baseOptions.Concat(CreateLayerOptions());
        var filtered = options.Where(option =>
            (category == "전체" || option.Category == category) &&
            option.Matches(query))
            .ToArray();
        PresetList.ItemsSource = filtered;
        if (PresetCountText is not null) PresetCountText.Text = $"{filtered.Length}개";
    }

    private IEnumerable<KeyOption> CreateLayerOptions()
    {
        yield return new KeyOption("레이어", "투명", "&trans", "Transparent · 아래 레이어 값을 통과", "transparent trans 투명");
        yield return new KeyOption("레이어", "입력 없음", "&none", "None · 아무 입력도 보내지 않음", "none disabled 입력 없음");
        if (_document is null) yield break;
        foreach (var layer in _document.Layers)
        {
            yield return new KeyOption("레이어", $"누르는 동안 {layer.Index}: {layer.DisplayName}", $"&mo {layer.Index}", "Momentary Layer", "momentary layer mo");
            yield return new KeyOption("레이어", $"{layer.Index}: {layer.DisplayName}(으)로 이동", $"&to {layer.Index}", "To Layer", "to layer 이동");
            yield return new KeyOption("레이어", $"{layer.Index}: {layer.DisplayName} 토글", $"&tog {layer.Index}", "Toggle Layer", "toggle layer tog");
            yield return new KeyOption("레이어", $"{layer.Index}: {layer.DisplayName} 고정", $"&sl {layer.Index}", "Sticky Layer", "sticky layer sl");
        }
    }

    private void RefreshDocumentState()
    {
        var loaded = _document is not null && _file is not null;
        if (!loaded) return;
        FileNameText.Text = _file!.Path;
        var layer = _document!.Layers[_selectedLayer];
        LayerTitleText.Text = $"{layer.DisplayName} · {KeymapDocument.ModuEditableKeyCount}키";
        DirtyText.Text = IsDirty ? "● 저장되지 않음" : "저장됨";
        DirtyText.SetResourceReference(TextBlock.ForegroundProperty, IsDirty ? "WarningBrush" : "SuccessBrush");
        Title = (IsDirty ? "* " : string.Empty) + "Unofficial MODU Keymap Studio — " + Path.GetFileName(_file.Path);
        UndoButton.IsEnabled = _history?.CanUndo == true;
        RedoButton.IsEnabled = _history?.CanRedo == true;
        DeleteLayerButton.IsEnabled = _selectedLayer != 0;
        TargetLayerBox.ItemsSource = _document.Layers.Select(item => new LayerChoice(item.Index, $"{item.Index}: {item.DisplayName}")).ToArray();
    }

    private void Commit(KeymapDocument document, int selectedLayer, int? selectedKey)
    {
        if (_history is null || !_history.Push(document.Source)) return;
        _document = document;
        _selectedLayer = selectedLayer;
        _selectedKey = selectedKey;
        RefreshAll();
    }

    private void ClampSelection()
    {
        if (_document is null) return;
        _selectedLayer = Math.Clamp(_selectedLayer, 0, _document.Layers.Count - 1);
        if (_selectedKey >= _document.Layers[_selectedLayer].Bindings.Count ||
            _selectedKey is not null && !KeymapDocument.IsEditableKeyIndex(_selectedKey.Value))
            _selectedKey = null;
    }

    private bool IsDirty => _file is not null && _history is not null && _history.Current != _file.OriginalText;

    private bool ConfirmDiscardChanges()
    {
        if (!IsDirty) return true;
        var result = MessageBox.Show(this, "저장되지 않은 변경이 있습니다. 저장할까요?", "Unofficial MODU Keymap Studio",
            MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Cancel) return false;
        return result != MessageBoxResult.Yes || SaveDocument(saveAs: false);
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (!ConfirmDiscardChanges()) e.Cancel = true;
    }

    private void SetDocumentControls(bool enabled)
    {
        SaveButton.IsEnabled = SaveAsButton.IsEnabled = AddLayerButton.IsEnabled = RenameLayerButton.IsEnabled = BuildButton.IsEnabled = enabled;
        DeleteLayerButton.IsEnabled = false;
        UndoButton.IsEnabled = RedoButton.IsEnabled = false;
    }

    private void ShowBindingWarning(string message) =>
        MessageBox.Show(this, message, "바인딩 확인", MessageBoxButton.OK, MessageBoxImage.Warning);

    private static string FriendlyBinding(string raw)
    {
        if (raw == "&bootloader") return ZmkBehaviorCatalog.Bootloader.Label;
        if (raw == ZmkBehaviorCatalog.HeldBootloaderBinding) return ZmkBehaviorCatalog.HeldBootloader.Label;
        if (raw == ZmkBehaviorCatalog.HeldSystemResetBinding) return ZmkBehaviorCatalog.HeldSystemReset.Label;
        if (raw == "&sys_reset") return "시스템 재시작";
        if (raw.StartsWith("&kp ", StringComparison.Ordinal))
        {
            var code = raw[4..].Trim();
            var option = ZmkKeycodeCatalog.All.FirstOrDefault(item =>
                item.Aliases.Any(alias => alias.Equals(code, StringComparison.OrdinalIgnoreCase)));
            if (option is not null) return option.KeycapLabel;
        }

        return raw
        .Replace("&mo ", "MO ", StringComparison.Ordinal)
        .Replace("&tog ", "TOG ", StringComparison.Ordinal)
        .Replace("&to ", "TO ", StringComparison.Ordinal)
        .Replace("&bt BT_SEL ", "BT SELECT ", StringComparison.Ordinal)
        .Replace("&bt BT_CLR", "BT CLEAR", StringComparison.Ordinal)
        .Replace("&bt ", "BT ", StringComparison.Ordinal)
        .Replace("&mkp ", "MOUSE ", StringComparison.Ordinal)
        .Replace("&", string.Empty, StringComparison.Ordinal)
        .Replace('_', ' ');
    }

    private static string DescribeBinding(string raw)
    {
        if (raw == "&bootloader" || raw == ZmkBehaviorCatalog.HeldBootloaderBinding)
            return FriendlyBinding(raw) + " · " + ZmkBehaviorCatalog.BootloaderCaution;
        if (raw == ZmkBehaviorCatalog.HeldSystemResetBinding)
            return FriendlyBinding(raw) + " · " + ZmkBehaviorCatalog.ResetSourceCaution;
        if (!raw.StartsWith("&kp ", StringComparison.Ordinal)) return FriendlyBinding(raw);
        var code = raw[4..].Trim();
        var option = ZmkKeycodeCatalog.All.FirstOrDefault(item =>
            item.Aliases.Any(alias => alias.Equals(code, StringComparison.OrdinalIgnoreCase)));
        return option is null ? code : $"{option.DisplayLabel} · {option.EnglishName}";
    }

    private static IReadOnlyList<KeyOption> CreateKeyOptions()
    {
        var options = ZmkKeycodeCatalog.All
            .Select(item => new KeyOption(item.Category, item.PickerLabel, item.Binding, item.PickerDetail,
                $"{string.Join(' ', item.Aliases)} {item.Symbols}"))
            .ToList();
        options.Add(new KeyOption("Bluetooth", "Bluetooth 지우기", "&bt BT_CLR", "Bluetooth Clear", "bluetooth clear bt clr"));
        options.AddRange(Enumerable.Range(0, 5).Select(number => new KeyOption("Bluetooth", $"Bluetooth 프로필 {number}", $"&bt BT_SEL {number}", "Bluetooth Profile Select", "bluetooth profile select bt sel")));
        options.AddRange(new[] { ("왼쪽 클릭", "LCLK"), ("오른쪽 클릭", "RCLK"), ("가운데 클릭", "MCLK") }
            .Select(item => new KeyOption("마우스", item.Item1, $"&mkp {item.Item2}", "Mouse Button", $"mouse click {item.Item2}")));
        options.AddRange(ZmkBehaviorCatalog.All.Select(item =>
            new KeyOption(item.Category, item.Label, item.Binding, item.Detail, item.SearchTerms)));
        return options;
    }

    private sealed record KeyOption(string Category, string Label, string Binding, string Detail = "", string SearchTerms = "")
    {
        public bool Matches(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return true;
            var trimmed = query.Trim();
            if (trimmed.All(character => !char.IsLetterOrDigit(character) && !char.IsWhiteSpace(character)))
                return SearchTerms.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(trimmed, StringComparer.Ordinal);
            return string.Join(' ', Category, Label, Binding, Detail, SearchTerms)
                .Contains(trimmed, StringComparison.CurrentCultureIgnoreCase);
        }
    }
    private sealed record BehaviorChoice(string Label, string Code)
    {
        public override string ToString() => Label;
    }
    private sealed record LayerChoice(int Index, string Label)
    {
        public override string ToString() => Label;
    }
    private sealed record ThemeChoice(string Label, ThemePreference Preference)
    {
        public override string ToString() => Label;
    }
    private sealed record KeycapPresentation(string CenterText, string? BaseCharacter, string? ShiftCharacter, string ZmkCode);
}
