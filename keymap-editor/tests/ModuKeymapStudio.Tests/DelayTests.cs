using ModuKeymapStudio.Core.Editing;
using ModuKeymapStudio.Core.IO;
using ModuKeymapStudio.Core.Keycodes;
using ModuKeymapStudio.Core.Models;
using ModuKeymapStudio.Core.Parsing;

static class DelayTests
{
    internal static KeymapDocument Original() => KeymapParser.Parse(File.ReadAllText(
        RepositoryLocator.FindKeymap(AppContext.BaseDirectory, Environment.CurrentDirectory)!));

    public static Task RoundTrip()
    {
        var original = Original();
        Check(KeymapEditor.DelayOptions.SequenceEqual(Enumerable.Range(1, 20).Select(i => i * 50)), "20개 시간 옵션");
        foreach (var ms in KeymapEditor.DelayOptions)
        {
            var doc = KeymapEditor.SetDelayedBinding(original, 0, 0, "&kp A", ms);
            Setting(doc, 0, 0, "&kp A", ms);
            Check(doc.Source.Contains("flavor = \"tap-preferred\";"), "시간 이전 인터럽트 실행 방지");
            Check(doc.Source.Contains("bindings = <&kp>, <&none>;"), "짧게 누르면 무시");
            var again = KeymapEditor.SetDelayedBinding(doc, 0, 0, doc.Layers[0].Bindings[0].Raw, ms);
            Check(again.Source == doc.Source, "동일 설정은 원문 그대로");
        }
        foreach (var raw in ZmkKeycodeCatalog.All.Select(item => item.Binding).Concat(new[]
        {
            "&bootloader", "&sys_reset", "&none", "&mo 1", "&to 1", "&tog 1", "&sl 1",
            "&lt 1 SPACE", "&mt LCTRL ESC", "&mkp LCLK", "&bt BT_CLR", "&bt BT_SEL 2",
            "&bt BT_NXT", "&bt BT_CLR_ALL", "&rgb_ug RGB_TOG", "&custom", "&custom ARG",
            "&custom ARG SECOND", "&kp LC(LS(A))", "&kp (A + 0)"
        }))
        {
            var doc = KeymapEditor.SetDelayedBinding(original, 0, 0, raw, 500);
            Setting(KeymapParser.Parse(doc.Source), 0, 0, raw, 500);
            var off = KeymapEditor.SetDelayedBinding(doc, 0, 0, doc.Layers[0].Bindings[0].Raw, null);
            Check(off.Layers[0].Bindings[0].Raw == raw, "해제 시 원래 바인딩: " + raw);
        }
        foreach (var ms in new[] { -50, 0, 25, 51, 1001, 1050 })
            Expect<ArgumentOutOfRangeException>(() => KeymapEditor.SetDelayedBinding(original, 0, 0, "&kp A", ms));
        foreach (var raw in new[] { "&kp A &kp B", "&kp A;", "&kp LC(A", "&kp A)", "&trans" })
            Expect<ArgumentException>(() => KeymapEditor.SetDelayedBinding(original, 0, 0, raw, 500));
        var bt = KeymapEditor.SetDelayedBinding(original, 0, 0, "&bt BT_CLR", 500);
        Check(bt.Source.Contains("<&macro_press &bt BT_CLR>, <&macro_pause_for_release>, <&macro_release &bt BT_CLR>"),
            "BT_CLR 매크로 확장과 press/release 보존");
        Check(bt.Layers[0].Bindings[0].Raw.EndsWith(" 0 0"), "BT_CLR를 hold-tap 인수에 직접 전달하면 안 됨");
        return Task.CompletedTask;
    }

    public static Task Editing()
    {
        var original = Original();
        var doc = KeymapEditor.SetDelayedBinding(original, 0, 0, "&kp A", 500);
        var shared = KeymapEditor.SetDelayedBinding(doc, 0, 1, "&kp A", 500);
        Check(shared.Layers[0].Bindings[0].Raw == shared.Layers[0].Bindings[1].Raw, "동일 정의 재사용");
        var changed = KeymapEditor.SetDelayedBinding(shared, 0, 0, shared.Layers[0].Bindings[0].Raw, 1000);
        Setting(changed, 0, 0, "&kp A", 1000);
        Setting(changed, 0, 1, "&kp A", 500);
        var history = new DocumentHistory(shared.Source);
        history.Push(changed.Source);
        Check(history.Undo() == shared.Source && history.Redo() == changed.Source, "지연 정의와 키의 단일 undo/redo");
        var copied = KeymapEditor.MoveBinding(changed, 0, 0, 2, KeyMoveOperation.OverwriteCopy);
        Setting(copied, 0, 2, "&kp A", 1000);
        var legacy = KeymapEditor.SetSafetyHoldBinding(original, 0, 0, SafetyHoldAction.Bootloader);
        legacy = KeymapEditor.SetSafetyHoldBinding(legacy, 0, 1, SafetyHoldAction.Bootloader);
        Setting(legacy, 0, 0, "&bootloader", 500);
        var updated = KeymapEditor.SetDelayedBinding(legacy, 0, 0, legacy.Layers[0].Bindings[0].Raw, 750);
        Setting(updated, 0, 0, "&bootloader", 750);
        Setting(updated, 0, 1, "&bootloader", 500);
        var external = KeymapParser.Parse(legacy.Source.Replace("tapping-term-ms = <500>", "tapping-term-ms = <650>"));
        Setting(external, 0, 0, "&bootloader", 650);
        foreach (var nl in new[] { "\n", "\r\n" })
        {
            var source = original.Source.Replace("\r\n", "\n").Replace("\n", nl);
            var parsed = KeymapParser.Parse(source);
            var applied = KeymapEditor.SetDelayedBinding(parsed, 0, 0, "&kp A", 50);
            Check(applied.NewLine == nl && (nl != "\r\n" || !applied.Source.Replace("\r\n", "").Contains('\n')), "줄바꿈 보존");
            for (var l = 0; l < parsed.Layers.Count; l++)
            for (var k = 0; k < parsed.Layers[l].Bindings.Count; k++)
                if (l != 0 || k != 0) Check(parsed.Layers[l].Bindings[k].Raw == applied.Layers[l].Bindings[k].Raw, "다른 키 보존");
            Check(applied.Source.StartsWith(source[..source.IndexOf("/ {")]), "헤더·includes 보존");
        }
        var minimal = KeymapParser.Parse("/ { keymap { compatible = \"zmk,keymap\"; default_layer { bindings = <&kp A>; }; }; };");
        Setting(KeymapEditor.SetDelayedBinding(minimal, 0, 0, "&kp B", 100), 0, 0, "&kp B", 100);
        var oneLine = KeymapParser.Parse("/ { behaviors { }; keymap { default_layer { bindings = <&kp A>; }; }; };");
        Setting(KeymapEditor.SetDelayedBinding(oneLine, 0, 0, "&kp B", 100), 0, 0, "&kp B", 100);
        return Task.CompletedTask;
    }

    public static Task LayerReferences()
    {
        var doc = KeymapEditor.AddLayer(Original(), "delay_spare", "Spare", false, 0);
        var deletedIndex = doc.Layers.Count - 1;
        doc = KeymapEditor.AddLayer(doc, "delay_target", "Target", false, 0);
        var target = doc.Layers.Count - 1;
        doc = KeymapEditor.SetDelayedBinding(doc, 0, 0, $"&mo {target}", 250);
        doc = KeymapEditor.SetDelayedBinding(doc, 0, 1, $"&lt {target} SPACE", 550);
        Expect<LayerDeletionException>(() => KeymapEditor.DeleteLayer(doc, target));
        var renumbered = KeymapEditor.DeleteLayer(doc, deletedIndex);
        Setting(renumbered, 0, 0, $"&mo {target - 1}", 250);
        Setting(renumbered, 0, 1, $"&lt {target - 1} SPACE", 550);
        var symbolic = KeymapEditor.SetDelayedBinding(doc, 0, 2, "&mo TARGET_LAYER", 500);
        Expect<LayerDeletionException>(() => KeymapEditor.DeleteLayer(symbolic, deletedIndex));
        return Task.CompletedTask;
    }

    internal static KeymapDocument CreateFixture()
    {
        var doc = Original();
        var actions = new[] { "&kp A", "&kp LCTRL", "&bt BT_CLR", "&bt BT_SEL 2", "&mo 1", "&lt 1 SPACE",
            "&mkp LCLK", "&kp C_VOL_UP", "&bootloader", "&sys_reset", "&kp LC(LS(A))", "&none", "&mt LCTRL ESC" };
        for (var i = 0; i < actions.Length; i++)
            doc = KeymapEditor.SetDelayedBinding(doc, 0, i, actions[i], i == 1 ? 1000 : (i + 1) * 50);
        return doc;
    }

    internal static void Setting(KeymapDocument doc, int layer, int key, string raw, int ms)
    {
        var actual = KeymapEditor.GetDelayedBinding(doc, doc.Layers[layer].Bindings[key].Raw);
        Check(actual == new DelayedBinding(raw, ms), $"지연 round-trip: {raw}, {ms}ms; 실제: {actual}");
    }
    internal static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    private static void Expect<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException(typeof(T).Name + " 예외 누락");
    }
}
