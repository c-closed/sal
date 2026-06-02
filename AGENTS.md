# AGENTS.md - Sboard 접속기

## 빌드 및 실행

```bash
# 개발 실행
dotnet run --project Sboard접속기

# 전체 빌드
dotnet build sal.sln -c Release

# MSI 패키징 (Visual Studio Installer Projects 필요)
# Setup.vdproj를 VS에서 열어 빌드

# CF Worker 배포
cd cf-worker
npm install
npx wrangler deploy
```

## 버전 규칙 (릴리스 시)

- **UI 변경** → major 증가 (v1.0.0 → v2.0.0)
- **기능 변경** → minor 증가 (v1.0.0 → v1.1.0)
- **버그 수정** → patch 증가 (v1.0.0 → v1.0.1)
- 4자리 버전: Major.Minor.Patch.0 (Revision은 항상 0)
- 모든 변경 시 함께 업데이트:
  - `Config.cs`의 `AppVersion`, `AppVersion4`
  - `Sboard접속기.csproj`의 `Version`, `ApplicationVersion`, `ApplicationDisplayVersion`
  - CF Worker `UPDATE_INFO.version`, `UPDATE_INFO.version4`, `UPDATE_INFO.url`
  - `cf-worker/src/index.js`의 `/api/update.xml` 응답 (version4 기반)
- **자동 처리**: 앞으로 버전 번호는 AI가 변경 사항을 보고 자체 판단하여 위 규칙에 따라 갱신한다 (Major/Minor/Patch 결정, 모든 파일 일괄 수정, git commit & tag, GitHub Release, CF Worker deploy).

## 아키텍처

- **GUI**: C# WinForms (.NET 8), ttk → WinForms Designer 패턴
- **업데이트**: AutoUpdater.NET (NuGet) — 자체 UI/진행률/재시작 처리
- **자동화**: `WindowsInput` (SendInput API), `ctypes` 대체
- **API 백엔드**: Cloudflare Workers (`https://sboard-api.sboard-auto-login.workers.dev`)
- **Cloudflare Worker**: `cf-worker/` 디렉토리 참조

## 폰트 규칙

| 위치 | 폰트 | 크기 |
|---|---|---|
| 메인 창, 버튼, 레이블, 입력창 | Consolas | 12pt |
| 사용자 목록 DataGridView (데이터) | Consolas | 10pt |
| 사용자 목록 DataGridView (헤더) | Consolas | 10pt Bold |
| LoginLogForm (제목, 로그, 상태) | Consolas | 8pt |

## 업데이트 흐름 (AutoUpdater.NET)

1. `MainForm_Load` → `AutoUpdater.Start(Config.UpdateXmlUrl)` 호출
2. 서버(`/api/update.xml`)에서 버전 확인 → `Application.ProductVersion`(2.3.0.0)과 비교
3. 새 버전이면 내장 다이얼로그 표시 (Mandatory=true → Skip/Remind 없음)
4. 사용자 "Update" 클릭 → ZIP 다운로드 + 추출 → 내장 ProgressBar 표시
5. 내장 `AutoUpdater.exe`가 PID 종료 감지 → 파일 교체 → 앱 재실행
6. 자체 `--updated` 플래그로 업데이트 직후 재검사 방지
7. 레지스트리 DisplayVersion 갱신은 AutoUpdater.NET 미지원 — 필요시 InnoSetup/MSI 담당

## 주요 제약사항

1. **창 종료 동작**: Form 닫을 때 `this.Hide()` / `e.Cancel = true`로 숨김. sboard.exe는 독립 프로세스.
2. **스레드 안전**: Tkinter 규칙과 동일 — 백그라운드 스레드에서 Tkinter/UI 직접 호출 금지. `Control.Invoke()` 사용.
3. **비동기 로그인**: `async/await` + `Task.Run` 사용. `while` 루프 금지.
4. **WinForms Designer**: `InitializeComponent()` 내 파일 I/O, 이벤트 바인딩 금지. Designer.cs는 VS가 자동 수정.
5. **AutoUpdater.NET**: `ReportErrors = false` (연결 실패 시 사용자 불편 방지). `RunUpdateAsAdmin = true` (권한 상승).
6. **Admin 체크**: `AdminHelper.IsAdministrator()` → 아니면 `RestartAsAdmin()`으로 자기 재실행.

## 개발 시 주의사항

- ❌ `catch { }` 비워두면 안 됨 (컴파일 경고). 반드시 처리 또는 `catch { /* 설명 */ }`.
- ❌ 백그라운드 스레드에서 UI 직접 호출 금지.
- ✅ WinForms Designer에서 컨트롤 배치 후 Designer.cs는 손수 수정 금지 (Designer 전용).
- ✅ DataGridView 컬럼 FillWeight 50/50 + MiddleCenter 정렬 (헤더/데이터 모두).

## 테스트 체크리스트

1. 로그인 성공 시 "로그인 성공!" + 3초 카운트다운 후 Form 자동 숨김
2. 로그인 실패 시 "로그인 정보가 일치하지 않습니다." 메시지
3. Form 종료 시 sboard.exe는 계속 실행 중
4. 업데이트 XML 응답이 올바른 XML 형식인지 확인
5. AutoUpdater.NET 다운로드 → 재시작까지 전체 플로우 동작 확인
6. CS 프로젝트 0 warning, 0 error
