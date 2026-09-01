using System.ComponentModel;
using System.Diagnostics;

namespace ModuKeymapStudio.Core.Build;

public enum EnvironmentCheckStatus
{
    Passed,
    Warning,
    Failed,
    Skipped
}

public sealed record EnvironmentCheckItem(
    string Name,
    EnvironmentCheckStatus Status,
    string Detail,
    string? Guidance = null);

public sealed record BuildEnvironmentReport(
    IReadOnlyList<EnvironmentCheckItem> Items,
    string? PythonPath = null,
    string? WestWorkspaceRoot = null)
{
    public bool CanBuild => Items.Count > 0 && Items.All(item =>
        item.Status is EnvironmentCheckStatus.Passed or EnvironmentCheckStatus.Warning);
}

public sealed record CommandProbeResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool Succeeded => ExitCode == 0;
    public string CombinedOutput => string.Join(Environment.NewLine,
        new[] { StandardOutput.Trim(), StandardError.Trim() }.Where(value => value.Length > 0));
}

public interface ICommandProbe
{
    Task<CommandProbeResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        CancellationToken cancellationToken);
}

public sealed class SystemCommandProbe : ICommandProbe
{
    public async Task<CommandProbeResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start()) throw new InvalidOperationException($"프로세스를 시작하지 못했습니다: {fileName}");

        using var registration = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException) { }
        });

        var standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardError = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        return new CommandProbeResult(
            process.ExitCode,
            await standardOutput.ConfigureAwait(false),
            await standardError.ConfigureAwait(false));
    }
}

public sealed class BuildEnvironmentChecker
{
    private static readonly string[] RemainingCheckNames =
    {
        "Python", "west", "west 워크스페이스", "west build", "CMake · Ninja", "Zephyr SDK"
    };

    private readonly ICommandProbe _probe;
    private readonly TimeSpan _probeTimeout;

    public BuildEnvironmentChecker(ICommandProbe? probe = null, TimeSpan? probeTimeout = null)
    {
        _probe = probe ?? new SystemCommandProbe();
        _probeTimeout = probeTimeout ?? TimeSpan.FromSeconds(15);
    }

    public async Task<BuildEnvironmentReport> CheckAsync(
        string repositoryRoot,
        string zmkAppPath,
        CancellationToken cancellationToken = default)
    {
        var items = new List<EnvironmentCheckItem>();
        repositoryRoot = Path.GetFullPath(repositoryRoot);

        var requiredProjectPaths = new[]
        {
            Path.Combine(repositoryRoot, "build.ps1"),
            Path.Combine(repositoryRoot, "modu-module"),
            Path.Combine(repositoryRoot, "zmk-pmw3610-driver"),
            Path.Combine(repositoryRoot, "tools", "uf2", "uf2conv.py")
        };
        var missingProjectPath = requiredProjectPaths.FirstOrDefault(path =>
            !File.Exists(path) && !Directory.Exists(path));
        if (!IsAscii(repositoryRoot))
        {
            items.Add(Failed("프로젝트 파일", "저장소 경로에 비 ASCII 문자가 있습니다.",
                "저장소를 C:\\work\\modu처럼 영문·숫자 경로로 옮기세요."));
        }
        else if (missingProjectPath is not null)
        {
            items.Add(Failed("프로젝트 파일", $"필수 파일을 찾지 못했습니다: {missingProjectPath}",
                "완전한 MODU-C 펌웨어 저장소에서 앱을 실행하세요."));
        }
        else
        {
            items.Add(Passed("프로젝트 파일", "build.ps1, MODU 모듈, UF2 변환기를 찾았습니다."));
        }

        if (string.IsNullOrWhiteSpace(zmkAppPath))
        {
            items.Add(Failed("ZMK app", "ZMK app 폴더가 지정되지 않았습니다.",
                "ZMK 소스를 준비한 뒤 C:\\zmk\\app 폴더를 선택하세요."));
            AddSkipped(items, RemainingCheckNames, "ZMK app 폴더를 먼저 선택해야 합니다.");
            return new BuildEnvironmentReport(items);
        }

        string normalizedZmkApp;
        try { normalizedZmkApp = Path.GetFullPath(zmkAppPath); }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            items.Add(Failed("ZMK app", $"올바르지 않은 경로입니다: {exception.Message}",
                "ZMK 소스를 준비한 뒤 C:\\zmk\\app 폴더를 선택하세요."));
            AddSkipped(items, RemainingCheckNames, "ZMK app 경로를 먼저 수정해야 합니다.");
            return new BuildEnvironmentReport(items);
        }

        if (!IsAscii(normalizedZmkApp))
        {
            items.Add(Failed("ZMK app", "경로에 비 ASCII 문자가 있습니다.",
                "ZMK를 C:\\zmk처럼 영문·숫자 경로에 설치하세요."));
            AddSkipped(items, RemainingCheckNames, "ZMK app 경로를 먼저 수정해야 합니다.");
            return new BuildEnvironmentReport(items);
        }
        if (!Directory.Exists(normalizedZmkApp) || !File.Exists(Path.Combine(normalizedZmkApp, "CMakeLists.txt")))
        {
            items.Add(Failed("ZMK app", "CMakeLists.txt가 있는 ZMK app 폴더를 찾지 못했습니다.",
                "ZMK 저장소를 준비하고 west update를 완료한 뒤 C:\\zmk\\app을 선택하세요."));
            AddSkipped(items, RemainingCheckNames, "유효한 ZMK app 폴더가 필요합니다.");
            return new BuildEnvironmentReport(items);
        }
        items.Add(Passed("ZMK app", normalizedZmkApp));

        var zmkRoot = Directory.GetParent(normalizedZmkApp)?.FullName ?? normalizedZmkApp;
        var pythonCandidates = new[]
        {
            Path.Combine(zmkRoot, ".venv", "Scripts", "python.exe"),
            "python.exe",
            "python",
            "python3"
        }.Where((candidate, index) => index > 0 || File.Exists(candidate)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        var workingPython = new List<(string Path, string Version)>();
        foreach (var candidate in pythonCandidates)
        {
            var result = await TryProbeAsync(candidate, new[] { "--version" }, normalizedZmkApp, cancellationToken);
            if (result is { Succeeded: true })
                workingPython.Add((candidate, FirstUsefulLine(result.CombinedOutput, "Python 실행 가능")));
        }

        if (workingPython.Count == 0)
        {
            items.Add(Failed("Python", "실행 가능한 Python을 찾지 못했습니다.",
                "Python 3.12를 설치하고 ZMK 루트에 .venv를 만드세요."));
            AddSkipped(items, RemainingCheckNames[1..], "Python이 준비되어야 확인할 수 있습니다.");
            return new BuildEnvironmentReport(items);
        }
        items.Add(Passed("Python", workingPython[0].Version));

        string? selectedPython = null;
        string? westVersion = null;
        foreach (var candidate in workingPython)
        {
            var result = await TryProbeAsync(candidate.Path, new[] { "-m", "west", "--version" }, normalizedZmkApp, cancellationToken);
            if (result is not { Succeeded: true }) continue;
            selectedPython = candidate.Path;
            westVersion = FirstUsefulLine(result.CombinedOutput, "west 실행 가능");
            break;
        }

        if (selectedPython is null)
        {
            items.Add(Failed("west", "Python 환경에서 west 모듈을 실행할 수 없습니다.",
                "ZMK의 .venv를 활성화하고 python -m pip install -U west를 실행하세요."));
            AddSkipped(items, RemainingCheckNames[2..], "west가 준비되어야 확인할 수 있습니다.");
            return new BuildEnvironmentReport(items);
        }
        items.Add(Passed("west", $"{westVersion} · {selectedPython}"));

        var topdirResult = await TryProbeAsync(selectedPython, new[] { "-m", "west", "topdir" }, normalizedZmkApp, cancellationToken);
        var westRoot = topdirResult is { Succeeded: true }
            ? LastExistingDirectoryLine(topdirResult.StandardOutput)
            : null;
        if (westRoot is null || !File.Exists(Path.Combine(westRoot, ".west", "config")))
        {
            items.Add(Failed("west 워크스페이스", "선택한 app이 초기화된 west 워크스페이스 안에 있지 않습니다.",
                "ZMK 루트에서 west init -l app 및 west update를 실행하세요."));
            AddSkipped(items, RemainingCheckNames[3..], "west 워크스페이스가 준비되어야 확인할 수 있습니다.");
            return new BuildEnvironmentReport(items, selectedPython);
        }
        items.Add(Passed("west 워크스페이스", westRoot));

        var westBuildResult = await TryProbeAsync(selectedPython,
            new[] { "-m", "west", "build", "-h" }, normalizedZmkApp, cancellationToken);
        if (westBuildResult is not { Succeeded: true })
        {
            items.Add(Failed("west build", CompactError(westBuildResult, "west build 명령을 불러오지 못했습니다."),
                "ZMK 루트에서 west zephyr-export와 west packages pip --install을 실행하세요."));
        }
        else
        {
            items.Add(Passed("west build", "Zephyr 빌드 확장 명령을 불러왔습니다."));
        }

        var cmakeResult = await TryProbeAsync("cmake", new[] { "--version" }, normalizedZmkApp, cancellationToken);
        var ninjaResult = await TryProbeAsync("ninja", new[] { "--version" }, normalizedZmkApp, cancellationToken);
        if (cmakeResult is not { Succeeded: true } || ninjaResult is not { Succeeded: true })
        {
            var missing = new List<string>();
            if (cmakeResult is not { Succeeded: true }) missing.Add("CMake");
            if (ninjaResult is not { Succeeded: true }) missing.Add("Ninja");
            items.Add(Failed("CMake · Ninja", $"실행할 수 없음: {string.Join(", ", missing)}",
                "ZMK 공식 Native Setup의 Windows 의존성 설치 단계를 따르세요. CMake는 최신 3.x를 권장합니다."));
        }
        else
        {
            var cmakeVersion = FirstUsefulLine(cmakeResult.CombinedOutput, "CMake");
            var ninjaVersion = FirstUsefulLine(ninjaResult.CombinedOutput, "Ninja");
            items.Add(Passed("CMake · Ninja", $"{cmakeVersion} · Ninja {ninjaVersion}"));
        }

        var zephyrRoot = Path.Combine(westRoot, "zephyr");
        if (!Directory.Exists(zephyrRoot))
        {
            items.Add(Failed("Zephyr SDK", "west 워크스페이스에 zephyr 소스가 없습니다.",
                "ZMK 루트에서 west update를 실행한 뒤 SDK를 설치하세요."));
        }
        else
        {
            var sdkResult = await TryProbeAsync(selectedPython,
                new[] { "-m", "west", "sdk", "list" }, zephyrRoot, cancellationToken);
            if (sdkResult is { Succeeded: true } && HasInstalledArmToolchain(sdkResult.CombinedOutput))
            {
                items.Add(Passed("Zephyr SDK", DescribeInstalledSdk(sdkResult.CombinedOutput)));
            }
            else if (sdkResult is { Succeeded: true })
            {
                items.Add(Failed("Zephyr SDK", "설치된 SDK에서 arm-zephyr-eabi 툴체인을 찾지 못했습니다.",
                    "zephyr 폴더에서 west sdk install --toolchains arm-zephyr-eabi를 실행하세요."));
            }
            else
            {
                var verificationScript = Path.Combine(zephyrRoot, "cmake", "verify-toolchain.cmake");
                var verifyResult = cmakeResult is { Succeeded: true } && File.Exists(verificationScript)
                    ? await TryProbeAsync("cmake", new[] { "-P", verificationScript }, zephyrRoot, cancellationToken)
                    : null;
                if (verifyResult is { Succeeded: true })
                {
                    items.Add(new EnvironmentCheckItem("Zephyr SDK", EnvironmentCheckStatus.Warning,
                        "CMake 툴체인 확인은 통과했지만 이 Zephyr 버전은 west sdk list를 지원하지 않습니다.",
                        "첫 실제 빌드가 최종 확인입니다."));
                }
                else
                {
                    items.Add(Failed("Zephyr SDK", CompactError(sdkResult, "등록된 Zephyr SDK를 찾지 못했습니다."),
                        "zephyr 폴더에서 west sdk install --toolchains arm-zephyr-eabi를 실행하세요."));
                }
            }
        }

        return new BuildEnvironmentReport(items, selectedPython, westRoot);
    }

    private async Task<CommandProbeResult?> TryProbeAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_probeTimeout);
        try
        {
            return await _probe.RunAsync(fileName, arguments, workingDirectory, timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new CommandProbeResult(-1, string.Empty, "응답 시간 초과");
        }
        catch (Exception exception) when (exception is Win32Exception or FileNotFoundException or InvalidOperationException)
        {
            return new CommandProbeResult(-1, string.Empty, exception.Message);
        }
    }

    private static EnvironmentCheckItem Passed(string name, string detail) =>
        new(name, EnvironmentCheckStatus.Passed, detail);

    private static EnvironmentCheckItem Failed(string name, string detail, string guidance) =>
        new(name, EnvironmentCheckStatus.Failed, detail, guidance);

    private static void AddSkipped(ICollection<EnvironmentCheckItem> items, IEnumerable<string> names, string detail)
    {
        foreach (var name in names)
            items.Add(new EnvironmentCheckItem(name, EnvironmentCheckStatus.Skipped, detail));
    }

    private static bool IsAscii(string path) => path.All(character => character <= 127);

    private static string FirstUsefulLine(string text, string fallback) =>
        text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.Length > 0) ?? fallback;

    private static string? LastExistingDirectoryLine(string text) =>
        text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .LastOrDefault(Directory.Exists);

    private static string CompactError(CommandProbeResult? result, string fallback)
    {
        if (result is null || string.IsNullOrWhiteSpace(result.CombinedOutput)) return fallback;
        var line = FirstUsefulLine(result.CombinedOutput, fallback);
        return line.Length <= 180 ? line : line[..177] + "…";
    }

    private static bool HasInstalledArmToolchain(string output)
    {
        var inInstalledToolchains = false;
        foreach (var rawLine in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (line.Contains("installed", StringComparison.OrdinalIgnoreCase) &&
                line.Contains("toolchain", StringComparison.OrdinalIgnoreCase))
            {
                inInstalledToolchains = true;
                continue;
            }
            if (inInstalledToolchains && line.Contains("available", StringComparison.OrdinalIgnoreCase) &&
                line.Contains("toolchain", StringComparison.OrdinalIgnoreCase))
            {
                inInstalledToolchains = false;
                continue;
            }
            if (inInstalledToolchains && line.TrimStart('-', ' ').StartsWith("arm-zephyr-eabi", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static string DescribeInstalledSdk(string output)
    {
        var version = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.EndsWith(':') && char.IsDigit(line[0]))?.TrimEnd(':');
        var pathLine = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.StartsWith("path:", StringComparison.OrdinalIgnoreCase));
        var suffix = pathLine is null ? string.Empty : $" · {pathLine[5..].Trim()}";
        return $"SDK {version ?? "감지됨"} · arm-zephyr-eabi{suffix}";
    }
}
