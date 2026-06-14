using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;

public class LoadingController : MonoBehaviour
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

    private int joinUserCnt;

    bool aIPlayOn = false;

    private float aiStartTime = 10;
	//private float aiStartTime = 2;

    public enum LoadingStatus
    {
        None,
        FindingPlayers,
		MatchingComplated,
		Loading,
	}

    public LoadingStatus loadingStatus = LoadingStatus.Loading;

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
		SoundManager.Instance.PlayBGM(BGMEnum.Loading);

		if (DataManager.Instance.isMultiOn)
        {
			ServerManager.Instance.ClearQueue();

			ServerManager.Instance.receiveDataOn += ReceiveDataOn;

			ServerManager.Instance.receiveOn = true;
		}

        SetData();
    }
	private void OnDestroy()
	{
		// 1. DataManager가 아직 메모리에 존재하는지 먼저 확인
		var dataManager = DataManager.Instance;
		if (dataManager != null && dataManager.isMultiOn)
		{
			// 2. ServerManager도 살아있는지 확인 후 이벤트 해제
			var serverManager = ServerManager.Instance;
			if (serverManager != null)
			{
				serverManager.receiveDataOn -= ReceiveDataOn;
			}
		}
	}

	private void ResetData()
    {
        //ResetText();
        ResetToggleIndex();
    }

    private void ReceiveDataOn(BaseTCPRequest baseRequest)
    {
        //Debug.LogWarning("Protocal = " + baseRequest.requestProtocal);

        switch (baseRequest.requestProtocal)
        {
            case RequestProtocal.GameReady:

                GameReadyRequest gameReadyRequest = (GameReadyRequest)baseRequest;

                if (isReadyOn == false) 
                {
                    isReadyOn = true;

                    DataManager.Instance.SetTurnPosition((int)gameReadyRequest.playerEnum);
                    DataManager.Instance.isOwner = gameReadyRequest.isOwner;

                }

                int maxCnt = DataManager.Instance.num_player;
                int userCnt = maxCnt - DataManager.Instance.off_line_num_player;

                joinUserCnt = gameReadyRequest.socketCnt;

                if (joinUserCnt == userCnt && DataManager.Instance.isOwner)
                {
                    ServerManager.Instance.SendMessageOn(new GameStartOn());
                }

                break;

            case RequestProtocal.GameOutOn:

                GameOutRequest resultData = (GameOutRequest)baseRequest;
                DataManager.Instance.SetTurnPosition((int)resultData.playerEnum);
                DataManager.Instance.isOwner = resultData.isOwner;

                break;

            case RequestProtocal.GameStartOn:

                Observable
                    .Timer(TimeSpan.FromSeconds(1))
                    .Subscribe(_ => LoadingClearOn())
                    .AddTo(this);

                break;

            case RequestProtocal.MapCreateOn:

                loadClearOn = true;

                MapCreateRequestOn mapCreateRequestOn = (MapCreateRequestOn)baseRequest;

                DataManager.Instance.areaDataList = mapCreateRequestOn.area;
                DataManager.Instance.playerDataList = mapCreateRequestOn.playerDataList;
                DataManager.Instance.SetPlayerColor(mapCreateRequestOn.playerDataList);
                
                GameSceneLoadOn();

                break;
        }
    }

    public void SetData()
    {
        timePanel.gameObject.SetActive(DataManager.Instance.isMultiOn);

        Observable
                .Timer(TimeSpan.FromSeconds(1f))
                .Repeat()
                .Subscribe(_ => timeCheck())
                .AddTo(this);

        if (DataManager.Instance.isMultiOn)
        {
            delayTime = -1;
            FindingPlayerOn().Forget();
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

    private void ResetToggleIndex()
    {
        toggleIndex++;

        if (toggleIndex >= toggleList.Count)
        {
            toggleIndex = 0;
        }

        toggleList[toggleIndex].isOn = true;
    }

    public async UniTask FindingPlayerOn()
    {
		SetStatus(LoadingStatus.FindingPlayers);

        DataManager.Instance.SetOffLinePlayerCnt(0);

        int maxCnt = DataManager.Instance.num_player;
        int userCnt = maxCnt-DataManager.Instance.off_line_num_player;

        await UniTask.Delay(1000);

        ServerManager.Instance.GameReadyRequestOn(maxCnt, userCnt);
    }

    void timeCheck()
    {
        delayTime++;

        int minValue = (int)(delayTime / 60);
        int secValue = (int)(delayTime % 60);

        string timeStr = $"{minValue:d2} : {secValue:d2}";

        timeText.text = timeStr;

  //      Debug.LogWarning(DataManager.Instance.isOwner);
		//Debug.LogWarning(secValue);

		//15초후 AI 플레이로 채우기
		if (aIPlayOn == false && DataManager.Instance.isOwner && secValue > aiStartTime)
        {
            SetAIPlayer();
        }
    }

    private void SetAIPlayer()
    {
        aIPlayOn = true;

        int maxCnt = DataManager.Instance.num_player;
        int offLineUserCount = maxCnt - joinUserCnt;

        DataManager.Instance.SetOffLinePlayerCnt(offLineUserCount);

        LoadingClearOn();
    }

    public void MatchngComplatedOn()
    {
        if (matchngComplatedOn)
            return;

        matchngComplatedOn = true;

		SetStatus(LoadingStatus.MatchingComplated);

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

        SetStatus(LoadingStatus.Loading);

		if (DataManager.Instance.isMultiOn)
        {
            //Debug.LogWarning(" isOwner = " + DataManager.Instance.isOwner);

            //오너 플레이어 라면 맵 생성 진행 ( 1명이 맵을 생성후 배포 한다 )
            if (DataManager.Instance.isOwner)
            {
				//DataManager.Instance.InitMapData();

				DataManager.Instance.CreateMap();
				ServerManager.Instance.MapCreateRequestOn();
            }
        }
        else 
        {
            DataManager.Instance.InitMapData();
            DataManager.Instance.CreateMap();

            GameSceneLoadOn();
        }
    }

    public void SetStatus(LoadingStatus loadingStatus)
    {
        this.loadingStatus = loadingStatus;

        int key = 0;

        switch (this.loadingStatus)
		{
            case LoadingStatus.FindingPlayers:      key = 0; break;
			case LoadingStatus.MatchingComplated:   key = 1; break;
			case LoadingStatus.Loading:             key = 2; break;
		}

		string resultStr = LocalizeManager.Instance.GetStrData(LocalizeStatus.Loading, key);

		SetTitle(resultStr);
	}

    private void GameSceneLoadOn()
    {
		//DataManager.Instance.GamePlayOn();

        string sceneName = DataManager.Instance.isMultiOn ? "Game_Multi" : "Game_Single";
		SceneManager.LoadScene(sceneName);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
