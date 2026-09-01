namespace ModuKeymapStudio.Core.Keycodes;

/// <summary>
/// The selectable key-press usages documented by ZMK's official "List of Keycodes".
/// One option is emitted for every table row. Alternate names remain searchable aliases.
/// </summary>
public static class ZmkKeycodeCatalog
{
    public static IReadOnlyList<string> Categories { get; } =
    [
        "문자", "숫자·기호", "제어·탐색", "기능키", "수정키", "국제·언어",
        "넘패드", "편집", "미디어", "앱·브라우저", "입력 보조", "시스템·전원"
    ];

    public static IReadOnlyList<ZmkKeycodeOption> All { get; } = Create();

    private static IReadOnlyList<ZmkKeycodeOption> Create()
    {
        var items = new List<ZmkKeycodeOption>();

        void Add(string category, string code, string label, string englishName, string symbols = "", params string[] aliases) =>
            items.Add(new ZmkKeycodeOption(category, code, label, englishName, symbols,
                new[] { code }.Concat(aliases).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()));

        foreach (var letter in "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
            Add("문자", letter.ToString(), letter.ToString(), $"Letter {letter} / {char.ToLowerInvariant(letter)} and {letter}");

        Add("숫자·기호", "N1", "숫자 1", "Number 1 and Exclamation Mark", "1 !", "NUMBER_1");
        Add("숫자·기호", "N2", "숫자 2", "Number 2 and At Sign", "2 @", "NUMBER_2");
        Add("숫자·기호", "N3", "숫자 3", "Number 3 and Hash / Pound", "3 #", "NUMBER_3");
        Add("숫자·기호", "N4", "숫자 4", "Number 4 and Dollar", "4 $", "NUMBER_4");
        Add("숫자·기호", "N5", "숫자 5", "Number 5 and Percent", "5 %", "NUMBER_5");
        Add("숫자·기호", "N6", "숫자 6", "Number 6 and Caret", "6 ^", "NUMBER_6");
        Add("숫자·기호", "N7", "숫자 7", "Number 7 and Ampersand", "7 &", "NUMBER_7");
        Add("숫자·기호", "N8", "숫자 8", "Number 8 and Asterisk / Star", "8 *", "NUMBER_8");
        Add("숫자·기호", "N9", "숫자 9", "Number 9 and Left Parenthesis", "9 (", "NUMBER_9");
        Add("숫자·기호", "N0", "숫자 0", "Number 0 and Right Parenthesis", "0 )", "NUMBER_0");
        Add("숫자·기호", "EXCL", "느낌표", "Exclamation Mark", "!", "EXCLAMATION");
        Add("숫자·기호", "AT", "골뱅이", "At Sign", "@", "AT_SIGN");
        Add("숫자·기호", "HASH", "해시 / 우물정", "Hash / Pound", "#", "POUND");
        Add("숫자·기호", "DLLR", "달러", "Dollar Sign", "$", "DOLLAR");
        Add("숫자·기호", "PRCNT", "퍼센트", "Percent Sign", "%", "PERCENT");
        Add("숫자·기호", "CARET", "캐럿", "Caret", "^");
        Add("숫자·기호", "AMPS", "앰퍼샌드", "Ampersand", "&", "AMPERSAND");
        Add("숫자·기호", "ASTRK", "별표", "Asterisk / Star", "*", "ASTERISK", "STAR");
        Add("숫자·기호", "LPAR", "왼쪽 소괄호", "Left Parenthesis", "(", "LEFT_PARENTHESIS");
        Add("숫자·기호", "RPAR", "오른쪽 소괄호", "Right Parenthesis", ")", "RIGHT_PARENTHESIS");
        Add("숫자·기호", "EQUAL", "등호", "Equal and Plus", "= +");
        Add("숫자·기호", "PLUS", "더하기", "Plus Sign", "+");
        Add("숫자·기호", "MINUS", "빼기 / 하이픈", "Minus and Underscore", "- _", "HYPHEN");
        Add("숫자·기호", "UNDER", "밑줄", "Underscore", "_", "UNDERSCORE");
        Add("숫자·기호", "FSLH", "슬래시", "Forward Slash and Question Mark", "/ ?", "SLASH", "FORWARD_SLASH");
        Add("숫자·기호", "QMARK", "물음표", "Question Mark", "?", "QUESTION");
        Add("숫자·기호", "BSLH", "역슬래시", "Backslash and Pipe", "\\ |", "BACKSLASH");
        Add("숫자·기호", "PIPE", "파이프", "Pipe / Vertical Bar", "|");
        Add("숫자·기호", "NUBS", "비미국 역슬래시", "Non-US Backslash and Pipe", "\\ |", "NON_US_BACKSLASH", "NON_US_BSLH");
        Add("숫자·기호", "PIPE2", "비미국 파이프", "Pipe using Shift and Non-US Backslash", "|");
        Add("숫자·기호", "SEMI", "세미콜론", "Semicolon and Colon", "; :", "SEMICOLON");
        Add("숫자·기호", "COLON", "콜론", "Colon", ":");
        Add("숫자·기호", "SQT", "작은따옴표", "Apostrophe / Single Quote and Double Quote", "' \"", "SINGLE_QUOTE", "APOSTROPHE", "APOS");
        Add("숫자·기호", "DQT", "큰따옴표", "Double Quote", "\"", "DOUBLE_QUOTES");
        Add("숫자·기호", "COMMA", "쉼표", "Comma and Less Than", ", <");
        Add("숫자·기호", "LT", "작다", "Less Than", "<", "LESS_THAN");
        Add("숫자·기호", "DOT", "마침표", "Period / Dot and Greater Than", ". >", "PERIOD");
        Add("숫자·기호", "GT", "크다", "Greater Than", ">", "GREATER_THAN");
        Add("숫자·기호", "LBKT", "왼쪽 대괄호", "Left Bracket and Left Brace", "[ {", "LEFT_BRACKET");
        Add("숫자·기호", "LBRC", "왼쪽 중괄호", "Left Brace", "{", "LEFT_BRACE");
        Add("숫자·기호", "RBKT", "오른쪽 대괄호", "Right Bracket and Right Brace", "] }", "RIGHT_BRACKET");
        Add("숫자·기호", "RBRC", "오른쪽 중괄호", "Right Brace", "}", "RIGHT_BRACE");
        Add("숫자·기호", "GRAVE", "백틱 / 억음", "Grave Accent and Tilde", "` ~", "BACKTICK");
        Add("숫자·기호", "TILDE", "물결표", "Tilde", "~");
        Add("숫자·기호", "NUHS", "비미국 해시", "Non-US Hash / Pound and Tilde", "# ~", "NON_US_HASH");
        Add("숫자·기호", "TILDE2", "비미국 물결표", "Tilde using Shift and Non-US Hash", "~");

        Add("제어·탐색", "ESC", "Escape", "Escape", "", "ESCAPE");
        Add("제어·탐색", "RET", "Enter", "Return / Enter", "↵", "RETURN", "ENTER");
        Add("제어·탐색", "RET2", "Return 2", "Return 2", "↵", "RETURN2");
        Add("제어·탐색", "SPACE", "스페이스", "Space", "␠");
        Add("제어·탐색", "TAB", "Tab", "Tab", "⇥");
        Add("제어·탐색", "BSPC", "Backspace", "Backspace", "⌫", "BACKSPACE");
        Add("제어·탐색", "DEL", "Delete", "Delete", "⌦", "DELETE");
        Add("제어·탐색", "INS", "Insert", "Insert", "", "INSERT");
        Add("제어·탐색", "HOME", "Home", "Home");
        Add("제어·탐색", "END", "End", "End");
        Add("제어·탐색", "PG_UP", "Page Up", "Page Up", "⇞", "PAGE_UP");
        Add("제어·탐색", "PG_DN", "Page Down", "Page Down", "⇟", "PAGE_DOWN");
        Add("제어·탐색", "UP", "위 화살표", "Up Arrow", "↑", "UP_ARROW");
        Add("제어·탐색", "DOWN", "아래 화살표", "Down Arrow", "↓", "DOWN_ARROW");
        Add("제어·탐색", "LEFT", "왼쪽 화살표", "Left Arrow", "←", "LEFT_ARROW");
        Add("제어·탐색", "RIGHT", "오른쪽 화살표", "Right Arrow", "→", "RIGHT_ARROW");
        Add("제어·탐색", "K_APP", "컨텍스트 메뉴", "Application / Context Menu Keyboard", "", "K_APPLICATION", "K_CONTEXT_MENU", "K_CMENU");
        Add("제어·탐색", "CAPS", "Caps Lock", "Caps Lock", "⇪", "CAPSLOCK", "CLCK");
        Add("제어·탐색", "LCAPS", "고정 Caps Lock", "Locking Caps Lock", "", "LOCKING_CAPS");
        Add("제어·탐색", "SLCK", "Scroll Lock", "Scroll Lock", "", "SCROLLLOCK");
        Add("제어·탐색", "LSLCK", "고정 Scroll Lock", "Locking Scroll Lock", "", "LOCKING_SCROLL");
        Add("제어·탐색", "LNLCK", "고정 Num Lock", "Locking Num Lock", "", "LOCKING_NUM");
        Add("제어·탐색", "PSCRN", "Print Screen", "Print Screen", "", "PRINTSCREEN");
        Add("제어·탐색", "PAUSE_BREAK", "Pause / Break", "Pause / Break");
        Add("제어·탐색", "ALT_ERASE", "대체 지우기", "Alternate Erase");
        Add("제어·탐색", "SYSREQ", "SysReq / Attention", "System Request / Attention", "", "ATTENTION");
        Add("제어·탐색", "K_CANCEL", "취소 (키보드)", "Cancel Keyboard");
        Add("제어·탐색", "CLEAR", "Clear", "Clear");
        Add("제어·탐색", "CLEAR_AGAIN", "Clear / Again", "Clear / Again");
        Add("제어·탐색", "CRSEL", "CrSel / Props", "CrSel / Properties");
        Add("제어·탐색", "PRIOR", "Prior", "Prior");
        Add("제어·탐색", "SEPARATOR", "Separator", "Separator");
        Add("제어·탐색", "OUT", "Out", "Out");
        Add("제어·탐색", "OPER", "Oper", "Oper");
        Add("제어·탐색", "EXSEL", "ExSel", "ExSel");
        Add("제어·탐색", "K_EDIT", "편집 (키보드)", "Edit Keyboard");

        for (var number = 1; number <= 24; number++)
            Add("기능키", $"F{number}", $"F{number}", $"Function Key F{number}");

        Add("수정키", "LSHFT", "왼쪽 Shift", "Left Shift", "⇧", "LEFT_SHIFT", "LSHIFT");
        Add("수정키", "RSHFT", "오른쪽 Shift", "Right Shift", "⇧", "RIGHT_SHIFT", "RSHIFT");
        Add("수정키", "LCTRL", "왼쪽 Ctrl", "Left Control", "", "LEFT_CONTROL");
        Add("수정키", "RCTRL", "오른쪽 Ctrl", "Right Control", "", "RIGHT_CONTROL");
        Add("수정키", "LALT", "왼쪽 Alt", "Left Alt", "", "LEFT_ALT");
        Add("수정키", "RALT", "오른쪽 Alt", "Right Alt / AltGr", "", "RIGHT_ALT", "ALTGR");
        Add("수정키", "LGUI", "왼쪽 GUI", "Left GUI / Windows / Command / Meta", "⊞", "LEFT_GUI", "LEFT_WIN", "LWIN", "LEFT_COMMAND", "LCMD", "LEFT_META", "LMETA");
        Add("수정키", "RGUI", "오른쪽 GUI", "Right GUI / Windows / Command / Meta", "⊞", "RIGHT_GUI", "RIGHT_WIN", "RWIN", "RIGHT_COMMAND", "RCMD", "RIGHT_META", "RMETA");

        for (var number = 1; number <= 9; number++)
        {
            var intlLabels = new[] { "ろ / Ro", "かな / Kana", "¥ / Yen", "変換 / Henkan", "無変換 / Muhenkan", ", / 일본어 쉼표", "International 7", "International 8", "International 9" };
            var intlAliases = number switch
            {
                1 => new[] { "INTERNATIONAL_1", "INT_RO" },
                2 => new[] { "INTERNATIONAL_2", "INT_KATAKANAHIRAGANA", "INT_KANA" },
                3 => new[] { "INTERNATIONAL_3", "INT_YEN" },
                4 => new[] { "INTERNATIONAL_4", "INT_HENKAN" },
                5 => new[] { "INTERNATIONAL_5", "INT_MUHENKAN" },
                6 => new[] { "INTERNATIONAL_6", "INT_KPJPCOMMA" },
                _ => new[] { $"INTERNATIONAL_{number}" }
            };
            Add("국제·언어", $"INT{number}", intlLabels[number - 1], $"International {number}", number == 3 ? "¥" : "", intlAliases);
        }
        for (var number = 1; number <= 9; number++)
        {
            var languageLabels = new[] { "한/영 / Hangeul", "한자 / Hanja", "カタカナ / Katakana", "ひらがな / Hiragana", "半角/全角 / Zenkaku Hankaku", "Language 6", "Language 7", "Language 8", "Language 9" };
            var languageAliases = number switch
            {
                1 => new[] { "LANGUAGE_1", "LANG_HANGEUL" },
                2 => new[] { "LANGUAGE_2", "LANG_HANJA" },
                3 => new[] { "LANGUAGE_3", "LANG_KATAKANA" },
                4 => new[] { "LANGUAGE_4", "LANG_HIRAGANA" },
                5 => new[] { "LANGUAGE_5", "LANG_ZENKAKUHANKAKU" },
                _ => new[] { $"LANGUAGE_{number}" }
            };
            Add("국제·언어", $"LANG{number}", languageLabels[number - 1], $"Language {number}", number == 1 ? "한/영" : "", languageAliases);
        }

        Add("넘패드", "KP_NLCK", "Num Lock / Clear", "Keypad Num Lock and Clear", "", "KP_NUMLOCK", "KP_NUM");
        Add("넘패드", "KP_CLEAR", "Clear", "Keypad Clear");
        Add("넘패드", "CLEAR2", "Clear 2", "Keypad Clear 2");
        Add("넘패드", "KP_ENTER", "Enter", "Keypad Enter", "↵");
        var keypadNavigation = new[] { "Insert", "End", "Down Arrow", "Page Down", "Left Arrow", "", "Right Arrow", "Home", "Up Arrow", "Page Up" };
        for (var number = 0; number <= 9; number++)
            Add("넘패드", $"KP_N{number}", $"넘패드 {number}", $"Keypad {number}{(keypadNavigation[number].Length == 0 ? string.Empty : " and " + keypadNavigation[number])}", number.ToString(), $"KP_NUMBER_{number}");
        Add("넘패드", "KP_PLUS", "더하기", "Keypad Plus", "+");
        Add("넘패드", "KP_MINUS", "빼기", "Keypad Minus / Subtract", "-", "KP_SUBTRACT");
        Add("넘패드", "KP_MULTIPLY", "곱하기", "Keypad Multiply / Asterisk", "*", "KP_ASTERISK");
        Add("넘패드", "KP_DIVIDE", "나누기", "Keypad Divide / Slash", "/", "KP_SLASH");
        Add("넘패드", "KP_EQUAL", "등호", "Keypad Equal", "=");
        Add("넘패드", "KP_EQUAL_AS400", "AS/400 등호", "Keypad Equal for AS/400 Keyboards", "=");
        Add("넘패드", "KP_DOT", "소수점 / Delete", "Keypad Dot and Delete", ".");
        Add("넘패드", "KP_COMMA", "쉼표", "Keypad Comma", ",");
        Add("넘패드", "KP_LPAR", "왼쪽 소괄호", "Keypad Left Parenthesis", "(", "KP_LEFT_PARENTHESIS");
        Add("넘패드", "KP_RPAR", "오른쪽 소괄호", "Keypad Right Parenthesis", ")", "KP_RIGHT_PARENTHESIS");

        Add("편집", "C_AC_CUT", "잘라내기 (Consumer)", "Cut Consumer AC");
        Add("편집", "K_CUT", "잘라내기 (Keyboard)", "Cut Keyboard");
        Add("편집", "C_AC_COPY", "복사 (Consumer)", "Copy Consumer AC");
        Add("편집", "K_COPY", "복사 (Keyboard)", "Copy Keyboard");
        Add("편집", "C_AC_PASTE", "붙여넣기 (Consumer)", "Paste Consumer AC");
        Add("편집", "K_PASTE", "붙여넣기 (Keyboard)", "Paste Keyboard");
        Add("편집", "C_AC_UNDO", "실행 취소 (Consumer)", "Undo Consumer AC");
        Add("편집", "K_UNDO", "실행 취소 (Keyboard)", "Undo Keyboard");
        Add("편집", "C_AC_REDO", "다시 실행 (Consumer)", "Redo / Repeat Consumer AC");
        Add("편집", "K_AGAIN", "다시 실행 (Keyboard)", "Again / Redo Keyboard", "", "K_REDO");

        AddMedia(items, Add);
        AddApplications(items, Add);

        Add("입력 보조", "C_KBIA_NEXT", "다음 입력 보조", "Keyboard Input Assist Next", "", "C_KEYBOARD_INPUT_ASSIST_NEXT");
        Add("입력 보조", "C_KBIA_PREV", "이전 입력 보조", "Keyboard Input Assist Previous", "", "C_KEYBOARD_INPUT_ASSIST_PREVIOUS");
        Add("입력 보조", "C_KBIA_NEXT_GRP", "다음 입력 보조 그룹", "Keyboard Input Assist Next Group", "", "C_KEYBOARD_INPUT_ASSIST_NEXT_GROUP");
        Add("입력 보조", "C_KBIA_PREV_GRP", "이전 입력 보조 그룹", "Keyboard Input Assist Previous Group", "", "C_KEYBOARD_INPUT_ASSIST_PREVIOUS_GROUP");
        Add("입력 보조", "C_KBIA_ACCEPT", "입력 보조 수락", "Keyboard Input Assist Accept", "", "C_KEYBOARD_INPUT_ASSIST_ACCEPT");
        Add("입력 보조", "C_KBIA_CANCEL", "입력 보조 취소", "Keyboard Input Assist Cancel", "", "C_KEYBOARD_INPUT_ASSIST_CANCEL");

        Add("시스템·전원", "C_POWER", "전원 (Consumer)", "Power Consumer", "⏻", "C_PWR");
        Add("시스템·전원", "K_POWER", "전원 (Keyboard)", "Power Keyboard", "⏻", "K_PWR");
        Add("시스템·전원", "C_RESET", "초기화", "Reset Consumer");
        Add("시스템·전원", "C_SLEEP", "절전 (Consumer)", "Sleep Consumer");
        Add("시스템·전원", "K_SLEEP", "절전 (Keyboard)", "Sleep Keyboard");
        Add("시스템·전원", "C_SLEEP_MODE", "절전 모드", "Sleep Mode Consumer");
        Add("시스템·전원", "C_AL_LOGOFF", "로그오프", "Logoff Consumer AL");
        Add("시스템·전원", "C_AL_LOCK", "잠금 / 화면보호기 (Consumer)", "Terminal Lock / Screensaver Consumer AL", "", "C_AL_SCREENSAVER", "C_AL_COFFEE");
        Add("시스템·전원", "K_LOCK", "잠금 (Keyboard)", "Lock / Screensaver Keyboard", "", "K_SCREENSAVER", "K_COFFEE");

        return items;
    }

    private static void AddMedia(List<ZmkKeycodeOption> _, Action<string, string, string, string, string, string[]> add)
    {
        void M(string code, string label, string english, params string[] aliases) => add("미디어", code, label, english, "", aliases);
        M("C_VOL_UP", "볼륨 높임 (Consumer)", "Volume Up Consumer", "C_VOLUME_UP");
        M("K_VOL_UP", "볼륨 높임 (Keyboard)", "Volume Up Keyboard", "K_VOLUME_UP");
        M("K_VOL_UP2", "볼륨 높임 2 (Keyboard)", "Volume Up 2 Keyboard", "K_VOLUME_UP2");
        M("C_VOL_DN", "볼륨 낮춤 (Consumer)", "Volume Down Consumer", "C_VOLUME_DOWN");
        M("K_VOL_DN", "볼륨 낮춤 (Keyboard)", "Volume Down Keyboard", "K_VOLUME_DOWN");
        M("K_VOL_DN2", "볼륨 낮춤 2 (Keyboard)", "Volume Down 2 Keyboard", "K_VOLUME_DOWN2");
        M("C_MUTE", "음소거 (Consumer)", "Mute Consumer");
        M("K_MUTE", "음소거 (Keyboard)", "Mute Keyboard");
        M("K_MUTE2", "음소거 2 (Keyboard)", "Mute 2 Keyboard");
        M("C_ALT_AUDIO_INC", "대체 오디오 증가", "Alternate Audio Increment Consumer", "C_ALTERNATE_AUDIO_INCREMENT");
        M("C_BASS_BOOST", "저음 강화", "Bass Boost Consumer");
        M("C_BRI_INC", "밝기 높임", "Increase Brightness Consumer", "C_BRIGHTNESS_INC", "C_BRI_UP");
        M("C_BRI_DEC", "밝기 낮춤", "Decrease Brightness Consumer", "C_BRIGHTNESS_DEC", "C_BRI_DN");
        M("C_BRI_MIN", "최소 밝기", "Minimum Brightness Consumer", "C_BRIGHTNESS_MINIMUM");
        M("C_BRI_MAX", "최대 밝기", "Maximum Brightness Consumer", "C_BRIGHTNESS_MAXIMUM");
        M("C_BRI_AUTO", "자동 밝기", "Auto Brightness Consumer", "C_BRIGHTNESS_AUTO");
        M("C_BKLT_TOG", "백라이트 토글", "Backlight Toggle Consumer", "C_BACKLIGHT_TOGGLE");
        M("C_ASPECT", "화면 비율", "Aspect Consumer");
        M("C_PIP", "화면 속 화면", "Picture-in-Picture Toggle Consumer", "PICTURE IN PICTURE");
        M("C_REC", "녹화", "Record Consumer", "C_RECORD");
        M("C_PLAY", "재생", "Play Consumer");
        M("C_PP", "재생 / 일시정지 (Consumer)", "Play / Pause Consumer", "C_PLAY_PAUSE");
        M("K_PP", "재생 / 일시정지 (Keyboard)", "Play / Pause Keyboard", "K_PLAY_PAUSE");
        M("C_PAUSE", "일시정지", "Pause Consumer");
        M("C_STOP", "정지 (Consumer)", "Stop Consumer");
        M("K_STOP2", "정지 2 (Keyboard)", "Stop 2 Keyboard");
        M("K_STOP3", "정지 3 (Keyboard)", "Stop 3 Keyboard");
        M("C_STOP_EJECT", "정지 / 꺼내기", "Stop / Eject Consumer");
        M("C_EJECT", "꺼내기 (Consumer)", "Eject Consumer");
        M("K_EJECT", "꺼내기 (Keyboard)", "Eject Keyboard");
        M("C_NEXT", "다음 트랙 (Consumer)", "Next Track Consumer");
        M("K_NEXT", "다음 트랙 (Keyboard)", "Next Track Keyboard");
        M("C_PREV", "이전 트랙 (Consumer)", "Previous Track Consumer", "C_PREVIOUS");
        M("K_PREV", "이전 트랙 (Keyboard)", "Previous Track Keyboard", "K_PREVIOUS");
        M("C_FF", "빨리 감기", "Fast Forward Consumer", "C_FAST_FORWARD");
        M("C_RW", "되감기", "Rewind Consumer", "C_REWIND");
        M("C_SLOW", "느리게 재생", "Slow Consumer");
        M("C_SLOW2", "느린 추적", "Slow Tracking Consumer", "C_SLOW_TRACKING");
        M("C_REPEAT", "반복", "Repeat Consumer");
        M("C_SHUFFLE", "무작위 재생", "Random Play / Shuffle Consumer", "C_RANDOM_PLAY");
        M("C_SUBTITLES", "자막", "Closed Caption / Subtitles Consumer", "C_CAPTIONS");
        M("C_DATA_ON_SCREEN", "화면 데이터", "Data On Screen Consumer");
        M("C_SNAPSHOT", "스냅샷", "Snapshot Consumer");
        M("C_MENU", "미디어 메뉴", "Menu Consumer");
        M("C_MENU_SELECT", "메뉴 선택", "Pick / Select Consumer Menu", "C_MENU_PICK");
        M("C_MENU_UP", "메뉴 위", "Up Consumer Menu");
        M("C_MENU_DOWN", "메뉴 아래", "Down Consumer Menu");
        M("C_MENU_LEFT", "메뉴 왼쪽", "Left Consumer Menu");
        M("C_MENU_RIGHT", "메뉴 오른쪽", "Right Consumer Menu");
        M("C_MENU_ESC", "메뉴 나가기", "Escape Consumer Menu", "C_MENU_ESCAPE");
        M("C_MENU_INC", "메뉴 값 증가", "Value Increase Consumer Menu", "C_MENU_INCREASE");
        M("C_MENU_DEC", "메뉴 값 감소", "Value Decrease Consumer Menu", "C_MENU_DECREASE");
        M("C_RED", "빨강 버튼", "Red Button Consumer Menu", "C_RED_BUTTON");
        M("C_GREEN", "초록 버튼", "Green Button Consumer Menu", "C_GREEN_BUTTON");
        M("C_BLUE", "파랑 버튼", "Blue Button Consumer Menu", "C_BLUE_BUTTON");
        M("C_YELLOW", "노랑 버튼", "Yellow Button Consumer Menu", "C_YELLOW_BUTTON");
        M("C_CHAN_INC", "채널 올림", "Channel Increment Consumer", "C_CHANNEL_INC");
        M("C_CHAN_DEC", "채널 내림", "Channel Decrement Consumer", "C_CHANNEL_DEC");
        M("C_CHAN_LAST", "이전 채널", "Recall Last Channel Consumer", "C_RECALL_LAST");
        M("C_MEDIA_VCR_PLUS", "VCR Plus", "VCR Plus Consumer Media");
        M("C_MEDIA_GUIDE", "프로그램 가이드", "Program Guide Consumer Media");
        M("C_MODE_STEP", "미디어 모드 전환", "Mode Step Consumer Media", "C_MEDIA_STEP");
        M("C_MEDIA_HOME", "미디어 홈", "Home Consumer Media");
        M("C_MEDIA_TV", "TV", "TV Consumer Media");
        M("C_MEDIA_CABLE", "케이블", "Cable Consumer Media");
        M("C_MEDIA_TUNER", "튜너", "Tuner Consumer Media");
        M("C_MEDIA_DVD", "DVD", "DVD Consumer Media");
        M("C_MEDIA_CD", "CD", "CD Consumer Media");
        M("C_MEDIA_SATELLITE", "위성", "Satellite Consumer Media");
        M("C_MEDIA_VCR", "VCR", "VCR Consumer Media");
        M("C_MEDIA_TAPE", "테이프", "Tape Consumer Media");
        M("C_MEDIA_COMPUTER", "컴퓨터 미디어", "Computer Consumer Media");
        M("C_MEDIA_WWW", "웹 미디어", "WWW Consumer Media");
        M("C_MEDIA_GAMES", "게임", "Games Consumer Media");
        M("C_MEDIA_PHONE", "전화", "Telephone Consumer Media");
        M("C_MEDIA_VIDEOPHONE", "영상 전화", "Video Phone Consumer Media");
        M("C_MEDIA_MESSAGES", "메시지", "Messages Consumer Media");
        M("C_QUIT", "종료", "Quit Consumer");
        M("C_HELP", "도움말", "Help Consumer");
    }

    private static void AddApplications(List<ZmkKeycodeOption> _, Action<string, string, string, string, string, string[]> add)
    {
        void A(string code, string label, string english, params string[] aliases) => add("앱·브라우저", code, label, english, "", aliases);
        A("K_MENU", "메뉴 (Keyboard)", "Menu Keyboard");
        A("C_AC_PROPS", "속성", "Properties Consumer AC", "C_AC_PROPERTIES");
        A("K_SELECT", "선택 (Keyboard)", "Select Keyboard");
        A("C_AC_CANCEL", "취소 (Consumer)", "Cancel Consumer AC");
        A("K_EXEC", "실행 (Keyboard)", "Execute Keyboard", "K_EXECUTE");
        A("C_AC_REFRESH", "새로고침 (Consumer)", "Refresh Consumer AC");
        A("K_REFRESH", "새로고침 (Keyboard)", "Refresh Keyboard");
        A("C_AC_STOP", "중지 (Consumer)", "Stop Consumer AC");
        A("K_STOP", "중지 (Keyboard)", "Stop Keyboard");
        A("C_AC_FORWARD", "앞으로 (Consumer)", "Forward Consumer AC");
        A("K_FORWARD", "앞으로 (Keyboard)", "Forward Keyboard");
        A("C_AC_BACK", "뒤로 (Consumer)", "Back Consumer AC");
        A("K_BACK", "뒤로 (Keyboard)", "Back Keyboard");
        A("C_AC_HOME", "홈 (Consumer)", "Home Consumer AC");
        A("C_AC_BOOKMARKS", "북마크 / 즐겨찾기", "Bookmarks / Favorites Consumer AC", "C_AC_FAVORITES", "C_AC_FAVOURITES");
        A("C_AC_NEW", "새로 만들기", "New Consumer AC");
        A("C_AC_OPEN", "열기", "Open Consumer AC");
        A("C_AC_SAVE", "저장", "Save Consumer AC");
        A("C_AC_CLOSE", "닫기", "Close Consumer AC");
        A("C_AC_EXIT", "나가기", "Exit Consumer AC");
        A("C_AC_PRINT", "인쇄", "Print Consumer AC");
        A("C_AC_FIND", "찾기 (Consumer)", "Find Consumer AC");
        A("K_FIND", "찾기 (Keyboard)", "Find Keyboard");
        A("K_FIND2", "찾기 2 (Keyboard)", "Find 2 Keyboard");
        A("C_AC_SEARCH", "검색", "Search Consumer AC");
        A("C_AC_GOTO", "이동", "Go To Consumer AC");
        A("C_AC_ZOOM", "확대/축소", "Zoom Consumer AC");
        A("C_AC_ZOOM_IN", "확대", "Zoom In Consumer AC");
        A("C_AC_ZOOM_OUT", "축소", "Zoom Out Consumer AC");
        A("C_AC_SCROLL_UP", "스크롤 위 (Consumer)", "Scroll Up Consumer AC");
        A("K_SCROLL_UP", "스크롤 위 (Keyboard)", "Scroll Up Keyboard");
        A("C_AC_SCROLL_DOWN", "스크롤 아래 (Consumer)", "Scroll Down Consumer AC");
        A("K_SCROLL_DOWN", "스크롤 아래 (Keyboard)", "Scroll Down Keyboard");
        A("C_AC_REPLY", "답장", "Reply Consumer AC");
        A("C_AC_FORWARD_MAIL", "메일 전달", "Forward Mail Consumer AC");
        A("C_AC_SEND", "보내기", "Send Consumer AC");
        A("C_AC_EDIT", "편집 (Consumer)", "Edit Consumer AC");
        A("C_AC_INS", "삽입 모드", "Insert Mode Consumer AC", "C_AC_INSERT");
        A("C_AC_DEL", "삭제 (Consumer)", "Delete Consumer AC");
        A("C_AC_VIEW_TOGGLE", "보기 전환", "View Toggle Consumer AC");
        A("C_AC_DESKTOP_SHOW_ALL_WINDOWS", "모든 창 보기", "Desktop Show All Windows Consumer AC");
        A("C_AC_DESKTOP_SHOW_ALL_APPLICATIONS", "모든 앱 보기", "Desktop Show All Applications Consumer AC");
        A("C_VOICE_COMMAND", "음성 명령", "Voice Command Consumer");
        A("GLOBE", "다음 키보드 배열 / Globe", "Next Keyboard Layout Select / Apple Globe", "C_AC_NEXT_KEYBOARD_LAYOUT_SELECT");
        A("C_AL_NEXT_TASK", "다음 작업 / 앱", "Next Task / Application Consumer AL");
        A("C_AL_PREV_TASK", "이전 작업 / 앱", "Previous Task / Application Consumer AL", "C_AL_PREVIOUS_TASK");
        A("C_AL_SELECT_TASK", "작업 / 앱 선택", "Select Task / Application Consumer AL");
        A("C_AL_MY_COMPUTER", "내 컴퓨터", "Local Machine Browser Consumer AL");
        A("C_AL_DOCS", "문서", "Documents Consumer AL", "C_AL_DOCUMENTS");
        A("C_AL_FILES", "파일 탐색기", "File Browser Consumer AL", "C_AL_FILE_BROWSER");
        A("C_AL_WWW", "인터넷 브라우저 (Consumer)", "Internet Browser Consumer AL");
        A("K_WWW", "인터넷 브라우저 (Keyboard)", "Internet Browser Keyboard");
        A("C_AL_MAIL", "이메일", "Email Reader Consumer AL", "C_AL_EMAIL");
        A("C_AL_IM", "인스턴트 메시징", "Instant Messaging Consumer AL", "C_AL_INSTANT_MESSAGING");
        A("C_AL_CHAT", "네트워크 채팅", "Network Chat Consumer AL", "C_AL_NETWORK_CHAT");
        A("C_AL_CONTACTS", "연락처 / 주소록", "Contacts / Address Book Consumer AL", "C_AL_ADDRESS_BOOK");
        A("C_AL_CAL", "캘린더 / 일정", "Calendar / Schedule Consumer AL", "C_AL_CALENDAR");
        A("C_AL_IMAGES", "이미지 브라우저", "Image Browser Consumer AL", "C_AL_IMAGE_BROWSER");
        A("C_AL_MUSIC", "오디오 / 음악 브라우저", "Audio / Music Browser Consumer AL", "C_AL_AUDIO_BROWSER", "C_AL_AUDIO");
        A("C_AL_MOVIES", "영화 브라우저", "Movie Browser Consumer AL", "C_AL_MOVIE_BROWSER");
        A("C_AL_TEXT_EDITOR", "텍스트 편집기", "Text Editor Consumer AL");
        A("C_AL_WORD", "워드 프로세서", "Word Processor Consumer AL");
        A("C_AL_SHEET", "스프레드시트", "Spreadsheet Consumer AL", "C_AL_SPREADSHEET");
        A("C_AL_PRESENTATION", "프레젠테이션", "Presentation Consumer AL");
        A("C_AL_GRAPHICS_EDITOR", "그래픽 편집기", "Graphics Editor Consumer AL");
        A("C_AL_CALC", "계산기 (Consumer)", "Calculator Consumer AL", "C_AL_CALCULATOR");
        A("K_CALC", "계산기 (Keyboard)", "Calculator Keyboard", "K_CALCULATOR");
        A("C_AL_NEWS", "뉴스 리더", "Newsreader Consumer AL");
        A("C_AL_DB", "데이터베이스 앱", "Database App Consumer AL", "C_AL_DATABASE");
        A("C_AL_VOICEMAIL", "음성 사서함", "Voicemail Consumer AL");
        A("C_AL_FINANCE", "금융 앱", "Checkbook / Finance Consumer AL");
        A("C_AL_TASK_MANAGER", "작업 / 프로젝트 관리자", "Task / Project Manager Consumer AL");
        A("C_AL_JOURNAL", "로그 / 저널 / 타임카드", "Log / Journal / Timecard Consumer AL");
        A("C_AL_AV_CAPTURE_PLAYBACK", "A/V 캡처 / 재생", "A/V Capture / Playback Consumer AL");
        A("C_AL_SPELL", "맞춤법 검사", "Spell Check Consumer AL", "C_AL_SPELLCHECK");
        A("C_AL_SCREEN_SAVER", "화면 보호기", "Screen Saver Consumer AL");
        A("C_AL_KEYBOARD_LAYOUT", "키보드 배열", "Keyboard Layout Consumer AL");
        A("C_AL_CONTROL_PANEL", "제어판", "Control Panel Consumer AL");
        A("C_AL_HELP", "통합 도움말", "Integrated Help Center Consumer AL");
        A("K_HELP", "도움말 (Keyboard)", "Help Keyboard");
        A("C_AL_TIPS", "OEM 기능 / 팁 / 튜토리얼", "OEM Features / Tips / Tutorial Consumer AL", "C_AL_OEM_FEATURES", "C_AL_TUTORIAL");
        A("C_AL_CCC", "소비자 제어 구성", "Consumer Control Configuration Consumer AL");
    }
}
