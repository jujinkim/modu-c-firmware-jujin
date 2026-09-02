using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ModuKeymapStudio.Core.Models;

namespace ModuKeymapStudio;

public partial class AllLayersPreviewWindow : Window
{
    public AllLayersPreviewWindow(KeymapDocument document, Func<int, FrameworkElement> keyboardFactory)
    {
        InitializeComponent();
        PreviewStatusText.Text = $"{document.Layers.Count}개 레이어 · 세로 스크롤 · 모든 레이어를 한 장의 PNG로 내보냅니다.";
        ExportSubtitleText.Text = $"{document.Layers.Count}개 레이어 · MODU {KeymapDocument.ModuEditableKeyCount}키 배열";

        foreach (var layer in document.Layers)
            LayersPanel.Children.Add(CreateLayerCard(layer, keyboardFactory(layer.Index)));
    }

    private static Border CreateLayerCard(Layer layer, FrameworkElement keyboard)
    {
        var displayName = new TextBlock
        {
            Text = $"{layer.Index}: {layer.DisplayName}",
            FontSize = 22,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        var nodeName = new TextBlock
        {
            Text = layer.NodeName,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0)
        };
        nodeName.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush");

        var header = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(2, 0, 2, 12) };
        header.Children.Add(displayName);
        header.Children.Add(nodeName);

        var keyboardSurface = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(16),
            Child = keyboard
        };
        keyboardSurface.SetResourceReference(Border.BackgroundProperty, "KeyboardSurfaceBrush");
        keyboardSurface.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");

        var content = new StackPanel();
        content.Children.Add(header);
        content.Children.Add(keyboardSurface);

        var card = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(18),
            Margin = new Thickness(0, 0, 0, 20),
            Child = content
        };
        card.SetResourceReference(Border.BackgroundProperty, "PanelBrush");
        card.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");
        return card;
    }

    private void SavePng_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "전체 레이어 PNG 저장",
            Filter = "PNG 이미지 (*.png)|*.png",
            DefaultExt = ".png",
            AddExtension = true,
            OverwritePrompt = true,
            FileName = $"modu-keymap-all-layers-{DateTime.Now:yyyyMMdd-HHmmss}.png"
        };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            File.WriteAllBytes(dialog.FileName, EncodePng(RenderAllLayers()));
            PreviewStatusText.Text = $"PNG를 저장했습니다: {dialog.FileName}";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            MessageBox.Show(this, exception.Message, "PNG를 저장할 수 없음", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CopyPng_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var bitmap = RenderAllLayers();
            using var pngStream = new MemoryStream(EncodePng(bitmap));
            var data = new DataObject();
            data.SetData(DataFormats.Bitmap, bitmap);
            data.SetData("PNG", pngStream);
            Clipboard.SetDataObject(data, copy: true);
            PreviewStatusText.Text = "모든 레이어를 한 장의 PNG 이미지로 클립보드에 복사했습니다.";
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            MessageBox.Show(this, exception.Message, "PNG를 복사할 수 없음", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private BitmapSource RenderAllLayers()
    {
        var horizontalOffset = PreviewScrollViewer.HorizontalOffset;
        var verticalOffset = PreviewScrollViewer.VerticalOffset;

        try
        {
            // ScrollViewer translates its child while scrolling. Render from the
            // origin so the exported bitmap is independent of the current view.
            PreviewScrollViewer.ScrollToHorizontalOffset(0);
            PreviewScrollViewer.ScrollToVerticalOffset(0);
            PreviewScrollViewer.UpdateLayout();
            ExportSurface.UpdateLayout();

            var width = ExportSurface.ActualWidth;
            var height = ExportSurface.ActualHeight;
            if (width <= 0 || height <= 0)
                throw new InvalidOperationException("미리보기 크기를 계산할 수 없습니다.");

            const double preferredScale = 1.5;
            const double maximumPixels = 80_000_000;
            var scale = Math.Min(preferredScale, Math.Min(30000 / width, 30000 / height));
            scale = Math.Min(scale, Math.Sqrt(maximumPixels / (width * height)));
            if (scale < 0.25)
                throw new InvalidOperationException("레이어 미리보기가 한 장의 PNG로 만들기에 너무 큽니다.");

            var pixelWidth = Math.Max(1, (int)Math.Ceiling(width * scale));
            var pixelHeight = Math.Max(1, (int)Math.Ceiling(height * scale));
            var bitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, 96 * scale, 96 * scale, PixelFormats.Pbgra32);
            bitmap.Render(ExportSurface);
            bitmap.Freeze();
            return bitmap;
        }
        finally
        {
            PreviewScrollViewer.ScrollToHorizontalOffset(horizontalOffset);
            PreviewScrollViewer.ScrollToVerticalOffset(verticalOffset);
            PreviewScrollViewer.UpdateLayout();
        }
    }

    private static byte[] EncodePng(BitmapSource bitmap)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
