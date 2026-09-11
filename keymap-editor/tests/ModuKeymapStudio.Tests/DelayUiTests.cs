using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ModuKeymapStudio;
using ModuKeymapStudio.Core.Editing;
using ModuKeymapStudio.Core.IO;
using ModuKeymapStudio.Core.Models;

static class DelayUiTests
{
    public static Task Run()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { Verify(); }
            catch (Exception exception) { failure = exception.InnerException ?? exception; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null) throw failure;
        return Task.CompletedTask;
    }

    private static void Verify()
    {
        var app = new App();
        app.InitializeComponent();
        var window = new MainWindow();
        void Call(string name, params object?[] parameters) => typeof(MainWindow)
            .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, parameters);
        void Field(string name, object value) => typeof(MainWindow)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, value);
        KeymapDocument Document() => (KeymapDocument)typeof(MainWindow)
            .GetField("_document", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
        T Control<T>(string name) => (T)window.FindName(name);

        Call("LoadKeymap", RepositoryLocator.FindKeymap(AppContext.BaseDirectory, Environment.CurrentDirectory));
        Field("_selectedKey", 0);
        Call("RefreshSelectionEditor");
        var toggle = Control<CheckBox>("DelayCheckBox");
        var time = Control<ComboBox>("DelayBox");
        DelayTests.Check(time.Items.Count == 20 && !time.IsEnabled, "지연 체크 전에는 시간 선택 비활성");
        toggle.IsChecked = true;
        time.SelectedValue = 500;
        Control<TextBox>("RawBindingBox").Text = "&kp A";
        Call("ApplyRaw_Click", window, new RoutedEventArgs());
        DelayTests.Setting(Document(), 0, 0, "&kp A", 500);
        DelayTests.Check(time.IsEnabled && toggle.IsChecked == true && (int)time.SelectedValue == 500, "적용 후 지연 선택 유지");

        // Exercise the other entry paths while a delay is selected.
        var presets = Control<ListBox>("PresetList");
        presets.SelectedItem = presets.Items.Cast<object>().First(item =>
            (string)item.GetType().GetProperty("Binding")!.GetValue(item)! == "&bt BT_CLR");
        Call("ApplyPreset_Click", window, new RoutedEventArgs());
        DelayTests.Setting(Document(), 0, 0, "&bt BT_CLR", 500);
        Control<ComboBox>("BehaviorBox").SelectedValue = "&kp";
        Control<TextBox>("ArgumentBox").Text = "N1";
        Call("ApplyBehavior_Click", window, new RoutedEventArgs());
        DelayTests.Setting(Document(), 0, 0, "&kp N1", 500);
        time.SelectedValue = 1000;
        Call("ApplyDelay_Click", window, new RoutedEventArgs());
        DelayTests.Setting(Document(), 0, 0, "&kp N1", 1000);
        var key = (Button)FindElements(Control<StackPanel>("KeyboardPanel")).First(item => item is Button { Tag: 0 });
        var texts = FindElements((DependencyObject)key.Content).OfType<TextBlock>().Select(text => text.Text).ToArray();
        DelayTests.Check(texts.Contains("(1000ms)") && texts.Contains("1") && texts.Contains("!"), "키 이름·Shift 문자·짧은 시간 표시");
        DelayTests.Check(texts.All(text => !text.Contains("길게") && !text.Contains("mks_delay")), "키캡의 긴 설명 제거");
        toggle.IsChecked = false;
        Call("ApplyDelay_Click", window, new RoutedEventArgs());
        DelayTests.Check(Document().Layers[0].Bindings[0].Raw == "&kp N1", "지연 해제");
        Call("Undo_Click", window, new RoutedEventArgs());
        DelayTests.Setting(Document(), 0, 0, "&kp N1", 1000);
        DelayTests.Check(toggle.IsChecked == true && (int)time.SelectedValue == 1000, "undo 후 패널 복원");

        var fixture = DelayTests.CreateFixture();
        Field("_document", fixture);
        Field("_history", new DocumentHistory(fixture.Source));
        Call("RefreshAll");
        var root = (FrameworkElement)window.Content;
        root.Measure(new Size(1680, 900));
        root.Arrange(new Rect(0, 0, 1680, 900));
        root.UpdateLayout();
        var bitmap = new RenderTargetBitmap(1680, 900, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(root);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        var repository = RepositoryLocator.FindRepositoryRoot(RepositoryLocator.FindKeymap(AppContext.BaseDirectory, Environment.CurrentDirectory)!)!;
        var output = Path.Combine(repository, "keymap-editor", "obj", "delay-preview.png");
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        using (var stream = File.Create(output)) encoder.Save(stream);
        app.Shutdown();
    }

    private static IEnumerable<DependencyObject> FindElements(DependencyObject root)
    {
        yield return root;
        // Logical traversal also works before a window is shown/template rendered.
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        foreach (var descendant in FindElements(child)) yield return descendant;
    }
}
