using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        if (DataManager.Instance.isMultiOn == false)
        {
            return;
        }

        //내 아이콘을 클릭했다면
        if (PlayerIcon.playerEnum == DataManager.Instance.playerData.playerEnum)
        {
            inGameBottomController.nonePlayPanel.SetNoneBtn();
        }
        else 
        {
            //내가 동맹중인 상태라면
            if (DataManager.Instance.IsAlliance(DataManager.Instance.playerData.playerEnum))
            {
                inGameBottomController.nonePlayPanel.SetActiveBtn(NonePlayPanel.InGameButtonStatus.Betray);
            }
            else
            {
                inGameBottomController.nonePlayPanel.SetActiveBtn(NonePlayPanel.InGameButtonStatus.Ally);
            }
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
        //맵 초기화가 안되어있다면
        if (DataManager.Instance.areaDataList.Count == 0)
        {
            DataManager.Instance.InitMapData();
            DataManager.Instance.CreateMap();
        }

        join = DataManager.Instance.GetJoinData();
        SetHexagonPanel();
        CreateHexagon();
        CreateMap();
    }

    void SetHexagonPanel()
    {
        hexagonPanel.anchoredPosition3D = DataManager.Instance.GetHexagonPanelPos();
    }

    void CreateHexagon()
    {
        RectTransform rectTransform = hexagonObjPrefab.transform as RectTransform;
        rectTransform.sizeDelta = DataManager.Instance.GetHexagonSizeDelta();

        for (int i = 0; i < DataManager.Instance.GetCelMax(); i++)
        {
            Hexagon hexagon = Instantiate(hexagonObjPrefab, hexagonObjPrefab.transform.parent);

            hexagon.gameObject.SetActive(true);
            hexagon.SetPos(i);
            hexagon.clickOn = HexagonClickOn;

            hexagonList.Add(hexagon);
        }
    }

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
        DataManager.Instance.gameStart.SetValueAndForceNotify(false);

        // 셀 초기화
        for (int i = 0; i < hexagonList.Count; i++)
        {
            hexagonList[i].DrawLineOnClear();
        }

        List<Vector2> readyData = new List<Vector2>();

        for (int i = 0; i < DataManager.Instance.GetCelMax(); i++)
        {
            Hexagon hexagon = hexagonList[i];

            //SetArea(i);

            AreaData areaData = DataManager.Instance.GetAreaDataForCel(i);

            if (areaData == null)
            {
                hexagon.gameObject.SetActive(false);
                continue;
            }

            hexagon.gameObject.SetActive(true);

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

        var areaDataList = DataManager.Instance.areaDataList;

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

        switch (DataManager.Instance.mapSizeEnum)
        {
            case MapSizeEnum.Small: scaleValue = Vector2.one; break;
            case MapSizeEnum.Medium: scaleValue = Vector2.one * 0.7f; break;
            case MapSizeEnum.Large: scaleValue = Vector2.one * 0.6f; break;
        }

        mapDicePrefab.transform.localScale = scaleValue;

        MapDice mapDice = Instantiate(mapDicePrefab, mapDicePrefab.transform.parent);

        mapDice.gameObject.SetActive(true);
        mapDice.SetDice(0);

        return mapDice;
    }

    void SetAroundLine(Vector2 posIndex, int i)
    {
        if (posIndex.y == 0)
        {
            hexagonList[i].DrawLineOn(2);
            hexagonList[i].DrawLineOn(3);
        }

        if (posIndex.y == (int)DataManager.Instance.GetMapSizeValue().y - 1)
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

        if (posIndex.x == DataManager.Instance.GetMapSizeValue().x - 1)
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

    public async void HexagonClickOn(int index)
    {
        if (attackEventOn || DataManager.Instance.gameStart.Value == false)
        {
            return;
        }

        AreaData areaData = DataManager.Instance.GetAreaDataForCel(index);

        PlayerEnum currentPlayerEnum = DataManager.Instance.playerData.playerEnum;

        bool isMyTurn = DataManager.Instance.IsMyTurn();

        if (isMyTurn)
        {
            //선택이 안되어있다면
            if (choisIndex == -1)
            {
                if (areaData.choisOn == false && areaData.dice > 1 && areaData.player == DataManager.Instance.playerData.playerEnum)
                {
                    SelectOn(areaData);
                }
            }
            //선택이 되어있다면
            else
            {
                AreaData beforeAreaData = DataManager.Instance.GetAreaData(choisIndex);

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
                    if (DataManager.Instance.IsMyAllAlliance(areaData.player))
                    {
                        return;
                    }

                    await AttackOn(beforeAreaData, areaData);
                }
            }
        }
        else
        {
            //싱글 플레이 게임이이라면
            if (DataManager.Instance.isMultiOn == false)
            {
                return;
            }

            //선택이 안되어있다면
            if (choisIndex == -1)
            {
                SelectOn(areaData);
            }
            //선택이 되어있다면
            else
            {
                AreaData beforeAreaData = DataManager.Instance.GetAreaData(choisIndex);

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

    async UniTask AttackOn(AreaData myData, AreaData enemyData )
    {
        attackEventOn = true;

        int myDice = myData.dice;
        int enemyDice = enemyData.dice;

        List<int> myDiceResult = GetReseultDiceCount(myDice);
        List<int> enemyDiceResult = GetReseultDiceCount(enemyDice);

        DiceWarData myDiceWarData = new DiceWarData(myData.player, myDiceResult);
        DiceWarData enemyDiceWarData = new DiceWarData(enemyData.player, enemyDiceResult);

        myData.ChoisEventOn(true);
        enemyData.ChoisEventOn(true);

        await inGameBottomController.nonePlayPanel.AttackOn(myDiceWarData, enemyDiceWarData);

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
        enemyData.ChoisEventOn(false);

        if (DataManager.Instance.isMultiOn)
        {
            AttackRequestOn(myData, enemyData , myDiceWarData , enemyDiceWarData);

            await Task.Delay(500);
        }

        attackEventOn = false;
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
        AreaData fromAreaData = DataManager.Instance.GetAreaData(attackRequest.fromAreaData.id);
        AreaData toAreaData = DataManager.Instance.GetAreaData(attackRequest.toAreaData.id);

        fromAreaData.ChoisEventOn(true);
        await Task.Delay(100);
        toAreaData.ChoisEventOn(true);
        await Task.Delay(100);

        await inGameBottomController.nonePlayPanel.AttackOn(attackRequest.fromDiceWarData, attackRequest.toDiceWarData);

        fromAreaData.ChoisEventOn(false);
        toAreaData.ChoisEventOn(false);

        DataManager.Instance.SetAreaData(attackRequest.fromAreaData);
        DataManager.Instance.SetAreaData(attackRequest.toAreaData);

        playerIconController.SetBundleKeyuAll();
    }

    void SelectOn(AreaData areaData)
    {
        choisIndex = areaData.id;
        areaData.ChoisEventOn(true);

        if (DataManager.Instance.isMultiOn && DataManager.Instance.IsMyTurn() == false)
        {
            AreaTradeCheck(areaData);
        }
    }

    public void AreaTradeCheck(AreaData areaData)
    {
        PlayerEnum currentPlayerEnum = DataManager.Instance.playerData.playerEnum;

        NonePlayPanel.InGameButtonStatus btnStatus = areaData.player == currentPlayerEnum ?
                                                        NonePlayPanel.InGameButtonStatus.Sell : //내 땅을 선택했을때
                                                        NonePlayPanel.InGameButtonStatus.Buy;   //다른 사람 땅을 선택했을때

        inGameBottomController.nonePlayPanel.SetAreaData(areaData , btnStatus);
    }

    public void DeSelectOn(AreaData areaData)
    {
        choisIndex = -1;
        areaData.ChoisEventOn(false);

        if (DataManager.Instance.isMultiOn && DataManager.Instance.IsMyTurn() == false)
        {
            inGameBottomController.nonePlayPanel.SetNoneBtn();
        }
    }

    public List<AreaData> EndTurnBtnClickOn()
    {
        if (diceAddEventOn || DataManager.Instance.gameStart.Value == false)
        {
            return null;
        }

        if (choisIndex != -1)
        {
            AreaData beforeAreaData = DataManager.Instance.GetAreaData(choisIndex);
            DeSelectOn(beforeAreaData);
        }

        PlayerEnum currentPlayerEnum = DataManager.Instance.playerData.playerEnum;

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
            diceAddEventOn = false;

            return null;
        }

        bool isMyTurn = DataManager.Instance.IsMyTurn();

        //최대 15개 까지만 회복 가능하도록
        //int connectedCount = Mathf.Min(playerIcon.connectedCount, 15);
        int connectedCount = playerIcon.connectedCount;

        List<AreaData> areaDataList = DataManager.Instance.areaDataList.Where(data => data.player == playerEnum).ToList();

        while (true)
        {
            bool allMax = false;

            foreach (AreaData areaData in areaDataList)
            {
                allMax = areaDataList.All(data => data.dice == DataManager.Instance.diceMaxCount);

                if (allMax || connectedCount == 0)
                {
                    break;
                }

                //주사위가 Max가 아니라면
                if (areaData.dice < DataManager.Instance.diceMaxCount)
                {
                    //주사위 추가
                    areaData.DiceAddOn();

                    if (isMyTurn && DataManager.Instance.stashCount > 0)
                    {    
                        DataManager.Instance.AddStashCount(-1);
                    }
                    else if (connectedCount > 0)
                    {
                        connectedCount--;
                    }
                }
            }

            if (allMax || connectedCount <= 0)
            {
                break;
            }
        }

        if (isMyTurn)
        {
            inGameBottomController.nonePlayPanel.SetStashText(connectedCount);
        }

        diceAddEventOn = false;

        return areaDataList;
    }

    public async UniTask AIAttackOn(PlayerEnum playerEnum)
    {
        List<AreaData> areaList = DataManager.Instance.areaDataList.Where(data => data.player == playerEnum).ToList();

        int diceMaxCount = DataManager.Instance.diceMaxCount;
        List<AreaData> attackAreaList = areaList.Where(data => data.dice > diceMaxCount-2).ToList();

        await Task.Delay(1000);

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

                    AreaData checkAreaData = DataManager.Instance.GetAreaData(targetArea);
                    if (checkAreaData.player != playerEnum &&
                        checkAreaData.dice > 0 &&
                        attackArea.dice >= checkAreaData.dice &&
                        DataManager.Instance.IsAllAlliance(new List<PlayerEnum>() { attackArea.player, checkAreaData.player }) == false)
                    {
                        await AttackOn(attackArea, checkAreaData);

                        attackOn = true;

                        break;
                    }
                }
            }
            
        }

        if (DataManager.Instance.gameStart.Value == true)
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
        DataManager.Instance.InitMapData();
        DataManager.Instance.CreateMap();

        CreateMap();
    }

    public void ReStartOn()
    {
        DataManager.Instance.ReStartOn();
        playerIconController.SetPlayerIcon();
        inGameBottomController.SetStatus(1);
    }
}
