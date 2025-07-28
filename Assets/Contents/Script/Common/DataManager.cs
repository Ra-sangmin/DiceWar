using Assets.SimpleSignIn.Google.Scripts;
using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class DataManager : MonoSingleton<DataManager>
{
    public UserData userData;
    public string loginId = string.Empty;
    public bool isMultiOn = false;

	public AILevel aiLevel = AILevel.Hard;
    public MapSizeEnum mapSizeEnum = MapSizeEnum.Medium;
    Dictionary<MapSizeEnum, MapData> mapDataDic = new Dictionary<MapSizeEnum, MapData>();
    
    public int num_player = 2;//총 플레이어의 수 (기본 값 : 3)
    public int off_line_num_player = 0;//AI 수 (기본 값 : 2)
    public int num_area = 10;//총 영토의 수 (기본값 : 10)
    public int diceMaxCount = 6;//영토의 주사위 최대 갯수

    //public Vector2 mapSizeValue = new Vector2(20, 15);
    
    public List<AreaData> backUpAreaDataList = new List<AreaData>();

    public PlayerData playerData = new PlayerData();
    public bool setTurnPositionOn = false;
    public bool choosePositionOn = false;
    public bool mapSelectionOn = true;
    public bool diceCompensationOn = false;

    //-1 = random , 0 ~ 6 selectPosition 
    public int turnPosition = -1;

    public bool isOwner = false;

    public int currentTurnIndex = 0;

    private Dictionary<PlayerEnum,int> playerColorIndexDic = new Dictionary<PlayerEnum,int>();
    public List<PlayerData> playerDataList = new List<PlayerData>();
    public ReactiveProperty<bool> gameStart = new ReactiveProperty<bool>(false);

    public bool playOn = false;

    private List<AllianceData> allianceDataList = new List<AllianceData>();

    public List<AreaData> areaDataList = new List<AreaData>();
    public MapCreateRequestOn testData;

	public List<int> stashCountList = new List<int>();

	//public int stashCount = 0;

    public int myTurnCount = 0;

    public bool inGameEditOn = false;

    public override void Init()
    {
        base.Init();

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

    void MapDataInit() 
    {
        mapDataDic = new Dictionary<MapSizeEnum, MapData>
        {
            { MapSizeEnum.Small, new MapData(MapSizeEnum.Small, new Vector2(20,15),new Vector2(-630,-360),new Vector2(65,70),new Vector2(70,70) , 3, 2)},
            { MapSizeEnum.Medium, new MapData(MapSizeEnum.Medium, new Vector2(30,23),new Vector2(-700,-400),new Vector2(50,50),new Vector2(50,50), 8, 4)},
            { MapSizeEnum.Large, new MapData(MapSizeEnum.Large, new Vector2(40,30),new Vector2(-750,-425),new Vector2(40,40),new Vector2(40,40), 14, 5)},
        };
    }

    public void UserDataSave(UserDataRespons data)
    {
        userData = new UserData(data.email, data.snsType, data.coin);
    }

    public void InitMapData()
    {
        SetPlayerColor();
    }

    void SetPlayerColor() 
    {
        List<int> colorList = new List<int>() { 0,1,2,3,4,5,6};
        //colorList.Shuffle();

        int index = colorList.IndexOf(playerData.colorIndex);

        int playerEnumIndex = (int)playerData.playerEnum;

        //int temp = colorList[playerEnumIndex];
        //colorList[playerEnumIndex] = playerData.colorIndex;
        //colorList[index] = temp;

        playerData.playerEnum = (PlayerEnum)playerData.colorIndex;


		for (int i = 0; i < num_player; i++) 
        {
            playerColorIndexDic[(PlayerEnum)i] = colorList[i];
        }
    }

    public void SetPlayerColor(List<PlayerData> playerDataList) 
    {
        foreach (var playerData in playerDataList)
        {
            playerColorIndexDic[(PlayerEnum)playerData.playerEnum] = playerData.colorIndex;
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

        for (int i = 0; i < playerDataList.Count; i++)
        {
            bool isAI = isMultiOn == false ? playerData.colorIndex != i :  i > onLineCnt;

			playerDataList[i].isAI = isAI;

			foreach (var area in GetAreaDtaList(playerDataList[i].playerEnum))
			{
				area.SetDice(GetDiceCount(isAI));
			}
		}
    }

	List<AreaData> GetAreaDtaList(PlayerEnum playerEnum)
    {
		return areaDataList.Where(data => data.player == playerEnum).ToList();
	}

    /// <summary>
    /// 초기 주사위 설정
    /// </summary>
    /// <param name="isAi"></param>
    /// <returns></returns>
    int GetDiceCount(bool isAi)
    {
		//DiceCountData data = Resources.Load<DiceCount>("DiceCount").GetData(isAi, aiLevel);
		DiceCountData data = DiceCountManager.Instance.GetData(isAi, aiLevel);

        //Debug.LogWarning(data.minCount);

		//int dice = ;

		//if (isAi)
		//{
		//	switch (aiLevel)
		//	{
		//		case AILevel.Easy: dice = Random.Range(1, 4); break;
		//		case AILevel.Normal: dice = Random.Range(2, 5); break;
		//		case AILevel.Hard: dice = Random.Range(3, 6); break;
		//	}
		//}
		//else
		//{
		//	switch (aiLevel)
		//	{
		//		case AILevel.Easy: dice = Random.Range(3, 5); break;
		//		case AILevel.Normal: dice = Random.Range(2, 5); break;
		//		case AILevel.Hard: dice = Random.Range(1, 4); break;
		//	}
		//}

		return Random.Range(data.minCount, data.maxCount+1);
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

    public void SetOffLinePlayerCnt(int offLinePlayer)
    {
        this.off_line_num_player = offLinePlayer;
    }

    public Color SetCurrentPlayerColor(int currentPlayerColorIndex)
    {
        this.playerData.colorIndex = currentPlayerColorIndex;
        return GetPlayerColor(currentPlayerColorIndex);
    }
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
        return GetPlayerColor(playerData.colorIndex);
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
            playerData.playerEnum = (PlayerEnum)Random.Range(0, num_player);
        }
        else 
        {
			playerData.playerEnum = (PlayerEnum)turnPosition;
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

	public void AddCoin(int addCoin)
    {
        if (userData == null)
        {
            return;
        }

        userData.myCoin.Value += addCoin;

        if (string.IsNullOrEmpty(userData.email) == false ) 
        {
            UserDataRequest request = new UserDataRequest()
            {
                requestStatus = 1,
                email = userData.email,
                coin = userData.myCoin.Value,
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
        currentTurnIndex++;

        if (currentTurnIndex >= num_player)
        {
            currentTurnIndex = 0;
        }
    }

    public bool IsMyTurn()
    {
		return currentTurnIndex == (int)playerData.playerEnum;
    }

    public PlayerData GetPlayerData(PlayerEnum playerEnum)
    {
        return playerDataList.FirstOrDefault(data => data.playerEnum == playerEnum);
    }

    public void SetPlayerSkillData(PlayerEnum playerEnum , int skillCount)
    {
        foreach (var playerData in playerDataList)
        {
            if (playerData.playerEnum == playerEnum) 
            {
                playerData.skillCardCount = skillCount;
            }   
        }
    }

    public bool IsAITurn()
    {
        bool isAiOn = false;

        PlayerData playerData = playerDataList.FirstOrDefault(data => (int)data.playerEnum == currentTurnIndex);

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

    public void SetAllianceList(List<AllianceData> allianceDataList)
    {
        this.allianceDataList = allianceDataList;
    }

    public bool IsAlliance(PlayerEnum playerEnum) 
    {
        return allianceDataList.Any(data => data.playerEnum == playerEnum);
    }

    public AllianceData GetMyAllianceData()
    {
        return allianceDataList.FirstOrDefault(data => data.playerEnum == playerData.playerEnum);
    }

    public bool IsAllAlliance(List<PlayerEnum> checkPlayerEnumList)
    {
        bool isAllAlliance = true;

        foreach (PlayerEnum playerEnum in checkPlayerEnumList)
        {
            if (IsAlliance(playerEnum) == false)
            {
                isAllAlliance = false;
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
            playerData.playerEnum
        };

        return IsAllAlliance(checkPlayerEnumList);
    }

    public List<AllianceData> GetAllAlliance(PlayerEnum playerEnum)
    {
        List<AllianceData> resultData = new List<AllianceData>();

        if (IsAlliance(playerEnum))
        {
            resultData = allianceDataList;
        }
        else 
        {
            int maxCoin = 3 * num_player;

            resultData.Add(new AllianceData(playerEnum, maxCoin));
        }

        return resultData;
    }

    public void AllianceClearOn() 
    {
        allianceDataList = new List<AllianceData>();
    }

    public void BetrayOn(PlayerEnum playerEnum)
    {
        foreach (var allianceData in allianceDataList)
        {
            if (allianceData.playerEnum == playerEnum)
            {
                allianceDataList.Remove(allianceData);
                break;
            }
        }
    }

    public int MyTurnAddOn()
    {
        myTurnCount++;

        return myTurnCount;
    }

    public void SkillCardCountAdd(int addCount = -1)
    {
        playerData.skillCardCount += addCount;

        playerData.skillCardCount = Mathf.Clamp(playerData.skillCardCount,0, 3);

        if (isMultiOn)
        {
            SkillCardRequest request = new SkillCardRequest()
            {
                playerEnum = playerData.playerEnum,
                skillCardCount = playerData.skillCardCount,
            };

            ServerManager.Instance.SendMessageOn(request);

            SetPlayerSkillData(playerData.playerEnum, playerData.skillCardCount);
        }
    }

    public void GameDataClearOn()
    {
        AllianceClearOn();
        StashCountClearOn();
        currentTurnIndex = 0;
	}

	void StashCountClearOn()
    {
		for (int i = 0; i < stashCountList.Count; i++)
        {
            stashCountList[i] = 0;
		}
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
    public PlayerEnum playerEnum = PlayerEnum.Player_0;
    public int colorIndex = 1;
    public int skillCardCount = 1;
    public bool isAI = false;

    public PlayerData() { }

    public PlayerData(PlayerEnum playerEnum , int colorIndex , int skillCardCount = 1, bool isAI = false)  
    {
        this.playerEnum = playerEnum;
        this.colorIndex = colorIndex;
        this.skillCardCount = skillCardCount;
        this.isAI = isAI;
    }
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

public class UserData
{
    public string email = string.Empty;
    public int snsType = 0;
    public ReactiveProperty<int> myCoin = new ReactiveProperty<int>(0);

    public UserData() { }
    public UserData(string email, int snsType , int coin) 
    {
        this.email = email;
        this.snsType = snsType;
        this.myCoin.Value = coin;
    }
}