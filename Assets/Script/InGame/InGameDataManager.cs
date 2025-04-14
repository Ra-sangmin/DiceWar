using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class InGameDataManager : MonoSingleton<InGameDataManager>
{
    public string loginId = string.Empty;
    public bool isMultiOn = false;
    // 셀 데이터 (cell data)
    public MapSizeEnum mapSizeEnum = MapSizeEnum.Small;
    public AILevel aiLevel = AILevel.Easy;
    public int num_player = 3;//총 플레이어의 수 (기본 값 : 3)
    public int off_line_num_player = 2;//AI 수 (기본 값 : 2)
    //public int num_X = 20; // 가로 육각형 타일의 개수 (기본값 : 20)
    //public int num_Y = 15; // 세로 육각형 타일의 개수 (기본값 : 15)
    public int num_area = 10;//총 영토의 수 (기본값 : 10)
    public int diceMaxCount = 6;//영토의 주사위 최대 갯수

    public Vector2 mapSizeValue = new Vector2(20, 15);
    //public int cel_max;
    //public int[] num;// 지역번호 (area serial number)
    //public int[] rcel;// 인접 셀(adjacent cell)
    //public int[] next_f;// 침투시 사용하는 주변 셀(peripheral cell to use for penetration)
    //public int[] chk;
    //public int[] cel;
    //public Join[] join;//인접 셀을 포함한 배열(arrangement with adjacent cells)
    
    //public List<AreaData> areaDataList = new List<AreaData>(); //인접 셀을 포함한 배열(arrangement with adjacent cells)
    public List<AreaData> backUpAreaDataList = new List<AreaData>();

    public PlayerData playerData = new PlayerData();
    public bool setTurnPositionOn = false;
    public bool choosePositionOn = false;
    public bool mapSelectionOn = true;
    public bool diceCompensationOn = false;

    //-1 = random , 0 ~ 6 selectPosition 
    public int turnPosition = -1;

    public int currentTurnIndex = 0;

    private Dictionary<PlayerEnum,int> playerColorIndexDic = new Dictionary<PlayerEnum,int>();
    public List<PlayerData> playerDataList = new List<PlayerData>();
    public ReactiveProperty<bool> gameStart = new ReactiveProperty<bool>(false);

    public ReactiveProperty<int> myCoin = new ReactiveProperty<int>(0);

    public bool playOn = false;

    private List<AllianceData> allianceDataList = new List<AllianceData>();

    //private MapCreater mapCreater;
    public List<AreaData> areaDataList = new List<AreaData>();
    //public Join[] join;
    public MapCreateRequestOn testData;

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
    }
    public void InitMapData()
    {
        SetMapSizeValue();

        SetPlayerColor();
    }

    public void SetMapSizeValue()
    {
        switch (mapSizeEnum)
        {
            case MapSizeEnum.Small: mapSizeValue.x = 20; mapSizeValue.y = 15; break;
            case MapSizeEnum.Medium: mapSizeValue.x = 30; mapSizeValue.y = 23; break;
            case MapSizeEnum.Large: mapSizeValue.x = 40; mapSizeValue.y = 30; break;
        }
    }

    void SetPlayerColor() 
    {
        List<int> colorList = new List<int>() { 0,1,2,3,4,5,6};
        colorList.Shuffle();

        int index = colorList.IndexOf(playerData.colorIndex);

        int playerEnumIndex = (int)playerData.playerEnum;

        int temp = colorList[playerEnumIndex];
        colorList[playerEnumIndex] = playerData.colorIndex;
        colorList[index] = temp;

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
        return (int)mapSizeValue.x * (int)mapSizeValue.y;
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
        mapCreater.InitMapData(mapSizeValue, num_player);

        areaDataList = mapCreater.CreateMap();
        areaDataList = areaDataList.Where(data => data.player !=   PlayerEnum.Player_None && data.cel.Count  > 0).ToList();

        //foreach (var areaData in areaDataList)
        //{
        //    //areaData.SetAdj(mapCreater.join);
        //}

        playerDataList = new List<PlayerData>();

        foreach (var item in playerColorIndexDic)
        {
            playerDataList.Add(new PlayerData(item.Key, item.Value));   
        }

        int onLineCnt = num_player - off_line_num_player -1;

        for (int i = 0; i < playerDataList.Count; i++)
        {
            playerDataList[i].isAI = i > onLineCnt;
        }

        //PlayerData playerData = new PlayerData();

        //MapCreateRequestOn test = new MapCreateRequestOn();
        //test.playerDataList = playerDataList;
        //test.area = areaDataList;
        

        //string jsonData = JsonUtility.ToJson(test);

        //Debug.LogWarning(jsonData);
        //Byte[] sendData = System.Text.Encoding.UTF8.GetBytes(jsonData);

        //Debug.LogWarning(sendData.Length);


        if (choosePositionOn)
        {
            SetTurnPosition(turnPosition);
        }
    }

    public Vector2 GetMapSizeValue() 
    {
        return mapSizeValue;
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

    public void SetAreaData(AreaData _areaData)
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

    public int SetBundleKey(PlayerEnum checkEnum)
    {
        foreach (AreaData areaData in areaDataList.Where(data => data.player == checkEnum).ToList())
        {
            areaData.bundleKey = string.Empty;
        }

        int maxCount = 0;
        int index = 0;

        while (true)
        {
            var checkList = areaDataList.Where(data => data.player == checkEnum && data.bundleKey == string.Empty).ToList();

            if (checkList.Count == 0)
            {
                break;
            }

            int count = SetBundleKey(checkEnum, index, checkList[0]);

            maxCount = Mathf.Max(maxCount, count);

            index++;
        }

        return maxCount;
    }

    public int SetBundleKey(PlayerEnum checkEnum, int index, AreaData areaData)
    {
        string bundleKey = GetConnectedIndex(checkEnum, index);
        areaData.SetBundleKey(areaDataList, checkEnum, bundleKey);

        var sameBundleKeyPieceList = areaDataList.Where(data => !string.IsNullOrEmpty(data.bundleKey) && data.bundleKey.Equals(bundleKey)).ToList();

        return sameBundleKeyPieceList.Count;
    }

    public Vector2 GetPos(int index)
    {
        //Vector2 mapSizeValue = mapSizeValue;

        //x 위치
        int ox = index % (int)mapSizeValue.x;
        //y 위치
        int oy = index / (int)mapSizeValue.x;

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

    public Vector2 GetHexagonPanelPos()
    {
        Vector2 pos = Vector2.zero;

        switch (mapSizeEnum)
        {
            case MapSizeEnum.Small: pos = new Vector2(200, 235); break;
            case MapSizeEnum.Medium: pos = new Vector2(150, 190); break;
            case MapSizeEnum.Large: pos = new Vector2(65, 170); break;
        }

        return pos;
    }

    public Vector2 GetHexagonSizeDelta()
    {
        Vector2 sizeDelta = Vector2.zero;

        switch (mapSizeEnum)
        {
            case MapSizeEnum.Small: sizeDelta = new Vector2(65, 70); break;
            case MapSizeEnum.Medium: sizeDelta = new Vector2(50, 50); break;
            case MapSizeEnum.Large: sizeDelta = new Vector2(40, 40); break;
        }

        return sizeDelta;
    }

    public Vector2 GetHexagonLineSizeDelta()
    {
        Vector2 sizeDelta = Vector2.zero;

        switch (mapSizeEnum)
        {
            case MapSizeEnum.Small: sizeDelta = new Vector2(70, 70); break;
            case MapSizeEnum.Medium: sizeDelta = new Vector2(50, 50); break;
            case MapSizeEnum.Large: sizeDelta = new Vector2(40, 40); break;
        }

        return sizeDelta;
    }

    public Color GetPlayerColor()
    {
        return GetPlayerColor(playerData.colorIndex);
    }

    public void ReStartOn()
    {
        //List <AreaData> testList = mapCreater.GetAreaDataList();

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

    public void AddCoin(int addCoin)
    {
        myCoin.Value += addCoin;
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

    public bool IsAllAlliance(List<PlayerEnum> checkPlayerEnumList)
    {
        //Debug.LogWarning(playerEnum);

        bool isAllAlliance = true;

        foreach (PlayerEnum playerEnum in checkPlayerEnumList)
        {
            if (IsAlliance(playerEnum) == false)
            {
                isAllAlliance = false;
            }
        }

        //playerEnumList.

        return isAllAlliance;

        //return allianceDataList.Any(data => data.playerEnum == playerEnum);
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
            resultData.Add(new AllianceData(playerEnum, 10));
        }

        return resultData;
    }

    public void BetrayOn() 
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
public enum AILevel
{
    Easy = 0,
    Normal = 1,
    Hard = 2,
}

[System.Serializable]
public enum MapSizeEnum
{
    Small = 0,
    Medium = 1,
    Large = 2,
}

[System.Serializable]
public enum PlayerEnum
{
    Player_None = -1,
    Player_0 = 0,
    Player_1 = 1,
    Player_2 = 2,
    Player_3 = 3,
    Player_4 = 4,
    Player_5 = 5,
    Player_6 = 6,
    Player_7 = 7,
}


[System.Serializable]
public class PlayerData
{
    public PlayerEnum playerEnum = PlayerEnum.Player_1;
    public int colorIndex = 1;
    public int skillCardCount = 3;
    public bool isAI = false;

    public PlayerData() { }

    public PlayerData(PlayerEnum playerEnum , int colorIndex , int skillCardCount = 3 , bool isAI = false)  
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

[System.Serializable]
public class MapCreater
{
    public int num_player = 3;//총 플레이어의 수 (기본 값 : 3)
    public int off_line_num_player = 2;//총 플레이어의 수 (기본 값 : 3)
    public Vector2 mapSizeValue = new Vector2(20, 15); // 가로 세로 육각형 타일의 개수 ( 기본 값 x = 20 , y = 15)

    //public int num_X = 20; // 가로 육각형 타일의 개수 (기본값 : 20)
    //public int num_Y = 15; // 세로 육각형 타일의 개수 (기본값 : 15)
    public int num_area = 10;//총 영토의 수 (기본값 : 10)
    public int diceMaxCount = 6;//영토의 주사위 최대 갯수

    private int cel_max;
    public int[] num;// 지역번호 (area serial number)
    public int[] rcel;// 인접 셀(adjacent cell)
    public int[] next_f;// 침투시 사용하는 주변 셀(peripheral cell to use for penetration)
    //private int[] chk;
    public int[] cel;
    public Join[] join;//인접 셀을 포함한 배열(arrangement with adjacent cells)

    public List<HexagonData> hexagonDataList = new List<HexagonData>();

    public List<AreaData> areaDataList = new List<AreaData>(); //인접 셀을 포함한 배열(arrangement with adjacent cells)

    public void InitMapData(Vector2 mapSizeValue ,int num_player)
    {
        this.num_player = num_player;

        this.mapSizeValue = mapSizeValue;

        ////int num_X = 20;
        ////int num_Y = 15;

        //switch (mapSizeEnum)
        //{
        //    case MapSizeEnum.Small: mapSizeValue.x = 20; mapSizeValue.y = 15; break;
        //    case MapSizeEnum.Medium: mapSizeValue.x = 30; mapSizeValue.y = 23; break;
        //    case MapSizeEnum.Large: mapSizeValue.x = 40; mapSizeValue.y = 30; break;
        //}


        this.cel_max = (int)this.mapSizeValue.x * (int)this.mapSizeValue.y;

        cel = new int[this.cel_max];

        //인접 셀을 포함한 배열(arrangement with adjacent cells)
        join = new Join[this.cel_max];

        for (int i = 0; i < this.cel_max; i++)
        {
            this.join[i] = new Join();
            for (int j = 0; j < 6; j++)
            {
                this.join[i].dir[j] = this.NextCel(i, j);
            }
        }

        // 지역 데이터 (area data)
        num_area = 100;//최대 영역 수 (maximum number of areas)
        //areaDataList = new List<AreaData>();

        //for (int i = 0; i < num_area; i++)
        //{
        //    this.areaDataList.Add(new AreaData(i));
        //}

        // 맵을 만들 때 사용 (used for map creation)
        num = new int[this.cel_max];// 지역번호 (area serial number)

        for (int i = 0; i < this.cel_max; i++)
        {
            this.num[i] = i;
        }

        rcel = new int[this.cel_max];// 인접 셀(adjacent cell)
        next_f = new int[this.cel_max];// 침투시 사용하는 주변 셀(peripheral cell to use for penetration)
        //this.chk = new int[this.num_area];        // 영역 그리기 선용 (for area drawing lines)

        // 일련 번호 셔플
        for (int i = 0; i < cel_max; i++)
        {
            int r = Random.Range(0, cel_max);
            int tmp = num[i];
            num[i] = num[r];
            num[r] = tmp;

            //Debug.LogWarning(i + " , "+num[i]);
        }

        //for (int i = 0; i < num_area; i++)
        //{
        //    areaDataList[i].SetDice(0);
        //}

        // 셀 초기화
        for (int i = 0; i < cel_max; i++)
        {
            cel[i] = 0;
            rcel[i] = 0; // 인접 셀
        }

        //SetPlayerColor();
    }

    public Join[] GetJoin()
    {
        return join;
    }
    public int[] GetCell()
    {
        return cel;
    }

    public List<AreaData> GetAreaDataList()
    {
        return areaDataList;
    }

    public void SetMapSize(MapSizeEnum mapSizeEnum)
    {
        
    }

    /// <summary>
    /// 메서드 옆의 셀 번호를 반환합니다 (return the cell number to the next method)
    /// </summary>
    /// <param name="opos"></param>
    /// <param name="dir"></param>
    /// <returns></returns>
    public int NextCel(int opos, int dir)
    {
        //x인덱스
        int ox = opos % (int)this.mapSizeValue.x;
        //int oy = Mathf.FloorToInt((float)opos / this.XMAX);
        //y인덱스
        int oy = opos / (int)this.mapSizeValue.x;
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

        if (x < 0 || y < 0 || x >= this.mapSizeValue.x || y >= this.mapSizeValue.y) return -1;

        return y * (int)this.mapSizeValue.x + x;
    }

    

    public List<AreaData> CreateMap()
    {
        int c, an;
        an = 1; // 지역 번호

        //cel = InGameDataManager.Instance.testData.mapCreater.cel;
        //rcel = InGameDataManager.Instance.testData.mapCreater.rcel;

        SetCel();

        //List<AreaData> tempAreaDataList = new List<AreaData>();
        areaDataList = new List<AreaData>();

        // 영역 데이터 초기화
        for (int i = 0; i < num_area; i++)
        {
            AreaData areaData = new AreaData(i);

            areaData.dice = Random.Range(1, 5);

            areaDataList.Add(areaData);

            //areaDataList[i] = new AreaData(i);
        }

        // 면적
        for (int i = 0; i < cel_max; i++)
        {
            an = cel[i];
            if (an > 0) 
            {
                areaDataList[an].AddCel(i);

                //AddCel
            } //areaDataList[an].size++;
        }

        // 면적 10 이하의 영역을 지우기
        for (int i = 1; i < num_area; i++)
        {
            if (areaDataList[i].cel.Count <= 10) areaDataList[i].ClearCel();
        }

        for (int i = 0; i < cel_max; i++)
        {
            an = cel[i];
            if (areaDataList[an].cel.Count == 0)
            {
                cel[i] = 0;
            }
        }

        ////인접 데이터 생성
        //foreach (AreaData areaData in areaDataList)
        //{

        //}

        //c = 0;
        //int x, y, len;
        //for (int i = 0; i < mapSizeValue.y; i++)
        //{
        //    for (int j = 0; j < mapSizeValue.y; j++)
        //    {
        //        an = cel[c];
        //        if (an > 0)
        //        {
        //            //// 중심지로부터의 거리(경계선 근처는 가능한 한 피한다)
        //            //x = Mathf.Abs(areaDataList[an].cx - j);
        //            //y = Mathf.Abs(areaDataList[an].cy - i);
        //            //len = x + y;
        //            int f = 0;
        //            for (int k = 0; k < 6; k++)
        //            {
        //                int pos = join[c].dir[k];
        //                if (pos > 0)
        //                {
        //                    int an2 = cel[pos];
        //                    if (an2 != an)
        //                    {
        //                        //f = 1;
        //                        // 이어서 인접 데이터도 작성
        //                        areaDataList[an].GetJoin()[an2] = 1;
        //                        areaDataList[an].SetConnectedPieceList();
        //                    }
        //                }
        //            }
        //            //if (f > 0) len += 4;
        //            //// 거리가 가까운 것을 중심지로 한다
        //            //if (len < areaDataList[an].len_min)
        //            //{
        //            //    areaDataList[an].len_min = len;
        //            //    //areaDataList[an].cpos = i * num_X + j;
        //            //}
        //        }
        //        c++;
        //    }
        //}

        // 지역 속군을 결정
        for (int i = 0; i < num_area; i++) areaDataList[i].PlayerChangeOn(PlayerEnum.Player_None);
        int arm = 0; // 속군
        int[] alist = new int[num_area]; // 지역 목록
        while (true)
        {
            c = 0;
            for (int i = 1; i < num_area; i++)
            {
                if (areaDataList[i].cel.Count == 0) continue;
                if ((int)areaDataList[i].player >= 0) continue;
                alist[c] = i;
                c++;
            }
            if (c == 0) break;
            an = alist[Random.Range(0, c)];
            areaDataList[an].player = (PlayerEnum)arm;

            arm++; if (arm >= num_player) arm = 0;
        }


        //for (int i = 0; i < num_area; i++)
        //{
        //    HexagonData hexagonData = new HexagonData();

        //    hexagonData.areaIndex = cel[i];
        //    hexagonData.playerEnum = PlayerEnum.Player_None;

        //    hexagonDataList.Add(hexagonData);
        //}
        

        return areaDataList;
        //areaDataList = tempAreaDataList;
        //areaDataList = tempAreaDataList.Where(data => data.size > 0).ToList();

        //foreach (var item in areaDataList)
        //{
        //    Debug.LogWarning($" {item.areaIndex} , {item.size}");
            
        //}

        // 영역 그리기 선 데이터 작성
        //for (int i = 0; i < this.num_area; i++) this.chk[i] = 0;
        //for (int i = 0; i < this.cel_max; i++)
        //{
        //    var area = this.cel[i];
        //    if (area == 0) continue;
        //    if (this.chk[area] > 0) continue;
        //    for (int k = 0; k < 6; k++)
        //    {
        //        if (this.chk[area] > 0) break;
        //        var n = this.join[i].dir[k];
        //        if (n >= 0)
        //        {
        //            if (this.cel[n] != area)
        //            {
        //                //this.set_area_line(i, k);
        //                this.chk[area] = 1;
        //            }
        //        }
        //    }
        //}

        //if (choosePositionOn)
        //{
        //    SetTurnPosition(turnPosition);
        //}
    }

    void SetCel()
    {
        int an = 1;

        int ranValue = Random.Range(0, cel_max);

        rcel[ranValue] = 1; // 첫 번째 셀

        while (true)
        {
            // 침투 개시 셀 결정
            int pos = -1;
            int min = 9999;
            for (int i = 0; i < cel_max; i++)
            {
                if (cel[i] > 0) continue;
                if (num[i] > min) continue;
                if (rcel[i] == 0) continue;
                min = num[i];
                pos = i;
            }

            if (min == 9999) break;

            // 침투 개시
            int ret = Percolate(pos, 8, an);

            if (ret == 0) break;
            an++;
            if (an >= num_area) break;
        }

        // 바다에서 면적 1의 셀을 없애기
        for (int i = 0; i < cel_max; i++)
        {
            if (cel[i] > 0) continue;
            int pos;
            int f = 0;
            int a = 0;
            for (int k = 0; k < 6; k++)
            {
                pos = join[i].dir[k];
                if (pos < 0) continue;
                if (cel[pos] == 0) f = 1; else a = cel[pos];
            }
            if (f == 0) cel[i] = a;
        }
    }

    public int Percolate(int pt, int cmax, int an)
    {
        if (cmax < 3) cmax = 3;

        int i, j, k;
        int opos = pt; // 시작 셀

        // 인접 플래그
        for (i = 0; i < cel_max; i++) next_f[i] = 0;

        int c = 0; // 셀 수
        while (true)
        {
            cel[opos] = an;

            c++;
            // 주변 셀
            for (i = 0; i < 6; i++)
            {
                int pos = join[opos].dir[i];
                if (pos < 0) continue;
                next_f[pos] = 1;
            }
            // 주변 셀에서 최소 번호를 다음 셀로 설정
            int min = 9999;
            for (i = 0; i < cel_max; i++)
            {
                if (next_f[i] == 0) continue; // 인접하지 않음
                if (cel[i] > 0) continue; // 이미 지역화
                if (num[i] > min) continue; // 최소 주문 번호가 아님
                min = num[i];
                opos = i;
            }

            if (min == 9999) break;
            if (c >= cmax) break; // 주어진 면적을 초과
        }

        // 인접 셀 추가
        for (i = 0; i < cel_max; i++)
        {
            if (next_f[i] == 0) continue;
            if (cel[i] > 0) continue; // 이미 지역화
            cel[i] = an;

            c++;
            // 또한, 인접 셀을 다음 영역의 후보로한다.
            for (k = 0; k < 6; k++)
            {
                int pos = join[i].dir[k];
                if (pos < 0) continue;
                rcel[pos] = 1;
            }
        }

        return c;
    }

    //public void set_area_line(int old_cel, int old_dir)
    //{
    //    var c = old_cel;
    //    var d = old_dir;
    //    var area = this.cel[c]; // 지역 번호

    //    var cnt = 0;
    //    this.areaDataList[area].line_cel[cnt] = c;
    //    this.areaDataList[area].line_dir[cnt] = d;
    //    cnt++;
    //    for (var i = 0; i < 100; i++)
    //    {
    //        d++; if (d >= 6) d = 0; // 방향 추가
    //        var n = this.join[c].dir[d];
    //        if (n >= 0)
    //        {
    //            if (this.cel[n] == area)
    //            {
    //                // 이웃이 같은 영역이면 셀 이동, 방향 마이너스 2
    //                c = n;
    //                d -= 2; if (d < 0) d += 6;
    //            }
    //        }

    //        this.areaDataList[area].line_cel[cnt] = c;
    //        this.areaDataList[area].line_dir[cnt] = d;
    //        cnt++;
    //        if (c == old_cel && d == old_dir) break;
    //    }
    //}
}

public class HexagonData
{
    public int areaIndex = 0;
    public PlayerEnum playerEnum = PlayerEnum.Player_None;
}