const express = require('express');
const bodyParser = require('body-parser');
const mysql = require('mysql2/promise');
const { db } = require('./dbConfig');

/* ======================
    MySQL Pool 설정
====================== */
const pool = mysql.createPool({
  host: db.host,
  port: db.port,
  user: db.user,
  password: db.password,
  database: db.database,
  waitForConnections: true,
  connectionLimit: 20, // 동시 접속이 늘어날 것에 대비해 조금 늘렸어요
  queueLimit: 0,
  enableKeepAlive: true, // DB 연결 유지 설정 추가
  keepAliveInitialDelay: 10000
});

/* ======================
    Express 설정
====================== */
const app = express();
app.use(express.json());
app.use(bodyParser.urlencoded({ extended: true }));

/* ======================
    유저 정보 조회 (개선)
====================== */
app.post('/getUserData', async (req, res) => {
  const { email, snsType = 0 } = req.body;

  if (!email) {
    return res.status(400).json({ error: 'Email is required' });
  }

  try {
    // 1. 유저 조회
    let [rows] = await pool.query('SELECT * FROM user WHERE email = ? LIMIT 1', [email]);

    // 2. 회원 없으면 자동 가입
    if (rows.length === 0) {
      await pool.query('INSERT INTO user (email, snsType) VALUES (?, ?)', [email, snsType]);
      // 가입 후 다시 조회
      [rows] = await pool.query('SELECT * FROM user WHERE email = ? LIMIT 1', [email]);
    }

    res.json(rows[0]);
  } catch (err) {
    console.error('[DB_ERROR] getUserData:', err.message);
    res.status(500).json({ error: 'Internal Server Error' });
  }
});

/* ======================
    코인 정보 업데이트 (개선)
====================== */
app.post('/updateCoinData', async (req, res) => {
  const { email, coin, freeCoin, chargeCoin } = req.body;

  if (!email) return res.status(400).json({ error: 'Email is required' });

  try {
    if (coin !== undefined && coin !== null) {
      await pool.query('UPDATE user SET coin = ? WHERE email = ?', [coin, email]);
    } else {
      await pool.query('UPDATE user SET freeCoin = ?, chargeCoin = ? WHERE email = ?', [freeCoin, chargeCoin, email]);
    }

    const [rows] = await pool.query('SELECT * FROM user WHERE email = ? LIMIT 1', [email]);
    res.json(rows[0] || { success: true });
  } catch (err) {
    console.error('[DB_ERROR] updateCoinData:', err.message);
    res.status(500).json({ error: 'Internal Server Error' });
  }
});

/* ======================
    서버 시작 (IPv4 명시)
====================== */
const HTTP_PORT = 8002;
// '0.0.0.0'을 넣어 IPv4 접속을 확실히 허용합니다.
app.listen(HTTP_PORT, '0.0.0.0', () => {
  console.log(`> DiceWar HTTP API server started on port ${HTTP_PORT} (IPv4)`);
});