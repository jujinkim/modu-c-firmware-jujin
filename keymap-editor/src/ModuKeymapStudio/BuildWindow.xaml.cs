using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using ModuKeymapStudio.Core.Build;
using ModuKeymapStudio.Services;

namespace ModuKeymapStudio;

public partial class BuildWindow : Window
{
    private const string ZmkSetupUrl = "https://zmk.dev/docs/development/local-toolchain/setup/native";
    private const string ZephyrSetupUrl = "https://docs.zephyrproject.org/latest/develop/getting_started/index.html";
    private const string WestSdkUrl = "https://docs.zephyrproject.org/latest/develop/west/zephyr-cmds.html#working-with-the-zephyr-sdk-west-sdk";

    private readonly string _repositoryRoot;
    private readonly string _outputsPath;
    private readonly BuildProcessRunner _runner = new();
    private readonly BuildEnvironmentChecker _environmentChecker = new();
    private CancellationTokenSource? _cancellation;
    private CancellationTokenSource? _environmentCancellation;
    private BuildEnvironmentReport? _environmentReport;
    private string? _checkedZmkPath;
    private BuildSide _currentSide = BuildSide.General;
    private string? _lastError;
    private bool _isLoaded;
    private bool _isChecking;
    private bool _isRunning;

    public BuildWindow(string repositoryRoot)
    {
        InitializeComponent();
        _repositoryRoot = repositoryRoot;
        _outputsPath = Path.Combine(repositoryRoot, "outputs");
        PrerequisiteCommandsBox.Text = PrerequisiteCommands;
        SetupCommandsBox.Text = InstallationCommands;
        EnvironmentChecksList.ItemsSource = new[]
        {
            EnvironmentCheckDisplay.Waiting("환경 확인", "ZMK app 경로를 선택하면 자동으로 점검합니다.")
        };
        ZmkPathBox.Text = AppSettingsStore.Load().ZmkAppPath ?? @"C:\zmk\app";
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = true;
        await CheckEnvironmentAsync();
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "ZMK 저장소의 app 폴더를 선택하세요.",
            InitialDirectory = Directory.Exists(ZmkPathBox.Text) ? ZmkPathBox.Text : string.Empty,
            Multiselect = false
        };
        if (dialog.ShowDialog(this) == true) ZmkPathBox.Text = dialog.FolderName;
    }

    private async void CheckEnvironment_Click(object sender, RoutedEventArgs e) => await CheckEnvironmentAsync();

    private async Task<bool> CheckEnvironmentAsync()
    {
        if (_isChecking || _isRunning) return _environmentReport?.CanBuild == true;

        var zmkPath = ZmkPathBox.Text.Trim();
        _isChecking = true;
        _environmentCancellation = new CancellationTokenSource();
        _environmentReport = null;
        _checkedZmkPath = null;
        EnvironmentTab.IsSelected = true;
        EnvironmentSummaryText.Text = "확인 중…";
        EnvironmentSummaryText.Foreground = BrushFor(EnvironmentCheckStatus.Warning);
        EnvironmentChecksList.ItemsSource = new[]
        {
            EnvironmentCheckDisplay.Waiting("환경 확인", "Python, west, 워크스페이스와 ARM 툴체인을 확인하는 중입니다…")
        };
        BuildStatusText.Text = "빌드 환경을 확인하는 중…";
        SetChecking(true);

        try
        {
            var report = await _environmentChecker.CheckAsync(_repositoryRoot, zmkPath, _environmentCancellation.Token);
            _environmentReport = report;
            _checkedZmkPath = zmkPath;
            EnvironmentChecksList.ItemsSource = report.Items.Select(EnvironmentCheckDisplay.From).ToArray();

            var passed = report.Items.Count(item => item.Status == EnvironmentCheckStatus.Passed);
            var warnings = report.Items.Count(item => item.Status == EnvironmentCheckStatus.Warning);
            var failed = report.Items.Count(item => item.Status == EnvironmentCheckStatus.Failed);
            if (report.CanBuild)
            {
                EnvironmentSummaryText.Text = warnings == 0 ? $"준비 완료 · {passed}개 통과" : $"빌드 가능 · 경고 {warnings}개";
                EnvironmentSummaryText.Foreground = BrushFor(warnings == 0
                    ? EnvironmentCheckStatus.Passed
                    : EnvironmentCheckStatus.Warning);
                BuildStatusText.Text = warnings == 0
                    ? "빌드 환경이 준비되었습니다."
                    : "빌드할 수 있지만 경고가 있습니다. 실제 빌드 결과를 확인하세요.";
                SetupGuideTab.Header = "설치 가이드";
            }
            else
            {
                EnvironmentSummaryText.Text = $"준비 필요 · {failed}개 실패";
                EnvironmentSummaryText.Foreground = BrushFor(EnvironmentCheckStatus.Failed);
                BuildStatusText.Text = "빌드 환경이 준비되지 않았습니다. 실패 항목과 설치 가이드를 확인하세요.";
                SetupGuideTab.Header = "설치 가이드 · 확인 필요";
            }

            if (Directory.Exists(zmkPath)) TrySaveSettings(zmkPath);
            return report.CanBuild;
        }
        catch (OperationCanceledException)
        {
            BuildStatusText.Text = "환경 확인을 취소했습니다.";
            EnvironmentSummaryText.Text = "확인 취소";
            return false;
        }
        catch (Exception exception)
        {
            EnvironmentChecksList.ItemsSource = new[]
            {
                EnvironmentCheckDisplay.Failure("환경 확인", exception.Message, "설치 가이드와 경로를 확인한 뒤 다시 시도하세요.")
            };
            EnvironmentSummaryText.Text = "확인 오류";
            EnvironmentSummaryText.Foreground = BrushFor(EnvironmentCheckStatus.Failed);
            BuildStatusText.Text = $"환경 확인 중 오류가 발생했습니다: {exception.Message}";
            return false;
        }
        finally
        {
            _isChecking = false;
            _environmentCancellation?.Dispose();
            _environmentCancellation = null;
            SetChecking(false);
        }
    }

    private void ZmkPathBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (!_isLoaded || _isChecking || _isRunning) return;
        if (string.Equals(_checkedZmkPath, ZmkPathBox.Text.Trim(), StringComparison.OrdinalIgnoreCase)) return;

        _environmentReport = null;
        _checkedZmkPath = null;
        StartBuildButton.IsEnabled = false;
        EnvironmentSummaryText.Text = "다시 확인 필요";
        EnvironmentSummaryText.Foreground = BrushFor(EnvironmentCheckStatus.Warning);
        BuildStatusText.Text = "ZMK app 경로가 변경되었습니다. 환경 확인을 다시 실행하세요.";
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        var zmkPath = ZmkPathBox.Text.Trim();
        if (_environmentReport?.CanBuild != true ||
            !string.Equals(_checkedZmkPath, zmkPath, StringComparison.OrdinalIgnoreCase))
        {
            if (!await CheckEnvironmentAsync()) return;
        }

        TrySaveSettings(zmkPath);
        AllLogBox.Clear();
        LeftLogBox.Clear();
        RightLogBox.Clear();
        AllLogTab.IsSelected = true;
        _lastError = null;
        _currentSide = BuildSide.General;
        _cancellation = new CancellationTokenSource();
        SetRunning(true);
        BuildStatusText.Text = "빌드를 시작하는 중…";

        var startInfo = new ProcessStartInfo("powershell.exe") { WorkingDirectory = _repositoryRoot };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(Path.Combine(_repositoryRoot, "build.ps1"));
        startInfo.ArgumentList.Add("-ZmkApp");
        startInfo.ArgumentList.Add(zmkPath);

        try
        {
            var result = await _runner.RunAsync(startInfo,
                output => Dispatcher.BeginInvoke(() => ProcessOutput(output)), _cancellation.Token);
            if (result.WasCancelled)
            {
                BuildStatusText.Text = "빌드를 취소했습니다.";
            }
            else if (result.Succeeded)
            {
                BuildStatusText.Text = $"왼쪽·오른쪽 빌드가 완료되었습니다. ({result.Duration:mm\\:ss})";
                OpenOutputsButton.IsEnabled = Directory.Exists(_outputsPath);
                if (OpenOutputsButton.IsEnabled) OpenOutputsFolder();
            }
            else
            {
                BuildStatusText.Text = $"빌드 실패 (종료 코드 {result.ExitCode})" +
                                       (_lastError is null ? string.Empty : $" · {_lastError}");
            }
        }
        catch (Exception exception)
        {
            BuildStatusText.Text = $"빌드 프로세스를 시작하지 못했습니다: {exception.Message}";
            Append(AllLogBox, "[오류] " + exception);
        }
        finally
        {
            SetRunning(false);
            _cancellation?.Dispose();
            _cancellation = null;
        }
    }

    private void ProcessOutput(BuildOutput output)
    {
        var line = output.IsError ? "[오류] " + output.Text : output.Text;
        if (output.Text.Contains("Building modu_left", StringComparison.OrdinalIgnoreCase)) _currentSide = BuildSide.Left;
        else if (output.Text.Contains("Building modu_right", StringComparison.OrdinalIgnoreCase)) _currentSide = BuildSide.Right;
        if (output.IsError) _lastError = output.Text;

        Append(AllLogBox, line);
        if (_currentSide == BuildSide.Left)
        {
            Append(LeftLogBox, line);
            BuildStatusText.Text = "왼쪽 펌웨어를 빌드하는 중…";
        }
        else if (_currentSide == BuildSide.Right)
        {
            Append(RightLogBox, line);
            BuildStatusText.Text = "오른쪽 펌웨어를 빌드하는 중…";
        }
    }

    private static void Append(System.Windows.Controls.TextBox box, string line)
    {
        box.AppendText(line + Environment.NewLine);
        box.ScrollToEnd();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        BuildStatusText.Text = "빌드 프로세스를 종료하는 중…";
        _cancellation?.Cancel();
    }

    private void OpenOutputs_Click(object sender, RoutedEventArgs e) => OpenOutputsFolder();

    private void OpenOutputsFolder()
    {
        if (!Directory.Exists(_outputsPath)) return;
        Process.Start(new ProcessStartInfo("explorer.exe", _outputsPath) { UseShellExecute = true });
    }

    private void OpenZmkGuide_Click(object sender, RoutedEventArgs e) => OpenUrl(ZmkSetupUrl);
    private void OpenZephyrGuide_Click(object sender, RoutedEventArgs e) => OpenUrl(ZephyrSetupUrl);
    private void OpenWestSdkGuide_Click(object sender, RoutedEventArgs e) => OpenUrl(WestSdkUrl);

    private void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch (Exception exception)
        {
            MessageBox.Show(this, $"문서를 열지 못했습니다: {exception.Message}\n\n{url}",
                "설치 가이드", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void TrySaveSettings(string zmkPath)
    {
        try { AppSettingsStore.SaveZmkAppPath(zmkPath); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"설정을 저장하지 못했습니다: {exception.Message}",
                "설정", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void SetChecking(bool checking)
    {
        CheckEnvironmentButton.IsEnabled = !checking;
        ZmkPathBox.IsEnabled = !checking;
        StartBuildButton.IsEnabled = !checking && _environmentReport?.CanBuild == true;
        BuildProgress.Visibility = checking ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SetRunning(bool running)
    {
        _isRunning = running;
        StartBuildButton.IsEnabled = !running && _environmentReport?.CanBuild == true;
        CheckEnvironmentButton.IsEnabled = !running;
        CancelBuildButton.IsEnabled = running;
        ZmkPathBox.IsEnabled = !running;
        BuildProgress.Visibility = running ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        _environmentCancellation?.Cancel();
        if (_cancellation is null) return;
        e.Cancel = true;
        BuildStatusText.Text = "실행 중인 빌드를 먼저 취소합니다…";
        _cancellation.Cancel();
    }

    private static Brush BrushFor(EnvironmentCheckStatus status) => ThemeManager.GetBrush(status switch
    {
        EnvironmentCheckStatus.Passed => "SuccessBrush",
        EnvironmentCheckStatus.Warning => "WarningBrush",
        EnvironmentCheckStatus.Failed => "DangerBrush",
        _ => "MutedBrush"
    });

    private sealed record EnvironmentCheckDisplay(
        string StatusLabel,
        Brush StatusBrush,
        string Name,
        string Detail,
        string Guidance)
    {
        public static EnvironmentCheckDisplay From(EnvironmentCheckItem item) => new(
            item.Status switch
            {
                EnvironmentCheckStatus.Passed => "통과",
                EnvironmentCheckStatus.Warning => "주의",
                EnvironmentCheckStatus.Failed => "실패",
                _ => "대기"
            },
            BrushFor(item.Status),
            item.Name,
            item.Detail,
            item.Guidance ?? string.Empty);

        public static EnvironmentCheckDisplay Waiting(string name, string detail) =>
            new("대기", BrushFor(EnvironmentCheckStatus.Skipped), name, detail, string.Empty);

        public static EnvironmentCheckDisplay Failure(string name, string detail, string guidance) =>
            new("실패", BrushFor(EnvironmentCheckStatus.Failed), name, detail, guidance);
    }

    private const string PrerequisiteCommands = """
        winget install --exact --id Python.Python.3.12 --source winget
        winget install --exact --id 7zip.7zip --source winget

        # 설치가 끝나면 새 PowerShell에서 확인
        py -3.12 --version

        # 새 터미널을 열 수 없고 7z.exe만 안 잡힐 때: 현재 세션에만 적용
        $sevenZipBin = Join-Path $env:ProgramFiles '7-Zip'
        if (-not (Test-Path (Join-Path $sevenZipBin '7z.exe'))) {
            throw 'C:\Program Files\7-Zip\7z.exe를 찾지 못했습니다.'
        }
        $env:Path = "$sevenZipBin;$env:Path"
        7z.exe i
        """;

    private const string InstallationCommands = """
        git clone https://github.com/zmkfirmware/zmk.git C:\zmk
        Set-Location C:\zmk
        py -3.12 -m venv .venv
        .\.venv\Scripts\Activate.ps1
        python -m pip install --upgrade west
        python -m west init -l app
        python -m west update
        python -m west zephyr-export
        python -m west packages pip --install
        Set-Location .\zephyr
        python -m west sdk install --toolchains arm-zephyr-eabi
        """;

    private enum BuildSide { General, Left, Right }
}
