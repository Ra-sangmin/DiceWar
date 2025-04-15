using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;
using static UnityEngine.Mesh;
using System.IO;

public class LoadingPopup : MonoBehaviour
{
    [SerializeField] Text titleText;
    [SerializeField] RectTransform timePanel;
    [SerializeField] Text timeText;
    [SerializeField] List<Toggle> toggleList = new List<Toggle>();

    private int titleIndex = 0;
    private int toggleIndex = 0;

    private float delayTime = 0; 

    private string titleValue = string.Empty;

    bool matchngComplatedOn = false;

    bool loadClearOn = false;

    bool isReadyOn = false;

    //private GameReadyRequest gameReadyRequest;

    //private GameStartOn gameStartOn;

    //private MapCreateRequestOn mapCreateRequestOn;
    private void Awake()
    {
        SetEvent();
        //SetData(true);

        
    }
    void SetEvent()
    {
        Observable
            .Timer(TimeSpan.FromSeconds(0.15f))
            .Repeat()
            .Subscribe(_ => ResetData())
            .AddTo(this);
    }
    // Start is called before the first frame update
    void Start()
    {
        if (DataManager.Instance.isMultiOn)
        {
            ServerManager.Instance.receiveDataOn += ReceiveDataOn;
        }
        
    }
    private void OnDestroy()
    {
        if (DataManager.Instance.isMultiOn)
        {
            ServerManager.Instance.receiveDataOn -= ReceiveDataOn;
        }
    }

    private void ResetData()
    {
        //ResetText();
        ResetToggleIndex();
    }

    private void ReceiveDataOn(BaseRequest baseRequest)
    {
        //Debug.LogWarning(baseRequest.requestProtocal);

        switch (baseRequest.requestProtocal)
        {
            case RequestProtocal.GameReady:

                GameReadyRequest gameReadyRequest = (GameReadyRequest)baseRequest;

                if (isReadyOn == false) 
                {
                    isReadyOn = true;
                    DataManager.Instance.SetTurnPosition((int)gameReadyRequest.playerEnum);   
                }

                int maxCnt = DataManager.Instance.num_player;
                int userCnt = maxCnt - DataManager.Instance.off_line_num_player;

                //Debug.LogWarning(maxCnt)

                if (gameReadyRequest.socketCnt == userCnt)
                {
                    ServerManager.Instance.GameStartRequestOn();
                }

                //Debug.LogWarning("GameReady = "+ gameReadyRequest.socketCnt +" , "+ userCnt);

                //Debug.LogWarning($"playerEnum = {turnRequest.playerEnum} ,  turn = {turnRequest.turnStartOn}");
                break;

            case RequestProtocal.GameStartOn:

                //GameStartOn gameStartOn = (GameStartOn)baseRequest;
                //MatchngComplatedOn();

                Observable
                    .Timer(TimeSpan.FromSeconds(1))
                    .Subscribe(_ => LoadingClearOn())
                    .AddTo(this);

                //Debug.LogWarning($"playerEnum = {landTradeRequest.fromPlayerEnum} ,  turn = {landTradeRequest.areaIndex} , buyOn = {landTradeRequest.buyOn}");
                break;

            case RequestProtocal.MapCreateOn:

                MapCreateRequestOn mapCreateRequestOn = (MapCreateRequestOn)baseRequest;

                //DataManager.Instance.SetMapSizeValue();
                DataManager.Instance.areaDataList = mapCreateRequestOn.area;
                DataManager.Instance.SetPlayerColor(mapCreateRequestOn.playerDataList);

                GameSceneLoadOn();

                //Debug.LogWarning($"playerEnum = {landTradeRequest.fromPlayerEnum} ,  turn = {landTradeRequest.areaIndex} , buyOn = {landTradeRequest.buyOn}");
                break;

                //case RequestProtocal.LandTradeApproveRequest:
                //    LandTradeApproveRequest LandTradeApproveRequest = (LandTradeApproveRequest)baseRequest;
                //    break;

                //case RequestProtocal.AllianceRequest:

                //    AllianceRequest AllianceRequest = (AllianceRequest)baseRequest;
                //    break;

                //case RequestProtocal.AllianceApproveRequest:

                //    AllianceApproveRequest AllianceApproveRequest = (AllianceApproveRequest)baseRequest;
                //    break;
        }
    }

    public void SetData(bool multyOn)
    {
        timePanel.gameObject.SetActive(multyOn);

        Observable
                .Timer(TimeSpan.FromSeconds(1f))
                .Repeat()
                .Subscribe(_ => timeCheck())
                .AddTo(this);

        if (multyOn)
        {
            delayTime = -1;
            FindingPlayerOn();
        }
        else
        {
            LoadingClearOn();
        }
    }

    private void SetTitle(string titleValue)
    {
        if (titleText != null)
        {
            titleText.text = titleValue;
        }
        else 
        {
            Debug.LogWarning("null");
        }
    }

    void ResetText()
    {
        string textValue = titleValue;

        titleIndex++;

        if (titleIndex > 4)
        {
            titleIndex = 0;
        }

        for (int i = 0; i < titleIndex; i++) 
        {
            textValue += ".";
        }

        titleText.text = textValue;
    }

    private void ResetToggleIndex()
    {
        toggleIndex++;

        if (toggleIndex >= toggleList.Count)
        {
            toggleIndex = 0;
        }

        toggleList[toggleIndex].isOn = true;
    }

    public void FindingPlayerOn()
    {
        SetTitle("Finding Players...");

        int maxCnt = DataManager.Instance.num_player;
        int userCnt = maxCnt-DataManager.Instance.off_line_num_player;

        ServerManager.Instance.GameReadyRequestOn(maxCnt, userCnt);

        //timeCheck();

        //Observable
        //        .Timer(TimeSpan.FromSeconds(5f))
        //        .Subscribe(_ => MatchngComplatedOn())
        //        .AddTo(this);
    }

    void timeCheck()
    {
        delayTime++;

        int minValue = (int)(delayTime / 60);
        int secValue = (int)(delayTime % 60);

        string timeStr = $"{minValue:d2} : {secValue:d2}";

        timeText.text = timeStr;

        //if (matchngComplatedOn == false && gameStartOn != null)
        //{
        //    //MatchngComplatedOn();
        //}

        //if (mapCreateRequestOn != null)
        //{
        //    GameSceneLoadOn();
        //}

    }

    public void MatchngComplatedOn()
    {
        if (matchngComplatedOn)
            return;

        matchngComplatedOn = true;

        SetTitle("Matching Complated");

        Observable
                .Timer(TimeSpan.FromSeconds(1))
                .Subscribe(_ => LoadingClearOn())
                .AddTo(this);
    }

    public void LoadingClearOn()
    {
        if (loadClearOn) 
        {
            return;
        }

        loadClearOn = true;

        SetTitle("Loading..");

        if (DataManager.Instance.isMultiOn)
        {
            //오너 플레이어 라면 맵 생성 진행 ( 1명이 맵을 생성후 배포 한다 )
            if (DataManager.Instance.playerData.playerEnum == PlayerEnum.Player_0)
            {
                DataManager.Instance.InitMapData();
                DataManager.Instance.CreateMap();

                ServerManager.Instance.MapCreateRequestOn();
            }
        }
        else 
        {
            //var test = Resources.Load<TextAsset>("test");
            ////Debug.LogWarning(test.text);
            //var testData = JsonUtility.FromJson<MapCreater>(test.text);
            //InGameDataManager.Instance.mapCreater = testData;

            DataManager.Instance.InitMapData();
            DataManager.Instance.CreateMap();

            //string jsonStr = JsonUtility.ToJson(InGameDataManager.Instance.mapCreater);
            //Debug.LogWarning(jsonStr);



            //Debug.LogWarning(Application.dataPath);



            //InGameDataManager.Instance.mapCreater = InGameDataManager.Instance.testData.mapCreater;



            //LoadJson();
            //SaveJson();


            //InGameDataManager.Instance.testData.mapCreater;
            GameSceneLoadOn();
        }

        

        //Observable
        //        .Timer(TimeSpan.FromSeconds(2f))
        //        .Subscribe(_ => GameSceneLoadOn())
        //        .AddTo(this);
    }
    private void LoadJson()
    {
        var test = Resources.Load<TextAsset>("test");
        var testData = JsonUtility.FromJson<MapCreater>(test.text);
        //InGameDataManager.Instance.mapCreater = testData;
    }

    private void SaveJson() 
    {
        DataManager.Instance.InitMapData();
        DataManager.Instance.CreateMap();

        //string jsonStr = JsonUtility.ToJson(InGameDataManager.Instance.mapCreater);

        string testPath = $"{Application.dataPath}/Prefabs/Resources/test.txt";

        StreamWriter sw = new StreamWriter(testPath);
        //sw.Write(jsonStr);
        sw.Flush();
        sw.Close();
    }

    private void GameSceneLoadOn()
    {
        //InGameDataManager.Instance.SetMapSize();
        //InGameDataManager.Instance.InitMapData();
        //InGameDataManager.Instance.CreateMap();

        SceneManager.LoadScene("Game");
    }

    

    // Update is called once per frame
    void Update()
    {
        
    }
}
