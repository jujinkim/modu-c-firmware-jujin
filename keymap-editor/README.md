# Unofficial MODU Keymap Studio

MODU-C의 ZMK `modu.keymap`을 시각적으로 편집하는 Windows 10/11 x64용 네이티브 앱입니다. .NET 8 WPF로 작성되었으며 키맵 편집에는 Python, 브라우저, 로컬 웹 서버가 필요하지 않습니다. 펌웨어를 컴파일할 때만 별도의 ZMK/Zephyr 개발 환경이 필요합니다.

> MODU 키맵에는 형식상 67개 바인딩이 필요하지만, 이 중 6개는 실제 스위치가 없는 예약 슬롯입니다. 에디터에는 본키 48개, 하단키 6개, 왼쪽/오른쪽 엄지키 각 3개, 오른쪽 추가모듈 키 1개로 총 **61키**만 표시됩니다.

## 주요 기능

- 실행 위치의 상위 폴더를 탐색해 `modu-module/boards/shields/modu/modu.keymap` 자동 열기
- 실제 레이어 수와 무관하게 MODU 61키 물리 배열 표시(엄지 3/3, 추가모듈 키는 오른쪽 N 왼쪽)
- 실제 스위치가 없는 6개 예약 슬롯은 숨기고 편집을 차단
- 투명 바인딩은 옅게 표시하고 기본 레이어의 실제 키 값을 대신 표시
- ZMK 공식 키코드 문서의 368개 항목 전체 제공: 문자, 숫자·기호, 제어·탐색, 기능키, 수정키, 국제·언어, 넘패드, 편집, 미디어, 앱·브라우저, 입력 보조, 시스템·전원
- 한글 이름, 실제 문자 기호, 영문명, ZMK 단축/장문 별칭으로 검색(예: `슬래시`, `/`, `?`, `forward slash`, `question mark`, `FSLH`, `SLASH`)
- 숫자열과 문장부호 키에는 Shift 입력까지 실제 문자로 함께 표시(예: `N1 1 !`, `FSLH / ?`, `SEMI ; :`); 키를 올리면 원문 바인딩과 전체 설명 표시
- 키캡은 키 ID를 좌측 하단에 표시하며, 실제 문자가 있는 키는 중앙에 큰 문자(Shift 문자는 위, 기본 문자는 아래)와 우측 하단 ZMK 코드를 표시
- 알파벳·기능키처럼 짧은 코드형 키는 중앙 글자를 크게 표시하고, Shift 문자가 함께 있는 숫자·문장부호 키는 두 문자의 크기를 균형 있게 표시
- 오른쪽 키 선택 목록의 주 정보는 실제 문자와 영문명으로 표시하고 한글명은 작은 설명 줄에만 표시
- 선택한 키 영역의 `?` 도움말에서 Keyboard(`K_`)·Consumer(`C_`)·Application Control(`C_AC_`) 키 차이와 ZMK 공식 호환성 표 안내
- Bluetooth, 마우스, 레이어 동작 분류 및 검색
- `&trans`, `&none`, `&mo`, `&to`, `&tog`, `&sl`, `&lt` 전용 입력과 고급 원문 입력
- 실행 취소/다시 실행, 명시적 저장, 다른 이름으로 저장, 미저장 종료 확인
- 상단 도구 모음을 파일·작업·레이어 그룹으로 구분하고, 전체보기와 펌웨어 빌드는 독립 실행 버튼으로 배치
- 전체보기 창에서 모든 레이어를 세로로 스크롤해 미리보고 한 장의 PNG로 저장하거나 클립보드에 복사
- 투명 레이어 추가 또는 현재 레이어 복제
- 표시 이름과 Devicetree 노드 이름 변경(기본 레이어의 `default_layer` 노드명은 보호)
- 현재 레이어에서 키를 드래그해 빈 자리로 이동·복사하거나 할당된 키에 덮어쓰기·교체; 드래그 중 소스 키 강조와 커서 옆 반투명 키 고스트 표시
- Light, Dark, Windows 시스템 테마 즉시 전환 및 preference 저장
- 기본 레이어 삭제 방지, 참조 중/심볼형 레이어 삭제 차단, 상위 숫자 참조 자동 보정
- Python, west, west 워크스페이스, `west build`, CMake/Ninja, ARM Zephyr SDK 사전 점검과 항목별 설치 안내
- 기존 저장소 `build.ps1`을 통한 좌/우 펌웨어 빌드, 로그 분리, 취소 및 결과 폴더 열기
- 앱 시작 시 먼저 표시되고 우측 하단에서 다시 열 수 있는 About 창에서 프로그램 버전, 비공식 도구 및 사용자 책임 안내, 프로젝트·원본 펌웨어·사용자 매뉴얼 링크 제공

`LANG3`, `LANG4`, `LANG5`는 키캡 중앙에 각각 **カタカナ**, **ひらがな**, **半角/全角**으로 표시되고 ZMK 코드는 보조 라벨로 남습니다.

시스템·전원 목록에는 즉시 실행하는 `&bootloader`와, 짧게 누르면 아무 동작도 하지 않고 500ms 이상 눌렀을 때 실행하는 부트로더·시스템 리셋 항목이 있습니다. 앱 오른쪽 패널의 접힌 **부트로더·리셋 안내**를 열면 설정 순서와 동작 차이를 확인할 수 있습니다. 500ms 항목을 처음 적용하면 에디터가 같은 `modu.keymap` 안에 `tap-preferred` 사용자 정의 hold-tap 동작을 한 번만 추가합니다. ZMK의 `&bootloader`와 `&sys_reset`은 누른 키가 위치한 하프에만 적용됩니다. 자세한 동작은 [ZMK Hold-Tap](https://zmk.dev/docs/keymaps/behaviors/hold-tap), [ZMK Reset Behaviors](https://zmk.dev/docs/keymaps/behaviors/reset), [ZMK Bootloader Integration](https://zmk.dev/docs/hardware-integration/bootloader)을 참고하세요.

키맵 파서는 바인딩의 원문 위치만 패치합니다. 헤더, 주석, 공백, 줄바꿈과 키맵 밖의 DTS 내용은 그대로 보존하며, 무수정 저장은 원본 바이트를 그대로 기록합니다. 저장 전 모든 레이어가 정확히 67개 바인딩인지 검증합니다.

## 개발 실행

.NET 8 SDK가 설치된 Windows에서:

```powershell
dotnet run --project .\keymap-editor\src\ModuKeymapStudio\ModuKeymapStudio.csproj
```

## 테스트

```powershell
.\keymap-editor\test.ps1
```

실제 키맵 파싱, 바이트 단위 round-trip, LF/CRLF와 주석 보존, 단일 키 패치, 레이어 추가/복제/이름 변경/검증/삭제/참조 보정, 다섯 가지 키 드래그 작업, 500ms 안전 홀드 동작, undo/redo, 공식 ZMK 키코드 카탈로그 및 영문명·기호·별칭 검색, 테마 설정 호환성, 가짜 빌드 프로세스의 성공/실패/취소를 검사합니다.

## 포터블 EXE 게시

```powershell
.\keymap-editor\publish.ps1
```

`keymap-editor/dist/UnofficialModuKeymapStudio.exe`에 `win-x64` self-contained 단일 파일을 만들고 smoke test를 실행합니다. `bin/`, `obj/`, `dist/`는 생성물이며 Git에 포함되지 않습니다.

## 펌웨어 빌드

앱의 **펌웨어 빌드** 버튼은 현재 키맵을 먼저 저장한 뒤 저장소의 기존 `build.ps1`을 호출합니다. ZMK app 폴더를 선택하면 경로를 `%LOCALAPPDATA%\ModuKeymapStudio\settings.json`에 기억합니다. 성공 결과는 다음 위치에 생성됩니다.

- `outputs/modu_left.uf2`
- `outputs/modu_right.uf2`

`west`는 ZMK와 Zephyr 소스·모듈을 워크스페이스 단위로 관리하고 Zephyr 빌드 명령을 제공하는 Python 도구입니다. Zephyr SDK에는 MODU의 nRF52840용 펌웨어를 만들 ARM 컴파일러, 어셈블러, 링커와 호스트 도구가 들어 있습니다.

빌드 창은 열릴 때 환경을 자동 점검합니다. 실패 항목에는 바로 적용할 수 있는 한국어 안내를 표시하며, **설치 가이드** 탭에서 공식 문서와 Windows 설치 명령을 확인할 수 있습니다. 점검은 설치나 파일 변경을 하지 않습니다. 또한 도구 실행 가능성을 미리 확인하는 절차이므로, 소스와 설정까지 포함한 최종 판정은 실제 `west build` 결과입니다.

처음 설정할 때는 [ZMK Native Setup](https://zmk.dev/docs/development/local-toolchain/setup/native)과 [Zephyr Getting Started](https://docs.zephyrproject.org/latest/develop/getting_started/index.html)를 따르세요. 기본 설치 흐름은 다음과 같습니다.

Windows에서 `python`/`py -3.12` 또는 `7z`/`7z.exe`를 찾을 수 없다는 오류가 나오면 먼저 다음 명령으로 Python 3.12와 7-Zip CLI를 설치하세요. 설치 후에는 새 PowerShell을 여는 것이 가장 간단합니다.

```powershell
winget install --exact --id Python.Python.3.12 --source winget
winget install --exact --id 7zip.7zip --source winget

py -3.12 --version
```

7-Zip을 설치했는데 현재 PowerShell에서 `7z.exe`만 찾지 못한다면 설치 폴더를 해당 세션의 `PATH`에 임시로 추가할 수 있습니다. 아래 설정은 사용자·시스템 환경 변수를 영구 변경하지 않습니다.

```powershell
$sevenZipBin = Join-Path $env:ProgramFiles '7-Zip'
if (-not (Test-Path (Join-Path $sevenZipBin '7z.exe'))) {
    throw 'C:\Program Files\7-Zip\7z.exe를 찾지 못했습니다.'
}
$env:Path = "$sevenZipBin;$env:Path"
7z.exe i
```

이 앱 자체는 Windows 전용입니다. 다른 운영체제에서 별도로 ZMK 환경을 구성할 때는 `apt`, `dnf`, `pacman`, Homebrew 등 해당 환경의 패키지 관리자로 Python 3.12와 7-Zip CLI에 해당하는 패키지를 설치하고 공식 안내를 따르세요. 패키지 이름과 `PATH` 설정은 운영체제마다 다릅니다.

그다음 ZMK 워크스페이스를 설정합니다.

```powershell
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
```

ZMK 및 이 저장소 경로에는 ASCII 문자만 사용해야 합니다. 현재 개발 환경에 `C:\zmk\app`이 없다면 앱과 파서 테스트는 실행할 수 있지만 실제 `west build`와 UF2 생성은 검증할 수 없습니다.
