# AGENTS.md - Sboard 접속기

## 빌드 및 실행

```bash
# 개발 실행
cd "D:\99. 개인자료\00. 개발\sal"
python sal.py

# PyInstaller 패키징 (dist/Sboard 접속기.exe 생성)
pyinstaller "Sboard 접속기.spec"

# InnoSetup 설치 패키징 (dist/Sboard_Setup.exe 생성)
iscc "Sboard 접속기.iss"
```

## 버전 규칙 (GitHub Release 시)

- **UI 변경** → major 증가 (v1.0.0 → v2.0.0)
- **기능 변경** → minor 증가 (v1.0.0 → v1.1.0)
- **버그 수정** → patch 증가 (v1.0.0 → v1.0.1)
- 모든 변경 시 `sal.py`의 `CURRENT_VERSION`과 `Sboard 접속기.iss`의 `MyAppVersion`을 함께 업데이트

## 아키텍처

- **단일 파일 앱**: 모든 로직이 `sal.py`에 포함
- **GUI 프레임워크**: Tkinter (ttk 위젯)
- **자동화**: `pyautogui`로 입력, `ctypes`로 Windows API 호출
- **API 백엔드**: Cloudflare Workers (`https://sboard-api.sboard-auto-login.workers.dev/api/users`)
- **Cloudflare Worker**: `cf-worker/` 디렉토리 참조

## 폰트 규칙

| 위치 | 폰트 | 크기 |
|---|---|---|
| LoginLogWindow (제목, 로그, 상태) | Consolas | 8pt |
| 메인 창, 버튼, 레이블, 입력창 | Consolas | 12pt |
| 사용자 목록 트리 (데이터 + 헤더) | Consolas | 12pt (행높이 28px) |

## 주요 제약사항

1. **창 종료 동작**: Tkinter 창 닫을 때 `self.root.withdraw()`로 숨김 (완전 종료 안 함). sboard.exe는 독립 프로세스로 유지.
2. **스레드 안전**: Tkinter 작업은 반드시 `_tk_task_queue` 사용. 백그라운드 스레드에서 직접 Tkinter 메서드 호출 금지.
3. **비동기 로그인**: `while` 루프 금지. `self.root.after()`로 비동기 실행 (`_async_find_window` → `_async_input` → `_async_check_result`).
4. **PyInstaller 호환**: `sys.stderr`/`sys.stdout`이 `None`일 때 `os.devnull`로 리다이렉트 처리됨.

## 사용자 관리

- 등록/비밀번호 변경/삭제: `InputDialog` 사용
- 사용자 목록: `ttk.Treeview` (스타일: `rowheight=28`, 폰트 Consolas 12pt)

## 개발 시 주의사항

- ❌ `except:` 블록 비워두면 안 됨 (문법 오류). 반드시 `pass`나 로직 추가.
- ❌ 백그라운드 스레드에서 Tkinter 직접 호출 금지 (스레드 안전한 큐 시스템 사용).
- ✅ 모든 `print("[DEBUG] ..."` 디버그 문은 배포 전 제거.
- ✅ `pyautogui`는 입력용, `ctypes`는 창 탐색용으로 구분 사용.

## 테스트 체크리스트

1. 로그인 성공 시 "로그인 성공!" + 3초 카운트다운 후 Tkinter 창 자동 숨김
2. 로그인 실패 시 "로그인 정보가 일치하지 않습니다." 메시지
3. Tkinter 창 종료 시 sboard.exe는 계속 실행 중
4. PyInstaller로 패키징 후 `RuntimeError: sys.stderr is None` 오류 없음
5. 모든 폰트가 지정대로 적용됨 (Consolas 8pt/12pt)
