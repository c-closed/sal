// ========================================
// Sboard API Server on Cloudflare Workers
// ========================================

export default {
  async fetch(request, env) {
    const url = new URL(request.url);
    const path = url.pathname;
    const method = request.method;

    // CORS 헤더 (모든 응답에 포함)
    const corsHeaders = {
      'Access-Control-Allow-Origin': '*',
      'Access-Control-Allow-Methods': 'GET, POST, PUT, DELETE, OPTIONS',
      'Access-Control-Allow-Headers': 'Content-Type',
    };

    // OPTIONS 프리플라이트 요청 처리
    if (method === 'OPTIONS') {
      return new Response(null, { headers: corsHeaders, status: 204 });
    }

    // 라우팅: GET /api/users
    if (path === '/api/users' && method === 'GET') {
      return handleGetUsers(env.DB, corsHeaders);
    }

    // 라우팅: POST /api/users
    if (path === '/api/users' && method === 'POST') {
      return handleCreateUser(request, env.DB, corsHeaders);
    }

    // 라우팅: PUT /api/users/{username}  또는  DELETE /api/users/{username}
    const userMatch = path.match(/^\/api\/users\/(.+)$/);
    if (userMatch) {
      const username = decodeURIComponent(userMatch[1]);

      if (method === 'PUT') {
        return handleUpdateUser(username, request, env.DB, corsHeaders);
      }
      if (method === 'DELETE') {
        return handleDeleteUser(username, env.DB, corsHeaders);
      }
    }

    // 라우팅: GET /api/meta
    if (path === '/api/meta' && method === 'GET') {
      return handleGetMeta(env, corsHeaders);
    }

    // 404 Not Found
    return jsonResponse(
      { error: 'Not Found' },
      { status: 404, headers: corsHeaders }
    );
  },
};

// ----------------------------------------
// GET /api/users  →  전체 사용자 목록 반환
// ----------------------------------------
async function handleGetUsers(DB, corsHeaders) {
  const { results } = await DB.prepare(
    'SELECT username, user_id, user_pw FROM users ORDER BY created_at'
  ).all();

  const users = {};
  for (const row of results) {
    users[row.username] = { id: row.user_id, pw: row.user_pw };
  }

  return jsonResponse({ users, last_updated: getLastUpdated() }, corsHeaders);
}

// ----------------------------------------
// POST /api/users  →  새 사용자 등록
// Body: { username: "홍길동", id: "123", pw: "456" }
// ----------------------------------------
async function handleCreateUser(request, DB, corsHeaders) {
  const body = await request.json();
  const { username, id, pw } = body;

  if (!username || !id || !pw) {
    return jsonError('username, id, pw 모두 필요합니다.', { status: 400, headers: corsHeaders });
  }

  const { success } = await DB.prepare(
    'INSERT OR IGNORE INTO users (username, user_id, user_pw) VALUES (?, ?, ?)'
  ).bind(username, id, pw).run();

  return jsonResponse({ success, username }, corsHeaders);
}

// ----------------------------------------
// PUT /api/users/{username}  →  사용자 정보(PW) 변경
// Body: { id: "123", pw: "789" }
// ----------------------------------------
async function handleUpdateUser(username, request, DB, corsHeaders) {
  const body = await request.json();
  const { id, pw } = body;

  if (!id || !pw) {
    return jsonError('id, pw 모두 필요합니다.', { status: 400, headers: corsHeaders });
  }

  await DB.prepare(
    'UPDATE users SET user_id = ?, user_pw = ?, updated_at = CURRENT_TIMESTAMP WHERE username = ?'
  ).bind(id, pw, username).run();

  return jsonResponse({ success: true, username }, corsHeaders);
}

// ----------------------------------------
// DELETE /api/users/{username}  →  사용자 삭제
// ----------------------------------------
async function handleDeleteUser(username, DB, corsHeaders) {
  await DB.prepare('DELETE FROM users WHERE username = ?').bind(username).run();
  return jsonResponse({ success: true, username }, corsHeaders);
}

// ========================================
// 업데이트 정보 (릴리스 시 함께 수정)
// ========================================
const UPDATE_INFO = {
  version: '2.0.0',
  sha256: 'e139ef15420e32dae507d381e5ed2024f7e8c19801aa5967c2ebe6bc8ddbf8de',
  url: 'https://github.com/c-closed/sal/releases/download/v2.0.0/Sboard_Setup.exe',
};

// GET /api/meta  →  메타정보 + 업데이트 정보 반환
async function handleGetMeta(env, corsHeaders) {
  const { count } = await env.DB.prepare('SELECT COUNT(*) as count FROM users').first();
  return jsonResponse({
    last_updated: getLastUpdated(),
    total_users: count,
    update_version: UPDATE_INFO.version,
    update_sha256: UPDATE_INFO.sha256,
    update_url: UPDATE_INFO.url,
  }, corsHeaders);
}

// ----------------------------------------
// 헬퍼 함수들
// ----------------------------------------
function getLastUpdated() {
  const now = new Date();
  const opts = { timeZone: 'Asia/Seoul', hour12: false };
  const parts = new Intl.DateTimeFormat('ko-KR', {
    year: 'numeric', month: '2-digit', day: '2-digit',
    hour: '2-digit', minute: '2-digit', second: '2-digit',
    ...opts
  }).formatToParts(now);

  const v = (type) => (parts.find(p => p.type === type) || {}).value || '';
  return `${v('year')}.${v('month')}.${v('day')} ${v('hour')}:${v('minute')}:${v('second')} 기준`;
}

function jsonResponse(data, options = {}) {
  return new Response(JSON.stringify(data), {
    status: options.status || 200,
    headers: { 'Content-Type': 'application/json', ...options.headers },
  });
}

function jsonError(message, options = {}) {
  return jsonResponse({ error: message }, options);
}
