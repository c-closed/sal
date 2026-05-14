# Quick Start - Cloudflare Workers API 배포

이 파일을 보고 따라하기만 하면 됩니다.

## 한 줄 요약
Cloudflare Workers + D1로 무료 API 서버를 만듭니다.

---

## 1. 준비 (한 번만)

```
# Cloudflare 가입
https://dash.cloudflare.com/sign-up

# Node.js 설치 (LTS 버전)
https://nodejs.org/

# 프로젝트 폴더로 이동
cd "D:\99. 개인자료\00. 개발\sal\cf-worker"

# Wrangler 설치
npm install

# Cloudflare 로그인
npx wrangler login
```

## 2. 데이터베이스 생성

```
npx wrangler d1 create sboard-db
```
→ 나오는 `database_id` 복사

→ `wrangler.toml` 파일 열어서 `database_id` 부분에 붙여넣기

```
npx wrangler d1 execute sboard-db --remote --file migrations/001_init.sql
```

## 3. 배포

```
npx wrangler deploy
```
→ 나오는 URL 복사 (예: `https://sboard-api.xxxxx.workers.dev`)

## 4. Python 스크립트 수정

`sal.py`에서 URL 변경:

```python
API_BASE = "https://sboard-api.xxxxx.workers.dev/api/users"
API_META = "https://sboard-api.xxxxx.workers.dev/api/meta"
```

`load_users` 함수 수정:

```python
def load_users(self, quiet: bool = False):
    data = self.api.get_users()
    self.users = data.get("users", data)
```

## 5. 데이터 이전

`migrate_data.py`에서 URL과 기존 데이터 입력 후 실행:

```
python migrate_data.py
```

## 6. 테스트

```
python sal.py
```

---

## 배포 명령어 (코드 수정 시마다)

```
cd "D:\99. 개인자료\00. 개발\sal\cf-worker"
npx wrangler deploy
```

끝!
