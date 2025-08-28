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


	void Start()
    {
        ConnectToServer();
    }

    public void ConnectToServer()
    {
		string IP = "52.78.148.28";
		int PORT = 8000;

		client = new TcpClient();
		client.Connect(IPAddress.Parse(IP), PORT);
		stream = client.GetStream();

		receiveThread = new Thread(ReceiveLoop);
		receiveThread.IsBackground = true;
		receiveThread.Start();
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
		if (stream == null) return;

		byte[] data = Encoding.UTF8.GetBytes(jsonUserString);
		byte[] length = BitConverter.GetBytes(data.Length);

		byte[] packet = new byte[length.Length + data.Length];
		Buffer.BlockCopy(length, 0, packet, 0, length.Length);
		Buffer.BlockCopy(data, 0, packet, length.Length, data.Length);

		stream.Write(packet, 0, packet.Length);
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

    public void GameOutRequestOn()
    {
        if (roomDataIndex != -1)
        {
            GameOutRequest gameOutRequest = new GameOutRequest()
            {
                outPlayerEnum = DataManager.Instance.playerData.pe,
            };

            SendMessageOn(gameOutRequest);

            roomDataIndex = -1;
        }
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

    private void OnDisable()
    {
        GameOutRequestOn();
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
}

[System.Serializable]
// 유저 정보 클레스
public class User
{
    public string Name;

    public string Message;
}