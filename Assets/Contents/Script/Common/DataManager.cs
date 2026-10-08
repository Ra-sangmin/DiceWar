using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using Random = UnityEngine.Random;

public class DataManager : MonoSingleton<DataManager>
{
    public UserData userData;
    public string loginId = string.Empty;
    public bool isMultiOn = false;

	public AILevel aiLevel = AILevel.Normal;
    public MapSizeEnum mapSizeEnum = MapSizeEnum.Small;
    Dictionary<MapSizeEnum, MapData> mapDataDic = new Dictionary<MapSizeEnum, MapData>();
    
    public int num_player = 2;//총 플레이어의 수 (기본 값 : 3)
    public int off_line_num_player = 0;//AI 수 (기본 값 : 2)
    public int matchingUserCnt = 2;//매칭 유저 수 (기본 값 : 2)

    public int num_area = 10;//총 영토의 수 (기본값 : 10)
    public int diceMaxCount = 6;//영토의 주사위 최대 갯수

    //public Vector2 mapSizeValue = new Vector2(20, 15);
    
    public List<AreaData> backUpAreaDataList = new List<AreaData>();

    public PlayerData playerData = new PlayerData();
    public bool setTurnPositionOn = false;
    public bool choosePositionOn = false;
    public bool mapSelectionOn = true;
    public bool diceCompensationOn = false;

    public bool randomPositionOn = false;

    //-1 = random , 0 ~ 6 selectPosition 
    public int turnPosition = -1;

    public bool isOwner = false;

    private int _currentTurnIndex = 0;
	public int currentTurnIndex  { get { return _currentTurnIndex; }}

	public PlayerEnum currentPlayer  { get { return (PlayerEnum)currentTurnIndex; } }

	private Dictionary<PlayerEnum,int> playerColorIndexDic = new Dictionary<PlayerEnum,int>();

	/// <summary>
	/// 싱글 연패 횟수. 패배 후 '다시하기' 로 같은 판을 이기면 보상이 (loseCount + 1) 배가 된다. (2026-09-20)
	/// 승리하거나 판을 떠날 때(홈으로 / 새로하기 / 새 게임 시작) 0 으로 돌아간다.
	/// </summary>
	public int loseCount = 0;
    public List<PlayerData> playerDataList = new List<PlayerData>();
    public ReactiveProperty<bool> gameStart = new ReactiveProperty<bool>(false);

    public bool playOn = false;

    private List<AllianceAllData> allianceAllDataList = new List<AllianceAllData>();
	//private List<List<AllianceData>> allianceDataList = new List<List<AllianceData>>();

	public List<AreaData> areaDataList = new List<AreaData>();
    public MapCreateRequestOn testData;

	public List<int> stashCountList = new List<int>();

	//public int stashCount = 0;

    public int myTurnCount = 0;

    public bool inGameEditOn = false;

	public bool soundOn = true;
	private const string soundOnKey = "soundOnKey";
	
	private bool mapCompensation = false;
    private const string mapCompensationKey = "mapCompensationKey";

	public int newGameNeedCoin = 2;

	public bool leaveEarlyPopupReadyOn = false;
	public bool leaveEarlyPopupOpenOn = false;

    public bool myBetrayWaitOn = false;

    /// <summary> 신규 유저 시작 코인 (기획 시트 : Get Coin 삭제 대신 시작 시 200코인, 2026-09-26) </summary>
    //신규 유저 시작 코인 (26.10.04 수정 제안 : 100). 실제 신규 유저는 서버 DB 의 coin 기본값으로 정해진다 — 이 값은 에디터/PC 테스트용
    public const int StartCoin = 100;

    /// <summary> 메인 씬에 들어가자마자 띄울 토스트 문구 (멀티 중 앱 이탈로 퇴장된 경우 등) </summary>
    public string mainToastText = string.Empty;

    //배신 패널티 집계 — 멀티 메뉴 팝업의 '예상 보상' 표시용 (2026-09-19)
    //코인 경제는 그대로다. 실제 차감은 지금처럼 배신 시점에만 일어나고 여기 값은 표시 전용이다.
    public int betrayReceivedCoin = 0;   //내 동맹원이 배신해서 물어낸 패널티 합계
    public int betrayPaidCoin = 0;       //내가 배신해서 지불한 패널티 합계

    public PlayerEnum areaGetPlayerEnum = PlayerEnum.Player_None;
	public int diceGetCount = 0;

	public override void Init()
    {
        base.Init();

        CommonDataInit();

        playerColorIndexDic = new Dictionary<PlayerEnum, int>
        {
            { PlayerEnum.Player_0, 0},
            { PlayerEnum.Player_1, 1},
            { PlayerEnum.Player_2, 2},
            { PlayerEnum.Player_3, 3},
            { PlayerEnum.Player_4, 4},
            { PlayerEnum.Player_5, 5},
            { PlayerEnum.Player_6, 6},
        };

        MapDataInit();
        //CoinDataInit();

        //Debug.LogWarning(mapSizeEnum);
    }

    void CommonDataInit()
    {
		soundOn = PlayerPrefs.GetInt(soundOnKey, 1) == 1;
		SoundManager.Instance.SoundOffOn(!soundOn);

        mapCompensation = false;
		mapCompensation = PlayerPrefs.GetInt(mapCompensationKey , 0) == 1;
	}


	void MapDataInit() 
    {
        mapDataDic = new Dictionary<MapSizeEnum, MapData>
        {
            { MapSizeEnum.Small, new MapData(MapSizeEnum.Small, new Vector2(20,15),new Vector2(-630,-360),new Vector2(65,70),new Vector2(70,70) , 3, 2)},
            { MapSizeEnum.Medium, new MapData(MapSizeEnum.Medium, new Vector2(30,23),new Vector2(-700,-400),new Vector2(50,50),new Vector2(50,50), 8, 4)},
            { MapSizeEnum.Large, new MapData(MapSizeEnum.Large, new Vector2(40,30),new Vector2(-750,-425),new Vector2(40,40),new Vector2(40,40), 14, 5)},
        };
    }

    public void CheckUserData()
    {
		if (userData == null)
		{
            UserDataRespons data = new UserDataRespons() { email = "fktkdals1@gmail.com" };
			data.freeCoin = StartCoin;
			UserDataSave(data);
		}
	}

	public void UserDataSave(UserDataRespons data)
    {
#if UNITY_STANDALONE
        data.freeCoin = StartCoin;
#endif

		userData = new UserData(data.email, data.snsType, data.freeCoin , data.chargeCoin);
    }

    public void InitMapData()
    {
		GameDataClearOn();
		SetPlayerColor();

		//새 판이므로 연패 보너스도 처음부터
		loseCount = 0;
    }

    void SetPlayerColor() 
    {
        List<int> colorList = new List<int>() { 0,1,2,3,4,5,6};
        //colorList.Shuffle();

        

        

        //int index = colorList.IndexOf(playerData.ci);

        //int playerEnumIndex = (int)playerData.pe;

        //int temp = colorList[playerEnumIndex];
        //colorList[playerEnumIndex] = playerData.colorIndex;
        //colorList[index] = temp;

        playerData.pe = (PlayerEnum)playerData.ci;


		for (int i = 0; i < num_player; i++) 
        {
            playerColorIndexDic[(PlayerEnum)i] = colorList[i];
        }
    }

    public void SetPlayerColor(List<PlayerData> playerDataList) 
    {
        foreach (var playerData in playerDataList)
        {
            playerColorIndexDic[(PlayerEnum)playerData.pe] = playerData.ci;
        }
    }

    public int GetCelMax() 
    {
        return (int)GetMapSize().x * (int)GetMapSize().y;
    }

    public Join[] GetJoinData()
    {
        int cel_max = GetCelMax();

        Join[] join = new Join[cel_max];

        for (int i = 0; i < cel_max; i++)
        {
            join[i] = new Join();
            for (int j = 0; j < 6; j++)
            {
                join[i].dir[j] = this.NextCel(i, j);
            }
        }

        return join;
    }

    /// <summary>
    /// 메서드 옆의 셀 번호를 반환합니다 (return the cell number to the next method)
    /// </summary>
    /// <param name="opos"></param>
    /// <param name="dir"></param>
    /// <returns></returns>
    public int NextCel(int opos, int dir)
    {
        Vector2 mapSizeValue = GetMapSizeValue();
        //x인덱스
        int ox = opos % (int)mapSizeValue.x;
        //int oy = Mathf.FloorToInt((float)opos / this.XMAX);
        //y인덱스
        int oy = opos / (int)mapSizeValue.x;
        int f = (oy % 2) * -1; // 짝수면 0 , 홀수면 -1
        int ax = 0;
        int ay = 0;

        switch (dir)
        {
            case 0: ax = f + 1; ay = +1; break;  // 오른쪽 상단 (upper right)
            case 1: ax = f; ay = +1; break;      // 왼쪽 상단 (upper left)
            case 2: ax = f + 1; ay = -1; break;  // 오른쪽 하단 (bottom right)
            case 3: ax = f; ay = -1; break;  // 왼쪽 하단 (bottom left)
            case 4: ax = 1; ay = +0; break;  // 오른쪽 (right)
            case 5: ax = -1; ay = +0; break;  // 왼쪽 (left)
        }

        int x = ox + ax;
        int y = oy + ay;

        if (x < 0 || y < 0 || x >= mapSizeValue.x || y >= mapSizeValue.y) return -1;

        return y * (int)mapSizeValue.x + x;
    }

    public void CreateMap()
    {
        MapCreater mapCreater = new MapCreater();
        mapCreater.InitMapData(mapSizeEnum, num_player);

        areaDataList = mapCreater.CreateMap();
        areaDataList = areaDataList.Where(data => data.player !=   PlayerEnum.Player_None && data.cel.Count  > 0).ToList();

        playerDataList = new List<PlayerData>();

        int count = 0;

        foreach (var item in playerColorIndexDic)
        {
            playerDataList.Add(new PlayerData(item.Key, item.Value));

            count++;

            if (count >= num_player)
            {
                break;
            }
        }

        int onLineCnt = num_player - off_line_num_player -1;

        if (isMultiOn == false)
        {
            onLineCnt = 0;
		}

        //플레이어당 시작 주사위 합 = (전체 땅 / 인원 * 2) 의 몫 (2026-09-26)
        //  시트는 × 3 이었지만, Dice Setting 의 Max 가 3 이면 모든 땅이 3개로 고정돼서 사용자 결정으로 × 2 로 낮췄다
        int diceTotalPerPlayer = areaDataList.Count * 2 / num_player;

        for (int i = 0; i < playerDataList.Count; i++)
        {
            bool isAI = isMultiOn == false ? playerData.ci != i :  i > onLineCnt;

			playerDataList[i].isAI = isAI;

			//땅 하나의 주사위 최소/최대 : Dice Setting 팝업(DiceCountManager) 의 난이도별 · 유저/AI 별 값 (2026-09-26)
			DiceCountData diceCountData = DiceCountManager.Instance.GetData(isAI, aiLevel);

			//싱글 쉬움/중간 : 유저는 다른 플레이어보다 4개 / 2개 더 가지고 시작한다 (26.10.04 수정 제안, 2026-10-08)
			int diceTotal = diceTotalPerPlayer + (isAI == false && isMultiOn == false ? GetUserStartDiceBonus(aiLevel) : 0);

			SetInitDice(GetAreaDtaList(playerDataList[i].pe), diceTotal, diceCountData);
        }
    }

    /// <summary>
    /// 플레이어 한 명의 시작 주사위 배치. 합계(totalDiceCount)는 그대로 두고 땅마다 개수가 제각각이 되게 나눈다 (2026-09-26)
    /// 모든 땅을 최소값(Dice Setting 의 Min)으로 깔고, 남은 주사위를 땅마다 다른 가중치로 하나씩 뿌린다.
    /// 땅 하나는 최대값(Dice Setting 의 Max)을 넘지 않는다.
    /// 합계가 '땅 수 × Min' 보다 작거나 '땅 수 × Max' 보다 크면 Min / Max 규칙이 우선이다 (합계가 그만큼 달라진다).
    /// </summary>
    void SetInitDice(List<AreaData> areaList, int totalDiceCount, DiceCountData diceCountData = null)
    {
        if (areaList.Count == 0)
            return;

        int minCount = 1;
        int maxCount = diceMaxCount;

        if (diceCountData != null)
        {
            minCount = Mathf.Clamp(diceCountData.minCount, 1, diceMaxCount);
            maxCount = Mathf.Clamp(diceCountData.maxCount, minCount, diceMaxCount);
        }

        foreach (var area in areaList)
        {
            area.SetDice(minCount);
        }

        //땅마다 주사위를 받을 확률(가중치)을 다르게 준다
        Dictionary<AreaData, float> weightDic = new Dictionary<AreaData, float>();

        foreach (var area in areaList)
        {
            weightDic[area] = UnityEngine.Random.Range(0.2f, 1.8f);
        }

        int remain = totalDiceCount - areaList.Count * minCount;

        while (remain > 0)
        {
            List<AreaData> targetList = areaList.Where(area => area.dice < maxCount).ToList();

            //모든 땅이 최대치라면 더 놓을 곳이 없다
            if (targetList.Count == 0)
                break;

            float weightSum = targetList.Sum(area => weightDic[area]);
            float pick = UnityEngine.Random.Range(0f, weightSum);

            AreaData selectArea = targetList[targetList.Count - 1];

            foreach (var area in targetList)
            {
                pick -= weightDic[area];

                if (pick <= 0f)
                {
                    selectArea = area;
                    break;
                }
            }

            selectArea.SetDice(selectArea.dice + 1);
            remain--;
        }
    }

	public List<AreaData> GetAreaDtaList(PlayerEnum playerEnum)
    {
		return areaDataList.Where(data => data.player == playerEnum).ToList();
	}


	public Vector2 GetMapSizeValue() 
    {
        return GetMapSize();
    }

    public void BackUpOn()
    {
        backUpAreaDataList = new List<AreaData>();

        for (int i = 0; i < areaDataList.Count; i++)
        {
            backUpAreaDataList.Add(new AreaData(areaDataList[i]));
        }
    }

    public int GetCelData(int celIndex)
    {
        int resultData = -1;

        var areaData = areaDataList.Where(data => data.IsHaveCelData(celIndex)).FirstOrDefault();

        if (areaData != null)
        {
            resultData = areaData.id;
        }

        return resultData;
    }

    public AreaData GetAreaDataForCel(int celIndex)
    {
        return GetAreaData(GetCelData(celIndex));
    }

    public AreaData GetAreaData(int areaIndex)
    {
        AreaData areadata = areaDataList.Where(data => data.id == areaIndex).FirstOrDefault();

        return areadata;
    }

    public void SetAreaData(SendAreaData _areaData)
    {
        AreaData areaData = GetAreaData(_areaData.id);

        areaData.SetDice(_areaData.dice);

        if (areaData.player != _areaData.player)
        {
            areaData.PlayerChangeOn(_areaData.player);
        }
    }

    public Color GetPlayerColor(int index)
    {
        Color color = Color.black;

        switch (index)
        {
            case 0: color = new Color32(255, 72, 90, 255); break;
            case 1: color = new Color32(255, 255, 103, 255); break;
            case 2: color = new Color32(193, 255, 48, 255); break;
            case 3: color = new Color32(32, 223, 175, 255); break;
            case 4: color = new Color32(135, 233, 244, 255); break;
            case 5: color = new Color32(255, 146, 204, 255); break;
            case 6: color = new Color32(151, 140, 253, 255); break;
        }

        return color;
    }

    public Color GetPlayerColor(PlayerEnum playerEnum)
    {
        int index = -1;

        if (playerColorIndexDic.ContainsKey(playerEnum))
        {
            index = playerColorIndexDic[playerEnum];
        }

        return GetPlayerColor(index);
    }

    public int GetPlayerColorIndex(PlayerEnum playerEnum)
    {
        int index = -1;

        if (playerColorIndexDic.ContainsKey(playerEnum))
        {
            index = playerColorIndexDic[playerEnum];
        }

        return index;
    }

    public string GetConnectedIndex(PlayerEnum playerEnum, int index)
    {
        string resultValue = string.Empty;

        switch (playerEnum)
        {
            case PlayerEnum.Player_0:
                resultValue = "Player0";
                break;
            case PlayerEnum.Player_1:
                resultValue = "Player1";
                break;
            case PlayerEnum.Player_2:
                resultValue = "Player2";
                break;
            case PlayerEnum.Player_3:
                resultValue = "Player3";
                break;
            case PlayerEnum.Player_4:
                resultValue = "Player4";
                break;
            case PlayerEnum.Player_5:
                resultValue = "Player5";
                break;
            case PlayerEnum.Player_6:
                resultValue = "Player6";
                break;
            case PlayerEnum.Player_7:
                resultValue = "Player7";
                break;
        }

        resultValue += string.Format("_{0}", index);

        return resultValue;

    }

    public List<AreaData> SetBundleKey(PlayerEnum checkEnum)
    {
        foreach (AreaData areaData in areaDataList.Where(data => data.player == checkEnum).ToList())
        {
            areaData.bundleKey = string.Empty;
        }

        List<AreaData> resultAreaDataList = new List<AreaData>();

		//int maxCount = 0;
        int index = 0;

        while (true)
        {
            var checkList = areaDataList.Where(data => data.player == checkEnum && data.bundleKey == string.Empty).ToList();

            if (checkList.Count == 0)
            {
                break;
            }

            List<AreaData> tempResultAreaDataList= SetBundleKey(checkEnum, index, checkList[0]);

			int count = resultAreaDataList.Count;

            if (resultAreaDataList.Count < tempResultAreaDataList.Count)
            {
                resultAreaDataList = tempResultAreaDataList;

			}

            //maxCount = Mathf.Max(maxCount, count);

            index++;
        }

        return resultAreaDataList;
    }

    public List<AreaData> SetBundleKey(PlayerEnum checkEnum, int index, AreaData areaData)
    {
        string bundleKey = GetConnectedIndex(checkEnum, index);
        areaData.SetBundleKey(areaDataList, checkEnum, bundleKey);

        var sameBundleKeyPieceList = areaDataList.Where(data => data.player == checkEnum && !string.IsNullOrEmpty(data.bundleKey) && data.bundleKey.Equals(bundleKey)).ToList();

        return sameBundleKeyPieceList;
    }

    public Vector2 GetPos(int index)
    {
        //x 위치
        int ox = index % (int)GetMapSize().x;
        //y 위치
        int oy = index / (int)GetMapSize().x;

        return new Vector2(ox, oy);
    }
    public void SetAILevelEnum(AILevel aiLevel)
    {
        this.aiLevel = aiLevel;
    }


    public void SetMapSizeEnum(MapSizeEnum mapSizeEnum)
    {
        this.mapSizeEnum = mapSizeEnum;
    }

    public void SetPlayerMaxCnt(int playerMaxCnt)
    {
        this.num_player = playerMaxCnt;
    }

    public void SetMatchingUserCnt(int matchingUserCnt)
    {
        this.matchingUserCnt = matchingUserCnt;
    }


    public void SetOffLinePlayerCnt(int offLinePlayer)
    {
        this.off_line_num_player = offLinePlayer;
    }

    public Color SetCurrentPlayerColor(int currentPlayerColorIndex)
    {
        this.playerData.ci = currentPlayerColorIndex;
        return GetPlayerColor(currentPlayerColorIndex);
    }
    #region 난이도별 맵 크기 / 인원 / 시작 주사위 (26.10.04 수정 제안, 2026-10-08)

    /// <summary> 싱글 난이도별 맵 크기 고정 : 쉬움 작은 맵 / 중간 중간 맵 / 어려움 큰 맵 </summary>
    public static MapSizeEnum GetDifficultyMapSize(AILevel level)
    {
        switch (level)
        {
            case AILevel.Easy: return MapSizeEnum.Small;
            case AILevel.Normal: return MapSizeEnum.Medium;
            default: return MapSizeEnum.Large;
        }
    }

    /// <summary> 싱글 난이도별 참가 인원 범위 (x = 최소, y = 최대). 보상은 그대로 난이도 × 인원 → 2~3 / 8~10 / 18~21 </summary>
    public static Vector2Int GetDifficultyPlayerRange(AILevel level)
    {
        switch (level)
        {
            case AILevel.Easy: return new Vector2Int(2, 3);
            case AILevel.Normal: return new Vector2Int(4, 5);
            default: return new Vector2Int(6, 7);
        }
    }

    /// <summary> 싱글에서 유저가 더 받는 시작 주사위 (쉬움 4 / 중간 2 / 어려움 0) </summary>
    public static int GetUserStartDiceBonus(AILevel level)
    {
        switch (level)
        {
            case AILevel.Easy: return 4;
            case AILevel.Normal: return 2;
            default: return 0;
        }
    }

    #endregion

    public int ActivePlayerCount()
    {
        return mapDataDic[mapSizeEnum].activePlayerCount;
    }

    public int DeleteAreaCount()
    {
        return mapDataDic[mapSizeEnum].deleteCount;
    }

    public Vector2 GetMapSize()
    {
        return mapDataDic[mapSizeEnum].mapSizeValue;
    }

    public Vector2 GetHexagonPanelPos()
    {
        return mapDataDic[mapSizeEnum].panelPos;
    }

    public Vector2 GetHexagonSizeDelta()
    {
        return mapDataDic[mapSizeEnum].hexagonSize;
    }

    public Vector2 GetHexagonLineSizeDelta()
    {
        return mapDataDic[mapSizeEnum].hexagonLineSize;
    }

    public Color GetPlayerColor()
    {
        return GetPlayerColor(playerData.ci);
    }

    public void ReStartOn()
    {
        GameDataClearOn();

		areaDataList.Clear();

        for (int i = 0; i < backUpAreaDataList.Count; i++)
        {
            AreaData areaData = new AreaData(backUpAreaDataList[i]);

            areaData.SetDice(areaData.dice);
            areaData.PlayerChangeOn(areaData.player);

            areaDataList.Add(areaData);
        }

        gameStart.SetValueAndForceNotify(true);
    }

    public void SetTurnPosition(int turnPosition)
    {
        this.turnPosition = turnPosition;
        if (turnPosition == -1) 
        {
            playerData.pe = (PlayerEnum)Random.Range(0, num_player);
        }
        else 
        {
			playerData.pe = (PlayerEnum)turnPosition;
		}
    }

    public void InitStashCount()
    {
		stashCountList = new List<int> {};

		for (int i = 0; i < 7; i++)
		{
            stashCountList.Add(0);
		}
	}

	public int GetStashCount(PlayerEnum playerEnum)
	{
		return stashCountList[(int)playerEnum];
	}

	public int AddStashCount(PlayerEnum playerEnum , int diceCount)
    {
        int stashCount = GetStashCount(playerEnum);

		stashCount += diceCount;

        stashCount = Mathf.Clamp(stashCount, 0 , 15);

		return SetStashCount(playerEnum, stashCount);
    }

	public int SetStashCount(PlayerEnum playerEnum, int stashCount)
	{
        stashCountList[(int)playerEnum] = stashCount;

        return stashCount;
	}

    public int GetRewardCoin()
    {
		int resultCoin = isMultiOn ? GetMultiRewardCoin() : GetSingleRewardCoin();

        return resultCoin;
    }

    public int GetSingleRewardCoin(GameResultPopup.GameResultEnum gameResultEnum = GameResultPopup.GameResultEnum.Win)
    {
		int addCoin = 0;

		switch (aiLevel)
		{
			case AILevel.Easy: addCoin = 1; break;
			case AILevel.Normal: addCoin = 2; break;
			case AILevel.Hard: addCoin = 3; break;
		}

		int resultCoin = addCoin * num_player;

        if (gameResultEnum == GameResultPopup.GameResultEnum.LeaveEarly)
        {
            resultCoin = Mathf.RoundToInt(resultCoin * 0.8f);
		}
        else
        {
			//연패 보너스 : 진 판을 '다시하기' 로 이기면 (연패 횟수 + 1) 배 (2026-09-20)
			resultCoin *= loseCount + 1;
		}

		return resultCoin;
	}

	public int GetMultiRewardCoin(int resultPlayCount = 1)
	{
        //멀티 상금 (26.10.04 수정 제안 : 60 → 120, 참가비 10 → 20 과 같이 모두 ×2)
        return 120;
	}

    public int GetNeedBetrayCoin()
    {
		//내 동맹 정보중 받을 몫 의 절반 값으로 취득
		return Mathf.RoundToInt(GetMyAllianceData().coinCount / 2.0f);
    }

	public async UniTask MyBetrayOn(PlayerIconController playerIconController)
	{
        myBetrayWaitOn = true;

		AllianceBetrayRequest request = new AllianceBetrayRequest()
		{
			betrayPlayerEnum = playerData.pe,
			needBetrayCoin = GetNeedBetrayCoin(),
			allianceDataList = GetNewAllianceData(playerIconController),
		};

		ServerManager.Instance.SendMessageOn(request);

        await UniTask.WhenAny(
            UniTask.WaitUntil(() => !myBetrayWaitOn),
            UniTask.Delay(2000) // 1000ms = 1초
        );
	}

	public async UniTask OrderBetrayOn(PlayerEnum playerEnum)
	{
		myBetrayWaitOn = true;

        int needBetrayCoin = Mathf.RoundToInt(GetAllianceData(playerEnum).coinCount / 2.0f);

		AllianceBetrayRequest request = new AllianceBetrayRequest()
		{
			betrayPlayerEnum = playerEnum,
			needBetrayCoin = needBetrayCoin,
			//allianceDataList = GetNewAllianceData(playerIconController),
		};

		ServerManager.Instance.SendMessageOn(request);

		await UniTask.Delay(2000);
	}

	List<AllianceData> GetNewAllianceData(PlayerIconController playerIconController)
	{
		List<AllianceData> resultAllianceDataList = new List<AllianceData>();

		PlayerEnum myPlayer = playerData.pe;

		//나를 제외한 동맹 정보
		List<AllianceData> allOriginDataList = GetAllAlliance(myPlayer).allianceDataList.Where(data => data.playerEnum != myPlayer).ToList();

		List<AllianceData> allDataList = new List<AllianceData>();

		if (playerIconController != null)
		{
			var playerIconList = playerIconController.GetActiveList().OrderByDescending(data => data.connectedCount);

			foreach (var playerIcon in playerIconList)
			{
				var data = allOriginDataList.FirstOrDefault(data => data.playerEnum == playerIcon.playerEnum);

				if (data != null)
				{
					allDataList.Add(data);
				}
			}
		}

		//내 동맹 정보
		AllianceData myAllianceData = GetMyAllianceData();

		//총 획득 코인
		int maxCoinCount = GetMultiRewardCoin();

		//한명당 나눠질 코인 정보
		int oneManCoinCount = Mathf.RoundToInt(myAllianceData.coinCount / (float)allDataList.Count);

		foreach (AllianceData data in allDataList)
		{
			//유저 정보에 코인 추가
			data.coinCount += oneManCoinCount;

			data.coinCount = Mathf.Min(data.coinCount, maxCoinCount);

			maxCoinCount -= data.coinCount;

			if (maxCoinCount < 0)
			{
				maxCoinCount = 0;
			}

			resultAllianceDataList.Add(data);
		}

		return resultAllianceDataList;

	}

	public int GetNeedCoin()
    {
        int needCoin = 0;

        if (isMultiOn)
        {
            needCoin = 20;   //멀티 참가비 (26.10.04 수정 제안 : 10 → 20)
		}
        else
        {
			switch (aiLevel)
			{
				case AILevel.Easy:
					needCoin = 0;
					break;
				case AILevel.Normal:
					needCoin = 1;
					break;
				case AILevel.Hard:
					needCoin = 2;
					break;
			}
		}

        return needCoin;
	}

    public void GamePlayOn()
    {
		AddCoin(-GetNeedCoin());
	}

	public void AddCoin(int freeCoin, int chargeCoin = 0)
    {
        if (userData == null)
        {
            return;
        }

        userData.AddCoin(freeCoin, chargeCoin);

		if (string.IsNullOrEmpty(userData.email) == false ) 
        {
            UserDataRequest request = new UserDataRequest()
            {
                requestStatus = 1,
                email = userData.email,
                freeCoin = userData.freeCoin,
				chargeCoin = userData.chargeCoin,
				successOn = ResultData => { }
            };

            request.RequestOn().Forget();
        }
    }

    public void TurnOffOn()
    {
        playOn = false;

        if (isMultiOn)
        {
            return;
        }

        int tempIndex = currentTurnIndex;

		tempIndex++;

        if (tempIndex >= num_player)
        {
			tempIndex = 0;
		}

        SetCurrentTurnIndex(tempIndex);
	}

    public void SetCurrentTurnIndex(int index)
    {
		_currentTurnIndex = index;
	}

    public bool CheckLeaveEarly(PlayerIconController pic)
    {
        return isMultiOn == false &&  //싱글 플레이
               IsMyTurn() &&          // 내턴
               mapSizeEnum == MapSizeEnum.Large && // Large 맵
               leaveEarlyPopupReadyOn &&           // 조기 종료 준비 Flag
               pic.GetPlayerIcon(currentPlayer).connectedCount >= 20; // 연결된 영토가 20개 이상인지
	}


	public bool IsMyTurn()
    {
		return currentTurnIndex == (int)playerData.pe;
    }

	public PlayerData GetMyPlayerData()
	{
		return playerDataList.FirstOrDefault(data => data.pe == playerData.pe);
	}

	public PlayerData GetPlayerData(PlayerEnum playerEnum)
    {
        return playerDataList.FirstOrDefault(data => data.pe == playerEnum);
    }

    public void SetPlayerSkillData(PlayerEnum playerEnum , int skillCount)
    {
        foreach (var playerData in playerDataList)
        {
            if (playerData.pe == playerEnum) 
            {
                //Debug.LogWarning($"{playerData.pe} , {skillCount}");
                playerData.sc = skillCount;
            }   
        }
    }

    public List<AllianceData> GetAllianceDefaultData(PlayerEnum orderPlayer, List<PlayerEnum> playerList , PlayerIconController playerIconController)
    {
	    int maxCoinCount = GetMultiRewardCoin();
		int oneManCoinCount = maxCoinCount / playerList.Count;

		List<AllianceData> allianceDataList = new List<AllianceData>();

		foreach (var playerData in playerList)
		{
			AllianceData orderData = new AllianceData()
			{
				playerEnum = playerData,
				//coinCount = oneManCoinCount
			};

			allianceDataList.Add(orderData);

			//maxCoinCount -= oneManCoinCount;
		}

		AllianceData orderPlayerData = allianceDataList.FirstOrDefault(data => data.playerEnum == orderPlayer);

		//동맹 제안할 플레이어 추출
		List<AllianceData> otherPlayerList = allianceDataList.Where(data => data.playerEnum != orderPlayer).ToList();
		
		//AI가 동맹 주체라면
		if (GetPlayerData(orderPlayer).isAI)
        {
			List<PlayerIconElement> allConnectedHigherList = playerIconController.GetActiveDiceHigherList(allianceDataList);

			int allAreaCount = allConnectedHigherList.Sum(data => data.connectedCount);

			foreach (var playerIcon in allConnectedHigherList)
            {
				var allianceData = allianceDataList.FirstOrDefault(data => data.playerEnum.Equals(playerIcon.playerEnum));

				if (allianceData != null)
				{
                    int coinCount = GetMultiRewardCoin() * playerIcon.connectedCount / allAreaCount;

					allianceData.coinCount += coinCount;
					maxCoinCount -= coinCount;
				}
			}

            if (maxCoinCount > 0)
            {
				orderPlayerData.coinCount += maxCoinCount;
				maxCoinCount = 0;
			}
		}
        else
        {
			List<PlayerIconElement> connectedHigherList = playerIconController.GetActiveDiceHigherList(otherPlayerList);

			while (true)
			{
				//공평하게 나누고 maxCoinCount 가 0 보다 크다면
				if (maxCoinCount > 0)
				{
					orderPlayerData.coinCount += 1;
					maxCoinCount -= 1;
				}

				if (maxCoinCount > 0)
				{
					foreach (var playerIcon in connectedHigherList)
					{
						var allianceData = otherPlayerList.FirstOrDefault(data => data.playerEnum.Equals(playerIcon.playerEnum));

						if (allianceData != null)
						{
							allianceData.coinCount += 1;
							maxCoinCount -= 1;
						}
					}
				}

				if (maxCoinCount <= 0)
				{
					break;
				}
			}
		}

        return allianceDataList;
	}


	public bool IsAITurn()
    {
        bool isAiOn = false;

        PlayerData playerData = playerDataList.FirstOrDefault(data => (int)data.pe == currentTurnIndex);

        //멀티가 아닐때
        if (isMultiOn == false )
        {
            isAiOn = !IsMyTurn();
        }
        else 
        {
            if (playerData != null)
            {
                isAiOn = playerData.isAI;
            }
        }
        
        return isAiOn;
    }

    public void SetAllianceList(AllianceAllData allianceAllData)
    {
        this.allianceAllDataList.Add(allianceAllData);
    }

	/// <summary> 이미 맺어진 동맹 묶음 전체. 동맹 제안 팝업의 좌하단 표시용이다 (2026-09-19) </summary>
	public List<AllianceAllData> GetAllianceAllDataList()
	{
		return allianceAllDataList;
	}

	public int GetAllianceCount()
	{
		return this.allianceAllDataList.Count;
	}

	/// <summary>
	/// 이미 동맹이 되어있는지 체크
	/// </summary>
	/// <param name="playerEnum"></param>
	/// <returns></returns>
	public bool IsAlliance(PlayerEnum playerEnum) 
    {
        bool IsAlliance = false;

		foreach (var allianceData in allianceAllDataList)
        {
            if (allianceData.allianceDataList.Any(data => data.playerEnum == playerEnum))
            {
                IsAlliance = true;
			}
		}

        return IsAlliance;
    }

    public AllianceData GetMyAllianceData()
    {
        return GetAllianceData(playerData.pe);

		//return allianceDataList.FirstOrDefault(data => data.playerEnum == playerData.pe);
	}

	public AllianceData GetAllianceData(PlayerEnum playerEnum)
	{
		AllianceData resultData = null;

		foreach (var allianceData in allianceAllDataList)
		{
			var data = allianceData.allianceDataList.FirstOrDefault(data => data.playerEnum == playerEnum);

			if (data != null)
			{
				resultData = data;
			}
		}

		return resultData;

		//return allianceDataList.FirstOrDefault(data => data.playerEnum == playerEnum);
	}

	public bool IsAllAlliance(List<PlayerEnum> checkPlayerEnumList)
    {
        bool isAllAlliance = false;

		foreach (var allianceData in allianceAllDataList)
		{
            List<PlayerEnum> allList = allianceData.allianceDataList.Select(data => data.playerEnum).ToList();

			isAllAlliance = checkPlayerEnumList.All(data => allList.Contains(data));

            if (isAllAlliance)
            {
                isAllAlliance = true;
				break;
            }
		}

        return isAllAlliance;
    }

    /// <summary>
    /// 넘겨받은 플레이어 정보가 본인과 동맹인지 확인
    /// </summary>
    /// <param name="checkPlayerEnum"></param>
    /// <returns></returns>
    public bool IsMyAllAlliance(PlayerEnum checkPlayerEnum)
    {
        List<PlayerEnum> checkPlayerEnumList = new List<PlayerEnum>() 
        {
            checkPlayerEnum,
            playerData.pe
        };

        return IsAllAlliance(checkPlayerEnumList);
    }

    public AllianceAllData GetAllAlliance(PlayerEnum playerEnum)
    {
        AllianceAllData resultData = null;

        if (IsAlliance(playerEnum))
        {
            foreach (var allianceData in allianceAllDataList)
            {
				var data = allianceData.allianceDataList.FirstOrDefault(data => data.playerEnum == playerEnum);

				if (allianceData.allianceDataList.Any(data => data.playerEnum == playerEnum))
				{
                    resultData = allianceData;
				}
			}
        }
        else 
        {
            int maxCoin = isMultiOn ? GetMultiRewardCoin() : 3 * num_player;

            resultData = new AllianceAllData();
			resultData.allianceDataList.Add(new AllianceData(playerEnum, maxCoin));
        }

        return resultData;
    }

	public bool AllianceClearCheckOn(PlayerIconController playerIconController)
	{
        bool clearOn = false;

		AllianceAllData removeTarget = null;

		foreach (var allianceData in allianceAllDataList)
		{
			if (allianceData.allianceDataList.Count <= 1)
			{
				playerIconController.SetBetrayPlayerIcon(allianceData.allianceDataList[0].playerEnum);
				//AllianceClearOn();
				clearOn = true;

                removeTarget = allianceData;
			}
		}

        if (removeTarget != null)
        {
			allianceAllDataList.Remove(removeTarget);
		}

        return clearOn;
	}

    public void AllianceRemoveOn(AllianceAllData removeTarget)
    {
		allianceAllDataList.Remove(removeTarget);
	}

	public void AllianceClearOn() 
    {
		//allianceDataList = new List<List<AllianceData>>();
		allianceAllDataList.Clear();
	}

    public AllianceAllData BetrayOn(PlayerEnum playerEnum)
    {
        AllianceAllData allianceAllData = null;

		foreach (var allianceData in allianceAllDataList)
        {
			foreach (var listData in allianceData.allianceDataList)
			{
				if (listData.playerEnum == playerEnum)
				{
                    allianceAllData = allianceData;
					allianceData.allianceDataList.Remove(listData);

					if (allianceAllData.orderData.playerEnum == playerEnum)
                    {
                        //가장 코인이 높은 플레이어 취득
						var tempOrderData = allianceAllData.allianceDataList.OrderByDescending(data => data.coinCount).FirstOrDefault();

                        if (tempOrderData != null)
                        {
							allianceAllData.orderData = tempOrderData;
						}
                    }

					break;
				}
			}
        }

        return allianceAllData;
	}

    public int MyTurnAddOn()
    {
        myTurnCount++;

        return myTurnCount;
    }

    public void SkillCardCountAdd(int addCount = -1)
    {
		PlayerData p_data = GetPlayerData(playerData.pe);

		p_data.sc += addCount;

		p_data.sc = Mathf.Clamp(p_data.sc,0, 3);

        if (isMultiOn)
        {
            SkillCardRequest request = new SkillCardRequest()
            {
                playerEnum = p_data.pe,
                skillCardCount = p_data.sc,
            };

            ServerManager.Instance.SendMessageOn(request);

            SetPlayerSkillData(p_data.pe, p_data.sc);
        }
    }

    public void SkillCardCountAddOn(PlayerEnum playerEnum , int addCount = 1)
    {
		PlayerData p_data = GetPlayerData(playerEnum);
        p_data.sc += addCount;
		p_data.sc = Mathf.Clamp(p_data.sc, 0, 3);

		SkillCardRequest request = new SkillCardRequest()
		{
			playerEnum = p_data.pe,
			skillCardCount = p_data.sc,
		};

		ServerManager.Instance.SendMessageOn(request);
	}

	public void GameDataClearOn()
    {
		betrayReceivedCoin = 0;
		betrayPaidCoin = 0;

		PlayerDataClearOn();
		AllianceClearOn();
        StashCountClearOn();
        SetCurrentTurnIndex(0);

		// [추가] 새 게임 시 이전 게임의 상태값들을 완벽하게 초기화합니다.
		myTurnCount = 0;
		playOn = false;
		turnPosition = -1;

		// [핵심] 이전 방에서 날아오던 패킷 찌꺼기를 완전히 비워줍니다.
		if (isMultiOn && ServerManager.Instance != null)
		{
			ServerManager.Instance.ClearQueue();
		}

        if (randomPositionOn)
        {
            turnPosition = Random.Range(0, num_player);

            //Debug.LogWarning($"{turnPosition} , {num_player}");

            playerData.ci = turnPosition;
        }
    }

    void PlayerDataClearOn()
    {
        playerData.sc = 0;

		foreach (var currentPlayerData in playerDataList)
        {
            currentPlayerData.sc = 0;
		}
    }

	void StashCountClearOn()
    {
		for (int i = 0; i < stashCountList.Count; i++)
        {
            stashCountList[i] = 0;
		}
    }
	public bool GetSoundOn()
	{
		return soundOn;
	}

	public void SetSoundOn(bool _soundOn)
	{
		soundOn = _soundOn;
		PlayerPrefs.SetInt(soundOnKey, soundOn ? 1 : 0);
		PlayerPrefs.Save();

        SoundManager.Instance.SoundOffOn(!soundOn);
	}

	public bool GetMapCompensation()
    {
        return mapCompensation;
	}

	public void SetMapCompensation(bool _mapCompensation)
	{
        mapCompensation = _mapCompensation;
		PlayerPrefs.SetInt(mapCompensationKey, mapCompensation ? 1 : 0);
		PlayerPrefs.Save();
	}

    public bool CheckNewGame()
    {
        int needCoin = GetNeedCoin();

		//bool newGameOn = userData != null && userData.myCoin.Value >= newGameNeedCoin;
		bool newGameOn = userData != null && userData.myCoin.Value >= needCoin;

		if (newGameOn == false)
        {
			//PopupManager.Instance.NeedCoinPopupOn(-newGameNeedCoin);
			PopupManager.Instance.NeedCoinPopupOn(-needCoin);
		}

        return newGameOn;
	}
}

[System.Serializable]
public class Join
{
    public int[] dir = new int[6];

    public Join()
    {
        for (int i = 0; i < dir.Length; i++)
        {
            dir[i] = -1;
        }
    }
}

[System.Serializable]
public class PlayerData
{
    public PlayerEnum pe = PlayerEnum.Player_0;
    public int ci = 0;
    public int sc = 0;
    public bool isAI = false;

    public PlayerData() { }

    public PlayerData(PlayerEnum playerEnum , int colorIndex , int skillCardCount = 0, bool isAI = false)  
    {
        this.pe = playerEnum;
        this.ci = colorIndex;
        this.sc = skillCardCount;
        this.isAI = isAI;
    }
}

[System.Serializable]
public class AllianceAllData
{
	public AllianceData orderData;
	public List<AllianceData> allianceDataList = new List<AllianceData>();
}

[System.Serializable]
public class AllianceData 
{
    public PlayerEnum playerEnum;
    public int coinCount;

    public AllianceData() { }
    public AllianceData(PlayerEnum playerEnum , int coinCount)
    {
        this.playerEnum = playerEnum;
        this.coinCount = coinCount;
    }
}

public class HexagonData
{
    public int areaIndex = 0;
    public PlayerEnum playerEnum = PlayerEnum.Player_None;
}

public class MapData
{
    public MapSizeEnum mapSizeEnum;
    public Vector2 mapSizeValue;
    public Vector2 panelPos;
    public Vector2 hexagonSize;
    public Vector2 hexagonLineSize;
    public int deleteCount;
    public int activePlayerCount;

    public MapData() { }
    public MapData(MapSizeEnum mapSizeEnum , Vector2 mapSizeValue, Vector2 panelPos, Vector2 hexagonSize, Vector2 hexagonLineSize , int deleteCount, int activeCount) 
    {
        this.mapSizeEnum = mapSizeEnum;
        this.mapSizeValue = mapSizeValue;
        this.panelPos = panelPos;
        this.hexagonSize = hexagonSize;
        this.hexagonLineSize = hexagonLineSize;
        this.deleteCount = deleteCount;
        this.activePlayerCount = activeCount;
    }
}

