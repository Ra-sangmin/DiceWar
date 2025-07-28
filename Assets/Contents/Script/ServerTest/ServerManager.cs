using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.Events;

public class ServerManager : MonoSingleton<ServerManager>
{
    private TcpClient tcpClient;
    private NetworkStream stream;

    private Socket _socket = null;
    byte[] _recvBuffer = new byte[10240];

    private Queue<string> sendQueue = new Queue<string>();

    private Queue<BaseTCPRequest> queue = new Queue<BaseTCPRequest>();

    private List<string> receiveList = new List<string>();

    public UnityAction<BaseTCPRequest> receiveDataOn = data => { };

    public int roomDataIndex = -1;

    private string beforeData = string.Empty;

    public bool receiveOn = true;

    void Start()
    {
        ConnectToServer();
    }

    public void ConnectToServer()
    {
        // tcp서버를 선언
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        string IP = "ec2-52-78-148-28.ap-northeast-2.compute.amazonaws.com";
        int PORT = 8000;

        // 서버 연결
        _socket.Connect(IP, PORT);

        if (_socket.Connected)
        {
            // 서버연결됨
            Debug.Log("connected");

            //sendMessage();
        }
        else
        {
            // 서버 연결 실패
            Debug.Log("fail to connect");
        }
    }

    //이렇게 하면 서버에 접속 완료
    private void SendMessageOn(string jsonUserString)
    {
        //Debug.Log("Send : " + jsonUserString);

        if(_socket == null)
        {
            return;
        }

        Byte[] sendData = System.Text.Encoding.UTF8.GetBytes(jsonUserString);

        try
        {
            //_socket.BeginSend(sendData, 0, sendData.Length, SocketFlags.None, new AsyncCallback(SendComplete), null);

            _socket.Send(sendData, 0, sendData.Length, SocketFlags.None);
        }

        catch (Exception e)
        {
            // 위 함수에서 뻑이 난다면 아래 에러 메시지 출력
            Debug.LogError("send fail exception = " + e.Message);

            // 서버 퇴장
            //Shutdown();
        }
    }

    private void SendComplete(IAsyncResult ar)
    {
        // 메시지 보네는게 성공하면 콜백 되는 함수 입니다.
        try
        {
            if (null == _socket)
            {
                return;
            }

            int len = _socket.EndSend(ar);

            if (len == 1)
            {
                Debug.Log("Send success");
            }
        }

        catch (Exception e)
        {
            Debug.LogError("Send Exception: " + e.Message);

            //Shutdown();
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

    public void GameOutRequestOn()
    {
        if (roomDataIndex != -1)
        {
            GameOutRequest gameOutRequest = new GameOutRequest()
            {
                outPlayerEnum = DataManager.Instance.playerData.playerEnum,
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

        //if (request.requestProtocal == RequestProtocal.GameOutOn)
        //{
        //    SendMessageOn(jsonStr);
        //}
        //else
        {
            sendQueue.Enqueue(jsonStr);
        }
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
        receiveMessage();
        ReceiveListDataCheck();
    }

    void QueueListCheck()
    {
        if (receiveOn && queue.Count > 0)
        {
            receiveDataOn(queue.Dequeue());
        }
    }

    void receiveMessage()
    {
        if (_socket == null || _socket.Available != 0)
            return;
        
        try
        {
            // 메시지를 받습니다. 이또한 패킷형태로 받아야 하지만 잘 모르니 데이터 형태로 받겟습니다.
            _socket.BeginReceive(_recvBuffer, 0, _recvBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveComplete), null);
        }
        catch (Exception e)
        {
            //실패시 에러 메시지
            Debug.LogError("BeginException: " + e.Message);
            //Shutdown();
        }
    }


    private void ReceiveComplete(IAsyncResult ar)
    {
        if (null == _socket)
        {
            //Debug.Log("socket is null");
            return;
        }

        int len = _socket.EndReceive(ar);

        if (len != 0)
        {
            byte[] cuttingBuffer = new byte[len];

            for (int i = 0; i < len; i++)
            {
                cuttingBuffer[i] = _recvBuffer[i];
            }

            string jsonString = System.Text.Encoding.UTF8.GetString(cuttingBuffer);

            receiveList.Add(jsonString);

            //출력이 끝나면 버퍼 초기화
            System.Array.Clear(_recvBuffer, 0, _recvBuffer.Length);
        }
    }

    void ReceiveListDataCheck()
    {
        if (receiveList.Count == 0)
            return;

        string checkJsonData = string.Empty;
        BaseRequest baseRequest = null;

        //Debug.LogWarning(receiveList.Count);

        if (receiveList.Count == 1)
        {
            checkJsonData = receiveList[0];
        }
        else if (receiveList.Count == 2)
        {
            string firstStr = string.Empty;
            string endStr = string.Empty;

            if (receiveList[0].IndexOf("{\"requestProtocal\"") == 0)
            {
                firstStr = receiveList[0];
                endStr = receiveList[1];
            }
            else
            {
                firstStr = receiveList[1];
                endStr = receiveList[0];
            }

            checkJsonData = firstStr + endStr;
        }
        else if (receiveList.Count == 3)
        {
            string firstStr = string.Empty;
            string middleStr = string.Empty;
            string endStr = string.Empty;

            for (int i = 0; i < receiveList.Count; i++)
            {
                string str = receiveList[i];

                if (str.IndexOf("{\"requestProtocal\"") == 0)
                {
                    firstStr = str;
                }
                else if (str.IndexOf("]}]}") != -1)
                {
                    endStr = str;
                }
                else
                {
                    middleStr = str;
                }
            }

            checkJsonData = firstStr + middleStr + endStr;
        }
        else
        {
            receiveList.Clear();
        }

        //if (string.IsNullOrEmpty(checkJsonData) == false) 
        //{
        //    Debug.LogWarning(checkJsonData);
        //    Debug.LogWarning("receiveList = "+ receiveList.Count);
        //}

        baseRequest = GetBaseRequest(checkJsonData);

        if (baseRequest != null)
        {
            ReceiveRequestDataOn(checkJsonData);

            receiveList.Clear();
        }

    }

    BaseRequest GetBaseRequest(string checkJsonData)
    {
        BaseRequest baseRequest = null;

        try
        {
            baseRequest = JsonUtility.FromJson<BaseRequest>(checkJsonData);
        }
        catch (Exception e)
        {
            Debug.LogWarning("fail = " + checkJsonData + " receivelist Count = " + receiveList.Count);
            //Debug.LogWarning(e);
        }

        return baseRequest;
    }

    private void ReceiveRequestDataOn(string str)
    {
        Debug.LogWarning(str);

        BaseTCPRequest baseRequest = null;

        RequestProtocal requestProtocal = JsonUtility.FromJson<BaseTCPRequest>(str).requestProtocal;

        switch (requestProtocal)
        {
            case RequestProtocal.GameReady:

                GameReadyRequest gameReadyRequest = JsonUtility.FromJson<GameReadyRequest>(str);
                roomDataIndex = gameReadyRequest.roomDataIndex;
                baseRequest = gameReadyRequest;
                break;

            case RequestProtocal.GameOutOn:                 baseRequest = JsonParser<GameOutRequest>(str); break;
            case RequestProtocal.GameStartOn:               baseRequest = JsonParser<GameStartOn>(str); break;
            case RequestProtocal.MapCreateOn:               baseRequest = JsonParser<MapCreateRequestOn>(str); break;
            case RequestProtocal.TurnRequest:               baseRequest = JsonParser<TurnRequest>(str); break;
            case RequestProtocal.AttackRequest:             baseRequest = JsonParser<AttackRequest>(str); break;
            case RequestProtocal.LandTradeRequest:          baseRequest = JsonParser<LandTradeRequest>(str); break;
            case RequestProtocal.LandTradeApproveRequest:   baseRequest = JsonParser<LandTradeApproveRequest>(str); break;
            case RequestProtocal.AllianceRequest:           baseRequest = JsonParser<AllianceRequest>(str); break;
            case RequestProtocal.AllianceApproveRequest:    baseRequest = JsonParser<AllianceApproveRequest>(str); break;
            case RequestProtocal.AllianceResultRequest:     baseRequest = JsonParser<AllianceResultRequest>(str); break;
            case RequestProtocal.AllianceBetrayRequest:     baseRequest = JsonParser<AllianceBetrayRequest>(str); break;
            case RequestProtocal.SkillCardRequest:          baseRequest = JsonParser<SkillCardRequest>(str); break;
        }

        if (baseRequest != null)
        {
            queue.Enqueue(baseRequest);
        }
    }

    BaseTCPRequest JsonParser<T>(string jsonString)
    {
        return JsonUtility.FromJson<T>(jsonString) as BaseTCPRequest;
    }

    private void Shutdown()
    {
        // 중간에 뻑났을때 소켓 초기화 해주는 부분
        if (_socket != null)
        {
            try
            {
                Debug.Log("shutdown");

                _socket.Shutdown(SocketShutdown.Both);

                _socket = null;

            }
            catch (Exception e)
            {
                Debug.LogError("Shutdown Exception: " + e.Message);

                _socket = null;
            }
        }

    }

    private void OnDisable()
    {
        GameOutRequestOn();

        Shutdown();
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
    public PlayerEnum playerEnum;

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