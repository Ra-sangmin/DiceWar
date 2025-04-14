using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Experimental.AI;

public class ServerManager : MonoSingleton<ServerManager>
{
    private Socket _socket = null;
    byte[] _recvBuffer = new byte[10240];

    private Queue<BaseRequest> queue = new Queue<BaseRequest>();

    public UnityAction<BaseRequest> receiveDataOn = data => { };

    public int roomDataIndex;

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
    async void SendMessageOn(string jsonUserString)
    {
        Debug.Log("Send : " + jsonUserString);

        if(_socket == null)
        {
            return;
        }

        Byte[] sendData = System.Text.Encoding.UTF8.GetBytes(jsonUserString);

        //Debug.LogWarning(sendData.Length);

        try
        {
            _socket.BeginSend(sendData, 0, sendData.Length, SocketFlags.None, new AsyncCallback(SendComplete), null);

            await Task.Delay(1000);

            //_socket.Send(sendData);
        }

        catch (Exception e)
        {
            // 위 함수에서 뻑이 난다면 아래 에러 메시지 출력
            Debug.LogError("send fail exception = " + e.Message);

            // 서버 퇴장
            Shutdown();
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

            Shutdown();
        }

    }

    public void GameReadyRequestOn(int maxPlayerCnt , int userCnt)
    {
        GameReadyRequest gameReadyRequest = new GameReadyRequest()
        {
            maxPlayerCnt = maxPlayerCnt,
            userCnt = userCnt,
        };
        string jsonStr = JsonUtility.ToJson(gameReadyRequest);

        SendMessageOn(jsonStr);
    }


    public void GameStartRequestOn()
    {
        GameStartOn gameStartOn = new GameStartOn()
        {
            roomDataIndex = roomDataIndex,
        };
        string jsonStr = JsonUtility.ToJson(gameStartOn);

        SendMessageOn(jsonStr);
    }

    public void MapCreateRequestOn()
    {
        MapCreateRequestOn mapCreateRequestOn = new MapCreateRequestOn()
        {
            roomDataIndex = roomDataIndex,
            playerDataList = InGameDataManager.Instance.playerDataList,
            area = InGameDataManager.Instance.areaDataList,
            //cel = mapCreater;//.GetCell(),
            //mapCreater = mapCreater,
        };
        string jsonStr = JsonUtility.ToJson(mapCreateRequestOn);

        SendMessageOn(jsonStr);
    }


    public void TurnRequestOn(TurnRequest turnRequest) 
    {
        turnRequest.roomDataIndex = roomDataIndex;
        string jsonStr = JsonUtility.ToJson(turnRequest);

        SendMessageOn(jsonStr);
    }

    public void AttackRequestOn(AttackRequest attackRequest)
    {
        attackRequest.roomDataIndex = roomDataIndex;
        string jsonStr = JsonUtility.ToJson(attackRequest);

        SendMessageOn(jsonStr);
    }
    

    public void LandTradeRequestOn(LandTradeRequest landTradeRequest )
    {
        landTradeRequest.roomDataIndex = roomDataIndex;
        string jsonStr = JsonUtility.ToJson(landTradeRequest);

        SendMessageOn(jsonStr);
    }

    public void LandTradeApproveRequestOn(LandTradeApproveRequest landTradeApproveRequest)
    {
        landTradeApproveRequest.roomDataIndex = roomDataIndex;
        string jsonStr = JsonUtility.ToJson(landTradeApproveRequest);

        SendMessageOn(jsonStr);
    }

    public void AllianceRequestOn(AllianceRequest allianceRequest)
    {
        allianceRequest.roomDataIndex = roomDataIndex;
        string jsonStr = JsonUtility.ToJson(allianceRequest);

        SendMessageOn(jsonStr);
    }

    public void AllianceApproveRequestOn(AllianceApproveRequest allianceApproveRequest)
    {
        allianceApproveRequest.roomDataIndex = roomDataIndex;
        string jsonStr = JsonUtility.ToJson(allianceApproveRequest);

        SendMessageOn(jsonStr);
    }

    public void AllianceResultRequestOn(AllianceResultRequest allianceRequest)
    {
        allianceRequest.roomDataIndex = roomDataIndex;
        string jsonStr = JsonUtility.ToJson(allianceRequest);

        SendMessageOn(jsonStr);
    }


    public void AllianceBetrayRequestOn(AllianceBetrayRequest allianceBetrayRequest)
    {
        allianceBetrayRequest.roomDataIndex = roomDataIndex;
        string jsonStr = JsonUtility.ToJson(allianceBetrayRequest);

        SendMessageOn(jsonStr);
    }

    private void Update()
    {
        QueueListCheck();
        receiveMessage();
    }

    void QueueListCheck()
    {
        if (queue.Count > 0)
        {
            receiveDataOn(queue.Dequeue());
        }
    }

    void receiveMessage()
    {
        if (_socket == null)
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
            Shutdown();
        }
    }


    private void ReceiveComplete(IAsyncResult ar)
    {
        // 메시지를 받는게 성공하면 콜백되는 함수 입니다.
        try
        {
            if (null == _socket)
            {
                Debug.Log("socket is null");
                return;
            }

            int len = _socket.EndReceive(ar);

            if (len == 0)
            {
                //Shutdown();
            }
            else
            {
                byte[] cuttingBuffer = new byte[len];

                for (int i = 0; i < len; i++)
                {
                    cuttingBuffer[i] = _recvBuffer[i];
                }

                string jsonString = System.Text.Encoding.UTF8.GetString(cuttingBuffer);

                //Debug.LogWarning(jsonString);

                ReceiveRequestDataOn(jsonString);

                //출력이 끝나면 버퍼 초기화
                System.Array.Clear(_recvBuffer, 0, _recvBuffer.Length);
            }
        }

        catch (Exception e)
        {
            Debug.LogError("Receive Exception: " + e.Message);
            Shutdown();
        }

    }

    private void ReceiveRequestDataOn(string jsonString)
    {
        BaseRequest baseRequest = null;

        RequestProtocal requestProtocal = JsonUtility.FromJson<BaseRequest>(jsonString).requestProtocal;

        Debug.LogWarning(jsonString);

        switch (requestProtocal)
        {
            case RequestProtocal.GameReady:

                GameReadyRequest gameReadyRequest = JsonUtility.FromJson<GameReadyRequest>(jsonString);

                roomDataIndex = gameReadyRequest.roomDataIndex;

                //Debug.LogWarning("playerEnum = "+gameReadyRequest.playerEnum);

                baseRequest = gameReadyRequest;

                break;

            case RequestProtocal.GameStartOn:

                GameStartOn GameStartOn = JsonUtility.FromJson<GameStartOn>(jsonString);

                //Debug.LogWarning(GameStartOn.roomDataIndex);

                baseRequest = GameStartOn;

                break;

            case RequestProtocal.MapCreateOn:

                MapCreateRequestOn mapCreateRequestOn = JsonUtility.FromJson<MapCreateRequestOn>(jsonString);

                //Debug.LogWarning(GameStartOn.roomDataIndex);

                baseRequest = mapCreateRequestOn;

                break;

            case RequestProtocal.TurnRequest:

                TurnRequest turnRequest = JsonUtility.FromJson<TurnRequest>(jsonString);

                baseRequest = turnRequest;

                break;

            case RequestProtocal.AttackRequest:

                AttackRequest attackRequest = JsonUtility.FromJson<AttackRequest>(jsonString);

                baseRequest = attackRequest;

                break;

            case RequestProtocal.LandTradeRequest:

                LandTradeRequest landTradeRequest = JsonUtility.FromJson<LandTradeRequest>(jsonString);

                baseRequest = landTradeRequest;

                //Debug.LogWarning($"playerEnum = {landTradeRequest.playerEnum} ,  turn = {landTradeRequest.areaData.areaIndex} , buyOn = {landTradeRequest.buyOn}");

                break;

            case RequestProtocal.LandTradeApproveRequest:

                LandTradeApproveRequest landTradeApproveRequest = JsonUtility.FromJson<LandTradeApproveRequest>(jsonString);

                baseRequest = landTradeApproveRequest;

                //Debug.LogWarning($"playerEnum = {landTradeRequest.playerEnum} ,  turn = {landTradeRequest.areaData.areaIndex} , buyOn = {landTradeRequest.buyOn}");

                break;

            case RequestProtocal.AllianceRequest:

                AllianceRequest allianceRequest = JsonUtility.FromJson<AllianceRequest>(jsonString);

                baseRequest = allianceRequest;

                //Debug.LogWarning($"playerEnum = {landTradeRequest.playerEnum} ,  turn = {landTradeRequest.areaData.areaIndex} , buyOn = {landTradeRequest.buyOn}");

                break;

            case RequestProtocal.AllianceApproveRequest:

                AllianceApproveRequest allianceApproveRequest = JsonUtility.FromJson<AllianceApproveRequest>(jsonString);

                baseRequest = allianceApproveRequest;

                //Debug.LogWarning($"playerEnum = {landTradeRequest.playerEnum} ,  turn = {landTradeRequest.areaData.areaIndex} , buyOn = {landTradeRequest.buyOn}");

                break;

            case RequestProtocal.AllianceResultRequest:

                AllianceResultRequest allianceResultRequest = JsonUtility.FromJson<AllianceResultRequest>(jsonString);

                baseRequest = allianceResultRequest;

                //Debug.LogWarning($"playerEnum = {landTradeRequest.playerEnum} ,  turn = {landTradeRequest.areaData.areaIndex} , buyOn = {landTradeRequest.buyOn}");

                break;

            case RequestProtocal.AllianceBetrayRequest:

                AllianceBetrayRequest allianceBetrayRequest = JsonUtility.FromJson<AllianceBetrayRequest>(jsonString);

                baseRequest = allianceBetrayRequest;

                //Debug.LogWarning($"playerEnum = {landTradeRequest.playerEnum} ,  turn = {landTradeRequest.areaData.areaIndex} , buyOn = {landTradeRequest.buyOn}");

                break;
        }

        if (baseRequest != null)
        {
            queue.Enqueue(baseRequest);
            //receiveDataOn(baseRequest);
        }
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
}

[System.Serializable]
public class BaseRequest
{
    public RequestProtocal requestProtocal = RequestProtocal.None;
    public int roomDataIndex = 0;
}

[System.Serializable]
public class GameReadyRequest : BaseRequest
{
    public int maxPlayerCnt = 0;
    public int userCnt = 0;
    public int socketCnt = 0;
    public PlayerEnum playerEnum;

    public GameReadyRequest()
    {
        base.requestProtocal = RequestProtocal.GameReady;
    }
}

[System.Serializable]
public class GameStartOn : BaseRequest
{
    public GameStartOn()
    {
        base.requestProtocal = RequestProtocal.GameStartOn;
    }
}

[System.Serializable]
public class MapCreateRequestOn : BaseRequest
{
    public List<PlayerData> playerDataList = new List<PlayerData>();
    public List<AreaData> area = new List<AreaData>();

    public MapCreateRequestOn()
    {
        base.requestProtocal = RequestProtocal.MapCreateOn;
    }
}

[System.Serializable]
public class TurnRequest : BaseRequest
{
    public PlayerEnum playerEnum = PlayerEnum.Player_0;
    public bool turnStartOn = false;
    public List<AreaData> areaDataList = new List<AreaData>();

    public TurnRequest()
    {
        base.requestProtocal = RequestProtocal.TurnRequest;
    }
}

[System.Serializable]
public class AttackRequest : BaseRequest
{
    public AreaData fromAreaData;
    public AreaData toAreaData;

    public AttackRequest()
    {
        base.requestProtocal = RequestProtocal.AttackRequest;
    }
}

[System.Serializable]
public class LandTradeRequest : BaseRequest
{
    public PlayerEnum fromPlayerEnum;
    public PlayerEnum toPlayerEnum;
    public AreaData areaData;
    public bool buyOn = false;


    public LandTradeRequest()
    {
        base.requestProtocal = RequestProtocal.LandTradeRequest;
    }
}

[System.Serializable]
public class LandTradeApproveRequest : BaseRequest
{
    public LandTradeRequest landTradeRequest;
    public bool approveOn;

    public LandTradeApproveRequest()
    {
        base.requestProtocal = RequestProtocal.LandTradeApproveRequest;
    }
}

[System.Serializable]
public class AllianceRequest : BaseRequest
{
    public AllianceData orderData;
    public List<AllianceData> allianceDataList = new List<AllianceData>();

    public AllianceRequest()
    {
        base.requestProtocal = RequestProtocal.AllianceRequest;
    }
}

[System.Serializable]
public class AllianceApproveRequest : BaseRequest
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
public class AllianceResultRequest : BaseRequest
{
    public AllianceData orderData;
    public List<AllianceData> allianceDataList = new List<AllianceData>();

    public AllianceResultRequest()
    {
        base.requestProtocal = RequestProtocal.AllianceResultRequest;
    }
}


[System.Serializable]
public class AllianceBetrayRequest : BaseRequest
{
    public PlayerEnum playerEnum;

    public AllianceBetrayRequest()
    {
        base.requestProtocal = RequestProtocal.AllianceBetrayRequest;
    }
}

public enum RequestProtocal
{
    None = 0,
    GameReady,
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
}

[System.Serializable]
// 유저 정보 클레스
public class User
{
    public string Name;

    public string Message;
}