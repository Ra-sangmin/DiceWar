// DB 접속 정보는 코드에 직접 쓰지 않고 환경변수에서 읽는다.
// 서버의 Server/.env 파일(.env.example 참고, 커밋 금지) 또는 PM2 ecosystem 의 env 로 넣는다.
try {
    require('dotenv').config({ path: require('path').join(__dirname, '.env') });
} catch (e) {
    // dotenv 가 설치되어 있지 않으면 시스템 환경변수만 사용
}

function getEnv(name, defaultValue) {
    const value = process.env[name];

    if (value === undefined || value === '') {
        if (defaultValue === undefined) {
            console.error(`[설정] 환경변수 ${name} 가 없습니다. Server/.env 를 확인하세요.`);
        }
        return defaultValue;
    }

    return value;
}

module.exports = {
    // 실서버 RDS (database.js, udpDiceWar.js)
    db: {
        host: getEnv('DB_HOST'),
        port: Number(getEnv('DB_PORT', '3306')),
        user: getEnv('DB_USER'),
        password: getEnv('DB_PASSWORD'),
        database: getEnv('DB_NAME'),
    },
    // 로컬 테스트 DB (userDBC.js)
    localDb: {
        host: getEnv('LOCAL_DB_HOST', 'localhost'),
        user: getEnv('LOCAL_DB_USER', 'root'),
        password: getEnv('LOCAL_DB_PASSWORD', ''),
        database: getEnv('LOCAL_DB_NAME', 'test'),
    },
};
