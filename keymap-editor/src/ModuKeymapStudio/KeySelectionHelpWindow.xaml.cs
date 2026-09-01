using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace ModuKeymapStudio;

public partial class KeySelectionHelpWindow : Window
{
    public KeySelectionHelpWindow()
    {
        InitializeComponent();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "링크를 열 수 없음", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        e.Handled = true;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
