# Cloudflare Workers로 Sboard API 이전하기
## 초보자용 단계별 가이드

---

## 📋 개요

현재 PythonAnywhere에서 실행 중인 API 서버를 **Cloudflare Workers**로 옮깁니다.

**장점:**
- ✅ 완전 무료 (하루 10만 요청)
- ✅ 슬립 없음 (24시간 항상 켜 있음)
- ✅ 한국에서 빠름 (전 세계 CDN)
- ✅ 카드 등록 불필요

**작동 방식:**
- Worker = 서버 코드 (JavaScript)
- D1 데이터베이스 = 사용자 정보 저장 (SQLite 기반)

---

## 단계 1: Cloudflare 계정 만들기

### 1-1. Cloudflare 가입

1. 브라우저에서 https://dash.cloudflare.com/sign-up 열기
2. 이메일 주소와 비밀번호 입력
3. 이메일 인증 클릭
4. 비밀번호 설정 완료

### 1-2. 무료 플랜 확인

- 가입 시 자동으로 **Free Plan** 적용됨
- 카드 등록 **불필요**
- Worker: 하루 100,000 요청 무료
- D1: 하루 500만 읽기, 10만 쓰기 무료

---

## 단계 2: Node.js 설치 (Wrangler 도구용)

Cloudflare Workers를 배포하려면 **Wrangler**라는 CLI 도구가 필요합니다.
Wrangler는 Node.js 기반으로 동작하므로 먼저 Node.js를 설치합니다.

### 2-1. Node.js 다운로드

1. https://nodejs.org/ 접속
2. **LTS** 버전 (오른쪽 초록 버튼) 다운로드
   - 예: "Recommended For Most Users"
3. 다운로드한 설치 파일 실행

### 2-2. 설치 진행

1. 설치 마법사에서 모두 "Next" 클릭
2. **"Automatically install the necessary tools"** 체크박스 → 체크 안 해도 됨
3. 설치 완료 후 "Close"

### 2-3. 설치 확인

1. **Windows 검색** → `cmd` 검색 → **명령 프롬프트** 실행
2. 다음 명령어 입력:
   ```
   node -v
   ```
3. `v20.x.x` 또는 `v22.x.x` 같은 버전이 나오면 성공
4. 다음 명령어도 확인:
   ```
   npm -v
   ```
5. `10.x.x` 또는 `9.x.x` 같은 버전이 나오면 성공

---

## 단계 3: Wrangler 설치 및 로그인

### 3-1. 프로젝트 폴더로 이동

1. 명령 프롬프트(cmd)를 다시 엽니다.
2. 프로젝트 폴더로 이동:
   ```
   cd "D:\99. 개인자료\00. 개발\sal\cf-worker"
   ```

### 3-2. Wrangler 설치

```
npm install
```

- package.json에 정의된 Wrangler가 설치됩니다.
- `node_modules` 폴더가 생성되고 설치가 완료됩니다.

### 3.3. Cloudflare 로그인

```
npx wrangler login
```

1. 실행하면 브라우저가 자동으로 열림
2. Cloudflare 계정으로 로그인
3. "Allow" 버튼 클릭하여 권한 부여
4. 명령 프롬프트에 `Successfully logged in` 메세지 확인

---

## 단계 4: D1 데이터베이스 생성

D1은 Cloudflare의 SQLite 기반 데이터베이스입니다.
사용자 정보(이름, ID, PW)를 여기에 저장합니다.

### 4-1. 데이터베이스 생성

```
npx wrangler d1 create sboard-db
```

- 실행하면 다음과 같은 결과가 나옵니다:
  ```
  ✅ Successfully created DB 'sboard-db'
  database_id = "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
  ```
- **database_id를 메모장에 복사해 두세요** (다음 단계에서 필요합니다)

### 4-2. wrangler.toml 설정 수정

1. `D:\99. 개인자료\00. 개발\sal\cf-worker\wrangler.toml` 파일을 메모장이나 VS Code로 엽니다.

2. 파일 하단의 `[[ d1_databases ]]` 부분을 찾아 다음과 같이 수정합니다:

   ```toml
   [[ d1_databases ]]
   binding = "DB"
   database_name = "sboard-db"
   database_id = "방금 복사한 database_id를 여기에 붙여넣기"
   preview_database_id = "D1_LOCAL_ID"
   ```

   - `database_id` 부분에 **4-1에서 복사한 ID**를 넣으세요
   - 예: `database_id = "a1b2c3d4-..."`

3. 파일 저장 (Ctrl+S)

### 4-3. 테이블 생성 (로컬)

```
npx wrangler d1 execute sboard-db --local --file migrations/001_init.sql
```

- `users` 테이블이 생성됩니다.

### 4-4. 테이블 생성 (실제 서버/remote)

```
npx wrangler d1 execute sboard-db --remote --file migrations/001_init.sql
```

- 실제 Cloudflare 서버에 `users` 테이블이 생성됩니다.
- 이 명령은 한 번만 실행하면 됩니다.

---

## 단계 5: Worker 코드 배포

### 5-1. 배포 전 확인

프로젝트 폴더 구조가 다음과 같은지 확인:

```
cf-worker/
├── package.json           ← 이미 있음
├── wrangler.toml          ← database_id 수정 완료
├── migrations/
│   └── 001_init.sql       ← 이미 있음
└── src/
    └── index.js           ← 이미 있음
```

### 5-2. 배포 명령

```
npx wrangler deploy
```

- 첫 배포 시 몇 분 걸릴 수 있습니다.
- 성공하면 다음과 같은 URL이 나옵니다:
  ```
  https://sboard-api.<당신의계정>.workers.dev
  ```
- **이 URL을 메모장에 복사하세요!** (Python 스크립트 수정에 필요합니다)

### 5-3. 배포 확인

브라우저에서 다음 URL에 접속해 테스트:

```
https://sboard-api.<당신의계정>.workers.dev/api/users
```

- `{"users":{},"last_updated":"..."}` 같은 응답이 나오면 성공!

---

## 단계 6: Python 스크립트 수정

`D:\99. 개인자료\00. 개발\sal\sal.py` 파일에서 API URL을 변경합니다.

### 6-1. sal.py 열기

VS Code나 메모장으로 `sal.py`를 엽니다.

### 6-2. URL 변경

**기존:**
```python
API_BASE = "https://sboardautologin.pythonanywhere.com/api/users"
API_META = "https://sboardautologin.pythonanywhere.com/api/meta"
```

**변경 후:**
```python
API_BASE = "https://sboard-api.<당신의계정>.workers.dev/api/users"
API_META = "https://sboard-api.<당신의계정>.workers.dev/api/meta"
```

- `<당신의계정>` 부분에 **5-2에서 복사한 실제 계정 이름**을 넣으세요.
- `API_META`을 별도로 유지하지 않아도 됩니다. Worker 코드에서 `/api/meta` 경로가 이미 처리됩니다.

### 6-3. 응답 형식 호환성 확인

**중요:** PythonAnywhere API는 `GET /api/users`에서 직접 `{"홍길동": {"id": "123", "pw": "456"}, ...}` 형식으로 반환했지만,
Cloudflare Worker는 `{"users": {...}, "last_updated": "..."}` 형식으로 반환합니다.

`sal.py`에서 `load_users()` 함수가 이를 처리하도록 수정이 필요합니다.

**sal.py에서 `load_users` 함수를 찾아 수정하세요:**

**기존:**
```python
def load_users(self, quiet: bool = False):
    self.users = self.api.get_users()
```

**수정:**
```python
def load_users(self, quiet: bool = False):
    data = self.api.get_users()
    self.users = data.get("users", data)
```

이렇게 하면 기존 형식과 새 형식 모두 호환됩니다.

---

## 단계 7: 기존 데이터 이전

PythonAnywhere에 저장된 기존 사용자 데이터를 새 Cloudflare DB로 옮깁니다.

### 7-1. 기존 데이터 확인

브라우저에서 현재 PythonAnywhere API로 접속:
```
https://sboardautologin.pythonanywhere.com/api/users
```

- 브라우저에 `{"홍길동": {"id": "123", "pw": "456"}, ...}` 같은 JSON이 표시됩니다.
- 이 내용을 **전부 복사**하세요.

### 7-2. 데이터 이전 스크립트 실행

프로젝트 폴더에 임시 스크립트를 만들어 데이터를 이전합니다:

```
cd "D:\99. 개인자료\00. 개발\sal"
```

다음 Python 스크립트를 `migrate_data.py`로 저장하고 실행:

```python
import requests

# 새 Cloudflare Worker URL (변경 필요)
BASE_URL = "https://sboard-api.<당신의계정>.workers.dev/api/users"

# PythonAnywhere에서 복사한 기존 데이터
OLD_USERS = {
    "홍길동": {"id": "123", "pw": "456"},
    # ... 여기에 모든 사용자 데이터 추가
}

print("데이터 이전 시작...")
for username, info in OLD_USERS.items():
    r = requests.post(BASE_URL, json={
        "username": username,
        "id": info["id"],
        "pw": info["pw"],
    })
    print(f"  {username}: {r.status_code}")
print("완료!")
```

**⚠ 주의:** `BASE_URL`과 `OLD_USERS`를 실제 데이터로 바꿔야 합니다.

### 7-3. 이전 확인

브라우저에서 새 API로 접속 확인:
```
https://sboard-api.<당신의계정>.workers.dev/api/users
```

- `{"users": {"홍길동": {"id": "123", "pw": "456"}, ...}, ...}` 가 나오면 성공!

---

## 단계 8: 테스트

### 8-1. sal.py 실행

```
python sal.py
```

1. 로그인 테스트: 기존 사용자 이름으로 로그인 시도
2. 사용자 관리: 등록/삭제/PW 변경 테스트
3. 사용자 목록 보기: 목록이 정상적으로 나오는지 확인

### 8-2. 문제 해결

| 증상 | 해결 방법 |
|------|-----------|
| `ConnectionError` | Worker URL이 맞는지 확인 |
| `404 Not Found` | wrangler.toml에서 D1 바인딩 확인 |
| 사용자 목록이 비어있음 | 7단계 데이터 이전 확인 |
| `{"error":"Not Found"}` | URL에 `/api/` 경로가 있는지 확인 |

---

## 📌 자주 쓰는 명령어 모음

### 배포 (코드 수정 후)
```
cd "D:\99. 개인자료\00. 개발\sal\cf-worker"
npx wrangler deploy
```

### 로컬에서 테스트
```
npx wrangler dev
```
- http://localhost:8787 에서 테스트 가능

### 데이터베이스 쿼리 (서버)
```
npx wrangler d1 execute sboard-db --remote --command "SELECT * FROM users"
```

### 데이터베이스 쿼리 (로컬)
```
npx wrangler d1 execute sboard-db --local --command "SELECT * FROM users"
```

### 로그 보기
```
npx wrangler tail
```

---

## 🔄 코드 수정 후 재배포 방법

Worker 코드(`src/index.js`)를 수정한 후:

1. 파일 저장
2. 명령 프롬프트에서:
   ```
   cd "D:\99. 개인자료\00. 개발\sal\cf-worker"
   npx wrangler deploy
   ```
3. 몇 초 안에 적용 완료

---

## 💡 팁

- **wrangler dev**: 로컬에서 테스트 가능 (배포 전 확인용)
- **wrangler tail**: 실시간 로그 확인
- **D1은 SQLite**: 표준 SQL 대부분 사용 가능
- **무료 한도**: 하루 10만 요청 → 개인/소규모 사용엔 충분

---

## ❓ 도움이 필요하면

- Cloudflare D1 문서: https://developers.cloudflare.com/d1/
- Workers 문서: https://developers.cloudflare.com/workers/
- Wrangler 문서: https://developers.cloudflare.com/workers/wrangler/
