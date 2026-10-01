using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static NonePlayPanel;

public class MapController : MonoBehaviour
{
    /// <summary> 원본 HexagonPanel 의 중심 오프셋 (씬에 박혀있던 값) </summary>
    [SerializeField] Vector2 mapPanelOffset = new Vector2(-80f, 79.5f);

    private MapLayerView mapLayerView;

    [SerializeField] PlayerDataPanel playerDataPanel;

    private List<HexagonElement> hexagonList = new List<HexagonElement>(); //인접 셀을 포함한 배열(arrangement with adjacent cells)
	private List<HexagonLineElement> hexagonLineList = new List<HexagonLineElement>(); //인접 셀을 포함한 배열(arrangement with adjacent cells)
	private List<MapDiceElement> mapDiceList = new List<MapDiceElement>();

    private bool diceAddEventOn = false;
    private bool attackEventOn = false;

    public PlayerIconController playerIconController;
    public InGameBottomController inGameBottomController;
	public Timer timer;
	public UnityAction gameWinOn;
    public UnityAction gameLoseOn;
    public UnityAction turnOffOn;
    public UnityAction<AreaData> selectAreaOn;

    private Join[] join;//인접 셀을 포함한 배열(arrangement with adjacent cells)

    private InGameButtonStatus selectBtnStatus = InGameButtonStatus.None;

    private AttackController attackController = new AttackController();

    private void Awake()
    {
        attackController.SetClass(playerIconController, inGameBottomController);
		//SetEvent();
	}

    /// <summary> UIDocument 의 map-layer 를 받아 맵 레이어를 구성한다 (InGameControllerBase 에서 호출) </summary>
    public void SetMapLayer(VisualElement mapLayer)
    {
        mapLayerView = new MapLayerView(mapLayer);
    }

    /// <summary> UI Toolkit 요소 연결 </summary>
    public void InitView(VisualElement root)
    {
        playerDataPanel.InitView(root);
    }

    public void SetTimer(Timer timer)
    {
        this.timer = timer;
	}

    public void SetEvent()
    {
        playerIconController.gameWinOn = gameWinOn;
        playerIconController.gameLoseOn = gameLoseOn;
        playerIconController.playerClickOn = PlayerClickOn;

        inGameBottomController.nonePlayPanel.skillBtnClickEventOn = SkillBtnClickEventOn;
    }
    private void PlayerClickOn(PlayerIconElement PlayerIcon)
    {
        if (DataManager.Instance.isMultiOn == false)
        {
            return;
        }

        playerDataPanel.SetData(PlayerIcon);
    }

    void SkillBtnClickEventOn(InGameButtonStatus btnStatus)
    {
        selectBtnStatus = btnStatus;

        if (selectBtnStatus == InGameButtonStatus.Cancel)
        {
            selectBtnStatus = InGameButtonStatus.None;
        }

        ResetSelectData();
    }

    void ResetSelectData() 
    {
        SelectClearOn();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void CreateMapInit()
    {
        //맵 초기화가 안되어있다면
        if (DataManager.Instance.areaDataList.Count == 0)
        {
            DataManager.Instance.InitMapData();
            DataManager.Instance.CreateMap();
        }

        join = DataManager.Instance.GetJoinData();
        SetHexagonPanel();
        CreateHexagon();
        CreateHexagonLine();
		CreateMap();
    }

    void SetHexagonPanel()
    {
        mapLayerView.SetOrigin(mapPanelOffset, DataManager.Instance.GetHexagonPanelPos());
    }

    void CreateHexagon()
    {
        Vector2 hexagonSize = DataManager.Instance.GetHexagonSizeDelta();

        for (int i = 0; i < DataManager.Instance.GetCelMax(); i++)
        {
            HexagonElement hexagon = mapLayerView.CreateHexagon(hexagonSize);

            hexagon.SetActive(true);
            hexagon.SetPos(i);
            hexagon.clickOn = HexagonClickOn;

            hexagonList.Add(hexagon);
        }
    }

	void CreateHexagonLine()
	{
		Vector2 hexagonSize = DataManager.Instance.GetHexagonSizeDelta();

		for (int i = 0; i < DataManager.Instance.GetCelMax(); i++)
		{
			HexagonLineElement hexagon = mapLayerView.CreateHexagonLine(hexagonSize);

			hexagon.SetActive(true);
			hexagon.SetPos(i);

			hexagonLineList.Add(hexagon);

			//hexagon.gameObject.SetActive(true);
			//hexagon.SetPos(i);
			//hexagon.clickOn = HexagonClickOn;

			//hexagonList.Add(hexagon);
		}
	}

	public void CreateMap()
    {
        DataManager.Instance.gameStart.SetValueAndForceNotify(false);
        DataManager.Instance.playOn = false;
		DataManager.Instance.InitStashCount();

        // 셀 초기화
        for (int i = 0; i < hexagonLineList.Count; i++)
        {
			hexagonLineList[i].DrawLineOnClear();
        }

        List<Vector2> readyData = new List<Vector2>();

        for (int i = 0; i < DataManager.Instance.GetCelMax(); i++)
        {
            HexagonElement hexagon = hexagonList[i];

            //SetArea(i);

            AreaData areaData = DataManager.Instance.GetAreaDataForCel(i);

            if (areaData == null)
            {
                hexagon.SetActive(false);
                continue;
            }

            hexagon.SetActive(true);

            hexagon.SetArea(areaData.id);
            hexagon.SetPlayer(areaData.player);

            areaData.AddHexagon(hexagon);

            Vector2 posIndex = DataManager.Instance.GetPos(i);

            if (posIndex.x == 0 || posIndex.y == 0 ||
                posIndex.x == DataManager.Instance.GetMapSizeValue().x - 1 || posIndex.y == DataManager.Instance.GetMapSizeValue().y - 1)
            {
                SetAroundLine(posIndex, i);
            }

            // 주변 셀
            for (int z = 0; z < 6; z++)
            {
                int pos = join[i].dir[z];
                if (pos < 0) continue;

                if (DataManager.Instance.GetAreaDataForCel(i) != DataManager.Instance.GetAreaDataForCel(pos) &&
                    readyData.Contains(new Vector2(i, pos)) == false &&
                    readyData.Contains(new Vector2(pos, i)) == false)
                {
					hexagonLineList[i].DrawLineOn(z);
                    readyData.Add(new Vector2(i, pos));
                }
            }
        }

        SetDice();
    }

    void SetDice()
    {
        MapDiceInit();

        var areaDataList = DataManager.Instance.areaDataList;

        //주사위 설정
        for (int i = 0; i < areaDataList.Count; i++)
        {
            AreaData areaData = areaDataList[i];

            mapDiceList[i].SetActive(true);

            if (areaData.id > 0)
            {
                areaData.SetCenterHexagon(mapDiceList[i] , areaData.dice);
            }
        }

        playerIconController.SetPlayerIcon();

        DataManager.Instance.BackUpOn();

        if (DataManager.Instance.isMultiOn == false && DataManager.Instance.mapSelectionOn)
        {
            inGameBottomController.SetStatus(0);
            playerIconController.SetMyEffect();

        }
        else
        {
            inGameBottomController.SetStatus(1);
        }
    }

    void MapDiceInit()
    {
        var areaDataList = DataManager.Instance.areaDataList;

        foreach (var mapDice in mapDiceList)
        {
            mapDice.SetActive(false);
        }

        if (mapDiceList.Count < areaDataList.Count)
        {
            for (int i = mapDiceList.Count; i < areaDataList.Count; i++)
            {
                MapDiceElement mapDice = CreateMapDice();
                mapDiceList.Add(mapDice);
            }
        }
    }

    MapDiceElement CreateMapDice()
    {
        Vector2 scaleValue = Vector2.zero;

        switch (DataManager.Instance.mapSizeEnum)
        {
            case MapSizeEnum.Small: scaleValue = Vector2.one; break;
            case MapSizeEnum.Medium: scaleValue = Vector2.one * 0.7f; break;
            case MapSizeEnum.Large: scaleValue = Vector2.one * 0.6f; break;
        }

        MapDiceElement mapDice = mapLayerView.CreateMapDice();

        mapDice.Root.style.scale = new Scale(scaleValue);

        mapDice.SetActive(true);
        mapDice.SetDice(0);

        return mapDice;
    }

    void SetAroundLine(Vector2 posIndex, int i)
    {
        HexagonLineElement hexagonLine = hexagonLineList[i];

		if (posIndex.y == 0)
        {
			hexagonLine.DrawLineOn(2);
			hexagonLine.DrawLineOn(3);
        }

        if (posIndex.y == (int)DataManager.Instance.GetMapSizeValue().y - 1)
        {
			hexagonLine.DrawLineOn(0);
			hexagonLine.DrawLineOn(1);
        }

        if (posIndex.x == 0)
        {
            List<int> checkList = new List<int>() { 1, 3, 5 };

            for (int z = 0; z < checkList.Count; z++)
            {
                int checkIndex = checkList[z];

                if (join[i].dir[checkIndex] <= 0)
                {
					hexagonLine.DrawLineOn(checkIndex);
                }
            }
        }

        if (posIndex.x == DataManager.Instance.GetMapSizeValue().x - 1)
        {
            List<int> checkList = new List<int>() { 0, 2, 4 };

            for (int z = 0; z < checkList.Count; z++)
            {
                int checkIndex = checkList[z];

                if (join[i].dir[checkIndex] <= 0)
                {
					hexagonLine.DrawLineOn(checkIndex);
                }
            }
        }
    }

    public async void HexagonClickOn(int index)
    {
        if (attackEventOn || DataManager.Instance.gameStart.Value == false)
        {
            return;
        }

		SoundManager.Instance.PlaySe(SeEnum.Yes);

		AreaData areaData = DataManager.Instance.GetAreaDataForCel(index);

        PlayerEnum currentPlayerEnum = DataManager.Instance.playerData.pe;

		bool isMyTurn = DataManager.Instance.IsMyTurn();

        if (selectBtnStatus == InGameButtonStatus.Buy || selectBtnStatus == InGameButtonStatus.Sell)
        {
            inGameBottomController.nonePlayPanel.SetAreaData(areaData, selectBtnStatus);
            return;
        }

		if (isMyTurn == false)
			return;

		if (DataManager.Instance.areaGetPlayerEnum != PlayerEnum.Player_None || DataManager.Instance.diceGetCount != 0)
		{
			attackController.ForceGetAreaOn(areaData);
			return;
		}

		if (DataManager.Instance.isMultiOn && timer != null && timer.timerCurrentDelay < 1)
        {
            return;
        }

		//선택이 안되어있다면
		if (attackController.choisIndex == -1)
		{
			if (areaData.choisOn == false && areaData.dice > 1 && areaData.player == DataManager.Instance.playerData.pe)
			{
				SelectOn(areaData);
			}
		}
		//선택이 되어있다면
		else
		{
			AreaData beforeAreaData = DataManager.Instance.GetAreaData(attackController.choisIndex);

			//재선택 했을때
			if (areaData.id == attackController.choisIndex)
			{
				DeSelectOn(areaData);
			}
			//본인의 땅중 처음 선택한땅이 아닌 다른땅을 선택했을때
			else if (areaData.player == currentPlayerEnum && areaData.dice > 1)
			{
				beforeAreaData.ChoisEventOn(false);

				SelectOn(areaData);
			}
			//다른 플레이어 땅을 선택했을때
			else if (areaData.player != currentPlayerEnum)
			{
				//인접하지 않을경우
				if (areaData.GetIsOnConnectedArea(beforeAreaData.id) == false)
				{
					return;
				}

				//동맹이었을 경우
				if (DataManager.Instance.IsMyAllAlliance(areaData.player))
				{
					return;
				}

                attackEventOn = true;

				try
				{
					await attackController.AttackOn(beforeAreaData, areaData, new CancellationTokenSource());
				}
				finally
				{
					attackEventOn = false;
				}

				if (DataManager.Instance.isMultiOn && timer != null)
				{
					timer.AddExtraTimeOn();
				}
			}
		}
	}

	private void AttackRequestOn(AreaData fromAreaData , AreaData toAreaData , DiceWarData fromDiceWarData, DiceWarData toDiceWarData)
    {
        AttackRequest request = new AttackRequest()
        {
            fromAreaData = fromAreaData.GetSendAreaData(),
            toAreaData = toAreaData.GetSendAreaData(),
            fromDiceWarData = fromDiceWarData,
            toDiceWarData = toDiceWarData,
        };

        ServerManager.Instance.SendMessageOn(request);
    }

    public async UniTask AttackReceiveDataOn(AttackRequest attackRequest)
    {
        await attackController.AttackReceiveDataOn(attackRequest);
    }

	void SelectOn(AreaData areaData)
    {
		attackController.SetChoisIndex(areaData.id);
		areaData.ChoisEventOn(true);
    }

    public void AreaTradeCheck(AreaData areaData)
    {
        PlayerEnum currentPlayerEnum = DataManager.Instance.playerData.pe;

        NonePlayPanel.InGameButtonStatus btnStatus = areaData.player == currentPlayerEnum ?
                                                        NonePlayPanel.InGameButtonStatus.Sell : //내 땅을 선택했을때
                                                        NonePlayPanel.InGameButtonStatus.Buy;   //다른 사람 땅을 선택했을때

        inGameBottomController.nonePlayPanel.SetAreaData(areaData , btnStatus);
    }

    public void SelectClearOn()
    {
        if (attackController.choisIndex != -1)
        {
            DataManager.Instance.GetAreaData(attackController.choisIndex).ChoisEventOn(false);
			attackController.SetChoisIndex();
        }
    }

    public void DeSelectOn(AreaData areaData)
    {
		attackController.SetChoisIndex();
        areaData.ChoisEventOn(false);
    }

    public List<AreaData> EndTurnBtnClickOn()
    {
		if (diceAddEventOn || DataManager.Instance.gameStart.Value == false)
        {
            return null;
        }

		SelectClearOn();

        PlayerEnum currentPlayerEnum = DataManager.Instance.playerData.pe;

        List<AreaData> areaDataList = DiceAddOn(currentPlayerEnum);

        //turnOffOn();

        return areaDataList;
    }

    public List<AreaData> DiceAddOn(PlayerEnum playerEnum)
    {
        diceAddEventOn = true;

        PlayerIconElement playerIcon = playerIconController.GetPlayerIcon(playerEnum);

        if (playerIcon == null)
        {
            diceAddEventOn = false;

            return null;
        }

        bool isMyTurn = DataManager.Instance.IsMyTurn();

        int connectedCount = playerIcon.connectedCount;

		List<AreaData> areaDataList = DataManager.Instance.areaDataList.Where(data => data.player == playerEnum).ToList();

		while (connectedCount > 0)
        {
			areaDataList = DataManager.Instance.areaDataList.Where(data => data.player == playerEnum).ToList();
            areaDataList.Shuffle();

			bool allMax = areaDataList.All(data => data.dice == DataManager.Instance.diceMaxCount);

			if (allMax)
			{
				break;
			}

			AreaData areaData = areaDataList.Where(data => data.dice < 6).FirstOrDefault();

            if (areaData != null && areaData.dice < DataManager.Instance.diceMaxCount)
            {
				//주사위 추가
				areaData.DiceAddOn();

				//StashCount가 있을때
				if (DataManager.Instance.GetStashCount(playerEnum) > 0)
				{
					DataManager.Instance.AddStashCount(playerEnum, -1);
				}
				else if (connectedCount > 0)
				{
					connectedCount--;
				}
			}
        }

        //      List<AreaData> areaDataList = DataManager.Instance.areaDataList.Where(data => data.player == playerEnum).ToList();

        //areaDataList.Shuffle();

        //      while (true)
        //      {
        //	bool allMax = false;

        //          foreach (AreaData areaData in areaDataList)
        //          {
        //              allMax = areaDataList.All(data => data.dice == DataManager.Instance.diceMaxCount);

        //              if (allMax || connectedCount == 0)
        //              {
        //                  break;
        //              }

        //              //주사위가 Max가 아니라면
        //              if (areaData.dice < DataManager.Instance.diceMaxCount)
        //              {
        //                  //주사위 추가
        //                  areaData.DiceAddOn();

        //                  //StashCount가 있을때
        //                  if (DataManager.Instance.GetStashCount(playerEnum) > 0)
        //                  {    
        //                      DataManager.Instance.AddStashCount(playerEnum ,- 1);
        //                  }
        //                  else if (connectedCount > 0)
        //                  {
        //                      connectedCount--;
        //                  }
        //              }
        //          }

        //          if (allMax || connectedCount <= 0)
        //          {
        //              break;
        //          }
        //      }

        int stashCount = DataManager.Instance.AddStashCount(playerEnum, connectedCount);

		if (isMyTurn)
        {
			inGameBottomController.nonePlayPanel.SetStashText();
        }

        diceAddEventOn = false;

        return areaDataList;
    }

    public async UniTask AIAttackOn(PlayerEnum playerEnum , CancellationTokenSource source)
    {
		await attackController.AIAttackOn(playerEnum, source);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CreateMap();
        }
    }
     
    public void NewGameOn()
    {
        DataManager.Instance.InitMapData();
        DataManager.Instance.CreateMap();

        CreateMap();

		//playerIconController.SetIconTurnEffect();
		playerIconController.SetMyEffect();
		inGameBottomController.nonePlayPanel.Init();
	}

    public void ReStartOn()
    {
        DataManager.Instance.ReStartOn();
        playerIconController.SetPlayerIcon();
        playerIconController.SetIconTurnEffect();
		inGameBottomController.SetStatus(1);
        inGameBottomController.nonePlayPanel.Init();
	}
}
