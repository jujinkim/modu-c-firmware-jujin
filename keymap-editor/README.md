# MODU Keymap Studio (Unofficial, by Jujin Kim)

MODU-C의 ZMK `modu.keymap`을 시각적으로 편집하는 Windows 10/11 x64용 네이티브 앱입니다. .NET 8 WPF로 작성되었으며 키맵 편집에는 Python, 브라우저, 로컬 웹 서버가 필요하지 않습니다. 펌웨어를 컴파일할 때만 별도의 ZMK/Zephyr 개발 환경이 필요합니다.

> MODU 키맵에는 형식상 67개 바인딩이 필요하지만, 이 중 6개는 실제 스위치가 없는 예약 슬롯입니다. 에디터에는 본키 48개, 하단키 6개, 왼쪽/오른쪽 엄지키 각 3개, 오른쪽 추가모듈 키 1개로 총 **61키**만 표시됩니다.

## 주요 기능

- 실행 위치의 상위 폴더를 탐색해 `modu-module/boards/shields/modu/modu.keymap` 자동 열기
- 두 레이어와 MODU 61키 물리 배열 표시(엄지 3/3, 추가모듈 키는 오른쪽 N 왼쪽)
- 실제 스위치가 없는 6개 예약 슬롯은 숨기고 편집을 차단
- 투명 바인딩은 옅게 표시하고 기본 레이어의 실제 키 값을 대신 표시
- ZMK 공식 키코드 문서의 368개 항목 전체 제공: 문자, 숫자·기호, 제어·탐색, 기능키, 수정키, 국제·언어, 넘패드, 편집, 미디어, 앱·브라우저, 입력 보조, 시스템·전원
- 한글 이름, 실제 문자 기호, 영문명, ZMK 단축/장문 별칭으로 검색(예: `슬래시`, `/`, `?`, `forward slash`, `question mark`, `FSLH`, `SLASH`)
- 숫자열과 문장부호 키에는 Shift 입력까지 실제 문자로 함께 표시(예: `N1 1 !`, `FSLH / ?`, `SEMI ; :`); 키를 올리면 원문 바인딩과 전체 설명 표시
- 키캡은 키 ID를 좌측 하단에 표시하며, 실제 문자가 있는 키는 중앙에 큰 문자(Shift 문자는 위, 기본 문자는 아래)와 우측 하단 ZMK 코드를 표시
- 알파벳·기능키처럼 짧은 코드형 키는 중앙 글자를 크게 표시하고, Shift 문자가 함께 있는 숫자·문장부호 키는 두 문자의 크기를 균형 있게 표시
- 오른쪽 키 선택 목록의 주 정보는 실제 문자와 영문명으로 표시하고 한글명은 작은 설명 줄에만 표시
- Bluetooth, 마우스, 레이어 동작 분류 및 검색
- `&trans`, `&none`, `&mo`, `&to`, `&tog`, `&sl`, `&lt` 전용 입력과 고급 원문 입력
- 실행 취소/다시 실행, 명시적 저장, 다른 이름으로 저장, 미저장 종료 확인
- 투명 레이어 추가 또는 현재 레이어 복제
- 기본 레이어 삭제 방지, 참조 중/심볼형 레이어 삭제 차단, 상위 숫자 참조 자동 보정
- Python, west, west 워크스페이스, `west build`, CMake/Ninja, ARM Zephyr SDK 사전 점검과 항목별 설치 안내
- 기존 저장소 `build.ps1`을 통한 좌/우 펌웨어 빌드, 로그 분리, 취소 및 결과 폴더 열기

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

실제 키맵 파싱, 바이트 단위 round-trip, LF/CRLF와 주석 보존, 단일 키 패치, 레이어 추가/복제/검증/삭제/참조 보정, undo/redo, 공식 ZMK 키코드 카탈로그 및 영문명·기호·별칭 검색, 가짜 빌드 프로세스의 성공/실패/취소를 검사합니다.

## 포터블 EXE 게시

```powershell
.\keymap-editor\publish.ps1
```

`keymap-editor/dist/ModuKeymapStudio.exe`에 `win-x64` self-contained 단일 파일을 만들고 smoke test를 실행합니다. `bin/`, `obj/`, `dist/`는 생성물이며 Git에 포함되지 않습니다.

## 펌웨어 빌드

앱의 **펌웨어 빌드** 버튼은 현재 키맵을 먼저 저장한 뒤 저장소의 기존 `build.ps1`을 호출합니다. ZMK app 폴더를 선택하면 경로를 `%LOCALAPPDATA%\ModuKeymapStudio\settings.json`에 기억합니다. 성공 결과는 다음 위치에 생성됩니다.

- `outputs/modu_left.uf2`
- `outputs/modu_right.uf2`

`west`는 ZMK와 Zephyr 소스·모듈을 워크스페이스 단위로 관리하고 Zephyr 빌드 명령을 제공하는 Python 도구입니다. Zephyr SDK에는 MODU의 nRF52840용 펌웨어를 만들 ARM 컴파일러, 어셈블러, 링커와 호스트 도구가 들어 있습니다.

빌드 창은 열릴 때 환경을 자동 점검합니다. 실패 항목에는 바로 적용할 수 있는 한국어 안내를 표시하며, **설치 가이드** 탭에서 공식 문서와 Windows 설치 명령을 확인할 수 있습니다. 점검은 설치나 파일 변경을 하지 않습니다. 또한 도구 실행 가능성을 미리 확인하는 절차이므로, 소스와 설정까지 포함한 최종 판정은 실제 `west build` 결과입니다.

처음 설정할 때는 [ZMK Native Setup](https://zmk.dev/docs/development/local-toolchain/setup/native)과 [Zephyr Getting Started](https://docs.zephyrproject.org/latest/develop/getting_started/index.html)를 따르세요. 기본 설치 흐름은 다음과 같습니다.

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
