using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UniRx;
using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

public class InGameController : MonoBehaviour
{
    [SerializeField] MapController mapController;
    [SerializeField] Button endTurnBtn;
    [SerializeField] Timer timer;

    private AllianceRequest allianceRequestData;

    private List<AllianceApproveRequest> allianceApproveRequestList = new List<AllianceApproveRequest>();

    private bool endTurnBtnClickOn = false;

    private bool gameEndOn = false;

    private void Awake()
    {
        SetEvent();
    }

    void SetEvent()
    {
        mapController.gameWinOn = GameWinOn;
        mapController.gameLoseOn = GameLoseOn;
        mapController.turnOffOn = TurnOffOn;
        mapController.selectAreaOn = SelectAreaOn;
        mapController.SetEvent();

        DataManager.Instance.gameStart
            .Subscribe(_ => 
            {
                SetEndTurnBtn();

                if (DataManager.Instance.gameStart.Value == false)
                {
                    timer.SetTimerOn(false);
                }
                else
                {
                    gameEndOn = false;
                }
            })
            .AddTo(gameObject);

        timer.timerOverOn = () =>
        {
            EndTurnBtnClickOn();
            PopupManager.Instance.TimeOverPopupOn();
            //popupController.GetTimeOverPopup().ActiveOn();
        };
    }



    private void Start()
    {
        if (DataManager.Instance.isMultiOn)
        {
            ServerManager.Instance.receiveDataOn += ReceiveDataOn;

            DataManager.Instance.playerData.skillCardCount = 1;
        }

		mapController.CreateMapInit();

        if (DataManager.Instance.mapSelectionOn == false || DataManager.Instance.isMultiOn)
        {
            GameStartOn();
        }

        PopupManager.Instance.SetCanvasParant(transform);

        if (DataManager.Instance.isMultiOn)
        {
            TurnCheck();
        }
    }

    private void OnDestroy()
    {
        if (DataManager.Instance.isMultiOn)
        {
            ServerManager.Instance.receiveDataOn -= ReceiveDataOn;
        }
    }

    private async UniTask TurnRequestOn(TurnRequest turnRequest)
    {
        PlayerEnum nextPlayer = GetNextPlayer();

        turnRequest.playerEnum = nextPlayer;
        turnRequest.turnStartOn = true;

        await Task.Delay(500);

        ServerManager.Instance.SendMessageOn(turnRequest);
    }

    private void ReceiveDataOn(BaseTCPRequest baseRequest)
    {
        //Debug.LogWarning(baseRequest.requestProtocal);

        PlayerEnum myPlayer = DataManager.Instance.playerData.playerEnum;

        switch (baseRequest.requestProtocal)
        {
            case RequestProtocal.TurnRequest:

                TurnRequest turnRequest = (TurnRequest)baseRequest;

                if (turnRequest.turnStartOn == false)
                {
                    foreach (var areaData in turnRequest.areaDataList)
                    {
                        DataManager.Instance.GetAreaData(areaData.id).SetDice(areaData.dice);
                    }

                    if (DataManager.Instance.isOwner)
                    {
                        TurnRequestOn(turnRequest).Forget();
                    }
                }
                else 
                {
                    DataManager.Instance.currentTurnIndex = (int)turnRequest.playerEnum;

                    mapController.inGameBottomController.nonePlayPanel.SetNoneBtn();

                    PlayerEnum currentPlayer = (PlayerEnum)DataManager.Instance.currentTurnIndex;

                    PlayerIcon playerIcon = mapController.playerIconController.GetPlayerIcon(currentPlayer);

                    if (playerIcon == null)
                    {
                        TurnRequestOn(turnRequest).Forget();
                    }
                    else
                    {
                        TurnCheck();
                    }
                }

                break;

            case RequestProtocal.AttackRequest:

                AttackRequest attackRequest = (AttackRequest)baseRequest;

                //내가 공격한 정보가 아니라면
                if (attackRequest.fromAreaData.player != DataManager.Instance.playerData.playerEnum )  
                {
                    PlayerData player = DataManager.Instance.GetPlayerData(attackRequest.fromAreaData.player);

                    if (player.isAI == false ||
                        player.isAI && DataManager.Instance.isOwner == false)
                    {
                        mapController.AttackReceiveDataOn(attackRequest).Forget();
                    }

                    mapController.playerIconController.SetBundleKeyuAll();
                }
                
                break;

            case RequestProtocal.LandTradeRequest:

                LandTradeRequest landTradeRequest = (LandTradeRequest)baseRequest;

                if (landTradeRequest.toPlayerEnum == myPlayer)
                {
                    PopupManager.Instance.LandTradeApprovePopupOn((LandTradeRequest)baseRequest);
                }

                break;

            case RequestProtocal.LandTradeApproveRequest:

                LandTradeApproveRequest landTradeApproveRequest = (LandTradeApproveRequest)baseRequest;

                bool approveOn = landTradeApproveRequest.approveOn;

                if (approveOn)
                {
                    LandTradeRequest tradeData = landTradeApproveRequest.landTradeRequest;

                    AreaData currentAreaData = DataManager.Instance.GetAreaData(tradeData.areaData.id);
                    PlayerEnum changePlayerEnum = tradeData.buyOn ? tradeData.fromPlayerEnum : tradeData.toPlayerEnum;
                    currentAreaData.PlayerChangeOn(changePlayerEnum);
                    mapController.playerIconController.SetBundleKeyuAll();

                    // 내가 거래 대상이라면
                    if (tradeData.fromPlayerEnum == myPlayer || tradeData.toPlayerEnum == myPlayer)
                    {
                        if (tradeData.fromPlayerEnum == myPlayer)
                        {
                            mapController.inGameBottomController.nonePlayPanel.SkillUseOn();
                        }

                        int coinCount = tradeData.coinCount;

                        //내가 땅을 샀거나, 상대방이 나에게 땅을 팔았다면
                        if (tradeData.fromPlayerEnum == myPlayer && tradeData.buyOn ||
                            tradeData.toPlayerEnum == myPlayer && tradeData.buyOn == false )
                        {
                            coinCount *= -1;
                        }

                        DataManager.Instance.AddCoin(coinCount);
                    }
                }

                break;

            case RequestProtocal.AllianceRequest:

                AllianceRequest allianceRequest = (AllianceRequest)baseRequest;

                //본인이 오더 플레이어 라면
                if (allianceRequest.orderData.playerEnum == myPlayer)
                {
                    allianceRequestData = allianceRequest;
                }
                else if (allianceRequest.allianceDataList.Any(data => data.playerEnum == myPlayer))
                {
                    PopupManager.Instance.AllianceApprovePopupOn(allianceRequest);
                }

                break;

            case RequestProtocal.AllianceApproveRequest:

                AllianceApproveRequest allianceApproveRequest = (AllianceApproveRequest)baseRequest;

                //본인이 오더 플레이어 라면
                if (allianceApproveRequest.orderData.playerEnum == myPlayer) 
                {
                    if (allianceRequestData == null)
                        return;
                    
                    if (allianceApproveRequest.approveOn == false)
                    {
                        allianceRequestData = null;
                        //실패 메시지 표시
                        break;
                    }

                    allianceApproveRequestList.Add(allianceApproveRequest);

                    if (allianceApproveRequestList.Count == allianceRequestData.allianceDataList.Count-1 &&  allianceApproveRequestList.All(data => data.approveOn))
                    {
                        AllianceResultRequest request = new AllianceResultRequest()
                        {
                            orderData = allianceApproveRequest.orderData,
                            allianceDataList = allianceRequestData.allianceDataList,
                        };

                        ServerManager.Instance.SendMessageOn(request);
                    }
                }

                break;

            case RequestProtocal.AllianceResultRequest:

                AllianceResultRequest allianceResultRequest = (AllianceResultRequest)baseRequest;

                mapController.inGameBottomController.nonePlayPanel.AllianceClearOn(allianceResultRequest);

                mapController.playerIconController.SetAlliancePlayerIcon(allianceResultRequest);

                mapController.playerIconController.SetBundleKeyuAll();

                break;

            case RequestProtocal.AllianceBetrayRequest:

                AllianceBetrayRequest allianceBetrayRequest = (AllianceBetrayRequest)baseRequest;

                var nonePlayPanel = mapController.inGameBottomController.nonePlayPanel;

                //내가 배신한것이라면
                if (allianceBetrayRequest.playerEnum == myPlayer)
                {
                    AllianceResultRequest resultData = new AllianceResultRequest() 
                    {
                        orderData = new AllianceData() 
                        {
                            playerEnum = myPlayer
                        }
                    };

                    //남은 동맹이 1명뿐이라면
                    List<AllianceData> originAllianceDataList = DataManager.Instance.GetAllAlliance(myPlayer);

                    if (originAllianceDataList.Count == 2)
                    {
                        mapController.playerIconController.SetBetrayPlayerIcon(myPlayer);

                        foreach (var originAllianceData in originAllianceDataList)
                        {
                            if (originAllianceData.playerEnum == myPlayer)
                                continue;
                            mapController.playerIconController.SetBetrayPlayerIcon(originAllianceData.playerEnum);
                        }   
                    }

                    nonePlayPanel.AllianceClearOn(resultData);
                }
                else 
                {
                    //나와 동맹중인 플레이어가 배신했다면
                    if (DataManager.Instance.IsAllAlliance(new List<PlayerEnum>() { myPlayer, allianceBetrayRequest.playerEnum }))
                    {
                        DataManager.Instance.BetrayOn(allianceBetrayRequest.playerEnum);
                    }

                    //남은 동맹이 1명뿐이라면
                    if (DataManager.Instance.GetAllAlliance(myPlayer).Count <= 1)
                    {
                        DataManager.Instance.AllianceClearOn();
                        mapController.playerIconController.SetBetrayPlayerIcon(myPlayer);
                        nonePlayPanel.SetNoneBtn();
                    }
                }

                mapController.playerIconController.SetBetrayPlayerIcon(allianceBetrayRequest.playerEnum);

                break;

            case RequestProtocal.GameOutOn:

                GameOutRequest outData = (GameOutRequest)baseRequest;

                //bool ownerChangeOn = DataManager.Instance.isOwner == false && outData.isOwner;

                DataManager.Instance.isOwner = outData.isOwner;

                for (int i = 0; i < DataManager.Instance.playerDataList.Count; i++)
                {
                    PlayerData playerData = DataManager.Instance.playerDataList[i];

                    if (playerData.playerEnum == outData.outPlayerEnum)
                    {
                        playerData.isAI = true;
                        break;
                    }
                }

                int currentIndex = DataManager.Instance.currentTurnIndex;

                //내가 오너 플레이어가 되었다면
                if (DataManager.Instance.isOwner)
                {
                    PlayerData playerData = DataManager.Instance.playerDataList[currentIndex];

                    if (playerData.isAI)
                    {
                        TurnCheck();
                    }
                }

                break;

            case RequestProtocal.SkillCardRequest:

                SkillCardRequest skillCardRequest = (SkillCardRequest)baseRequest;

                if (skillCardRequest.playerEnum != myPlayer)
                {
                    DataManager.Instance.SetPlayerSkillData(skillCardRequest.playerEnum, skillCardRequest.skillCardCount);
                }

                break;
                
        }
    }

    void GameWinOn()
    {
        GameEndOn(true);
    }

    void GameLoseOn()
    {
        GameEndOn(false);
    }

    void GameEndOn(bool win)
    {
        if (gameEndOn)
            return;

        gameEndOn = true;

        DataManager.Instance.gameStart.SetValueAndForceNotify(false);

        PopupManager.Instance.GameResultPopupOn(this,win , GetCoin(win));
    }

    int GetCoin(bool win)
    {
        int coinCount = 0;

        if (win)
        {
            int addCoin = 0;

            if (DataManager.Instance.isMultiOn)
            {
                var resultList = mapController.playerIconController.GetActiveList();

                if (resultList.Count == 1)
                {
                    addCoin = 3;
                    coinCount = addCoin * DataManager.Instance.num_player;
                }
                else
                {
                    var allianceData = DataManager.Instance.GetMyAllianceData();

                    if (allianceData != null)
                    {
                        coinCount = allianceData.coinCount;
                    }
                }
            }
            else
            {
                switch (DataManager.Instance.aiLevel)
                {
                    case AILevel.Easy: addCoin = 1; break;
                    case AILevel.Normal: addCoin = 2; break;
                    case AILevel.Hard: addCoin = 3; break;
                }

                coinCount = addCoin * DataManager.Instance.num_player;
            }
        }

        return coinCount;
    }

    public void GameStartOn()
    {
        gameEndOn = false;

        DataManager.Instance.gameStart.SetValueAndForceNotify(true);

        mapController.inGameBottomController.SetStatus(1);
    }

    public void GoMainOn()
    {
		DataManager.Instance.GameDataClearOn();

		if (DataManager.Instance.isMultiOn)
        {
            ServerManager.Instance.GameOutRequestOn();
        }

        SceneManager.LoadScene("Main");
    }

    public void TurnOffOn()
    {
        DataManager.Instance.TurnOffOn();
        SetEndTurnBtn();
        mapController.playerIconController.SetIconTurnEffect();
    }

    public void SelectAreaOn(AreaData areaData)
    {
        //PlayerEnum currentPlayerEnum = InGameDataManager.Instance.playerData.playerEnum;

        ////내 땅을 선택했을때
        //if (areaData.player == currentPlayerEnum) 
        //{
        //    Debug.LogWarning("내 땅");
        //}
        //else 
        //{
        //    Debug.LogWarning("남의 땅");
        //}
    }

    private void Update()
    {
        //if (DataManager.Instance.isMultiOn == false)
        //{
            TurnCheckDelayCheck();
        //}   

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            //PopupManager.Instance.LandTradeApprovePopupOn();

            //yourTurnPopup.MyTurnActiveOn();
        }

        //Debug.LogWarning("enum = "+DataManager.Instance.playerData.playerEnum);
    }

    void TurnCheckDelayCheck() 
    {
		if (DataManager.Instance.gameStart.Value == false || DataManager.Instance.playOn) 
        {
            return;
        }

        TurnCheck();
    }


    public async void TurnCheck()
    {
        DataManager.Instance.playOn = true;

        bool myTurn = DataManager.Instance.IsMyTurn();

        if (myTurn)
        {
            PopupManager.Instance.YourTurnPopupOn();

            if (DataManager.Instance.isMultiOn)
            {
                int myTurnCount = DataManager.Instance.MyTurnAddOn();

                if (myTurnCount > 5)
                {
                    DataManager.Instance.myTurnCount = 0;
                    mapController.inGameBottomController.nonePlayPanel.SkillUseOn(1);
                }
            }

            mapController.SelectClearOn();

            if (DataManager.Instance.isMultiOn)
            {
				timer.SetTimerOn(true);
			}
		}
        else
        {
            if (DataManager.Instance.isMultiOn == false || DataManager.Instance.IsAITurn() && DataManager.Instance.isOwner)
            {
                SetEndTurnBtn();
                mapController.playerIconController.SetIconTurnEffect();


                PlayerEnum currentPlayer = (PlayerEnum)DataManager.Instance.currentTurnIndex;
                PlayerIcon playerIcon = mapController.playerIconController.GetPlayerIcon(currentPlayer);

                if (playerIcon != null)
                {
                    await AIPlayOn(currentPlayer);
                }
                else 
                {
                    if (DataManager.Instance.isMultiOn)
                    {
                        TurnEndRequestOn(currentPlayer, new List<AreaData>());
                    }
                    else
                    {
                        DataManager.Instance.currentTurnIndex = (int)GetNextPlayer();

                        TurnCheck();
                    }
                }
                
            }
		}
        SetEndTurnBtn();

        mapController.playerIconController.SetIconTurnEffect();
    }

    PlayerEnum GetNextPlayer()
    {
        PlayerEnum currentPlayer = (PlayerEnum)DataManager.Instance.currentTurnIndex;

        PlayerEnum nextPlayer = currentPlayer;

        while (true)
        {
            nextPlayer = (PlayerEnum)(((int)nextPlayer) + 1);

            if ((int)nextPlayer == DataManager.Instance.num_player)
            {
                nextPlayer = PlayerEnum.Player_0;
            }

            PlayerIcon playerIcon = mapController.playerIconController.GetPlayerIcon(nextPlayer);

            if (playerIcon != null)
            {
                break;
            }
        }

        return nextPlayer;
    }

    private async UniTask AIPlayOn(PlayerEnum currentPlayer)
    {
        await mapController.AIAttackOn(currentPlayer);

        List<AreaData> areaDataList = mapController.DiceAddOn(currentPlayer);

        if (DataManager.Instance.isMultiOn)
        {
            TurnEndRequestOn(currentPlayer, areaDataList);
        }
        else 
        {
			await Task.Delay(100);
			TurnOffOn();
        }
    }

    private void TurnEndRequestOn(PlayerEnum currentPlayer , List<AreaData> areaDataList)
    {
        List<SendAreaData> sendAreaData = new List<SendAreaData>();

        foreach (var areaData in areaDataList)
        {
            sendAreaData.Add(areaData.GetSendAreaData());
        }

        TurnRequest turnRequest = new TurnRequest()
        {
            playerEnum = currentPlayer,
            turnStartOn = false,
            areaDataList = sendAreaData,
        };

        ServerManager.Instance.SendMessageOn(turnRequest);
    }

    void SetEndTurnBtn()
    {
        bool gameStart = DataManager.Instance.gameStart.Value;

        endTurnBtn.interactable = gameStart && DataManager.Instance.IsMyTurn();

        //Debug.LogWarning(DataManager.Instance.IsMyTurn());

    }

    public void EndTurnBtnClickOn()
    {
        EndTurnEventOn().Forget();
    }

    async UniTask EndTurnEventOn()
    {
        if (endTurnBtnClickOn)
        {
            return;
        }

        endTurnBtnClickOn = true;

        timer.SetTimerOn(false);

        List<AreaData> areaDataList = mapController.EndTurnBtnClickOn();

        if (DataManager.Instance.isMultiOn)
        {
            TurnEndRequestOn(DataManager.Instance.playerData.playerEnum, areaDataList);
            endTurnBtn.interactable = false;
		}
        else
        {
            SetEndTurnBtn();
        }

        endTurnBtnClickOn = false;
    }

    public void NewGameOn()
    {
        DataManager.Instance.gameStart.SetValueAndForceNotify(false);

        mapController.NewGameOn();

        DataManager.Instance.playOn = false;

        if (DataManager.Instance.mapSelectionOn == false)
        {
            GameStartOn();
        }
    }
    public void ReStartOn()
    {
        mapController.ReStartOn();

        DataManager.Instance.playOn = false;
    }

    public void OptionPopupOnOn()
    {
        PopupManager.Instance.OptionPopupOn(this);
    }
}