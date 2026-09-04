namespace ModuKeymapStudio.Core.Keycodes;

public static class ZmkBehaviorCatalog
{
    public const string HeldBootloaderBinding = "&mks_boot_hold 0 0";
    public const string HeldSystemResetBinding = "&mks_reset_hold 0 0";
    public const string BootloaderScopeNote = "누른 쪽 하프만 부트로더로 진입합니다.";
    public const string ResetSourceCaution = "누른 쪽 하프만 다시 시작합니다.";

    public static ZmkBehaviorOption Bootloader { get; } = new(
        "시스템·전원",
        "부트로더 진입",
        "&bootloader",
        BootloaderScopeNote,
        "bootloader boot mode retention dfu 부트로더 진입 재시작");

    public static ZmkBehaviorOption HeldBootloader { get; } = new(
        "시스템·전원",
        "500ms 길게 눌러 부트로더 진입",
        HeldBootloaderBinding,
        $"짧게 누르면 아무 동작도 하지 않습니다. {BootloaderScopeNote}",
        "hold held 500ms long press bootloader boot mode retention dfu 길게 홀드 부트로더");

    public static ZmkBehaviorOption HeldSystemReset { get; } = new(
        "시스템·전원",
        "500ms 길게 눌러 시스템 재시작",
        HeldSystemResetBinding,
        $"짧게 누르면 아무 동작도 하지 않습니다. {ResetSourceCaution}",
        "hold held 500ms long press sys reset restart 길게 홀드 시스템 재시작 리셋");

    public static IReadOnlyList<ZmkBehaviorOption> All { get; } = [Bootloader, HeldBootloader, HeldSystemReset];
}
