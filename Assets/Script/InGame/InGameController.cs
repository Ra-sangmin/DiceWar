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
    [SerializeField] PopupController popupController;
    [SerializeField] Button endTurnBtn;
    [SerializeField] Timer timer;
    //[SerializeField] Slider timer;
    //private int currentTurnIndex = 0;
    //private float turnDelay = 0;
    //private bool playOn = false;

    private AllianceRequest allianceRequestData;

    private List<AllianceApproveRequest> allianceApproveRequestList = new List<AllianceApproveRequest>();

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

    private void ReceiveDataOn(BaseRequest baseRequest)
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

                    if (myPlayer == PlayerEnum.Player_0)
                    {
                        PlayerEnum nextPlayer = (PlayerEnum)(((int)turnRequest.playerEnum)+1);

                        Debug.LogWarning(DataManager.Instance.num_player);
                        Debug.LogWarning(nextPlayer);

                        if ((int)nextPlayer == DataManager.Instance.num_player)
                        {
                            nextPlayer = PlayerEnum.Player_0;
                        }

                        turnRequest.playerEnum = nextPlayer;
                        turnRequest.turnStartOn = true;
                        ServerManager.Instance.TurnRequestOn(turnRequest);
                    }
                }
                else 
                {
                    DataManager.Instance.currentTurnIndex = (int)turnRequest.playerEnum;

                    TurnCheck();
                }

                break;

            case RequestProtocal.AttackRequest:

                AttackRequest attackRequest = (AttackRequest)baseRequest;

                DataManager.Instance.SetAreaData(attackRequest.fromAreaData);
                DataManager.Instance.SetAreaData(attackRequest.toAreaData);

                mapController.playerIconController.SetBundleKeyuAll();
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

                    if (tradeData.fromPlayerEnum == myPlayer || tradeData.toPlayerEnum == myPlayer)
                    {
                        AreaData currentAreaData = DataManager.Instance.GetAreaData(tradeData.areaData.id);

                        PlayerEnum changePlayerEnum = tradeData.buyOn ? tradeData.fromPlayerEnum : tradeData.toPlayerEnum;

                        currentAreaData.PlayerChangeOn(changePlayerEnum);

                        if (tradeData.fromPlayerEnum == myPlayer)
                        {
                            mapController.inGameBottomController.nonePlayPanel.SkillUseOn();
                        }

                        mapController.playerIconController.SetBundleKeyuAll();
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

                        ServerManager.Instance.AllianceResultRequestOn(request);
                    }
                }

                break;

            case RequestProtocal.AllianceResultRequest:

                AllianceResultRequest allianceResultRequest = (AllianceResultRequest)baseRequest;

                mapController.inGameBottomController.nonePlayPanel.AllianceClearOn(allianceResultRequest);

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
                        DataManager.Instance.BetrayOn();
                        nonePlayPanel.SetNoneBtn();
                    }
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
        DataManager.Instance.gameStart.SetValueAndForceNotify(false);
        //bool win = playerEnum == InGameDataManager.Instance.playerData.playerEnum;
        PopupManager.Instance.GameResultPopupOn(win);
    }

    public void GameStartOn()
    {
        DataManager.Instance.gameStart.SetValueAndForceNotify(true);

        mapController.inGameBottomController.SetStatus(1);
    }

    public void GoMainOn()
    {
        popupController.PopupAllInActive();
        SceneManager.LoadScene("Main");
    }

    public void TurnOffOn()
    {
        DataManager.Instance.TurnOffOn();
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
        if (DataManager.Instance.isMultiOn == false)
        {
            TurnCheckDelayCheck();
        }   

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            //PopupManager.Instance.LandTradeApprovePopupOn();

            //yourTurnPopup.MyTurnActiveOn();
        }
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

        if (DataManager.Instance.isMultiOn)
        {
            timer.SetTimerOn(myTurn);
        }
        

        if (myTurn)
        {
            PopupManager.Instance.YourTurnPopupOn();

            if (mapController.choisIndex != -1)
            {
                mapController.DeSelectOn(DataManager.Instance.GetAreaData(mapController.choisIndex));
            }
        }
        else
        {
            if (DataManager.Instance.IsAITurn())
            {
                //StartCoroutine();
               await AIPlayOn();
            }
        }
        SetEndTurnBtn();
    }

    private async UniTask AIPlayOn()
    {
        //yield return mapController.AIAttackOn((PlayerEnum)InGameDataManager.Instance.currentTurnIndex);


        PlayerEnum currentPlayer = (PlayerEnum)DataManager.Instance.currentTurnIndex;

        mapController.AIAttackOn(currentPlayer);

        List<AreaData> areaDataList = mapController.DiceAddOn(currentPlayer);

        if (DataManager.Instance.isMultiOn)
        {
            await Task.Delay(2000);

            //   yield return new WaitForSeconds(2);

            if (DataManager.Instance.isMultiOn)
            {
                TurnRequest turnRequest = new TurnRequest()
                {
                    playerEnum = currentPlayer,
                    turnStartOn = false,
                    areaDataList = areaDataList,
                };

                ServerManager.Instance.TurnRequestOn(turnRequest);
            }
        }

        TurnOffOn();
    }

    void SetEndTurnBtn()
    {
        bool gameStart = DataManager.Instance.gameStart.Value;

        endTurnBtn.interactable = gameStart && DataManager.Instance.IsMyTurn();
    }

    public void EndTurnBtnClickOn()
    {
        timer.SetTimerOn(false);
        List<AreaData> areaDataList = mapController.EndTurnBtnClickOn();

        if (DataManager.Instance.isMultiOn)
        {
            TurnRequest turnRequest = new TurnRequest()
            {
                playerEnum = DataManager.Instance.playerData.playerEnum,
                turnStartOn = false,
                areaDataList = areaDataList,
            };

            ServerManager.Instance.TurnRequestOn(turnRequest);
        }
    }

    public void NewGameOn()
    {
        DataManager.Instance.gameStart.SetValueAndForceNotify(false);

        popupController.PopupAllInActive();

        mapController.NewGameOn();

        DataManager.Instance.playOn = false;

        if (DataManager.Instance.mapSelectionOn == false)
        {
            GameStartOn();
        }
    }
    public void ReStartOn()
    {
        popupController.PopupAllInActive();

        mapController.ReStartOn();

        DataManager.Instance.playOn = false;
    }

    public void OptionPopupOnOn()
    {
        PopupManager.Instance.OptionPopupOn();
    }
}