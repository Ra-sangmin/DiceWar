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
	//private TcpClient tcpClient;
	//private NetworkStream stream;

	//private Socket _socket = null;
	//byte[] _recvBuffer = new byte[10240];

	private float heartbeatInterval = 20f; // 20초마다 전송
	private float heartbeatTimer = 0f;

	private Queue<string> sendQueue = new Queue<string>();

    private Queue<BaseTCPRequest> queue = new Queue<BaseTCPRequest>();

	private Queue<string> receiveQueue = new Queue<string>();

    public UnityAction<BaseTCPRequest> receiveDataOn = data => { };

    public int roomDataIndex = -1;

    public bool receiveOn = true;

    public TcpClientExample tcpClientTest;


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
		try
		{
			// [추가] 기존에 남아있는 찌꺼기 연결 확실히 치우기
			if (stream != null) { stream.Close(); stream = null; }
			if (client != null) { client.Close(); client = null; }

			string IP = "52.78.148.28";
			int PORT = 8000;

			client = new TcpClient();
			client.Connect(IPAddress.Parse(IP), PORT);
			stream = client.GetStream();

			receiveThread = new Thread(ReceiveLoop);
			receiveThread.IsBackground = true;
			receiveThread.Start();
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
				int byteCount = stream.Read(buffer, 0, buffer.Length);
				if (byteCount <= 0)
				{
					Debug.LogWarning("서버 연결 끊김");
					break;
				}

				// 받은 데이터 버퍼에 저장
				receiveBuffer.AddRange(buffer[..byteCount]);

				// 패킷 파싱 시도
				ParsePackets();
			}
			catch (Exception e)
			{
				Debug.LogWarning($"예외: {e.Message}");
				break;
			}
        }
    }

    private void ParsePackets()
    {
        while (true)
        {
            // 최소한 4바이트는 있어야 길이 읽을 수 있음
            if (receiveBuffer.Count < 4)
                return;

            // 길이 읽기
            int packetLength = BitConverter.ToInt32(receiveBuffer.ToArray(), 0);

            // 전체 패킷이 도착했는지 확인
            if (receiveBuffer.Count < 4 + packetLength)
                return;

            // 패킷 추출
            byte[] packetData = receiveBuffer.GetRange(4, packetLength).ToArray();

            // 버퍼에서 제거
            receiveBuffer.RemoveRange(0, 4 + packetLength);

            // 받은 데이터 처리 (예: 문자열로 출력)
            string jsonString = Encoding.UTF8.GetString(packetData);

			receiveQueue.Enqueue(jsonString);

			//Debug.LogWarning($"받은 메시지: {jsonString}");
		}
    }

	public void SendMessageOn(string jsonUserString)
	{
		try
		{
			// 1. 이미 연결이 끊어졌다고 유니티가 알고 있다면 선제적으로 재연결
			if (client == null || !client.Connected || stream == null)
			{
				Debug.Log("[TCP] 연결이 없어서 다시 뚫습니다.");
				ConnectToServer();
			}

			// 2. 패킷 전송
			byte[] data = Encoding.UTF8.GetBytes(jsonUserString);
			byte[] length = BitConverter.GetBytes(data.Length);
			byte[] packet = new byte[length.Length + data.Length];
			Buffer.BlockCopy(length, 0, packet, 0, 4); // int는 4바이트
			Buffer.BlockCopy(data, 0, packet, 4, data.Length);

			stream.Write(packet, 0, packet.Length);
			stream.Flush();
		}
		catch (System.IO.IOException e) // <--- 미쿠짱이 본 그 에러를 여기서 잡습니다!
		{
			Debug.LogWarning($"[TCP] 통신 끊김 감지! 재연결 시도 중... : {e.Message}");

			// 3. 강제 재연결
			ConnectToServer();

			// 4. 재연결에 성공했다면, 아까 못 보낸 패킷 다시 쏘기
			if (client != null && client.Connected && stream != null)
			{
				byte[] data = Encoding.UTF8.GetBytes(jsonUserString);
				byte[] length = BitConverter.GetBytes(data.Length);
				byte[] packet = new byte[length.Length + data.Length];
				Buffer.BlockCopy(length, 0, packet, 0, 4);
				Buffer.BlockCopy(data, 0, packet, 4, data.Length);

				stream.Write(packet, 0, packet.Length);
				stream.Flush();
				Debug.Log("[TCP] 자동 재연결 및 패킷 복구 완벽 성공!");
			}
		}
		catch (Exception e)
		{
			Debug.LogError($"[TCP] 치명적 전송 에러: {e.Message}");
		}
	}

	public void GameReadyRequestOn(int maxPlayerCnt , int userCnt)
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
        if (sendQueue.Count != 0)
        {
            SendMessageOn(sendQueue.Dequeue());
        }
    }

    private void Update()
    {
        SendQueueListCheck();
        QueueListCheck();
        ReceiveListDataCheck();

		// 하트비트 타이머 체크
		heartbeatTimer += Time.deltaTime;
		if (heartbeatTimer >= heartbeatInterval)
		{
			SendHeartbeat();
			heartbeatTimer = 0f;
		}
	}

    void QueueListCheck()
    {
        if (receiveOn && queue.Count > 0)
        {
            receiveDataOn(queue.Dequeue());
        }
    }

    void ReceiveListDataCheck()
    {
        if (receiveQueue.Count == 0)
            return;

		string checkJsonData = receiveQueue.Dequeue();

		ReceiveRequestDataOn(checkJsonData);
	}

    private void ReceiveRequestDataOn(string str)
    {
		BaseTCPRequest baseRequest = GetBaseReqeust(str);

		if (baseRequest.requestProtocal == RequestProtocal.GameReady)
        {
			roomDataIndex = baseRequest.roomDataIndex;
		}

        if (baseRequest != null)
        {
            queue.Enqueue(baseRequest);
        }
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
		// 큐를 거치지 않고 직접 쏘거나, SendMessageOn을 통해 큐에 넣을 수 있습니다.
		// 여기서는 안전하게 큐에 넣는 방식을 사용합니다.
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
			Debug.Log("[TCP] 종료 패킷 즉시 전송 완료!");
		}
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

//[System.Serializable]
//public class GameEndOnRequest : BaseTCPRequest
//{
//    public PlayerEnum outPlayerEnum = PlayerEnum.Player_None;
//    public bool isOwner = false;

//    public GameEndOnRequest()
//    {
//        base.requestProtocal = RequestProtocal.GameEndOn;
//    }
//}

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