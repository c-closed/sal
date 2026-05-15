# -*- coding: utf-8 -*-
# sal.py
"""
⚡ Sboard 접속기 - GUI 버전 (Tkinter)
기능:
1. 로그인 완료 후 3초 카운트다운 및 자동 종료
2. 작업 중 팝업 제거 (메인 창 비활성화 + 로그 창 사용)
3. 모든 창 화면 중앙 배치
4. 동적 아이콘 적용
"""

import io
import json
import os
import re
import sys
import threading
import time
import queue
import subprocess

# PyInstaller 패키징 후 sys.stderr/stdout이 None이 되는 문제 방지
if sys.stderr is None:
    sys.stderr = open(os.devnull, 'w')
if sys.stdout is None:
    sys.stdout = open(os.devnull, 'w')

import faulthandler
faulthandler.enable()

from datetime import datetime
from typing import Optional

# 전역 예외 처리기 (모든 스레드에서 발생하는 예외 잡기)
def global_excepthook(exc_type, exc_value, exc_traceback):
    import traceback
    print("=== 전역 예외 발생 ===", flush=True)
    print(f"Type: {exc_type}", flush=True)
    print(f"Value: {exc_value}", flush=True)
    traceback.print_tb(exc_traceback)
    print("=== 전역 예외 끝 ===", flush=True)
    # 기본 예외 처리기도 호출
    sys.__excepthook__(exc_type, exc_value, exc_traceback)

sys.excepthook = global_excepthook

# 스레드 예외 처리
def thread_excepthook(args):
    import traceback
    print(f"=== 스레드 예외 발생: {args.thread.name} ===", flush=True)
    print(f"Exception: {args.exc_type}", flush=True)
    print(f"Value: {args.exc_value}", flush=True)
    traceback.print_tb(args.exc_traceback)
    print("=== 스레드 예외 끝 ===", flush=True)

threading.excepthook = thread_excepthook

# 프로그램 종료 시 호출되는 핸들러
def atexit_handler():
    print("=== 프로그램 종료 ===", flush=True)
    import traceback
    traceback.print_stack()
    print("=== 종료 끝 ===", flush=True)

import atexit
atexit.register(atexit_handler)

import tkinter as tk
from tkinter import ttk, messagebox, scrolledtext

# 한글 깨짐 방지 (콘솔 + 소스 인코딩)
if sys.platform == "win32":
    os.environ["PYTHONIOENCODING"] = "utf-8"
    try:
        import ctypes
        ctypes.windll.kernel32.SetConsoleOutputCP(65001)
        ctypes.windll.kernel32.SetConsoleCP(65001)
    except:
        pass

TIMEOUT = 10
MAX_MISMATCH_ATTEMPTS = 5
RETRY_ATTEMPTS = 3
RETRY_BASE_DELAY = 0.6

VK_CTRL = 0x11
VK_A = 0x41
VK_TAB = 0x09
VK_ENTER = 0x0D
KEYEVENTF_KEYUP = 0x0002

# =========================
# 설정
# =========================
API_BASE = "https://sboard-api.sboard-auto-login.workers.dev/api/users"
API_META = "https://sboard-api.sboard-auto-login.workers.dev/api/meta"

CURRENT_VERSION = "1.2.0"
REPO_OWNER = "c-closed"
REPO_NAME = "sal"

# =========================
# 유틸 함수
# =========================
def _get_installed_version() -> str:
    import winreg
    subkey = r"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{B8F7A3E2-1D4C-4A9E-8B6F-3C2D5E7A9F1B}_is1"
    for root, path, flags in [
        (winreg.HKEY_LOCAL_MACHINE, subkey, winreg.KEY_READ | 0x100),
        (winreg.HKEY_LOCAL_MACHINE, subkey, winreg.KEY_READ | 0x200),
        (winreg.HKEY_CURRENT_USER, subkey, winreg.KEY_READ),
    ]:
        try:
            key = winreg.OpenKey(root, path, access=flags)
            version, _ = winreg.QueryValueEx(key, "DisplayVersion")
            winreg.CloseKey(key)
            return version
        except (FileNotFoundError, OSError):
            continue
    return CURRENT_VERSION

def _get_icon_path() -> str:
    apd = os.environ.get("LOCALAPPDATA") or os.environ.get("APPDATA") or os.path.expanduser("~")
    path = os.path.join(apd, "Sboard 접속기", "icon.ico")
    if os.path.exists(path):
        return path
    app_dir = os.path.dirname(os.path.abspath(sys.executable if getattr(sys, 'frozen', False) else __file__))
    for sub in ('', '_internal'):
        path = os.path.join(app_dir, sub, "icon.ico")
        if os.path.exists(path):
            return path
    return os.path.join(apd, "Sboard 접속기", "icon.ico")

def _try_set_icon(window):
    """아이콘 적용 시도"""
    path = _get_icon_path()
    if os.path.exists(path):
        try:
            window.iconbitmap(path)
        except tk.TclError:
            pass

def _is_hangul(ch: str) -> bool:
    cp = ord(ch)
    return (0xAC00 <= cp <= 0xD7A3) or (0x1100 <= cp <= 0x11FF) or (0x3131 <= cp <= 0x318E)

def _center_window(window, width: int, height: int):
    """창을 화면 중앙에 위치"""
    ws = window.winfo_screenwidth()
    hs = window.winfo_screenheight()
    x = int((ws / 2) - (width / 2))
    y = int((hs / 2) - (height / 2))
    window.geometry(f"{width}x{height}+{x}+{y}")

# =========================
# API 클라이언트 (Lazy)
# =========================
class SboardApi:
    def __init__(self, base_url: str):
        self.base_url = base_url.rstrip("/")
        self._session = None
        self._requests_mod = None

    def _ensure_session(self):
        if self._session is None:
            import requests
            self._requests_mod = requests
            self._session = requests.Session()
        return self._session, self._requests_mod

    def _request(self, method: str, url: str, **kwargs):
        session, requests = self._ensure_session()
        for attempt in range(1, RETRY_ATTEMPTS + 1):
            try:
                r = session.request(method, url, timeout=TIMEOUT, **kwargs)
                retryable = (r.status_code == 429) or (500 <= r.status_code < 600)
                if retryable and attempt < RETRY_ATTEMPTS:
                    time.sleep(RETRY_BASE_DELAY * (2 ** (attempt - 1)))
                    continue
                return r
            except (requests.ConnectionError, requests.Timeout):
                if attempt < RETRY_ATTEMPTS:
                    time.sleep(RETRY_BASE_DELAY * (2 ** (attempt - 1)))
                    continue
                raise

    def get_users(self) -> dict:
        r = self._request("GET", self.base_url)
        r.raise_for_status()
        data = r.json()
        return data if isinstance(data, dict) else {}

    def get_meta(self) -> dict:
        r = self._request("GET", API_META)
        r.raise_for_status()
        data = r.json()
        return data if isinstance(data, dict) else {}

    def create_user(self, username: str, user_id: str, user_pw: str):
        r = self._request("POST", self.base_url, json={"username": username, "id": user_id, "pw": user_pw})
        r.raise_for_status()

    def update_user_pw_only(self, username: str, user_id: str, new_pw: str):
        r = self._request("PUT", f"{self.base_url}/{username}", json={"id": user_id, "pw": new_pw})
        r.raise_for_status()

    def delete_user(self, username: str):
        r = self._request("DELETE", f"{self.base_url}/{username}")
        r.raise_for_status()

class VersionManager:
    @staticmethod
    def parse_version(version_str: str) -> tuple:
        try:
            clean_ver = version_str.lstrip('v')
            parts = clean_ver.split('.')
            major = int(parts[0]) if len(parts) > 0 else 0
            minor = int(parts[1]) if len(parts) > 1 else 0
            patch = int(parts[2]) if len(parts) > 2 else 0
            return (major, minor, patch)
        except (ValueError, AttributeError, IndexError):
            return (0, 0, 0)

    @staticmethod
    def is_newer(current: str, latest: str) -> bool:
        return VersionManager.parse_version(latest) > VersionManager.parse_version(current)


class GitHubAPIClient:
    # Return values:
    #   dict -> release found
    #   None -> connection/network error
    #   {}   -> no releases yet (404)
    def get_latest_release(self, owner: str, repo: str) -> Optional[dict]:
        api_url = f"https://api.github.com/repos/{owner}/{repo}/releases/latest"
        try:
            import requests
            resp = requests.get(api_url, timeout=10)
            if resp.status_code == 404:
                return {}  # No releases yet (non-fatal)
            resp.raise_for_status()
            return resp.json()
        except:
            return None  # Connection error


# =========================
# Win32 입력 도우미 (Lazy)
# =========================
class Win32Input:
    _user32 = None

    @classmethod
    def user32(cls):
        if cls._user32 is None:
            import ctypes
            cls._user32 = ctypes.windll.user32
        return cls._user32

    @classmethod
    def press_key(cls, vk: int):
        u = cls.user32()
        u.keybd_event(vk,0,0,0)
        time.sleep(0.02)
        u.keybd_event(vk,0, KEYEVENTF_KEYUP,0)
        time.sleep(0.02)

    @classmethod
    def type_text(cls, text: str):
        u = cls.user32()
        for ch in text:
            vk = ord(ch.upper())
            u.keybd_event(vk,0,0,0)
            u.keybd_event(vk,0, KEYEVENTF_KEYUP,0)
            time.sleep(0.01)

    @classmethod
    def ctrl_a(cls):
        u = cls.user32()
        u.keybd_event(VK_CTRL,0,0,0)
        u.keybd_event(VK_A,0,0,0)
        u.keybd_event(VK_A,0, KEYEVENTF_KEYUP,0)
        u.keybd_event(VK_CTRL,0, KEYEVENTF_KEYUP,0)
        time.sleep(0.02)

# =========================
# 로그인 로그 팝업 (Topmost + 카운트다운)
# =========================
class LoginLogWindow:
    def __init__(self, master, username: str):
        self.root = tk.Toplevel(master)
        self.root.title(f"{username} 로그인 진행")
        _center_window(self.root, 380, 240)
        _try_set_icon(self.root)
        self.root.resizable(False, True)
        self.root.attributes("-topmost", True)
        self.root.transient(master)
        
        # 로그 영역만 (Consolas 8pt) - 레이블 없음
        self.log_text = scrolledtext.ScrolledText(self.root, wrap=tk.WORD, state="disabled", height=10, font=("Consolas", 8))
        self.log_text.pack(fill="both", expand=True, padx=12, pady=(12, 8))
    
    def log(self, msg: str):
        ts = datetime.now().strftime("%H:%M:%S")
        self.log_text.config(state="normal")
        self.log_text.insert(tk.END, f"[{ts}] {msg}\n")
        self.log_text.see(tk.END)
        self.log_text.config(state="disabled")
    
    def set_status(self, msg: str, color: str = "black"):
        self.status_label.config(text=msg, foreground=color)
    
    def start_countdown(self):
        """3초 카운트다운 후 창 닫기"""
        def _tick(seconds_left: int):
            if seconds_left <= 0:
                self.root.destroy()
                return
            self.log(f"{seconds_left}초 후에 창이 종료됩니다.")
            self.root.after(1000, lambda: _tick(seconds_left - 1))
        
        _tick(3)

# =========================
# 커스텀 입력 다이얼로그
# =========================
class InputDialog:
    """여러 입력 필드를 받는 커스텀 모달 다이얼로그"""
    def __init__(self, parent, title, fields, check_func=None):
        self.parent = parent
        self.title = title
        self.fields = fields
        self.check_func = check_func
        self.result = None

        self.win = tk.Toplevel(parent)
        self.win.title(title)
        self.win.transient(parent)
        self.win.grab_set()
        self.win.resizable(False, False)

        frame = ttk.Frame(self.win, padding=(15, 15, 15, 5))
        frame.pack()

        self.entries = {}
        vcmd_hangul = self.win.register(lambda p: all(_is_hangul(c) for c in p) if p else True)
        vcmd_digit = self.win.register(lambda p: p.isdigit() if p else True)
        for i, f in enumerate(fields):
            lbl = ttk.Label(frame, text=f["label"])
            lbl.config(font=("맑은 고딕", 11))
            lbl.grid(row=i, column=0, sticky="w", pady=4)
            show_char = "*" if f.get("show") else ""
            ent = ttk.Entry(frame, show=show_char)
            ent.grid(row=i, column=1, sticky="ew", padx=(8,0), pady=4)
            ent.config(font=("맑은 고딕", 11))
            if f["key"] == "name":
                ent.config(validate="key", validatecommand=(vcmd_hangul, "%P"))
            elif f["key"] == "uid":
                ent.config(validate="key", validatecommand=(vcmd_digit, "%P"))
            self.entries[f["key"]] = ent
            self.win.bind("<Return>", lambda e: self.on_ok())

        target = self.entries.get("name") or next(iter(self.entries.values()))
        target.focus()

        btn_frame = ttk.Frame(frame)
        btn_frame.grid(row=len(fields), column=0, columnspan=2, pady=(8,5))
        ttk.Button(btn_frame, text="확인", command=self.on_ok).pack(side="left", padx=12)
        ttk.Button(btn_frame, text="취소", command=self.win.destroy).pack(side="left", padx=12)

        self.win.bind("<Escape>", lambda e: self.win.destroy())

        # Size to content
        self.win.update_idletasks()
        w = max(280, self.win.winfo_reqwidth())
        h = self.win.winfo_reqheight()
        ws = self.win.winfo_screenwidth()
        hs = self.win.winfo_screenheight()
        self.win.geometry(f"{w}x{h}+{(ws - w) // 2}+{(hs - h) // 2}")
    
    def on_ok(self):
        values = {key: ent.get() for key, ent in self.entries.items()}
        if self.check_func:
            err = self.check_func(values)
            if err:
                messagebox.showerror("입력 오류", err, parent=self.win)
                return
        self.result = values
        self.win.destroy()
    
    def show(self):
        self.win.wait_window(self.win)
        return self.result

# =========================
# 업데이트 확인 창
# =========================
class UpdateLogWindow:
    def __init__(self):
        self.root = tk.Tk()
        self.root.title("업데이트 확인")
        _center_window(self.root, 380, 170)
        _try_set_icon(self.root)
        self.root.resizable(False, False)

        self.log_text = scrolledtext.ScrolledText(self.root, wrap=tk.WORD, state="disabled", font=("Consolas", 8))
        self.log_text.pack(fill="both", expand=True, padx=12, pady=(12, 8))

        self.should_launch = True
        self._done = False
        self.root.protocol("WM_DELETE_WINDOW", self._on_close)
        self.root.bind("<Escape>", lambda e: self._on_close())
        self.root.after(100, self._start)

    def _on_close(self):
        self.should_launch = False
        self._done = True
        self.root.quit()

    def _start(self):
        threading.Thread(target=self._worker, daemon=True).start()
        self.root.after(300, self._poll)

    def _poll(self):
        if self._done:
            self.root.quit()
        else:
            self.root.after(200, self._poll)

    def _log(self, msg: str):
        if self._done:
            return
        try:
            ts = datetime.now().strftime("%H:%M:%S")
            self.log_text.config(state="normal")
            self.log_text.insert(tk.END, f"[{ts}] {msg}\n")
            self.log_text.see(tk.END)
            self.log_text.config(state="disabled")
        except tk.TclError:
            pass

    def _worker(self):
        try:
            self._log("서버에 연결중")
            client = GitHubAPIClient()
            data = client.get_latest_release(REPO_OWNER, REPO_NAME)
            if data is None:
                self._log("서버에 연결할 수 없습니다.")
                time.sleep(1.5)
                self.should_launch = False
                self._done = True
                return
            if not data:
                self._log("저장소에 릴리스가 없습니다.")
                time.sleep(1)
                self.should_launch = True
                self._done = True
                return

            self._log("서버에 연결되었습니다.")
            latest_ver = data.get("tag_name", "v0.0.0")
            self._log(f"최신버전 : {latest_ver}")
            current_ver = _get_installed_version()
            self._log(f"현재버전 : v{current_ver}")
            time.sleep(0.5)
            if not VersionManager.is_newer(current_ver, latest_ver):
                self._log("최신버전입니다.")
                time.sleep(0.5)
                for i in range(3, 0, -1):
                    self._log(f"{i}초 후 프로그램이 시작됩니다.")
                    time.sleep(1)
                self.should_launch = True
            else:
                self._log("업데이트를 시작합니다.")
                time.sleep(0.5)
                import webbrowser
                webbrowser.open(data.get("html_url", ""))
                self.should_launch = False
        except Exception as e:
            self._log(f"오류: {e}")
            time.sleep(1.5)
            self.should_launch = True
        finally:
            self._done = True

    def run(self):
        self.root.mainloop()
        self.root.destroy()


# =========================
# GUI 애플리케이션
# =========================
class SboardGUI:
    def __init__(self):
        self.api = SboardApi(API_BASE)
        self.users_cache: dict[str, dict] = {}
        self._busy = False
        self._log_window = None
        self.sboard_pid = None
        self._log_queue = queue.Queue()  # 스레드 안전한 로그 큐
        self._tk_task_queue = queue.Queue()  # 스레드 안전한 Tkinter 작업 큐
        
        # Tkinter 초기화 + 폰트 설정 (Consolas 12pt)
        self.root = tk.Tk()
        self.root.title("Sboard 접속기")
        # ttk 전체 위젯에 폰트 적용 (TclError 방지)
        style = ttk.Style()
        style.configure(".", font=("맑은 고딕", 9))
        _try_set_icon(self.root)
        self.root.resizable(False, False)
        
        # 창 종료 프로토콜 설정
        self.root.protocol("WM_DELETE_WINDOW", self._on_close)
        
        # 로그 큐 폴링 타이머 시작
        self._poll_queues()
        
        # 상단 메뉴바
        menubar = tk.Menu(self.root, tearoff=0)
        menubar.add_command(label="사용자 목록", command=self.show_users_list)
        
        manage_menu = tk.Menu(menubar, tearoff=0)
        manage_menu.add_command(label="사용자 등록", command=lambda: self._register_action(self.root))
        manage_menu.add_command(label="PW 변경", command=lambda: self._change_pw_action(self.root))
        manage_menu.add_command(label="사용자 삭제", command=lambda: self._delete_action(self.root))
        menubar.add_cascade(label="사용자 관리", menu=manage_menu)
        
        self.root.config(menu=menubar)
        
        # 메인 프레임
        frame = ttk.LabelFrame(self.root, text="자동 로그인", padding=(15, 12, 15, 8))
        frame.pack(fill="x", padx=8, pady=(8, 5))
        
        ttk.Label(frame, text="사용자명", font=("맑은 고딕", 9)).pack(anchor="w")
        
        self.login_entry = ttk.Entry(frame, justify="center", font=("맑은 고딕", 9))
        vcmd = self.root.register(lambda p: all(_is_hangul(c) for c in p) if p else True)
        self.login_entry.config(validate="key", validatecommand=(vcmd, "%P"))
        self.login_entry.pack(fill="x", ipady=5, pady=(0,5))
        self.login_entry.bind("<Return>", lambda e: self.do_login())
        
        self.login_btn = ttk.Button(frame, text="로그인", command=self.do_login)
        self.login_btn.pack(fill="x", ipady=3)
        
        # Size to content
        self.root.update_idletasks()
        w = max(300, self.root.winfo_reqwidth())
        h = self.root.winfo_reqheight()
        ws = self.root.winfo_screenwidth()
        hs = self.root.winfo_screenheight()
        self.root.geometry(f"{w}x{h}+{(ws - w) // 2}+{(hs - h) // 2}")
        self.login_entry.focus()
    
    def _poll_queues(self):
        """메인 스레드에서 로그 큐와 Tkinter 작업 큐를 폴링"""
        # 로그 큐 처리
        try:
            while True:
                msg = self._log_queue.get_nowait()
                if self._log_window and hasattr(self._log_window, 'log'):
                    self._log_window.log(msg)
        except queue.Empty:
            pass
        # Tkinter 작업 큐 처리 (메인 스레드에서 실행)
        try:
            while True:
                task = self._tk_task_queue.get_nowait()
                task()
        except queue.Empty:
            pass
        # 100ms마다 다시 폴링
        self.root.after(100, self._poll_queues)
    
    def _log(self, msg: str):
        """스레드 안전한 로그 메서드 - 큐에 메시지만 전달"""
        self._log_queue.put(msg)
    
    def _set_busy(self, busy: bool):
        self._busy = busy
        state = "disabled" if busy else "normal"
        self.login_btn.config(state=state)
        self.login_entry.config(state=state)
    
    def do_login(self):
        username = self.login_entry.get().strip()
        if not re.fullmatch(r"^[가-힣]+$", username):
            messagebox.showwarning("입력 오류", "사용자명은 한글로만 입력해주세요.", parent=self.root)
            return
        if self._busy:
            return
        
        # 로그 창 열기
        self._log_window = LoginLogWindow(self.root, username)
        self._set_busy(True)
        
        # 로그인 작업 시작 (스레드 사용)
        threading.Thread(target=self._login_worker, args=(username,), daemon=False).start()
    
    def _login_worker(self, username: str):
        if not self._log_window:
            return
        try:
            self._log("서버 연결 중...")
            if not self.users_cache or username not in self.users_cache:
                data = self.api.get_users()
                self.users_cache = data.get("users", data)
            
            if username not in self.users_cache:
                self._log(f"'{username}' 은 등록되지 않았습니다.")
                return
            
            info = self.users_cache[username]
            uid = info.get("id", "")
            upw = info.get("pw", "")
            self._log(f"사용자 확인됨 (ID: {uid})")
            
            # Sboard 로그인 자동화 실행
            self._run_sboard_login(username, uid, upw)
            
        except Exception as e:
            self._set_status(f"오류: {e}", "red")
            self._log(f"예외 발생: {e}")
        finally:
            self._tk_task_queue.put(lambda: self._set_busy(False))
    
    def _run_sboard_login(self, username, uid, upw):
        try:
            self._log("기존 Sboard 세션 확인...")
            
            import pyautogui
            import ctypes
            user32 = ctypes.windll.user32
            
            # 기존 Sboard 창 닫기
            try:
                hwnd = user32.FindWindowW(None, "Sboard")
                if hwnd != 0:
                    user32.SendMessageW(hwnd, 0x0010, 0, 0)  # WM_CLOSE
                time.sleep(0.5)
            except Exception as e:
                self._log(f"창 정리 중 오류: {e}")
            
            self._log("Sboard 실행...")
            exe_paths = [
                os.path.join(os.path.dirname(sys.executable), "sboard.exe"),
                os.path.join(os.path.dirname(sys.executable), "Sboard.exe"),
                r"C:\Program Files (x86)\sprog\sboard.exe",
                r"C:\Program Files\sprog\sboard.exe"
            ]
            launched = False
            for p in exe_paths:
                if os.path.exists(p):
                    subprocess.Popen(
                        [p],
                        creationflags=subprocess.DETACHED_PROCESS | subprocess.CREATE_NEW_PROCESS_GROUP,
                        stdin=subprocess.DEVNULL,
                        stdout=subprocess.DEVNULL,
                        stderr=subprocess.DEVNULL
                    )
                    launched = True
                    break
            if not launched:
                self._log("Sboard 실행 파일을 찾지 못함")
                self._log("환경변수 SBOARD_EXE_PATH를 설정하거나 실행 파일을 확인하세요.")
                return
            
            # 비동기식으로 창 탐색 및 로그인 진행
            self._login_state = {
                "username": username,
                "uid": uid,
                "upw": upw,
                "user32": user32,
                "start_time": time.time(),
                "window_found": False,
                "input_done": False
            }
            self._async_find_window()
            
        except BaseException as e:
            import traceback
            traceback.print_exc()
            self._log(f"로그인 처리 중 예외 발생: {e}")
            if self._log_window:
                self._tk_task_queue.put(lambda: self._log_window.log("로그인 중 오류가 발생했습니다."))
                self._tk_task_queue.put(self._log_window.start_countdown)
    
    def _async_find_window(self):
        """비동기식 창 탐색 - pyautogui 사용"""
        state = self._login_state
        user32 = state["user32"]
        
        try:
            import pyautogui
            windows = pyautogui.getWindowsWithTitle("Sboard")
            for w in windows:
                if w.title.strip() == "Sboard":
                    state["window_found"] = True
                    state["window"] = w
                    self._log("로그인 창 발견, 포커스 이동")
                    
                    # PID 저장
                    try:
                        hwnd = w._hWnd
                        pid = ctypes.c_ulong()
                        user32.GetWindowThreadProcessId(hwnd, ctypes.byref(pid))
                        self.sboard_pid = pid.value
                    except Exception as pe:
                        self.sboard_pid = None
                    
                    # 입력 시작
                    self.root.after(100, self._async_input)
                    return
        except Exception as e:
            self._log(f"창 탐색 오류: {e}")
        
        # 5초 동안 탐색
        if time.time() - state["start_time"] < 5.0:
            self.root.after(100, self._async_find_window)
        else:
            self._log("로그인 창 탐색 실패")
            self._log("Sboard 로그인 창('Sboard')을 찾지 못했습니다.")
            return
    
    def _async_input(self):
        """비동기식 입력 - pyautogui 사용"""
        state = self._login_state
        if state.get("input_done"):
            return
            
        self._log("정보 입력 중...")
        
        import pyautogui
        
        # 포커스 이동
        try:
            w = state["window"]
            w.activate()
            w.focus()
            time.sleep(0.2)
        except:
            pass
        
        # 탭 이동
        pyautogui.press('tab')
        time.sleep(0.05)
        pyautogui.press('tab')
        time.sleep(0.05)
        
        self._log("ID 입력")
        pyautogui.typewrite(state["uid"], interval=0.01)
        time.sleep(0.05)
        
        pyautogui.press('tab')
        time.sleep(0.05)
        
        self._log("PW 입력")
        pyautogui.hotkey('ctrl', 'a')
        time.sleep(0.05)
        pyautogui.typewrite(state["upw"], interval=0.01)
        time.sleep(0.1)
        
        self._log("로그인 엔터 입력")
        pyautogui.press('enter')
        self._log("로그인 진행 중...")
        state["input_done"] = True
        
        # 결과 확인 시작
        state["start_time"] = time.time()
        self.root.after(200, self._async_check_result)
    
    def _async_check_result(self):
        """비동기식 결과 확인 - pyautogui로 성공 감지"""
        state = self._login_state
        
        # 1. 성공 확인 (Sboard [xxx] 창) - pyautogui 사용
        try:
            import pyautogui
            windows = pyautogui.getWindowsWithTitle("Sboard")
            for w in windows:
                if w.title.startswith("Sboard ["):
                    self._log("로그인 성공!")
                    if self._log_window:
                        self._tk_task_queue.put(self._log_window.start_countdown)
                    return
        except Exception as e:
            self._log(f"성공 확인 중 오류: {e}")
        
        # 2. 실패 팝업 확인
        if self.sboard_pid:
            try:
                dialog = self._detect_information_dialog()
                if dialog:
                    try:
                        ctypes.windll.user32.SetForegroundWindow(dialog["hwnd"])
                        time.sleep(0.05)
                        import pyautogui
                        pyautogui.press('enter')
                    except:
                        pass
                    self._log("로그인 실패 (정보 불일치)")
                    if self._log_window:
                        self._tk_task_queue.put(lambda: self._log_window.log("로그인 정보가 일치하지 않습니다."))
                    return
            except Exception as e:
                self._log(f"팝업 확인 오류: {e}")
        
        # 6초 타임아웃 확인
        if time.time() - state["start_time"] < 6.0:
            self.root.after(200, self._async_check_result)
        else:
            # 타임아웃
            self._log("로그인 실패 (시간 초과)")
            try:
                import pyautogui
                windows = pyautogui.getWindowsWithTitle("Sboard")
                titles = [w.title for w in windows]
                self._log(f"현재 Sboard 창 목록: {titles}")
                self._log(f"sboard_pid: {self.sboard_pid}")
            except Exception as e:
                self._log(f"창 목록 확인 오류: {e}")
            if self._log_window:
                self._tk_task_queue.put(lambda: self._log_window.log("로그인 정보가 일치하지 않습니다."))
    
    # =========================
    # Win32 헬퍼 (팝업 감지용)
    # =========================
    def _win_get_text(self, hwnd: int) -> str:
        import ctypes
        user32 = ctypes.windll.user32
        n = user32.GetWindowTextLengthW(hwnd)
        buf = ctypes.create_unicode_buffer(n + 1)
        user32.GetWindowTextW(hwnd, buf, n + 1)
        return (buf.value or "").strip()
    
    def _win_get_pid(self, hwnd: int) -> int:
        import ctypes
        pid = ctypes.c_ulong(0)
        ctypes.windll.user32.GetWindowThreadProcessId(hwnd, ctypes.byref(pid))
        return int(pid.value)
    
    def _detect_information_dialog(self):
        pid = self.sboard_pid
        if not pid: return None
        import ctypes
        user32 = ctypes.windll.user32
        user32 = ctypes.windll.user32
        EnumWindowsProc = ctypes.WINFUNCTYPE(ctypes.c_bool, ctypes.c_void_p, ctypes.c_void_p)
        
        result = {"hwnd": None}
        
        @EnumWindowsProc
        def cb(hwnd, lparam):
            try:
                if not user32.IsWindowVisible(hwnd): return True
                if self._win_get_pid(int(hwnd)) != pid: return True
                title = self._win_get_text(int(hwnd))
                if title == "Information":
                    result["hwnd"] = int(hwnd)
            except:
                pass
            return True
        
        user32.EnumWindows(cb, 0)
        return result if result["hwnd"] else None
    
    # =========================
    # 메뉴 기능
    # =========================
    def show_users_list(self):
        list_win = tk.Toplevel(self.root)
        list_win.title("사용자 목록")
        _center_window(list_win, 280, 220)
        _try_set_icon(list_win)
        list_win.transient(self.root)
        
        frame = ttk.Frame(list_win, padding=(12, 12, 12, 5))
        frame.pack(fill="both", expand=True)

        loading_label = ttk.Label(frame, text="불러오는 중입니다...", font=("맑은 고딕", 11))
        loading_label.pack(expand=True)
        
        tree = ttk.Treeview(frame, columns=("name", "id"), show="headings", height=8)
        tree.heading("name", text="이름")
        tree.heading("id", text="ID")
        tree.column("name", width=100, anchor="center")
        tree.column("id", width=100, anchor="center")
        
        # 12pt 폰트에 맞게 행 높이 조정 (ttk 스타일)
        style = ttk.Style()
        style.configure("Treeview", rowheight=28, font=("맑은 고딕", 11))
        style.configure("Treeview.Heading", font=("맑은 고딕", 11, "bold"))
        
        list_win.bind("<Escape>", lambda e: list_win.destroy())
        
        def _on_success(items):
            loading_label.pack_forget()
            tree.pack(fill="both", expand=True, padx=8, pady=8)
            tree.delete(*tree.get_children())
            for name, uid in items:
                tree.insert("", "end", values=(name, uid))
        
        def _on_failure(error_msg):
            loading_label.config(text=f"오류: {error_msg}", foreground="red")
        
        def _do_fetch():
            try:
                data = self.api.get_users()
                users = data.get("users", data)
                items = [(name, users[name]["id"]) for name in sorted(users.keys())]
                list_win.after(0, lambda: _on_success(items))
            except Exception as e:
                list_win.after(0, lambda: _on_failure(str(e)))
        
        def start_loading():
            loading_label.config(text="불러오는 중입니다...", foreground="black")
            loading_label.pack(expand=True)
            tree.pack_forget()
            threading.Thread(target=_do_fetch, daemon=True).start()
        
        start_loading()
    
    def _register_action(self, parent):
        dlg = InputDialog(parent, "사용자 등록", [
            {"label": "이름", "key": "name"},
            {"label": "ID", "key": "uid"},
            {"label": "PW", "key": "pw", "show": True}
        ])
        res = dlg.show()
        if res:
            try:
                self.api.create_user(res["name"], res["uid"], res["pw"])
                messagebox.showinfo("성공", "등록 완료!", parent=parent)
                self.users_cache.clear()
            except Exception as e:
                messagebox.showerror("오류", str(e), parent=parent)
    
    def _change_pw_action(self, parent):
        # Step 1: Verify current credentials
        dlg = InputDialog(parent, "PW 변경 - 본인 확인", [
            {"label": "이름", "key": "name"},
            {"label": "ID", "key": "uid"},
            {"label": "현재 PW", "key": "pw", "show": True}
        ])
        res = dlg.show()
        if not res:
            return

        try:
            data = self.api.get_users()
            users = data.get("users", data)
        except Exception as e:
            messagebox.showerror("오류", f"서버 연결 실패: {e}", parent=parent)
            return

        name, uid, pw = res["name"], res["uid"], res["pw"]
        if name not in users or users[name]["id"] != uid or users[name]["pw"] != pw:
            messagebox.showerror("오류", "사용자 정보가 일치하지 않습니다.", parent=parent)
            return

        # Step 2: Enter new password
        dlg2 = InputDialog(parent, "PW 변경 - 새 비밀번호", [
            {"label": "새 PW", "key": "new_pw", "show": True}
        ], check_func=lambda v: (
            "새 PW를 입력해주세요." if not v.get("new_pw") else None
        ))
        res2 = dlg2.show()
        if not res2:
            return

        try:
            self.api.update_user_pw_only(name, uid, res2["new_pw"])
            messagebox.showinfo("성공", "변경 완료!", parent=parent)
            self.users_cache.clear()
        except Exception as e:
            messagebox.showerror("오류", str(e), parent=parent)
    
    def _delete_action(self, parent):
        try:
            data = self.api.get_users()
            self.users_cache = data.get("users", data)
        except:
            pass
        dlg = InputDialog(parent, "사용자 삭제", [
            {"label": "이름", "key": "name"},
            {"label": "ID", "key": "uid"},
            {"label": "PW", "key": "pw", "show": True}
        ], check_func=lambda v: (
            "모든 값을 입력해주세요." if not all([v.get("name"), v.get("uid"), v.get("pw")]) else
            "사용자 정보가 일치하지 않습니다." if v["name"] not in self.users_cache or self.users_cache[v["name"]]["id"] != v["uid"] or self.users_cache[v["name"]]["pw"] != v["pw"] else None
        ))
        res = dlg.show()
        if res:
            if messagebox.askyesno("확인", f"{res['name']} 님을 삭제하시겠습니까?", parent=parent):
                try:
                    self.api.delete_user(res["name"])
                    messagebox.showinfo("성공", "삭제 완료!", parent=parent)
                    self.users_cache.clear()
                except Exception as e:
                    messagebox.showerror("오류", str(e), parent=parent)
    
    def _on_close(self):
        self.root.destroy()
    
    def run(self):
        self.root.mainloop()

if __name__ == "__main__":
    update_win = UpdateLogWindow()
    update_win.run()
    time.sleep(0.5)
    if update_win.should_launch:
        app = SboardGUI()
        app.run()
