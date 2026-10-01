const net = require("net");
const crypto = require("crypto");
const express = require('express');

const clients = new Map();
const HEARTBEAT_TIMEOUT = 60000; // 60초
const port = 8000;

// 인게임 중 연결이 끊긴 유저를 바로 퇴장시키지 않고 재입장을 기다리는 시간
// (앱이 잠깐 백그라운드에 갔다가 돌아오며 소켓이 새로 열리는 경우 대비)
const REJOIN_GRACE_MS = 15000;
// 재입장 대기 중 쌓아둘 최대 패킷 수
const MAX_PENDING_PACKETS = 200;

const PROTOCOL_GAME_READY = 1;
const PROTOCOL_GAME_OUT = 2;
const PROTOCOL_REJOIN = 15;
const PROTOCOL_HEARTBEAT = 99;
const monitorPort = 8004;

let roomDatas = []; // 가급적 let 사용

/* ======================
    서버 현황 모니터링 로직
====================== */
function GetServerStats() {
    let totalConnected = clients.size;
    let waitingInRoom = 0;
    let playingInGame = 0;

    roomDatas.forEach(room => {
        if (room.gameReadyOn) playingInGame += room.sockets.length;
        else waitingInRoom += room.sockets.length;
    });

    let lobbyUsers = totalConnected - (waitingInRoom + playingInGame);
    // 로그가 너무 많으면 PM2 로그 파일이 커지니 30초 정도로 조절해도 좋아요.
    //console.log(`[${new Date().toLocaleTimeString()}] 접속:${totalConnected} (로비:${lobbyUsers}/대기:${waitingInRoom}/게임:${playingInGame}) | 방:${roomDatas.length}`);
}
setInterval(GetServerStats, 30000); 

/* ======================
    방 데이터 클래스
====================== */
class RoomData { 
    constructor(index, maxPlayerCnt, userCnt, mapSizeEnum, sockets, gameOuts, gameReadyOn) {
        this.index = index;
        this.maxPlayerCnt = maxPlayerCnt;
        this.userCnt = userCnt;
        this.mapSizeEnum = mapSizeEnum;
        this.sockets = sockets;
        this.gameOuts = gameOuts;
        this.gameReadyOn = gameReadyOn;
        // 플레이어 번호별 재입장 대기 정보 { timer, pending: [] } (연결이 살아있으면 null)
        this.waits = [];
        // 플레이어 번호별 재입장 토큰 (매칭 때 본인에게만 발급, 재입장 / 소켓이 바뀐 뒤의 퇴장 요청 때 확인)
        this.tokens = [];
    }
}

/* ======================
    헬퍼 함수 (최적화)
====================== */
function CreateRejoinToken() {
    return crypto.randomBytes(16).toString('hex');
}

// 해당 자리의 토큰과 같은지 (길이가 다르거나 문자열이 아니면 실패)
function IsValidToken(room, idx, token) {
    const saved = room.tokens[idx];
    if (typeof saved !== 'string' || typeof token !== 'string') return false;

    const a = Buffer.from(saved);
    const b = Buffer.from(token);
    return a.length === b.length && crypto.timingSafeEqual(a, b);
}

function GetRoomDataFromIndex(roomDataIndex) {
    return roomDatas.find(r => r.index === roomDataIndex) || null;
}

// 방 제거 로직 (더 안전하게 수정)
function RemoveRoom(index) {
    const initialLen = roomDatas.length;
    roomDatas = roomDatas.filter(r => r.index !== index);
    if (initialLen !== roomDatas.length) {
        //console.log(`[방 삭제] Index: ${index} | 남은 방: ${roomDatas.length}`);
    }
}

function BroadcastOn(getRoomData, resultData) {

    if (!getRoomData) return;
    getRoomData.sockets.forEach((s, i) => {
        if (getRoomData.gameOuts[i]) return;

        // 재입장 대기 중인 유저 몫은 쌓아뒀다가 재입장하면 보내준다
        const wait = getRoomData.waits[i];
        if (wait) {
            if (wait.pending.length < MAX_PENDING_PACKETS) wait.pending.push(resultData);
            return;
        }

        if (s && !s.destroyed) {
            sendMessage(s, resultData);
        }
    });
}

function sendMessage(socket, message) {
    if (!socket || socket.destroyed || !socket.writable) return;

    try {
        const messageBuffer = Buffer.from(message, 'utf8');
        const lengthBuffer = Buffer.alloc(4);
        lengthBuffer.writeInt32LE(messageBuffer.length);
        socket.write(Buffer.concat([lengthBuffer, messageBuffer]));
    } catch (err) {
        console.error("전송 에러:", err.message);
    }
}

/* ======================
    TCP 서버 본체
====================== */
const server = net.createServer((socket) => {
    const clientIp = socket.remoteAddress;
    // 봇들의 단순 접속은 로그를 남기지 않거나 간략히 처리할 수도 있습니다.
    // console.log(`연결됨: ${clientIp}`); 

    socket.setTimeout(HEARTBEAT_TIMEOUT);
    clients.set(socket, Buffer.alloc(0));

    // 데이터 수신 및 패킷 파싱
    socket.on('data', (data) => {
        // [방어막 1] 이미 끊긴 소켓에서 뒤늦게 데이터가 오면 무시
        if (!clients.has(socket)) return;

        let buffer = Buffer.concat([clients.get(socket), data]);
        
        while (buffer.length >= 4) {
            const packetLength = buffer.readInt32LE(0);
            
            // [방어막 2] 봇이 보낸 비정상적인 쓰레기 데이터 차단 (예: 길이가 음수이거나 1MB 이상일 때)
            if (packetLength <= 0 || packetLength > 1024 * 1024) {
                console.log(`[방어] 비정상 패킷 감지 및 차단: ${clientIp}`);
                socket.destroy(); // 즉시 통로 파괴
                return;
            }

            if (buffer.length < 4 + packetLength) break;

            const message = buffer.slice(4, 4 + packetLength).toString('utf8');
            SetData(socket, message);
            buffer = buffer.slice(4 + packetLength);
        }
        
        // 방어막을 무사히 통과했다면 버퍼 저장
        if (clients.has(socket)) {
            clients.set(socket, buffer);
        }
    });

    // 모든 종료 이벤트 통합 처리
    const closeConnection = (reason) => {
        if (clients.has(socket)) {
            // 진짜 유저가 나갔을 때만 로그를 예쁘게 찍고, 봇 에러는 조용히 넘깁니다.
            if (reason !== 'Error') {
                //console.log(`연결 종료 (${reason}): ${clientIp}`);
            }
            DisconnectCheckOn(socket);
            clients.delete(socket);
            socket.destroy();
        }
    };

    socket.on('timeout', () => {
        //console.log(`time out`);
        closeConnection('Timeout')
    });
    socket.on('end', () => {
        //console.log(`end`);
        closeConnection('End')   
    });
    socket.on('error', (err) => {
        // 클라이언트(또는 봇)가 일방적으로 끊었을 때 발생하는 ECONNRESET 에러는 콘솔에 출력하지 않고 무시합니다.
        if (err.code !== 'ECONNRESET') {
             //console.error(`소켓 에러 (${clientIp}):`, err.message);
        }
        closeConnection('Error');
    });
});

// [중요] '0.0.0.0'을 넣어 IPv4 접속을 강제합니다. (tcp6 문제 해결)
server.listen(port, '0.0.0.0', () => {
    console.log(`[DiceWar] TCP 서버 IPv4(0.0.0.0) ${port}에서 실행 중`);
});

/* ======================
    게임 로직 처리
====================== */
function SetData(socket, data) {
    let jsonData;
    try { jsonData = JSON.parse(data); } catch (e) { return; }

    const protocol = jsonData.requestProtocal;

    //console.log(`protocol ` +protocol );

    if (protocol === PROTOCOL_HEARTBEAT) return; // 하트비트

    if (protocol === PROTOCOL_GAME_READY) {
        GameReadyOn(socket, jsonData);
    } else if (protocol === PROTOCOL_REJOIN) {
        RejoinOn(socket, jsonData);
    } else {
        let room = GetRoomDataFromIndex(jsonData.roomDataIndex);
        
        if (protocol === PROTOCOL_GAME_OUT) {
            // [핵심] 소켓 정보뿐만 아니라, 유니티가 보낸 내 플레이어 번호(outPlayerEnum)와 토큰을 같이 넘깁니다.
            if (room) GameOutOn(socket, room, jsonData.outPlayerEnum, jsonData.rejoinToken);
            return;
        }

        if (!room) return;

        // 이 방에 속한 소켓이 보낸 패킷만 중계한다
        // (끊겼다가 재입장하지 않은 소켓이나, 없어진 방 번호를 재사용한 새 방으로 이전 판 패킷이 섞이는 것 방지)
        if (!room.sockets.includes(socket)) return;
        
        if (protocol === 3 || protocol === 4) room.gameReadyOn = true;
        BroadcastOn(room, data);
    }
}

function GameReadyOn(socket, jsonData) {
    GameOutCheckOn(socket);

    let room = roomDatas.find(r => 
        !r.gameReadyOn && 
        r.maxPlayerCnt === jsonData.maxPlayerCnt && 
        r.userCnt === jsonData.userCnt &&
        r.mapSizeEnum === jsonData.mapSizeEnum &&
        r.userCnt > r.sockets.length
    );

    if (!room) {
        const newIndex = roomDatas.length > 0 ? roomDatas[roomDatas.length - 1].index + 1 : 0;
        room = new RoomData(newIndex, jsonData.maxPlayerCnt, jsonData.userCnt, jsonData.mapSizeEnum, [], [], false);
        roomDatas.push(room);
    }

    const rejoinToken = CreateRejoinToken();

    room.sockets.push(socket);
    room.gameOuts.push(false);
    room.tokens.push(rejoinToken);

    const socketCnt = room.sockets.length;
    const playerEnum = socketCnt - 1;
    const resultData = {
        requestProtocal: 1,
        socketCnt,
        roomDataIndex: room.index,
        playerEnum,
        isOwner: playerEnum === 0
    };

    // 토큰은 새로 들어온 본인에게만 보낸다 (다른 유저에게는 토큰 없이)
    const publicData = JSON.stringify(resultData);
    const privateData = JSON.stringify({ ...resultData, rejoinToken });

    room.sockets.forEach((s, i) => {
        if (!s || s.destroyed || room.gameOuts[i]) return;
        sendMessage(s, s === socket ? privateData : publicData);
    });
}

function GameOutCheckOn(socket) {
    const room = roomDatas.find(r => r.sockets.includes(socket));
    if (room) GameOutOn(socket, room);
}

/* 연결이 끊겼을 때 : 대기방이면 바로 퇴장, 인게임이면 REJOIN_GRACE_MS 동안 재입장을 기다린다.
   그 사이 퇴장 알림(GameOut)을 보내지 않으므로 다른 유저 화면에서 AI 로 바뀌지 않고, 방도 삭제되지 않는다. */
function DisconnectCheckOn(socket) {
    const room = roomDatas.find(r => r.sockets.includes(socket));
    if (!room) return;

    if (!room.gameReadyOn) {
        GameOutOn(socket, room);
        return;
    }

    const idx = room.sockets.indexOf(socket);
    room.sockets[idx] = null;

    const wait = { pending: [], timer: null };
    wait.timer = setTimeout(() => {
        // 재입장하지 않았으면 그때 퇴장 처리
        if (room.waits[idx] !== wait) return;
        room.waits[idx] = null;

        if (GetRoomDataFromIndex(room.index) === room) {
            GameOutOn(undefined, room, idx);
        }
    }, REJOIN_GRACE_MS);

    room.waits[idx] = wait;
}

/* 재입장 : 클라이언트가 소켓을 새로 연 뒤 { roomDataIndex, playerEnum } 을 보내면 그 자리에 새 소켓을 연결한다 */
function RejoinOn(socket, jsonData) {
    const room = GetRoomDataFromIndex(jsonData.roomDataIndex);
    const idx = jsonData.playerEnum;

    const rejoinOn = !!room &&
        room.gameReadyOn &&
        Number.isInteger(idx) &&
        idx >= 0 && idx < room.sockets.length &&
        !room.gameOuts[idx] &&
        IsValidToken(room, idx, jsonData.rejoinToken);

    if (rejoinOn) {
        // 이전 소켓의 종료 이벤트가 아직 안 왔어도, 자리를 새 소켓으로 바꿔두면 나중에 와도 무시된다
        room.sockets[idx] = socket;

        const wait = room.waits[idx];
        room.waits[idx] = null;
        if (wait) clearTimeout(wait.timer);

        sendMessage(socket, JSON.stringify({
            requestProtocal: PROTOCOL_REJOIN,
            roomDataIndex: room.index,
            playerEnum: idx,
            rejoinOn: true
        }));

        // 끊겨있는 동안 못 받은 패킷 전달
        if (wait) wait.pending.forEach(message => sendMessage(socket, message));
    } else {
        sendMessage(socket, JSON.stringify({
            requestProtocal: PROTOCOL_REJOIN,
            roomDataIndex: jsonData.roomDataIndex,
            playerEnum: idx,
            rejoinOn: false
        }));
    }
}

function GameOutOn(socket, room, outPlayerEnum, rejoinToken) {
    let idx = room.sockets.indexOf(socket);

    // [유령 방어] 재연결 때문에 소켓이 바뀌었어도, 유니티가 보낸 번호(outPlayerEnum)로 찾습니다.
    // 단, 다른 사람을 내보내지 못하도록 그 자리의 토큰이 맞을 때만 허용합니다.
    // (서버가 직접 호출하는 재입장 대기 만료는 socket 없이 번호만 넘기므로 토큰 확인을 하지 않습니다)
    if (idx === -1 && outPlayerEnum !== undefined && outPlayerEnum !== null) {
        if (socket && !IsValidToken(room, outPlayerEnum, rejoinToken)) return;
        idx = outPlayerEnum;
    }

    if (idx === -1 || idx >= room.sockets.length) return;

    // 1. 게임 시작 전 (대기방 로비 상태)
    if (!room.gameReadyOn) {
        room.sockets.splice(idx, 1);
        room.gameOuts.splice(idx, 1);
        room.waits.splice(idx, 1);
        room.tokens.splice(idx, 1);
        
        // 대기방에 아무도 없으면 파괴
        if (room.sockets.length === 0) {
            //console.log(`[대기방 파괴] 방 ${room.index}에 아무도 없어 파괴합니다.`);
            RemoveRoom(room.index);
        } else {
            // 누군가 남아있으면 나갔다고 알려줌
            room.sockets.forEach((s, i) => {
                const msg = JSON.stringify({ requestProtocal: 2, playerEnum: i, outPlayerEnum: idx, isOwner: i === 0 });
                sendMessage(s, msg);
            });
        }
    } 
    // 2. 게임 시작 후 (인게임 상태)
    else {
        if (room.gameOuts[idx]) return; // 이미 퇴장 처리됨

        // 재입장 대기 중이었다면 대기 취소
        const wait = room.waits[idx];
        room.waits[idx] = null;
        if (wait) clearTimeout(wait.timer);

        room.gameOuts[idx] = true; // 나감 표시
        room.sockets[idx] = null;  // 소켓 연결 해제 (메모리 정리)

        // [미쿠짱의 기획 적용] 방에 '실제 유저(Human)'가 한 명이라도 남아있는지 검사!
        // gameOuts 배열에서 false인 사람이 실제 접속 중인 유저입니다.
        const isHumanLeft = room.gameOuts.includes(false);

        if (!isHumanLeft) {
            // 진짜 유저가 없고 모두 AI(또는 나감)라면 방 파괴!
            //console.log(`[방 파괴] 방 ${room.index}에 실제 유저가 없어 파괴합니다. (모두 AI로 전환됨)`);
            RemoveRoom(room.index);
        } else {
            // 다른 실제 유저가 있다면 방은 유지하고, 남은 사람들에게 "누가 나갔다"고 알려줌
            //console.log(`[방 유지] 방 ${room.index} 유지. (다른 유저가 플레이 중)`);
            
            // 살아있는 사람 중 가장 번호가 빠른 사람에게 방장(Owner) 권한 위임
            let newOwnerIdx = room.gameOuts.indexOf(false); 

            room.sockets.forEach((s, i) => {
                if (s && !room.gameOuts[i]) {
                    const msg = JSON.stringify({ 
                        requestProtocal: 2, 
                        playerEnum: i, 
                        outPlayerEnum: idx, 
                        isOwner: i === newOwnerIdx // 새로운 방장 갱신
                    });
                    sendMessage(s, msg);
                }
            });
        }
    }
}

/* ======================
    모니터링 API (Express)
====================== */
const monitorApp = express();
monitorApp.get('/status', (req, res) => {
    // 실제로 연결이 살아있고(writable) 파괴되지 않은 소켓만 카운트
    let actualConnections = 0;
    clients.forEach((buffer, socket) => {
        if (socket.writable && !socket.destroyed) {
            actualConnections++;
        } else {
            // 죽어있는 소켓이 Map에 남아있다면 여기서 정리
            clients.delete(socket);
        }
    });

    res.json({
        time: new Date().toLocaleString(),
        connections: actualConnections, // 정확한 수치
        rooms: roomDatas.length,
        details: roomDatas.map(r => ({ id: r.index, players: r.sockets.length }))
    });
});
monitorApp.listen(monitorPort, '0.0.0.0');

// [최후의 보루] 예외 발생 시 서버가 죽지 않도록 방어
process.on('uncaughtException', (err) => {
    console.error('죽지 마! 예외 발생:', err);
});