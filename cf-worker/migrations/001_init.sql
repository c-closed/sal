-- ========================================
-- D1 데이터베이스 테이블 생성 SQL
-- ========================================
-- wrangler d1 execute sboard-db --file migrations/001_init.sql --remote
-- 명령어로 실행합니다.

CREATE TABLE IF NOT EXISTS users (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    username TEXT UNIQUE NOT NULL,
    user_id TEXT NOT NULL,
    user_pw TEXT NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
);
