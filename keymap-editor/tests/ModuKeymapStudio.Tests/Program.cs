using System.Diagnostics;
using System.Text;
using ModuKeymapStudio.Core.Build;
using ModuKeymapStudio.Core.Editing;
using ModuKeymapStudio.Core.IO;
using ModuKeymapStudio.Core.Keycodes;
using ModuKeymapStudio.Core.Models;
using ModuKeymapStudio.Core.Parsing;

var tests = new (string Name, Func<Task> Run)[]
{
    ("실제 키맵: 2개 레이어와 레이어당 67개 바인딩", RealKeymapShape),
    ("실제 스위치가 없는 예약 슬롯 6개", NonexistentHardwareSlots),
    ("무수정 저장은 바이트 단위 동일", UnchangedRoundTrip),
    ("UTF-8 BOM은 수정 저장 후에도 보존", Utf8BomPreservation),
    ("LF/CRLF, 주석, 공백 보존", NewLineAndCommentPreservation),
    ("단일 키 수정은 해당 바인딩만 변경", SingleBindingPatch),
    ("투명 레이어 추가와 현재 레이어 복제", AddAndCloneLayers),
    ("레이어 이름 검증", LayerNameValidation),
    ("기본/참조/심볼 레이어 삭제 차단", DeleteBlockers),
    ("레이어 삭제 후 상위 숫자 참조 보정", DeleteRenumbering),
    ("실행 취소와 다시 실행", UndoRedo),
    ("공식 ZMK 368개 키코드와 영문명·기호·별칭 검색", ZmkKeycodeCatalogCoverage),
    ("빌드 프로세스 성공/실패/취소", BuildProcessScenarios),
    ("빌드 환경 사전 점검 통과/실패", BuildEnvironmentPreflight)
};

var failures = new List<string>();
foreach (var test in tests)
{
    try
    {
        await test.Run();
        Console.WriteLine($"PASS  {test.Name}");
    }
    catch (Exception exception)
    {
        failures.Add(test.Name);
        Console.WriteLine($"FAIL  {test.Name}");
        Console.WriteLine("      " + exception.Message);
    }
}

Console.WriteLine();
Console.WriteLine($"{tests.Length - failures.Count}/{tests.Length} tests passed");
return failures.Count == 0 ? 0 : 1;

static Task RealKeymapShape()
{
    var document = LoadRealDocument();
    Equal(2, document.Layers.Count, "레이어 수");
    True(document.Layers.All(layer => layer.Bindings.Count == KeymapDocument.ModuBindingCount), "모든 레이어가 67개 바인딩이어야 합니다.");
    Equal("default_layer", document.Layers[0].NodeName, "기본 레이어 이름");
    Equal("lower_layer", document.Layers[1].NodeName, "lower 레이어 이름");
    return Task.CompletedTask;
}

static Task NonexistentHardwareSlots()
{
    var document = LoadRealDocument();
    Equal(61, KeymapDocument.ModuEditableKeyCount, "편집 가능한 실제 키 수");
    True(KeymapDocument.NonexistentBindingIndexes.SequenceEqual(new[] { 51, 52, 53, 54, 55, 56 }), "예약 슬롯 위치");
    True(KeymapDocument.NonexistentBindingIndexes.All(index => document.Layers[0].Bindings[index].Raw == "&none"), "기본 레이어 예약 슬롯은 &none이어야 합니다.");
    True(KeymapDocument.NonexistentBindingIndexes.All(index => !KeymapDocument.IsEditableKeyIndex(index)), "예약 슬롯이 편집 가능으로 노출됩니다.");
    True(KeymapDocument.IsEditableKeyIndex(50) && KeymapDocument.IsEditableKeyIndex(57), "예약 슬롯 이외 키가 숨겨졌습니다.");
    return Task.CompletedTask;
}

static Task UnchangedRoundTrip()
{
    var realPath = FindRealKeymap();
    var original = File.ReadAllBytes(realPath);
    var temp = Path.Combine(Path.GetTempPath(), "ModuKeymapStudioTests", Guid.NewGuid() + ".keymap");
    Directory.CreateDirectory(Path.GetDirectoryName(temp)!);
    try
    {
        var file = KeymapFile.Load(realPath);
        file.Save(file.Document.Source, temp);
        True(original.SequenceEqual(File.ReadAllBytes(temp)), "무수정 저장 결과가 원본 바이트와 다릅니다.");
    }
    finally { if (File.Exists(temp)) File.Delete(temp); }
    return Task.CompletedTask;
}

static Task NewLineAndCommentPreservation()
{
    var lf = File.ReadAllText(FindRealKeymap()).Replace("\r\n", "\n", StringComparison.Ordinal);
    var decorated = lf.Replace("        lower_layer {", "        // keep-this-comment\n        lower_layer {", StringComparison.Ordinal);
    var parsedLf = KeymapParser.Parse(decorated);
    Equal(decorated, parsedLf.Source, "LF round-trip");
    var crlf = decorated.Replace("\n", "\r\n", StringComparison.Ordinal);
    var parsedCrlf = KeymapParser.Parse(crlf);
    Equal("\r\n", parsedCrlf.NewLine, "CRLF 감지");
    Equal(crlf, parsedCrlf.Source, "CRLF round-trip");
    return Task.CompletedTask;
}

static Task Utf8BomPreservation()
{
    var directory = Path.Combine(Path.GetTempPath(), "ModuKeymapStudioTests", Guid.NewGuid().ToString());
    var sourcePath = Path.Combine(directory, "bom.keymap");
    var savedPath = Path.Combine(directory, "saved.keymap");
    Directory.CreateDirectory(directory);
    try
    {
        var source = File.ReadAllText(FindRealKeymap());
        var preamble = Encoding.UTF8.GetPreamble();
        var content = Encoding.UTF8.GetBytes(source);
        File.WriteAllBytes(sourcePath, preamble.Concat(content).ToArray());
        var file = KeymapFile.Load(sourcePath);
        var edited = KeymapEditor.ReplaceBinding(file.Document, 0, 0, "&kp F24");
        file.Save(edited.Source, savedPath);
        True(File.ReadAllBytes(savedPath).AsSpan().StartsWith(preamble), "수정 저장에서 UTF-8 BOM이 사라졌습니다.");
    }
    finally
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }
    return Task.CompletedTask;
}

static Task SingleBindingPatch()
{
    var document = LoadRealDocument();
    var target = document.Layers[0].Bindings[0];
    var expected = document.Source[..target.Start] + "&kp F24" + document.Source[target.End..];
    var updated = KeymapEditor.ReplaceBinding(document, 0, 0, "&kp F24");
    Equal(expected, updated.Source, "단일 패치 결과");
    Equal("&kp F24", updated.Layers[0].Bindings[0].Raw, "수정 바인딩");
    return Task.CompletedTask;
}

static Task AddAndCloneLayers()
{
    var document = LoadRealDocument();
    var transparent = KeymapEditor.AddLayer(document, "nav_layer", "탐색", false, 0);
    Equal(3, transparent.Layers.Count, "추가 후 레이어 수");
    Equal("탐색", transparent.Layers[2].DisplayName, "display-name");
    True(transparent.Layers[2].Bindings.All(binding => binding.Raw == "&trans"), "투명 레이어 내용");

    var clone = KeymapEditor.AddLayer(transparent, "copy_layer", "복제", true, 0);
    True(clone.Layers[3].Bindings.Select(binding => binding.Raw).SequenceEqual(clone.Layers[0].Bindings.Select(binding => binding.Raw)), "복제 내용");
    return Task.CompletedTask;
}

static Task LayerNameValidation()
{
    var document = LoadRealDocument();
    Throws<ArgumentException>(() => KeymapEditor.AddLayer(document, "2bad", "Bad", false, 0));
    Throws<ArgumentException>(() => KeymapEditor.AddLayer(document, "default_layer", "Duplicate", false, 0));
    True(KeymapEditor.IsValidNodeName("nav-layer_2"), "유효 이름 거부");
    return Task.CompletedTask;
}

static Task DeleteBlockers()
{
    var document = KeymapEditor.AddLayer(LoadRealDocument(), "third_layer", "Third", false, 0);
    Throws<LayerDeletionException>(() => KeymapEditor.DeleteLayer(document, 0));

    var referenced = KeymapEditor.ReplaceBinding(document, 0, 0, "&mo 2");
    var referencedError = Throws<LayerDeletionException>(() => KeymapEditor.DeleteLayer(referenced, 2));
    True(referencedError.References.Any(reference => reference.SourceLayerIndex == 0 && reference.KeyIndex == 0), "참조 위치 누락");

    var symbolic = KeymapEditor.ReplaceBinding(document, 0, 0, "&mo NAV_LAYER");
    Throws<LayerDeletionException>(() => KeymapEditor.DeleteLayer(symbolic, 1));
    return Task.CompletedTask;
}

static Task DeleteRenumbering()
{
    var document = LoadRealDocument();
    document = KeymapEditor.AddLayer(document, "third_layer", "Third", false, 0);
    document = KeymapEditor.AddLayer(document, "fourth_layer", "Fourth", false, 0);
    document = KeymapEditor.ReplaceBinding(document, 0, 0, "&mo 3");
    var deleted = KeymapEditor.DeleteLayer(document, 2);
    Equal(3, deleted.Layers.Count, "삭제 후 레이어 수");
    Equal("&mo 2", deleted.Layers[0].Bindings[0].Raw, "상위 참조 보정");
    Equal("fourth_layer", deleted.Layers[2].NodeName, "정의 순서");
    return Task.CompletedTask;
}

static Task UndoRedo()
{
    var source = LoadRealDocument().Source;
    var changed = KeymapEditor.ReplaceBinding(KeymapParser.Parse(source), 0, 0, "&kp F24").Source;
    var history = new DocumentHistory(source);
    True(history.Push(changed), "변경 push");
    Equal(source, history.Undo(), "undo");
    Equal(changed, history.Redo(), "redo");
    return Task.CompletedTask;
}

static Task ZmkKeycodeCatalogCoverage()
{
    Equal(368, ZmkKeycodeCatalog.All.Count, "ZMK 공식 키코드 표 항목 수");
    Equal(368, ZmkKeycodeCatalog.All.Select(item => item.Binding).Distinct(StringComparer.OrdinalIgnoreCase).Count(), "중복 바인딩");
    True(ZmkKeycodeCatalog.Categories.All(category => ZmkKeycodeCatalog.All.Any(item => item.Category == category)), "비어 있는 키 분류");

    var slash = ZmkKeycodeCatalog.All.Single(item => item.Code == "FSLH");
    True(slash.Matches("forward slash"), "슬래시 영문명 검색");
    True(slash.Matches("/"), "슬래시 기호 검색");
    True(slash.Matches("SLASH"), "슬래시 장문 별칭 검색");
    True(slash.Matches("question mark"), "같은 물리 키의 Shift 문자 영문명 검색");
    True(slash.KeycapLabel.Contains("/ ?", StringComparison.Ordinal), "슬래시 키캡 실제 문자 표기");
    Equal("/", slash.BaseCharacter, "슬래시 기본 문자");
    Equal("?", slash.ShiftCharacter, "슬래시 Shift 문자");
    True(!slash.PickerLabel.Contains("슬래시", StringComparison.Ordinal), "선택 목록 주 정보에 한글 이름 노출");
    True(slash.PickerDetail.Contains("설명: 슬래시", StringComparison.Ordinal), "선택 목록 설명의 한글 이름 누락");
    True(string.IsNullOrEmpty(ZmkKeycodeCatalog.All.Single(item => item.Code == "A").PickerDetail), "문자키의 불필요한 설명");
    var escape = ZmkKeycodeCatalog.All.Single(item => item.Code == "ESC");
    Equal<string?>(null, escape.BaseCharacter, "문자 없는 키의 기본 문자");
    Equal(2, ZmkKeycodeCatalog.All.Count(item => item.Matches("/")), "슬래시 기호 검색 잡음");
    Equal(2, ZmkKeycodeCatalog.All.Count(item => item.Matches("?")), "물음표 기호 검색 잡음");

    foreach (var code in new[] { "F24", "INT9", "LANG9", "KP_EQUAL_AS400", "K_COPY", "C_VOL_UP", "C_AC_NEW", "C_KBIA_ACCEPT", "C_POWER" })
        True(ZmkKeycodeCatalog.All.Any(item => item.Code == code), $"공식 키코드 누락: {code}");
    return Task.CompletedTask;
}

static async Task BuildProcessScenarios()
{
    var runner = new BuildProcessRunner();
    var output = new List<BuildOutput>();
    var success = await runner.RunAsync(Cmd("echo modu_left-ok"), item => output.Add(item), CancellationToken.None);
    True(success.Succeeded, "성공 프로세스");
    True(output.Any(item => item.Text.Contains("modu_left-ok", StringComparison.Ordinal)), "성공 로그");

    var failure = await runner.RunAsync(Cmd("echo failed 1>&2 & exit /b 7"), _ => { }, CancellationToken.None);
    Equal(7, failure.ExitCode, "실패 종료 코드");

    using var cancellation = new CancellationTokenSource(200);
    var cancelled = await runner.RunAsync(Cmd("ping 127.0.0.1 -n 8 > nul"), _ => { }, cancellation.Token);
    True(cancelled.WasCancelled, "취소 상태");
}

static async Task BuildEnvironmentPreflight()
{
    var root = Path.Combine(Path.GetTempPath(), "ModuKeymapStudioTests", Guid.NewGuid().ToString("N"));
    var zmkRoot = Path.Combine(root, "zmk");
    var zmkApp = Path.Combine(zmkRoot, "app");
    var zephyr = Path.Combine(zmkRoot, "zephyr");
    var python = Path.Combine(zmkRoot, ".venv", "Scripts", "python.exe");
    try
    {
        Directory.CreateDirectory(Path.Combine(root, "modu-module"));
        Directory.CreateDirectory(Path.Combine(root, "zmk-pmw3610-driver"));
        Directory.CreateDirectory(Path.Combine(root, "tools", "uf2"));
        Directory.CreateDirectory(Path.Combine(zmkRoot, ".west"));
        Directory.CreateDirectory(zmkApp);
        Directory.CreateDirectory(Path.Combine(zephyr, "cmake"));
        Directory.CreateDirectory(Path.GetDirectoryName(python)!);
        File.WriteAllText(Path.Combine(root, "build.ps1"), string.Empty);
        File.WriteAllText(Path.Combine(root, "tools", "uf2", "uf2conv.py"), string.Empty);
        File.WriteAllText(Path.Combine(zmkRoot, ".west", "config"), "[manifest]");
        File.WriteAllText(Path.Combine(zmkApp, "CMakeLists.txt"), "find_package(Zephyr)");
        File.WriteAllText(Path.Combine(zephyr, "cmake", "verify-toolchain.cmake"), string.Empty);
        File.WriteAllText(python, string.Empty);

        var passingProbe = new FakeCommandProbe((fileName, arguments, _) =>
        {
            var command = string.Join(" ", arguments);
            if (command == "--version" && fileName.Equals("cmake", StringComparison.OrdinalIgnoreCase))
                return Success("cmake version 3.30.5");
            if (command == "--version" && fileName.Equals("ninja", StringComparison.OrdinalIgnoreCase))
                return Success("1.12.1");
            if (command == "--version") return Success("Python 3.12.7");
            if (command == "-m west --version") return Success("West version: v1.4.0");
            if (command == "-m west topdir") return Success(zmkRoot);
            if (command == "-m west build -h") return Success("usage: west build");
            if (command == "-m west sdk list")
                return Success($"0.16.8:{Environment.NewLine} path: {Path.Combine(root, "zephyr-sdk-0.16.8")}{Environment.NewLine} gnu-installed-toolchains:{Environment.NewLine} - arm-zephyr-eabi");
            return new CommandProbeResult(1, string.Empty, "unexpected command");
        });

        var passing = await new BuildEnvironmentChecker(passingProbe).CheckAsync(root, zmkApp);
        True(passing.CanBuild, "준비된 가짜 환경이 빌드 가능으로 판정되지 않았습니다.");
        True(passing.Items.Any(item => item.Name == "Zephyr SDK" && item.Status == EnvironmentCheckStatus.Passed),
            "ARM Zephyr SDK 통과 항목");
        Equal(python, passing.PythonPath, "build.ps1과 같은 가상 환경 Python 선택");

        var missingWestProbe = new FakeCommandProbe((fileName, arguments, _) =>
        {
            var command = string.Join(" ", arguments);
            if (command == "--version") return Success("Python 3.12.7");
            return new CommandProbeResult(1, string.Empty, "No module named west");
        });
        var missingWest = await new BuildEnvironmentChecker(missingWestProbe).CheckAsync(root, zmkApp);
        True(!missingWest.CanBuild, "west가 없는데 빌드 가능으로 판정되었습니다.");
        var westFailure = missingWest.Items.Single(item => item.Name == "west");
        Equal(EnvironmentCheckStatus.Failed, westFailure.Status, "west 실패 상태");
        True(!string.IsNullOrWhiteSpace(westFailure.Guidance), "west 설치 안내 누락");
    }
    finally
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }
}

static CommandProbeResult Success(string output) => new(0, output, string.Empty);

static ProcessStartInfo Cmd(string command)
{
    var info = new ProcessStartInfo("cmd.exe");
    info.ArgumentList.Add("/d");
    info.ArgumentList.Add("/c");
    info.ArgumentList.Add(command);
    return info;
}

static KeymapDocument LoadRealDocument() => KeymapParser.Parse(File.ReadAllText(FindRealKeymap()));

static string FindRealKeymap() => RepositoryLocator.FindKeymap(AppContext.BaseDirectory, Environment.CurrentDirectory)
    ?? throw new InvalidOperationException("실제 modu.keymap을 찾지 못했습니다.");

static void True(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static void Equal<T>(T expected, T actual, string message)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{message}: expected={expected}, actual={actual}");
}

static TException Throws<TException>(Action action) where TException : Exception
{
    try { action(); }
    catch (TException exception) { return exception; }
    throw new InvalidOperationException($"{typeof(TException).Name} 예외가 발생하지 않았습니다.");
}

sealed class FakeCommandProbe(
    Func<string, IReadOnlyList<string>, string, CommandProbeResult> handler) : ICommandProbe
{
    public Task<CommandProbeResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        CancellationToken cancellationToken) =>
        Task.FromResult(handler(fileName, arguments, workingDirectory));
}
