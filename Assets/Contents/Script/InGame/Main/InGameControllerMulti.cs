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
using System.Threading;
using static GameResultPopup;

public class InGameControllerMulti : InGameControllerBase
{
	[SerializeField] Timer timer;
	[SerializeField] ApproveController approveController;

	private AllianceRequest allianceRequestData;
	private List<AllianceApproveRequest> allianceApproveRequestList = new List<AllianceApproveRequest>();
	
	protected override void Awake()
	{
		base.Awake();
	}

	protected override void SetEvent()
	{
		base.SetEvent();

		DataManager.Instance.gameStart
			.Subscribe(_ =>
			{
				//SetEndTurnBtn();

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
		};

		base.mapController.SetTimer(timer);
	}

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	protected override void Start()
    {
		base.Start();

		ServerManager.Instance.receiveDataOn += ReceiveDataOn;

		//DataManager.Instance.playerData.skillCardCount = 1;

		GameStartOn();
	}

	protected override void MyTurnPlayOn()
	{
		int myTurnCount = DataManager.Instance.MyTurnAddOn();

		if (myTurnCount > 5)
		{
			DataManager.Instance.myTurnCount = 0;
			mapController.inGameBottomController.nonePlayPanel.SkillUseOn(1);
		}
	}

	protected override async UniTask OtherTurnPlayOn()
	{
		if (DataManager.Instance.IsAITurn() && DataManager.Instance.isOwner)
		{
			await base.OtherTurnPlayOn();
		}
	}

	protected override void OtherPlayerNotOn()
	{
		PlayerEnum currentPlayer = DataManager.Instance.currentPlayer;

		TurnEndRequestOn(currentPlayer, new List<AreaData>());
	}

	protected override async UniTask AIPlayOn(PlayerEnum currentPlayer , CancellationTokenSource source)
	{
		await mapController.AIAttackOn(currentPlayer, source);

		List<AreaData> areaDataList = mapController.DiceAddOn(currentPlayer);

		TurnEndRequestOn(currentPlayer, areaDataList);
	}

	// Update is called once per frame
	protected override void Update()
	{
		base.Update();
	}

	private void OnDestroy()
	{
		ServerManager.Instance.receiveDataOn -= ReceiveDataOn;
	}

	protected override void GameStartOn()
	{
		base.GameStartOn();

		if (DataManager.Instance.isOwner)
		{
			TurnRequest turnRequest = new TurnRequest()
			{
				playerEnum = DataManager.Instance.playerData.pe,
				turnStartOn = true,
			};

			ServerManager.Instance.SendMessageOn(turnRequest);
		}
	}

	private async UniTask TurnRequestOn(TurnRequest turnRequest)
	{
		PlayerEnum nextPlayer = GetNextPlayer();

		turnRequest.playerEnum = nextPlayer;
		turnRequest.turnStartOn = true;

		await UniTask.Delay(0);

		ServerManager.Instance.SendMessageOn(turnRequest);
	}

	private void ReceiveDataOn(BaseTCPRequest baseRequest)
	{
		//Debug.LogWarning(baseRequest.requestProtocal);

		PlayerEnum myPlayer = DataManager.Instance.playerData.pe;

		switch (baseRequest.requestProtocal)
		{
			case RequestProtocal.TurnRequest:

				TurnRequest turnRequest = (TurnRequest)baseRequest;

				mapController.SelectClearOn();

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
					DataManager.Instance.SetCurrentTurnIndex((int)turnRequest.playerEnum);

					mapController.inGameBottomController.nonePlayPanel.SetNoneBtn();

					PlayerEnum currentPlayer = DataManager.Instance.currentPlayer;

					PlayerIcon playerIcon = mapController.playerIconController.GetPlayerIcon(currentPlayer);

					if (playerIcon == null)
					{
						TurnRequestOn(turnRequest).Forget();
					}
					else
					{
						TurnCheck();
					}

					if (DataManager.Instance.IsMyTurn())
					{
						timer.SetTimerOn(true);
						endTurnBtn.interactable = true;
					}
				}

				break;

			case RequestProtocal.AttackRequest:

				AttackRequest attackRequest = (AttackRequest)baseRequest;

				//내가 공격한 정보가 아니라면
				if (attackRequest.fromAreaData.player != DataManager.Instance.playerData.pe)
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
					ApproveData approveData = new ApproveData()
					{
						landTradeRequest = landTradeRequest,
						isAlliance = false,
					};

					approveController.AddPopupOn(approveData);

					//PopupManager.Instance.LandTradeApprovePopupOn((LandTradeRequest)baseRequest);
				}
				else if (landTradeRequest.fromPlayerEnum == myPlayer)
				{
					mapController.inGameBottomController.nonePlayPanel.SkillUseOn();
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
						//if (tradeData.fromPlayerEnum == myPlayer)
						//{
						//	mapController.inGameBottomController.nonePlayPanel.SkillUseOn();
						//}

						int coinCount = tradeData.coinCount;

						//내가 땅을 샀거나, 상대방이 나에게 땅을 팔았다면
						if (tradeData.fromPlayerEnum == myPlayer && tradeData.buyOn ||
							tradeData.toPlayerEnum == myPlayer && tradeData.buyOn == false)
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
					mapController.inGameBottomController.nonePlayPanel.SkillUseOn();

					allianceApproveRequestList.Clear();
				}
				else if (allianceRequest.allianceDataList.Any(data => data.playerEnum == myPlayer))
				{
					ApproveData approveData = new ApproveData()
					{
						allianceRequest = allianceRequest,
						isAlliance = true,
					};

					approveController.AddPopupOn(approveData);

					//PopupManager.Instance.AllianceApprovePopupOn(allianceRequest);
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


					if (allianceApproveRequestList.Count == allianceRequestData.allianceDataList.Count - 1 && allianceApproveRequestList.All(data => data.approveOn))
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

				mapController.inGameBottomController.nonePlayPanel.AllianceClearOn(allianceResultRequest.allianceDataList);

				mapController.playerIconController.SetAlliancePlayerIcon(allianceResultRequest);

				mapController.playerIconController.SetBundleKeyuAll();

				approveController.ResetApproveData();

				break;

			case RequestProtocal.AllianceBetrayRequest:

				AllianceBetrayRequest allianceBetrayRequest = (AllianceBetrayRequest)baseRequest;

				var nonePlayPanel = mapController.inGameBottomController.nonePlayPanel;

				DataManager.Instance.BetrayOn(allianceBetrayRequest.betrayPlayerEnum);
				mapController.playerIconController.SetBetrayPlayerIcon(allianceBetrayRequest.betrayPlayerEnum);

				nonePlayPanel.AllianceClearOn(allianceBetrayRequest.allianceDataList);

				//내가 배신한것이라면
				if (allianceBetrayRequest.betrayPlayerEnum == myPlayer)
				{
					mapController.inGameBottomController.nonePlayPanel.SkillUseOn();

					int addCoin = DataManager.Instance.GetNeedBetrayCoin();
					DataManager.Instance.AddCoin(-addCoin);
				}

				//남은 동맹이 1명뿐인지 체크
				DataManager.Instance.AllianceClearCheckOn(mapController.playerIconController);

				nonePlayPanel.SetNoneBtn();
				nonePlayPanel.SkillCardPanelActiveOn(nonePlayPanel.skillCardPanelActive);

				approveController.ResetApproveData();

				break;

			case RequestProtocal.GameOutOn:

				GameOutRequest outData = (GameOutRequest)baseRequest;

				//bool ownerChangeOn = DataManager.Instance.isOwner == false && outData.isOwner;

				DataManager.Instance.isOwner = outData.isOwner;

				for (int i = 0; i < DataManager.Instance.playerDataList.Count; i++)
				{
					PlayerData playerData = DataManager.Instance.playerDataList[i];

					if (playerData.pe == outData.outPlayerEnum)
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
	protected override void EndTurnEventOn()
	{
		base.EndTurnEventOn();

		timer.SetTimerOn(false);

		List<AreaData> areaDataList = mapController.EndTurnBtnClickOn();

		TurnEndRequestOn(DataManager.Instance.playerData.pe, areaDataList);
	}

	private async void TurnEndRequestOn(PlayerEnum currentPlayer, List<AreaData> areaDataList)
	{
		await UniTask.Delay(200);

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

	protected override int GetCoin(GameResultEnum gameResultEnum)
	{
		int coinCount = 0;

		if (gameResultEnum == GameResultEnum.Win)
		{
			int resultPlayCount = mapController.playerIconController.GetActiveList().Count;

			coinCount = DataManager.Instance.GetMultiRewardCoin(resultPlayCount);
		}

		return coinCount;
	}

}
