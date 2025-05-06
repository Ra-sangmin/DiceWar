using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;

public class ServerTest : MonoBehaviour
{
    string clientName;

    TcpClient socket;
    NetworkStream stream;
    StreamWriter writer;
    StreamReader reader;
    bool socketReady;

    public PlayerEnum playerEnum;
    public PlayerEnum toPlayerEnum;

    private void Awake()
    {
        ServerManager.Instance.receiveDataOn += ReceiveDataOn;
    }

    private void OnDestroy()
    {
        ServerManager.Instance.receiveDataOn -= ReceiveDataOn;
    }

    private void ReceiveDataOn(BaseRequest baseRequest) 
    {
        switch (baseRequest.requestProtocal)
        {
            case RequestProtocal.TurnRequest:
                TurnRequest turnRequest = (TurnRequest)baseRequest;
                Debug.LogWarning($"playerEnum = {turnRequest.playerEnum} ,  turn = {turnRequest.turnStartOn}");
                break;

            case RequestProtocal.LandTradeRequest:
                LandTradeRequest landTradeRequest = (LandTradeRequest)baseRequest;
                //Debug.LogWarning($"playerEnum = {landTradeRequest.fromPlayerEnum} ,  turn = {landTradeRequest.areaIndex} , buyOn = {landTradeRequest.buyOn}");
                break;

            case RequestProtocal.LandTradeApproveRequest:
                LandTradeApproveRequest LandTradeApproveRequest = (LandTradeApproveRequest)baseRequest;
                break;

            case RequestProtocal.AllianceRequest:

                AllianceRequest AllianceRequest = (AllianceRequest)baseRequest;
                break;

            case RequestProtocal.AllianceApproveRequest:

                AllianceApproveRequest AllianceApproveRequest = (AllianceApproveRequest)baseRequest;
                break;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        

        //ServerManager.Instance.ConnectToServer();
        //ConnectToServer();
    }

    // Update is called once per frame


    //public void ConnectToServer()
    //{
    //    // 이미 연결되었다면 함수 무시
    //    if (socketReady) return;

    //    // 기본 호스트/ 포트번호
    //    //string ip = IPInput.text == "" ? "127.0.0.1" : IPInput.text;
    //    //int port = PortInput.text == "" ? 7777 : int.Parse(PortInput.text);

    //    string ip = "ec2-52-78-148-28.ap-northeast-2.compute.amazonaws.com";
    //    //int port = 2031;
    //    int port = 8000;

    //    // 소켓 생성
    //    try
    //    {
    //        socket = new TcpClient(ip, port);
    //        stream = socket.GetStream();
    //        writer = new StreamWriter(stream);
    //        reader = new StreamReader(stream);
    //        socketReady = true;
    //    }
    //    catch (Exception e)
    //    {
    //        Chat.instance.ShowMessage($"소켓에러 : {e.Message}");
    //    }
    //}

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            //ServerManager.Instance.TurnRequestOn(playerEnum,true);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            //ServerManager.Instance.TurnRequestOn(playerEnum, false);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            //ServerManager.Instance.LandTradeRequestOn(playerEnum, true);
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            //ServerManager.Instance.LandTradeRequestOn(playerEnum, false);
        }
    }

    public int maxCnt;
    public int userCnt;

    public void GameReadyClickOn()
    {
        ServerManager.Instance.GameReadyRequestOn(maxCnt, userCnt);
    }

    public void GameStartClickOn()
    {
        ServerManager.Instance.SendMessageOn(new GameStartOn());
    }

    public void TurnEndBtnClickOn()
    {
        //ServerManager.Instance.TurnRequestOn(playerEnum, false);
    }

    public void LandTradeClickOn()
    {
        //ServerManager.Instance.LandTradeRequestOn(playerEnum, toPlayerEnum, false);
    }

    public void LandTradeApproveClickOn()
    {
        //ServerManager.Instance.LandTradeApproveRequestOn(playerEnum, toPlayerEnum, false, true);
    }

    public void AllianceClickOn()
    {
        AllianceData orderData = new AllianceData()
        {
            playerEnum = playerEnum,
            coinCount = 5
        };
        
        List< AllianceData > allianceDataList = new List<AllianceData>() 
        {
            new AllianceData() 
            {
                playerEnum = PlayerEnum.Player_1,
                coinCount = 5
            }
        };

        //ServerManager.Instance.AllianceRequestOn(orderData, allianceDataList);
    }

    public void AllianceApproveClickOn()
    {
        //ServerManager.Instance.AllianceApproveRequestOn(playerEnum,true);
    }

    //    void OnIncomingData(string data)
    //    {
    //        if (data == "%NAME")
    //        {
    //            clientName = "Guest" + UnityEngine.Random.Range(1000, 10000);
    //            Send($"&NAME|{clientName}");
    //            return;
    //        }

    //        Debug.LogWarning(data);

    //        //Chat.instance.ShowMessage(data);
    //    }

    //    void Send(string data)
    //    {
    //        if (!socketReady) return;

    //        Debug.LogWarning(data);

    //        writer.WriteLine(data);
    //        writer.Flush();
    //    }

    //    public void OnSendButton(InputField SendInput)
    //    {
    //#if (UNITY_EDITOR || UNITY_STANDALONE)
    //        if (!Input.GetButtonDown("Submit")) return;
    //        SendInput.ActivateInputField();
    //#endif
    //        if (SendInput.text.Trim() == "") return;

    //        string message = SendInput.text;
    //        SendInput.text = "";
    //        Send(message);
    //    }


    //    void OnApplicationQuit()
    //    {
    //        CloseSocket();
    //    }

    //    void CloseSocket()
    //    {
    //        if (!socketReady) return;

    //        writer.Close();
    //        reader.Close();
    //        socket.Close();
    //        socketReady = false;
    //    }
}
