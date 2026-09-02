using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;

namespace ModuKeymapStudio.Services;

public static class ThemeManager
{
    private const string PersonalizeKey = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private static bool _initialized;

    public static ThemePreference Preference { get; private set; } = ThemePreference.System;
    public static bool IsDark { get; private set; }

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        Preference = AppSettingsStore.Load().ThemePreference;
        EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent,
            new RoutedEventHandler((sender, _) => ApplyWindow((Window)sender)));
        SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
        ApplyCurrentTheme();
    }

    public static void Shutdown()
    {
        if (!_initialized) return;
        SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
        _initialized = false;
    }

    public static void SetPreference(ThemePreference preference, bool save)
    {
        if (!Enum.IsDefined(preference)) preference = ThemePreference.System;
        Preference = preference;
        ApplyCurrentTheme();
        if (save) AppSettingsStore.SaveThemePreference(preference);
    }

    public static Brush GetBrush(string resourceKey) =>
        Application.Current.TryFindResource(resourceKey) as Brush ?? Brushes.Transparent;

    private static void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (Preference != ThemePreference.System || Application.Current is null) return;
        Application.Current.Dispatcher.BeginInvoke(ApplyCurrentTheme);
    }

    private static void ApplyCurrentTheme()
    {
        if (Application.Current is null) return;
        IsDark = Preference switch
        {
            ThemePreference.Dark => true,
            ThemePreference.Light => false,
            _ => IsWindowsAppThemeDark()
        };

        var palette = IsDark ? DarkPalette : LightPalette;
        foreach (var (key, color) in palette)
        {
            if (Application.Current.Resources[key] is SolidColorBrush existing && !existing.IsFrozen)
                existing.Color = color;
            else
                Application.Current.Resources[key] = new SolidColorBrush(color);
        }

        foreach (Window window in Application.Current.Windows)
            ApplyWindow(window);
    }

    private static bool IsWindowsAppThemeDark()
    {
        try
        {
            var value = Registry.GetValue(PersonalizeKey, "AppsUseLightTheme", 1);
            return value is int number && number == 0;
        }
        catch
        {
            return false;
        }
    }

    private static void ApplyWindow(Window window)
    {
        try
        {
            var handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero) return;
            var dark = IsDark ? 1 : 0;
            if (DwmSetWindowAttribute(handle, 20, ref dark, sizeof(int)) != 0)
                _ = DwmSetWindowAttribute(handle, 19, ref dark, sizeof(int));
        }
        catch (DllNotFoundException) { }
        catch (EntryPointNotFoundException) { }
    }

    private static readonly IReadOnlyDictionary<string, Color> LightPalette = new Dictionary<string, Color>
    {
        ["WindowBrush"] = Color.FromRgb(243, 244, 246),
        ["PanelBrush"] = Colors.White,
        ["PanelAltBrush"] = Color.FromRgb(247, 248, 250),
        ["BorderBrush"] = Color.FromRgb(210, 214, 220),
        ["TextBrush"] = Color.FromRgb(32, 33, 36),
        ["MutedBrush"] = Color.FromRgb(102, 112, 133),
        ["AccentBrush"] = Color.FromRgb(15, 108, 189),
        ["AccentSoftBrush"] = Color.FromRgb(220, 235, 250),
        ["AccentTextBrush"] = Color.FromRgb(7, 59, 102),
        ["AccentHoverBrush"] = Color.FromRgb(199, 223, 245),
        ["AccentPressedBrush"] = Color.FromRgb(174, 209, 239),
        ["ControlBrush"] = Color.FromRgb(248, 249, 250),
        ["ControlHoverBrush"] = Color.FromRgb(233, 236, 239),
        ["ControlPressedBrush"] = Color.FromRgb(221, 225, 230),
        ["TabBrush"] = Color.FromRgb(241, 243, 245),
        ["TabHoverBrush"] = Color.FromRgb(229, 231, 235),
        ["KeyboardSurfaceBrush"] = Color.FromRgb(238, 241, 245),
        ["KeyBrush"] = Colors.White,
        ["KeyBorderBrush"] = Color.FromRgb(201, 206, 214),
        ["SelectedKeyBrush"] = Color.FromRgb(220, 235, 250),
        ["TransparentKeyBrush"] = Color.FromRgb(245, 246, 248),
        ["TransparentKeyBorderBrush"] = Color.FromRgb(225, 228, 232),
        ["KeyTextBrush"] = Color.FromRgb(31, 41, 55),
        ["KeyDetailBrush"] = Color.FromRgb(107, 114, 128),
        ["TransparentKeyTextBrush"] = Color.FromRgb(143, 151, 163),
        ["TransparentKeyDetailBrush"] = Color.FromRgb(181, 187, 196),
        ["DragSourceBrush"] = Color.FromRgb(255, 232, 153),
        ["DragSourceBorderBrush"] = Color.FromRgb(173, 104, 0),
        ["DragTargetBrush"] = Color.FromRgb(214, 250, 232),
        ["DragTargetBorderBrush"] = Color.FromRgb(16, 124, 65),
        ["SuccessBrush"] = Color.FromRgb(16, 124, 65),
        ["WarningBrush"] = Color.FromRgb(154, 101, 0),
        ["DangerBrush"] = Color.FromRgb(196, 43, 28),
        ["InfoPanelBrush"] = Color.FromRgb(234, 243, 252),
        ["InfoBorderBrush"] = Color.FromRgb(155, 196, 234),
        ["InfoTextBrush"] = Color.FromRgb(9, 79, 145),
        ["WarningPanelBrush"] = Color.FromRgb(255, 244, 229),
        ["WarningBorderBrush"] = Color.FromRgb(231, 182, 107),
        ["WarningTextBrush"] = Color.FromRgb(138, 75, 8),
        ["AccentForegroundBrush"] = Colors.White
    };

    private static readonly IReadOnlyDictionary<string, Color> DarkPalette = new Dictionary<string, Color>
    {
        ["WindowBrush"] = Color.FromRgb(24, 26, 30),
        ["PanelBrush"] = Color.FromRgb(31, 34, 39),
        ["PanelAltBrush"] = Color.FromRgb(38, 42, 48),
        ["BorderBrush"] = Color.FromRgb(68, 74, 84),
        ["TextBrush"] = Color.FromRgb(235, 238, 242),
        ["MutedBrush"] = Color.FromRgb(166, 175, 189),
        ["AccentBrush"] = Color.FromRgb(90, 169, 240),
        ["AccentSoftBrush"] = Color.FromRgb(39, 67, 94),
        ["AccentTextBrush"] = Color.FromRgb(205, 230, 255),
        ["AccentHoverBrush"] = Color.FromRgb(48, 82, 114),
        ["AccentPressedBrush"] = Color.FromRgb(57, 95, 130),
        ["ControlBrush"] = Color.FromRgb(43, 47, 54),
        ["ControlHoverBrush"] = Color.FromRgb(55, 60, 69),
        ["ControlPressedBrush"] = Color.FromRgb(67, 73, 83),
        ["TabBrush"] = Color.FromRgb(40, 44, 50),
        ["TabHoverBrush"] = Color.FromRgb(53, 58, 66),
        ["KeyboardSurfaceBrush"] = Color.FromRgb(22, 24, 28),
        ["KeyBrush"] = Color.FromRgb(45, 49, 56),
        ["KeyBorderBrush"] = Color.FromRgb(78, 85, 96),
        ["SelectedKeyBrush"] = Color.FromRgb(41, 71, 99),
        ["TransparentKeyBrush"] = Color.FromRgb(34, 37, 42),
        ["TransparentKeyBorderBrush"] = Color.FromRgb(58, 63, 71),
        ["KeyTextBrush"] = Color.FromRgb(240, 242, 245),
        ["KeyDetailBrush"] = Color.FromRgb(169, 178, 191),
        ["TransparentKeyTextBrush"] = Color.FromRgb(128, 136, 148),
        ["TransparentKeyDetailBrush"] = Color.FromRgb(91, 98, 109),
        ["DragSourceBrush"] = Color.FromRgb(112, 75, 18),
        ["DragSourceBorderBrush"] = Color.FromRgb(255, 196, 77),
        ["DragTargetBrush"] = Color.FromRgb(31, 83, 59),
        ["DragTargetBorderBrush"] = Color.FromRgb(80, 201, 132),
        ["SuccessBrush"] = Color.FromRgb(80, 201, 132),
        ["WarningBrush"] = Color.FromRgb(244, 190, 85),
        ["DangerBrush"] = Color.FromRgb(255, 115, 102),
        ["InfoPanelBrush"] = Color.FromRgb(31, 55, 78),
        ["InfoBorderBrush"] = Color.FromRgb(61, 111, 156),
        ["InfoTextBrush"] = Color.FromRgb(174, 215, 252),
        ["WarningPanelBrush"] = Color.FromRgb(76, 54, 27),
        ["WarningBorderBrush"] = Color.FromRgb(145, 103, 45),
        ["WarningTextBrush"] = Color.FromRgb(255, 213, 143),
        ["AccentForegroundBrush"] = Colors.White
    };

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
