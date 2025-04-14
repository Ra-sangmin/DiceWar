using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MapController : MonoBehaviour
{
    [SerializeField] RectTransform hexagonPanel;
    [SerializeField] Hexagon hexagonObjPrefab;
    [SerializeField] HexagonLine hexagonLinePrefab;
    [SerializeField] MapDice mapDicePrefab;
    [SerializeField] Button endTurnBtn;

    [SerializeField] PlayerDataPanel playerDataPanel;

    private List<Hexagon> hexagonList = new List<Hexagon>(); //인접 셀을 포함한 배열(arrangement with adjacent cells)
    //private List<HexagonLine> hexagonLineList = new List<HexagonLine>();
    private List<MapDice> mapDiceList = new List<MapDice>();

    
    private bool diceAddEventOn = false;
    private bool attackEventOn = false;

    public PlayerIconController playerIconController;
    public InGameBottomController inGameBottomController;
    public UnityAction gameWinOn;
    public UnityAction gameLoseOn;
    public UnityAction turnOffOn;
    public UnityAction<AreaData> selectAreaOn;
    public int choisIndex = -1;

    public Join[] join;//인접 셀을 포함한 배열(arrangement with adjacent cells)

    private void Awake()
    {
        //SetEvent();
    }

    public void SetEvent()
    {
        playerIconController.gameWinOn = gameWinOn;
        playerIconController.gameLoseOn = gameLoseOn;
        playerIconController.playerClickOn = PlayerClickOn;
    }
    private void PlayerClickOn(PlayerIcon PlayerIcon)
    {
        if (PlayerIcon.playerEnum == InGameDataManager.Instance.playerData.playerEnum)
        {
            inGameBottomController.nonePlayPanel.SetActiveBtn(NonePlayPanel.InGameButtonStatus.None);
        }
        else 
        {
            inGameBottomController.nonePlayPanel.SetActiveBtn(NonePlayPanel.InGameButtonStatus.Ally);
        }

        playerDataPanel.gameObject.SetActive(true);
        playerDataPanel.SetData(PlayerIcon);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void CreateMapInit()
    {
        InGameDataManager.Instance.SetMapSizeValue();
        join = InGameDataManager.Instance.GetJoinData();
        SetHexagonPanel();
        CreateHexagon();
        //CreateHexagonLine();
        //CreateMapDice();
        CreateMap();
    }

    void SetHexagonPanel()
    {
        //InGameDataManager.Instance.SetMapSize();

        hexagonPanel.anchoredPosition3D = InGameDataManager.Instance.GetHexagonPanelPos();
    }

    void CreateHexagon()
    {
        RectTransform rectTransform = hexagonObjPrefab.transform as RectTransform;
        rectTransform.sizeDelta = InGameDataManager.Instance.GetHexagonSizeDelta();

        for (int i = 0; i < InGameDataManager.Instance.GetCelMax(); i++)
        {
            Hexagon hexagon = Instantiate(hexagonObjPrefab, hexagonObjPrefab.transform.parent);

            hexagon.gameObject.SetActive(true);
            hexagon.SetPos(i);
            //hexagon.SetJoin(InGameDataManager.Instance.mapCreater.GetJoin()[i]);
            hexagon.clickOn = HexagonClickOn;

            hexagonList.Add(hexagon);
        }
    }

    //void CreateHexagonLine()
    //{
    //    RectTransform rectTransform = hexagonLinePrefab.transform as RectTransform;
    //    rectTransform.sizeDelta = InGameDataManager.Instance.GetHexagonSizeDelta();

    //    for (int i = 0; i < InGameDataManager.Instance.cel_max; i++)
    //    {
    //        HexagonLine hexagonLine = Instantiate(hexagonLinePrefab, hexagonLinePrefab.transform.parent);

    //        hexagonLine.gameObject.SetActive(true);
    //        hexagonLine.SetPos(i);

    //        hexagonLineList.Add(hexagonLine);
    //    }
    //}

    //void CreateMapDice()
    //{
    //    //Vector2 cellSize = Vector2.zero;
    //    //switch (InGameDataManager.Instance.mapSizeEnum)
    //    //{
    //    //    case MapSizeEnum.Small: cellSize = new Vector2(50, 50); break;
    //    //    case MapSizeEnum.Medium: cellSize = new Vector2(40, 40); break;
    //    //    case MapSizeEnum.Large: cellSize = new Vector2(30, 30); break;
    //    //}

    //    //GridLayoutGroup glg = mapDicePrefab.GetComponent<GridLayoutGroup>();
    //    //glg.cellSize = cellSize;

    //    Vector2 scaleValue = Vector2.zero;

    //    switch (InGameDataManager.Instance.mapSizeEnum)
    //    {
    //        case MapSizeEnum.Small: scaleValue = Vector2.one; break;
    //        case MapSizeEnum.Medium: scaleValue = Vector2.one * 0.7f; break;
    //        case MapSizeEnum.Large: scaleValue = Vector2.one * 0.6f ; break;
    //    }

    //    //GridLayoutGroup glg = mapDicePrefab.GetComponent<GridLayoutGroup>();
    //    mapDicePrefab.transform.localScale = scaleValue;


    //    for (int i = 0; i < InGameDataManager.Instance.areaDataList.Count; i++)
    //    {
    //        MapDice mapDice = Instantiate(mapDicePrefab, mapDicePrefab.transform.parent);

    //        mapDice.gameObject.SetActive(true);
    //        mapDice.SetDice(0);

    //        mapDiceList.Add(mapDice);
    //    }
    //}

    List<int> GetReseultDiceCount(int dice)
    {
        List<int> result = new List<int>();

        for (int i = 0; i < dice; i++)
        {
            int ranDice = Random.Range(1, 7);
            result.Add(ranDice);
        }

        return result;
    }


    public void CreateMap()
    {
        InGameDataManager.Instance.gameStart.SetValueAndForceNotify(false);

        // 셀 초기화
        for (int i = 0; i < hexagonList.Count; i++)
        {
            hexagonList[i].DrawLineOnClear();
        }

        //인접 데이터 설정
        foreach (var areaData in InGameDataManager.Instance.areaDataList)
        {
            areaData.SetAdj(join);
        }

        List<Vector2> readyData = new List<Vector2>();

        for (int i = 0; i < InGameDataManager.Instance.GetCelMax(); i++)
        {
            Hexagon hexagon = hexagonList[i];

            //SetArea(i);

            AreaData areaData = InGameDataManager.Instance.GetAreaDataForCel(i);

            if (areaData == null)
            {
                hexagon.gameObject.SetActive(false);
                continue;
            }

            hexagon.gameObject.SetActive(true);

            hexagon.SetArea(areaData.id);
            hexagon.SetPlayer(areaData.player);


            areaData.AddHexagon(hexagon);

            Vector2 posIndex = InGameDataManager.Instance.GetPos(i);

            if (posIndex.x == 0 || posIndex.y == 0 ||
                posIndex.x == InGameDataManager.Instance.GetMapSizeValue().x - 1 || posIndex.y == InGameDataManager.Instance.GetMapSizeValue().y - 1)
            {
                SetAroundLine(posIndex, i);
            }

            // 주변 셀
            for (int z = 0; z < 6; z++)
            {
                int pos = join[i].dir[z];
                if (pos < 0) continue;

                if (InGameDataManager.Instance.GetAreaDataForCel(i) != InGameDataManager.Instance.GetAreaDataForCel(pos) &&
                    readyData.Contains(new Vector2(i, pos)) == false &&
                    readyData.Contains(new Vector2(pos, i)) == false)
                {
                    hexagonList[i].DrawLineOn(z);
                    readyData.Add(new Vector2(i, pos));
                }
            }
        }

        SetDice();
    }

    void SetDice()
    {
        MapDiceInit();

        var areaDataList = InGameDataManager.Instance.areaDataList;

        //주사위 설정
        for (int i = 0; i < areaDataList.Count; i++)
        {
            AreaData areaData = areaDataList[i];

            mapDiceList[i].gameObject.SetActive(true);

            if (areaData.id > 0)
            {
                areaData.SetCenterHexagon(mapDiceList[i] , areaData.dice);
            }
        }

        playerIconController.SetPlayerIcon();

        InGameDataManager.Instance.BackUpOn();

        if (InGameDataManager.Instance.isMultiOn == false && InGameDataManager.Instance.mapSelectionOn)
        {
            inGameBottomController.SetStatus(0);
        }
        else
        {
            inGameBottomController.SetStatus(1);
        }
    }

    void MapDiceInit()
    {
        var areaDataList = InGameDataManager.Instance.areaDataList;

        foreach (var mapDice in mapDiceList)
        {
            mapDice.gameObject.SetActive(false);
        }

        if (mapDiceList.Count < areaDataList.Count)
        {
            for (int i = mapDiceList.Count; i < areaDataList.Count; i++)
            {
                MapDice mapDice = CreateMapDice();
                mapDiceList.Add(mapDice);
            }
        }
    }

    MapDice CreateMapDice()
    {
        Vector2 scaleValue = Vector2.zero;

        switch (InGameDataManager.Instance.mapSizeEnum)
        {
            case MapSizeEnum.Small: scaleValue = Vector2.one; break;
            case MapSizeEnum.Medium: scaleValue = Vector2.one * 0.7f; break;
            case MapSizeEnum.Large: scaleValue = Vector2.one * 0.6f; break;
        }

        //GridLayoutGroup glg = mapDicePrefab.GetComponent<GridLayoutGroup>();
        mapDicePrefab.transform.localScale = scaleValue;

        MapDice mapDice = Instantiate(mapDicePrefab, mapDicePrefab.transform.parent);

        mapDice.gameObject.SetActive(true);
        mapDice.SetDice(0);

        return mapDice;

        //for (int i = 0; i < InGameDataManager.Instance.areaDataList.Count; i++)
        //{
        //    MapDice mapDice = Instantiate(mapDicePrefab, mapDicePrefab.transform.parent);

        //    mapDice.gameObject.SetActive(true);
        //    mapDice.SetDice(0);

        //    mapDiceList.Add(mapDice);
        //}
    }

    private void SetArea(int index)
    {
        //Debug.LogWarning(index);
        //Debug.LogWarning(InGameDataManager.Instance.GetCelData(index));

        //Debug.LogWarning(InGameDataManager.Instance.GetAreaDataForCel(index));

        hexagonList[index].SetArea(InGameDataManager.Instance.GetCelData(index));

        if (InGameDataManager.Instance.GetAreaDataForCel(index) != null)
        {
            hexagonList[index].SetPlayer(InGameDataManager.Instance.GetAreaDataForCel(index).player);
        }

        //Debug.LogWarning(InGameDataManager.Instance.GetAreaDataForCel(index).player);

        
        
    }

    void SetAroundLine(Vector2 posIndex, int i)
    {
        if (posIndex.y == 0)
        {
            hexagonList[i].DrawLineOn(2);
            hexagonList[i].DrawLineOn(3);
        }

        if (posIndex.y == (int)InGameDataManager.Instance.GetMapSizeValue().y - 1)
        {
            hexagonList[i].DrawLineOn(0);
            hexagonList[i].DrawLineOn(1);
        }

        if (posIndex.x == 0)
        {
            List<int> checkList = new List<int>() { 1, 3, 5 };

            for (int z = 0; z < checkList.Count; z++)
            {
                int checkIndex = checkList[z];

                if (join[i].dir[checkIndex] <= 0)
                {
                    hexagonList[i].DrawLineOn(checkIndex);
                }
            }
        }

        if (posIndex.x == InGameDataManager.Instance.GetMapSizeValue().x - 1)
        {
            List<int> checkList = new List<int>() { 0, 2, 4 };

            for (int z = 0; z < checkList.Count; z++)
            {
                int checkIndex = checkList[z];

                if (join[i].dir[checkIndex] <= 0)
                {
                    hexagonList[i].DrawLineOn(checkIndex);
                }
            }
        }
    }

    public void HexagonClickOn(int index)
    {
        if (attackEventOn || InGameDataManager.Instance.gameStart.Value == false)
        {
            return;
        }

        AreaData areaData = InGameDataManager.Instance.GetAreaDataForCel(index);

        PlayerEnum currentPlayerEnum = InGameDataManager.Instance.playerData.playerEnum;

        bool isMyTurn = InGameDataManager.Instance.IsMyTurn();

        if (isMyTurn)
        {
            //선택이 안되어있다면
            if (choisIndex == -1)
            {
                if (areaData.choisOn == false && areaData.dice > 1 && areaData.player == InGameDataManager.Instance.playerData.playerEnum)
                {
                    SelectOn(areaData);
                }
            }
            //선택이 되어있다면
            else
            {
                AreaData beforeAreaData = InGameDataManager.Instance.GetAreaData(choisIndex);

                //재선택 했을때
                if (areaData.id == choisIndex)
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
                    if (InGameDataManager.Instance.IsAlliance(areaData.player))
                    {
                        return;
                    }

                    AttackOn(beforeAreaData, areaData);
                }
            }
        }
        else
        {
            //if (areaData.player == currentPlayerEnum)
            //{
            //    return;
            //}

            //선택이 안되어있다면
            if (choisIndex == -1)
            {
                SelectOn(areaData);
            }
            //선택이 되어있다면
            else
            {
                AreaData beforeAreaData = InGameDataManager.Instance.GetAreaData(choisIndex);

                //재선택 했을때
                if (areaData.id == choisIndex)
                {
                    DeSelectOn(areaData);
                }
                else 
                {
                    beforeAreaData.ChoisEventOn(false);
                    SelectOn(areaData);
                }
            }
        }
    }

    void AttackOn(AreaData myData, AreaData enemyData)
    {
        //inGameBottomController.SetStatus(2);

        attackEventOn = true;

        int myDice = myData.dice;
        int enemyDice = enemyData.dice;

        List<int> myDiceResult = GetReseultDiceCount(myDice);
        List<int> enemyDiceResult = GetReseultDiceCount(enemyDice);

        DiceWarData myDiceWarData = new DiceWarData(myData.player, myDiceResult);
        DiceWarData enemyDiceWarData = new DiceWarData(enemyData.player, enemyDiceResult);

        //yield return inGameBottomController.diceWarUIController.AttackOn(myDiceWarData, enemyDiceWarData);

        //점령에 성공하였다면
        if (myDiceWarData.diceSum > enemyDiceWarData.diceSum)
        {
            int resultDice = myData.dice - 1;
            enemyData.SetDice(resultDice);
            enemyData.PlayerChangeOn(myData.player);

            playerIconController.SetBundleKeyuAll();
        }

        myData.SetDice(1);

        DeSelectOn(myData);

        attackEventOn = false;

        if (InGameDataManager.Instance.isMultiOn)
        {
            AttackRequest request = new AttackRequest()
            {
                fromAreaData = myData,
                toAreaData = enemyData,
            };

            ServerManager.Instance.AttackRequestOn(request);
        }

        //yield return null;
    }

    void SelectOn(AreaData areaData)
    {
        choisIndex = areaData.id;
        areaData.ChoisEventOn(true);

        if (InGameDataManager.Instance.isMultiOn && InGameDataManager.Instance.IsMyTurn() == false)
        {
            AreaTradeCheck(areaData);
            //selectAreaOn(areaData);
        }
    }

    public void AreaTradeCheck(AreaData areaData)
    {
        PlayerEnum currentPlayerEnum = InGameDataManager.Instance.playerData.playerEnum;

        NonePlayPanel.InGameButtonStatus btnStatus = areaData.player == currentPlayerEnum ?
                                                        NonePlayPanel.InGameButtonStatus.Sell : //내 땅을 선택했을때
                                                        NonePlayPanel.InGameButtonStatus.Buy;   //다른 사람 땅을 선택했을때

        inGameBottomController.nonePlayPanel.SetAreaData(areaData , btnStatus);
        //if (areaData.player == currentPlayerEnum)
        //{
        //    inGameBottomController.nonePlayPanel.SetActiveBtn(NonePlayPanel.InGameButtonStatus.Sell);
        //    Debug.LogWarning("내 땅");
        //}
        //else
        //{
            
        //    Debug.LogWarning("남의 땅");
        //}
    }

    public void DeSelectOn(AreaData areaData)
    {
        choisIndex = -1;
        areaData.ChoisEventOn(false);

        if (InGameDataManager.Instance.isMultiOn && InGameDataManager.Instance.IsMyTurn() == false)
        {
            inGameBottomController.nonePlayPanel.SetNoneBtn();
            //selectAreaOn(areaData);
        }
    }

    public List<AreaData> EndTurnBtnClickOn()
    {
        if (diceAddEventOn || InGameDataManager.Instance.gameStart.Value == false)
        {
            return null;
        }

        if (choisIndex != -1)
        {
            AreaData beforeAreaData = InGameDataManager.Instance.GetAreaData(choisIndex);
            DeSelectOn(beforeAreaData);
        }

        PlayerEnum currentPlayerEnum = InGameDataManager.Instance.playerData.playerEnum;

        //StartCoroutine();

        List<AreaData> areaDataList = DiceAddOn(currentPlayerEnum);

        turnOffOn();

        return areaDataList;
    }

    public List<AreaData> DiceAddOn(PlayerEnum playerEnum)
    {
        diceAddEventOn = true;

        PlayerIcon playerIcon = playerIconController.GetPlayerIcon(playerEnum);

        if (playerIcon == null)
        {
            return null;
            //yield break;
        }

        //최대 15개 까지만 회복 가능하도록
        int connectedCount = Mathf.Min(playerIcon.connectedCount, 15);

        List<AreaData> areaDataList = InGameDataManager.Instance.areaDataList.Where(data => data.player == playerEnum).ToList();

        while (true)
        {
            if (connectedCount <= 0)
            {
                break;
            }

            for (int i = 0; i < areaDataList.Count; i++)
            {
                if (connectedCount <= 0)
                {
                    break;
                }

                int addOn = Random.Range(0, 2);

                if (addOn == 1 && areaDataList[i].dice < InGameDataManager.Instance.diceMaxCount)
                {
                    connectedCount--;
                    areaDataList[i].DiceAddOn();

                    //yield return new WaitForSeconds(0.1f);
                }
            }

            bool allMax = areaDataList.All(data => data.dice == InGameDataManager.Instance.diceMaxCount);

            if (allMax)
            {
                break;
            }
        }

        diceAddEventOn = false;

        return areaDataList;

        //turnOffOn();

        //yield return null;
    }

    public void AIAttackOn(PlayerEnum playerEnum)
    {
        List<AreaData> areaList = InGameDataManager.Instance.areaDataList.Where(data => data.player == playerEnum).ToList();

        int diceMaxCount = InGameDataManager.Instance.diceMaxCount;
        List<AreaData> attackAreaList = areaList.Where(data => data.dice > diceMaxCount-2).ToList();

        bool attackRandomOn = Random.Range(0, 2) == 0;

        if (attackAreaList.Count > 0 && attackRandomOn)
        {
            bool attackOn = false;
            
            foreach (var attackArea in attackAreaList)
            {
                if (attackOn) 
                {
                    break;
                }

                foreach (int targetArea in attackArea.GetAdj())
                {
                    if (targetArea == -1)
                    {
                        continue;
                    }

                    AreaData checkAreaData = InGameDataManager.Instance.GetAreaData(targetArea);
                    if (checkAreaData.player != playerEnum &&
                        checkAreaData.dice > 0 &&
                        attackArea.dice >= checkAreaData.dice &&
                        InGameDataManager.Instance.IsAllAlliance(new List<PlayerEnum>() { attackArea.player, checkAreaData.player }) == false)
                    {
                        AttackOn(attackArea, checkAreaData);

                        attackOn = true;

                        break;
                    }
                }
            }
            
        }

        if (InGameDataManager.Instance.gameStart.Value == true)
        {
            
        }
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
        InGameDataManager.Instance.InitMapData();
        InGameDataManager.Instance.CreateMap();

        CreateMap();
    }

    public void ReStartOn()
    {
        InGameDataManager.Instance.ReStartOn();
        playerIconController.SetPlayerIcon();
        //inGameBottomController.diceWarUIController.DiceClear();
        inGameBottomController.SetStatus(1);
    }
}
