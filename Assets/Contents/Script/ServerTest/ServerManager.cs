using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

public class ServerManager : MonoSingleton<ServerManager>
{
	private float heartbeatInterval = 20f; // 20초마다 전송
	private float heartbeatTimer = 0f;

	// 스레드 충돌 방지용 자물쇠입니다.
	private readonly object queueLock = new object();

	private Queue<string> sendQueue = new Queue<string>();

	private Queue<BaseTCPRequest> queue = new Queue<BaseTCPRequest>();

	private Queue<string> receiveQueue = new Queue<string>();

	public UnityAction<BaseTCPRequest> receiveDataOn = data => { };

	public int roomDataIndex = -1;

	public bool receiveOn = true;

	private TcpClient client;
	private NetworkStream stream;
	private Thread receiveThread;
	private byte[] buffer = new byte[1024 * 4];

	// 수신 버퍼
	private List<byte> receiveBuffer = new List<byte>();

	private bool isAppQuitting = false;

	void Start()
	{
		ConnectToServer();
	}

	public void ConnectToServer()
	{
		// 1. 기존 찌꺼기 통로 청소
		try { if (stream != null) { stream.Close(); stream = null; } } catch { }
		try { if (client != null) { client.Close(); client = null; } } catch { }

		// 🚀 [핵심 추가] 이전 소켓에서 받다 만 쓰레기 버퍼를 완벽하게 삭제합니다! 
		// 이거 하나면 수신 스레드가 꼬여서 죽는 일이 사라집니다.
		receiveBuffer.Clear();

		// 2. 새 연결 뚫기
		try
		{
			string IP = "52.78.148.28";
			int PORT = 8000;

			client = new TcpClient();
			client.Connect(IPAddress.Parse(IP), PORT);
			stream = client.GetStream();

			receiveThread = new Thread(ReceiveLoop);
			receiveThread.IsBackground = true;
			receiveThread.Start();

			//Debug.Log("[TCP] 서버 접속/재접속 및 버퍼 초기화 완벽 성공!");
		}
		catch (Exception e)
		{
			Debug.LogWarning($"[TCP] 서버 접속 실패: {e.Message}");
		}
	}

	private void ReceiveLoop()
	{
		while (true)
		{
			try
			{
				// [안전 장치] 만약 메인 스레드가 통로를 파괴했다면 조용히 종료합니다.
				if (stream == null) break;

				int byteCount = stream.Read(buffer, 0, buffer.Length);
				if (byteCount <= 0)
				{
					//Debug.LogWarning("서버 연결 끊김");
					break;
				}

				receiveBuffer.AddRange(buffer[..byteCount]);
				ParsePackets();
			}
			catch (Exception e)
			{
				if (!isAppQuitting)
				{
					//Debug.LogWarning($"예외 (ReceiveLoop): {e.Message}");
				}

				// ❌ [최악의 범인 삭제] 여기서 절대 stream = null; 을 하면 안 됩니다! ❌
				// 과거의 스레드가 죽으면서, 새 게임을 위해 만든 새 통로를 부숴버리는 '팀킬'이 발생하기 때문입니다.
				break;
			}
		}
	}

	private void ParsePackets()
	{
		while (true)
		{
			if (receiveBuffer.Count < 4) return;
			int packetLength = BitConverter.ToInt32(receiveBuffer.ToArray(), 0);
			if (receiveBuffer.Count < 4 + packetLength) return;

			byte[] packetData = receiveBuffer.GetRange(4, packetLength).ToArray();
			receiveBuffer.RemoveRange(0, 4 + packetLength);
			string jsonString = Encoding.UTF8.GetString(packetData);

			// [추가] 큐에 넣을 때 다른 스레드가 건드리지 못하게 잠급니다.
			lock (queueLock)
			{
				receiveQueue.Enqueue(jsonString);
			}
		}
	}

	public void SendMessageOn(string jsonUserString)
	{
		try
		{
			// 선제적 재연결
			if (client == null || !client.Connected || stream == null)
			{
				//Debug.Log("[TCP] 연결이 없어서 다시 뚫습니다.");
				ConnectToServer();
			}

			byte[] data = Encoding.UTF8.GetBytes(jsonUserString);
			byte[] length = BitConverter.GetBytes(data.Length);
			byte[] packet = new byte[4 + data.Length];
			Buffer.BlockCopy(length, 0, packet, 0, 4);
			Buffer.BlockCopy(data, 0, packet, 4, data.Length);

			stream.Write(packet, 0, packet.Length);
			stream.Flush();
		}
		catch (System.IO.IOException e)
		{
			Debug.LogWarning($"[TCP] 통신 끊김 감지! 즉시 재연결 시도... : {e.Message}");

			// 다시 뚫기 (이제 여기서 멈추지 않습니다!)
			ConnectToServer();

			// 새 통로가 뚫렸다면 아까 못 보낸 패킷 다시 발사!
			if (client != null && client.Connected && stream != null)
			{
				byte[] data = Encoding.UTF8.GetBytes(jsonUserString);
				byte[] length = BitConverter.GetBytes(data.Length);
				byte[] packet = new byte[4 + data.Length];
				Buffer.BlockCopy(length, 0, packet, 0, 4);
				Buffer.BlockCopy(data, 0, packet, 4, data.Length);

				stream.Write(packet, 0, packet.Length);
				stream.Flush();
				Debug.Log("[TCP] 잃어버린 패킷 복구 전송 성공!");
			}
		}
		catch (Exception e)
		{
			Debug.LogError($"[TCP] 치명적 전송 에러: {e.Message}");
		}
	}

	public void GameReadyRequestOn(int maxPlayerCnt, int userCnt)
	{
		GameReadyRequest gameReadyRequest = new GameReadyRequest()
		{
			maxPlayerCnt = maxPlayerCnt,
			userCnt = userCnt,
			mapSizeEnum = DataManager.Instance.mapSizeEnum
		};

		SendMessageOn(gameReadyRequest);
	}

	public void MapCreateRequestOn()
	{
		//Debug.LogWarning($"create Map Data = {roomDataIndex}");

		MapCreateRequestOn mapCreateRequestOn = new MapCreateRequestOn()
		{
			roomDataIndex = roomDataIndex,
			playerDataList = DataManager.Instance.playerDataList,
			area = DataManager.Instance.areaDataList,
		};

		SendMessageOn(mapCreateRequestOn);
	}

	public void SendMessageOn(BaseTCPRequest request)
	{
		request.roomDataIndex = roomDataIndex;

		string jsonStr = JsonUtility.ToJson(request);

		//Debug.LogWarning("send "+jsonStr);
		sendQueue.Enqueue(jsonStr);
	}

	void SendQueueListCheck()
	{
		int processLimit = 20;
		int count = 0;

		while (sendQueue.Count != 0 && count < processLimit)
		{
			SendMessageOn(sendQueue.Dequeue());
			count++;
		}
	}

	private void Update()
	{
		SendQueueListCheck();
		QueueListCheck();
		ReceiveListDataCheck();

		// 하트비트 타이머 체크
		if (client != null && client.Connected)
		{
			heartbeatTimer += Time.deltaTime;
			if (heartbeatTimer >= heartbeatInterval)
			{
				SendHeartbeat();
				heartbeatTimer = 0f;
			}
		}
	}

	void QueueListCheck()
	{
		int processLimit = 20;
		int count = 0;

		while (receiveOn && queue.Count > 0 && count < processLimit)
		{
			receiveDataOn(queue.Dequeue());
			count++;
		}
	}

	void ReceiveListDataCheck()
	{
		int processLimit = 20; // 한 프레임에 최대 20개만 처리 (수치 조절 가능)
		int count = 0;

		while (count < processLimit)
		{
			string checkJsonData = null;

			lock (queueLock)
			{
				if (receiveQueue.Count == 0) break; // 더 이상 없으면 탈출
				checkJsonData = receiveQueue.Dequeue();
			}

			if (checkJsonData != null)
			{
				ReceiveRequestDataOn(checkJsonData);
			}
			count++;
		}
	}

	private void ReceiveRequestDataOn(string str)
	{
		BaseTCPRequest baseRequest = GetBaseReqeust(str);

		if (baseRequest == null) return;

		// 🛡️ [유령 패킷 완벽 차단 방어막]
		// 현재 방 번호가 -1 (방을 나와서 로비/로딩 대기 중)일 때 들어오는 패킷 중,
		// '새 게임 응답(GameReady)'이 아닌 과거의 모든 쓰레기 패킷은 큐에 넣지 않고 즉시 소각합니다!
		if (roomDataIndex == -1 && baseRequest.requestProtocal != RequestProtocal.GameReady)
		{
			// 디버깅이 필요하면 아래 주석을 풀어서 어떤 유령 패킷이 차단되었는지 구경(?)해 보세요.
			// Debug.LogWarning($"[TCP] 이전 방의 유령 패킷 차단 완벽 성공: {baseRequest.requestProtocal}");
			return;
		}

		// GameReady가 도착하면 드디어 새 방 번호를 발급받고 차단기를 해제합니다.
		if (baseRequest.requestProtocal == RequestProtocal.GameReady)
		{
			roomDataIndex = baseRequest.roomDataIndex;
		}

		// 안전한 패킷만 큐에 넣습니다.
		queue.Enqueue(baseRequest);
	}

	BaseTCPRequest GetBaseReqeust(string str)
	{
		BaseTCPRequest baseRequest = null;

		RequestProtocal requestProtocal = JsonParser<BaseTCPRequest>(str).requestProtocal;

		switch (requestProtocal)
		{
			case RequestProtocal.GameReady: baseRequest = JsonParser<GameReadyRequest>(str); break;
			case RequestProtocal.GameOutOn: baseRequest = JsonParser<GameOutRequest>(str); break;
			case RequestProtocal.GameStartOn: baseRequest = JsonParser<GameStartOn>(str); break;
			case RequestProtocal.MapCreateOn: baseRequest = JsonParser<MapCreateRequestOn>(str); break;
			case RequestProtocal.TurnRequest: baseRequest = JsonParser<TurnRequest>(str); break;
			case RequestProtocal.AttackRequest: baseRequest = JsonParser<AttackRequest>(str); break;
			case RequestProtocal.LandTradeRequest: baseRequest = JsonParser<LandTradeRequest>(str); break;
			case RequestProtocal.LandTradeApproveRequest: baseRequest = JsonParser<LandTradeApproveRequest>(str); break;
			case RequestProtocal.AllianceRequest: baseRequest = JsonParser<AllianceRequest>(str); break;
			case RequestProtocal.AllianceApproveRequest: baseRequest = JsonParser<AllianceApproveRequest>(str); break;
			case RequestProtocal.AllianceResultRequest: baseRequest = JsonParser<AllianceResultRequest>(str); break;
			case RequestProtocal.AllianceBetrayRequest: baseRequest = JsonParser<AllianceBetrayRequest>(str); break;
			case RequestProtocal.SkillCardRequest: baseRequest = JsonParser<SkillCardRequest>(str); break;
		}

		return baseRequest;
	}

	T JsonParser<T>(string jsonString)
	{
		return JsonUtility.FromJson<T>(jsonString);
	}

	// --- 하트비트 전송 함수 추가 ---
	private void SendHeartbeat()
	{
		HeartbeatRequest heartbeat = new HeartbeatRequest();

		SendMessageOn(heartbeat);

		// 디버깅이 필요하다면 아래 주석을 해제하세요.
		// Debug.Log("[TCP] Heartbeat sent to server.");
	}

	protected override void OnApplicationQuit()
	{
		isAppQuitting = true;

		// 1. 서버에 나간다고 즉시 패킷 전송
		GameOutRequestOn();

		// 2. [핵심] 소켓 강제 종료 (서버가 연결 끊김을 즉시 알아차리게 함)
		if (stream != null)
		{
			stream.Close();
			stream = null;
		}
		if (client != null)
		{
			client.Close();
			client = null;
		}

		// 3. 싱글톤 잠금
		base.OnApplicationQuit();
	}

	private void OnDisable()
	{
		if (!isAppQuitting)
		{
			GameOutRequestOn();
		}
	}

	public void GameOutRequestOn()
	{
		DataManager dataMgr = UnityEngine.Object.FindFirstObjectByType<DataManager>();

		// 씬에 DataManager가 있고 방에 들어가 있는 상태라면
		if (roomDataIndex != -1 && dataMgr != null && dataMgr.playerData != null)
		{
			GameOutRequest gameOutRequest = new GameOutRequest()
			{
				outPlayerEnum = dataMgr.playerData.pe,
				roomDataIndex = this.roomDataIndex // [추가] 방 번호도 확실히 세팅
			};

			// 1. JSON 문자열로 변환
			string jsonStr = JsonUtility.ToJson(gameOutRequest);

			// 2. [핵심] 큐에 넣는 오버로딩 함수 대신, 즉시 전송하는 함수를 직접 호출!
			SendMessageOn(jsonStr);

			// 3. 남은 버퍼 밀어내기
			if (stream != null) stream.Flush();

			roomDataIndex = -1;
			//Debug.Log("[TCP] 종료 패킷 즉시 전송 완료!");
		}
	}

	// 큐에 쌓인 유령 패킷들을 싹 비우는 함수입니다.
	public void ClearQueue()
	{
		sendQueue.Clear();
		queue.Clear();

		// [추가] 청소 중에도 패킷이 들어올 수 있으니 잠그고 비웁니다.
		lock (queueLock)
		{
			receiveQueue.Clear();
		}

		receiveDataOn = data => { };

		//Debug.Log("[TCP] 이전 게임의 패킷 대기열을 안전하게 청소했습니다!");
	}

	public void DisconnectServer()
	{
		// 1. 패킷 수신 및 이벤트 즉시 차단
		receiveOn = false;
		receiveDataOn = data => { };

		// 2. 물리적 통로 파괴 (여기서 기존 백그라운드 스레드가 안전하게 사망합니다)
		if (stream != null) { stream.Close(); stream = null; }
		if (client != null) { client.Close(); client = null; }

		// 3. 큐와 버퍼 완전 초기화
		lock (queueLock)
		{
			receiveQueue.Clear();
		}
		sendQueue.Clear();
		queue.Clear();
		receiveBuffer.Clear();

		//Debug.Log("[TCP] 서버 연결 강제 절단 및 완전 초기화 완료!")
	}
}

[System.Serializable]
public class BaseTCPRequest
{
	public RequestProtocal requestProtocal = RequestProtocal.None;
	public int roomDataIndex = 0;
}

[System.Serializable]
public class GameReadyRequest : BaseTCPRequest
{
	public int maxPlayerCnt = 0;
	public int userCnt = 0;
	public int socketCnt = 0;
	public PlayerEnum playerEnum;
	public bool isOwner = false;
	public MapSizeEnum mapSizeEnum;

	public GameReadyRequest()
	{
		base.requestProtocal = RequestProtocal.GameReady;
	}
}

[System.Serializable]
public class GameOutRequest : BaseTCPRequest
{
	public PlayerEnum playerEnum; //게임 시작전에만 사용

	public PlayerEnum outPlayerEnum = PlayerEnum.Player_None;
	public bool isOwner = false;

	public GameOutRequest()
	{
		base.requestProtocal = RequestProtocal.GameOutOn;
	}
}

[System.Serializable]
public class GameStartOn : BaseTCPRequest
{
	public GameStartOn()
	{
		base.requestProtocal = RequestProtocal.GameStartOn;
	}
}

[System.Serializable]
public class MapCreateRequestOn : BaseTCPRequest
{
	public List<PlayerData> playerDataList = new List<PlayerData>();
	public List<AreaData> area = new List<AreaData>();

	public MapCreateRequestOn()
	{
		base.requestProtocal = RequestProtocal.MapCreateOn;
	}
}

[System.Serializable]
public class TurnRequest : BaseTCPRequest
{
	public PlayerEnum playerEnum = PlayerEnum.Player_0;
	public bool turnStartOn = false;
	public List<SendAreaData> areaDataList = new List<SendAreaData>();

	public TurnRequest()
	{
		base.requestProtocal = RequestProtocal.TurnRequest;
	}
}

[System.Serializable]
public class AttackRequest : BaseTCPRequest
{
	public SendAreaData fromAreaData;
	public SendAreaData toAreaData;

	public DiceWarData fromDiceWarData;
	public DiceWarData toDiceWarData;

	public AttackRequest()
	{
		base.requestProtocal = RequestProtocal.AttackRequest;
	}
}

[System.Serializable]
public class LandTradeRequest : BaseTCPRequest
{
	public PlayerEnum fromPlayerEnum;
	public PlayerEnum toPlayerEnum;
	public AreaData areaData;
	public int coinCount;
	public bool buyOn = false;


	public LandTradeRequest()
	{
		base.requestProtocal = RequestProtocal.LandTradeRequest;
	}
}

[System.Serializable]
public class LandTradeApproveRequest : BaseTCPRequest
{
	public LandTradeRequest landTradeRequest;
	public bool approveOn;

	public LandTradeApproveRequest()
	{
		base.requestProtocal = RequestProtocal.LandTradeApproveRequest;
	}
}

[System.Serializable]
public class AllianceRequest : BaseTCPRequest
{
	public AllianceData orderData;
	public List<AllianceData> allianceDataList = new List<AllianceData>();

	public AllianceRequest()
	{
		base.requestProtocal = RequestProtocal.AllianceRequest;
	}
}

[System.Serializable]
public class AllianceApproveRequest : BaseTCPRequest
{
	public AllianceData orderData;
	public PlayerEnum playerEnum;
	public bool approveOn;

	public AllianceApproveRequest()
	{
		base.requestProtocal = RequestProtocal.AllianceApproveRequest;
	}
}

[System.Serializable]
public class AllianceResultRequest : BaseTCPRequest
{
	public AllianceData orderData;
	public List<AllianceData> allianceDataList = new List<AllianceData>();

	public AllianceResultRequest()
	{
		base.requestProtocal = RequestProtocal.AllianceResultRequest;
	}
}


[System.Serializable]
public class AllianceBetrayRequest : BaseTCPRequest
{
	public AllianceData orderData;
	public List<AllianceData> allianceDataList = new List<AllianceData>();

	public PlayerEnum betrayPlayerEnum;

	public AllianceBetrayRequest()
	{
		base.requestProtocal = RequestProtocal.AllianceBetrayRequest;
	}
}

[System.Serializable]
public class SkillCardRequest : BaseTCPRequest
{
	public PlayerEnum playerEnum;
	public int skillCardCount = 0;

	public SkillCardRequest()
	{
		base.requestProtocal = RequestProtocal.SkillCardRequest;
	}
}

[System.Serializable]
public class HeartbeatRequest : BaseTCPRequest
{
	public HeartbeatRequest()
	{
		base.requestProtocal = RequestProtocal.Heartbeat;
	}
}

public enum RequestProtocal
{
	None = 0,
	GameReady,
	GameOutOn,
	GameStartOn,
	MapCreateOn,
	TurnRequest,
	AttackRequest,
	LandTradeRequest,
	LandTradeApproveRequest,
	AllianceRequest,
	AllianceApproveRequest,
	AllianceResultRequest,
	AllianceBetrayRequest,
	SkillCardRequest,
	Heartbeat = 99
}

[System.Serializable]
// 유저 정보 클레스
public class User
{
	public string Name;

	public string Message;
}