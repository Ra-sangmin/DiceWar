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
	[SerializeField] MyAllianceApproveController myAllianceApproveController;
	
	List<allianceRequestCheckData> allianceRequestCheckDataList = new List<allianceRequestCheckData>();

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
		//int myTurnCount = DataManager.Instance.MyTurnAddOn();

		//Debug.LogWarning(myTurnCount);

		//if (myTurnCount > 2 && DataManager.Instance.isOwner)
		{
			//;Debug.LogWarning("카드 On!");
			//DataManager.Instance.myTurnCount = 0;
			//mapController.inGameBottomController.nonePlayPanel.SkillUseOn(1);
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

	protected override async UniTask AIPlayOn(PlayerEnum currentPlayer, CancellationTokenSource source)
	{
		await AISkillUseOn(currentPlayer, source);

		await mapController.AIAttackOn(currentPlayer, source);

		List<AreaData> areaDataList = mapController.DiceAddOn(currentPlayer);

		TurnEndRequestOn(currentPlayer, areaDataList);
	}

	public async UniTask AISkillUseOn(PlayerEnum playerEnum, CancellationTokenSource source)
	{
		int countIndex = DataManager.Instance.GetPlayerData(playerEnum).sc;
		
		if ( countIndex == 0 || //스킬 카드가 없다면
			DataManager.Instance.IsAlliance(playerEnum)) //이미 동맹을 맺고 있다면
		{
			return;
		}

		List<PlayerIcon> playerList = mapController.playerIconController.CheckAIAllianceOn(playerEnum);

		// 동맹을 맺을 플레이어가 없다면
		if (playerList.Count == 0)
		{
			return;
		}
		else
		{
			List<PlayerEnum> playerEnumList = playerList.Select(data => data.playerEnum).ToList();

			playerEnumList.Insert(0, playerEnum);

			await AllianceRequestOn(playerEnum , playerEnumList);
		}
	}

	public async UniTask AllianceRequestOn(PlayerEnum playerEnum , List<PlayerEnum> playerEnumList)
	{
		List<AllianceData> allianceDataList = DataManager.Instance.GetAllianceDefaultData(playerEnum, playerEnumList, mapController.playerIconController);

		AllianceData orderData = allianceDataList.FirstOrDefault(data => data.playerEnum == playerEnum);

		foreach (var allianceRequestCheck in allianceRequestCheckDataList)
		{
			List<PlayerEnum> playerEnumCheckList = allianceRequestCheck.allianceRequestData.allianceDataList.Select(data => data.playerEnum).ToList();

			//기존에 요청한 같은 동맹이 있는지 체크
			bool sameRequestOn = AreListsScrambledEquals(playerEnumList, playerEnumCheckList);

			if (sameRequestOn)
			{
				return;
			}
		}

		DataManager.Instance.SkillCardCountAddOn(playerEnum, -1);

		bool allAIOn = allianceDataList.All(data => base.IsAIOn(data.playerEnum));

		int delayTime = allAIOn ? 200 : 500;

		//요청 보내기
		AllianceRequest request = new AllianceRequest()
		{
			orderData = orderData,
			allianceDataList = allianceDataList,
		};

		ServerManager.Instance.SendMessageOn(request);

		await UniTask.Delay(delayTime, cancellationToken: source.Token);
	}

	bool AreListsScrambledEquals<T>(List<T> list1, List<T> list2)
	{
		// 1. 개수가 다르면 무조건 다른 리스트
		if (list1.Count != list2.Count) return false;

		// 2. 첫 번째 리스트의 원소별 개수를 딕셔너리에 담기
		var counts = new Dictionary<T, int>();
		foreach (T item in list1)
		{
			if (counts.ContainsKey(item)) counts[item]++;
			else counts[item] = 1;
		}

		// 3. 두 번째 리스트를 돌며 딕셔너리 카운트를 차감
		foreach (T item in list2)
		{
			// 첫 번째 리스트에 없는 원소가 있거나, 개수가 더 많다면 일치하지 않음
			if (!counts.ContainsKey(item) || counts[item] == 0)
			{
				return false;
			}
			counts[item]--;
		}

		return true;
	}

	// Update is called once per frame
	protected override void Update()
	{
		base.Update();
	}

	private void OnDestroy()
	{
		// [수정] ServerManager 인스턴스가 파괴되지 않고 살아있을 때만 이벤트를 해제합니다.
		if (ServerManager.Instance != null)
		{
			ServerManager.Instance.receiveDataOn -= ReceiveDataOn;
		}
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

		await UniTask.Delay(0, cancellationToken: source.Token);

		ServerManager.Instance.SendMessageOn(turnRequest);
	}

	private void ReceiveDataOn(BaseTCPRequest baseRequest)
	{
		//Debug.LogWarning(baseRequest.requestProtocal);

		PlayerEnum myPlayer = DataManager.Instance.playerData.pe;

		switch (baseRequest.requestProtocal)
		{
			case RequestProtocal.TurnRequest:
					TurnResponseOn(baseRequest);break;

			case RequestProtocal.AttackRequest:
					AttackResponseOn(baseRequest);break;

			case RequestProtocal.LandTradeRequest:
					LandTradeResponseOn(baseRequest);break;

			case RequestProtocal.LandTradeApproveRequest:
					LandTradeApproveResponseOn(baseRequest);break;

			case RequestProtocal.AllianceRequest: 
					AllianceResponseOn(baseRequest); break;

			case RequestProtocal.AllianceApproveRequest:
					AllianceApproveResponseOn(baseRequest); break;

			case RequestProtocal.AllianceResultRequest:
					AllianceResultResponseOn(baseRequest); break;

			case RequestProtocal.AllianceBetrayRequest:
					AllianceBetrayResponseOn(baseRequest); break;

			case RequestProtocal.GameOutOn:
					GameOutResponseOn(baseRequest); break;

			case RequestProtocal.ForceGetArea:
					ForceGetAreaResponseOn(baseRequest); break;

			case RequestProtocal.SkillCardRequest:
					SkillCardResponseOn(baseRequest); break;
		}
	}

	/// <summary>
	/// 턴 변경 
	/// </summary>
	/// <param name="baseRequest"></param>
	private void TurnResponseOn(BaseTCPRequest baseRequest)
	{
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

			bool myTurn = DataManager.Instance.IsMyTurn();

			if (playerIcon == null)
			{
				TurnRequestOn(turnRequest).Forget();
			}
			else
			{
				if (myTurn && DataManager.Instance.isOwner)
				{
					int myTurnCount = DataManager.Instance.MyTurnAddOn();

					if (myTurnCount > 1)
					{
						DataManager.Instance.myTurnCount = 0;

						List<PlayerEnum> addOnPlayerList = mapController.playerIconController.GetActiveDiceLowerList();

						mapController.inGameBottomController.nonePlayPanel.SkillCardAddOn(addOnPlayerList);
					}
				}

				TurnCheck();
			}

			if (myTurn)
			{
				timer.SetTimerOn(true, playerIcon.connectedCount == 0);
				endTurnBtn.interactable = true;
			}
		}
	}

	/// <summary>
	/// 공격 정보
	/// </summary>
	/// <param name="baseRequest"></param>
	private void AttackResponseOn(BaseTCPRequest baseRequest)
	{
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
	}

	/// <summary>
	/// 영토 교환 
	/// </summary>
	/// <param name="baseRequest"></param>
	private void LandTradeResponseOn(BaseTCPRequest baseRequest) 
	{
		LandTradeRequest landTradeRequest = (LandTradeRequest)baseRequest;
		
		//내가 제안한 경우
		if (landTradeRequest.fromPlayerEnum == GetMyPlayer())
		{
			mapController.inGameBottomController.nonePlayPanel.SkillUseOn();
		}
		//나에게 제안이 왔을경우
		else if (landTradeRequest.toPlayerEnum == GetMyPlayer())
		{
			ApproveData approveData = new ApproveData()
			{
				landTradeRequest = landTradeRequest,
				isAlliance = false,
			};

			approveController.AddPopupOn(approveData);
		}

		//제안 받은 플레이어가 AI 이고, 내가 게임 리더라면
		if (IsAIOn(landTradeRequest.toPlayerEnum) && DataManager.Instance.isOwner)
		{
			bool approveOn = true;

			var areaDataList = DataManager.Instance.SetBundleKey(landTradeRequest.toPlayerEnum);

			AreaData areaData = landTradeRequest.areaData;

			List<PlayerEnum> checkPlayerEnumList = new List<PlayerEnum>() { landTradeRequest.fromPlayerEnum  , landTradeRequest.toPlayerEnum };

			bool isMyAlliance = DataManager.Instance.IsAllAlliance(checkPlayerEnumList);

			if (areaDataList.Any(data => data.id == areaData.id) || //큰 덩어리에 속해 있거나,
				landTradeRequest.coinCount <= 0 ||  //  0코인 이라면
				!landTradeRequest.buyOn || // 영토 구매라면
				!isMyAlliance) //나와 동맹이 아니라면
			{
				approveOn = false;
			}

			LandTradeApproveRequest request = new LandTradeApproveRequest()
			{
				landTradeRequest = landTradeRequest,
				approveOn = approveOn,
			};

			ServerManager.Instance.SendMessageOn(request);
		}
	}

	/// <summary>
	/// 영토 교환 수락
	/// </summary>
	/// <param name="baseRequest"></param>
	private void LandTradeApproveResponseOn(BaseTCPRequest baseRequest)
	{
		LandTradeApproveRequest landTradeApproveRequest = (LandTradeApproveRequest)baseRequest;

		LandTradeRequest tradeData = landTradeApproveRequest.landTradeRequest;

		bool approveOn = landTradeApproveRequest.approveOn;

		if (tradeData.fromPlayerEnum == GetMyPlayer())
		{
			LandTradeWarningPopupOn(tradeData , approveOn);
		}

		if (approveOn)
		{
			AreaData currentAreaData = DataManager.Instance.GetAreaData(tradeData.areaData.id);
			PlayerEnum changePlayerEnum = tradeData.buyOn ? tradeData.fromPlayerEnum : tradeData.toPlayerEnum;
			currentAreaData.PlayerChangeOn(changePlayerEnum);
			mapController.playerIconController.SetBundleKeyuAll();

			// 내가 거래 대상이라면
			if (tradeData.fromPlayerEnum == GetMyPlayer() || tradeData.toPlayerEnum == GetMyPlayer())
			{
				int coinCount = tradeData.coinCount;

				//내가 땅을 샀거나, 상대방이 나에게 땅을 팔았다면
				if (tradeData.fromPlayerEnum == GetMyPlayer() && tradeData.buyOn ||
					tradeData.toPlayerEnum == GetMyPlayer() && tradeData.buyOn == false)
				{
					coinCount *= -1;
				}

				DataManager.Instance.AddCoin(coinCount);
			}
		}
		else
		{
			//내가 제안해서 실패했을 경우
			if (tradeData.fromPlayerEnum == GetMyPlayer())
			{
				DataManager.Instance.SkillCardCountAdd(1);
			}
		}
	}

	private void LandTradeWarningPopupOn(LandTradeRequest tradeData , bool approveOn)
	{
		int localizeKey = -1;

		if (approveOn)
		{
			localizeKey = tradeData.buyOn ? 41 : 43;
		}
		else
		{
			localizeKey = tradeData.buyOn ? 42 : 44;
		}

		string resultStr = LocalizeManager.Instance.GetStrData(LocalizeStatus.Game, localizeKey);
		PopupManager.Instance.InGameWarningPopupOn(resultStr);
	}

	/// <summary>
	/// 동맹 맺기 시작
	/// </summary>
	/// <param name="baseRequest"></param>
	private async void AllianceResponseOn(BaseTCPRequest baseRequest) 
	{
		AllianceRequest allianceRequest = (AllianceRequest)baseRequest;

		//본인이 요청을 주최한 플레이어 라면
		if (allianceRequest.orderData.playerEnum == GetMyPlayer())
		{
			allianceRequestCheckData checkData = new allianceRequestCheckData();
			checkData.allianceRequestData = allianceRequest;
			checkData.allianceApproveRequestList.Clear();

			allianceRequestCheckDataList.Add(checkData);

			mapController.inGameBottomController.nonePlayPanel.SkillUseOn();

			myAllianceApproveController.SetData(allianceRequest);
		}
		else
		{
			//동맹 요청 플레이어에 본인이 포함되어있다면
			if (allianceRequest.allianceDataList.Any(data => data.playerEnum == GetMyPlayer()))
			{
				ApproveData approveData = new ApproveData()
				{
					allianceRequest = allianceRequest,
					isAlliance = true,
				};

				approveController.AddPopupOn(approveData);
			}
		}

		//내가 현재 게임의 리더라면
		if (DataManager.Instance.isOwner)
		{
			bool isAIOn = allianceRequest.allianceDataList.Any(data => IsAIOn(data.playerEnum));

			//동맹 플레이어에 AI가 포함되어있다면
			if (isAIOn)
			{
				bool IsOrderAI = IsAIOn(allianceRequest.orderData.playerEnum);

				//동맹 요청 주최가 AI라면 (유저가 포함되어있음 , 만약 모두 AI라면 즉각처리중)
				if (IsOrderAI)
				{
					allianceRequestCheckData checkData = new allianceRequestCheckData();
					checkData.allianceRequestData = allianceRequest;
					checkData.allianceApproveRequestList.Clear();

					allianceRequestCheckDataList.Add(checkData);

					foreach (var allianceData in checkData.allianceRequestData.allianceDataList)
					{
						if (allianceData.playerEnum == allianceRequest.orderData.playerEnum || //오더 플레이어이거나
							!IsAIOn(allianceData.playerEnum)) // AI 아니라면
						{
							continue;
						}

						await UniTask.Delay(200, cancellationToken: source.Token);

						AllianceApproveRequest request = new AllianceApproveRequest()
						{
							orderData = allianceRequest.orderData,
							playerEnum = allianceData.playerEnum,
							approveOn = true,
						};

						ServerManager.Instance.SendMessageOn(request);
					}
				}
				//동맹 주최가 유저 이지만 , 동맹 요청에 AI가 포함되어 리더가 AI 수락을 처리
				else
				{
					int maxCoinCount = DataManager.Instance.GetNeedCoin() * DataManager.Instance.num_player;
					int oneManCoinCount = maxCoinCount / allianceRequest.allianceDataList.Count;

					foreach (var allianceData in allianceRequest.allianceDataList)
					{
						//AI가 아닌 유저라면
						if (!IsAIOn(allianceData.playerEnum))
						{
							continue;
						}

						await UniTask.Delay(200, cancellationToken: source.Token);

						float needCoin = oneManCoinCount;

						var allReadyData = DataManager.Instance.GetAllianceData(allianceData.playerEnum);

						//만약 이미 동맹이 되어있다면
						if (allReadyData != null)
						{
							needCoin = (allReadyData.coinCount * 1.5f);
						}

						//분배 받은 코인이 1인당 코인배분 보다 크거나 같다면
						bool approveOn = allianceData.coinCount >= needCoin;

						AllianceApproveRequest request = new AllianceApproveRequest()
						{
							orderData = allianceRequest.orderData,
							playerEnum = allianceData.playerEnum,
							approveOn = approveOn,
						};

						ServerManager.Instance.SendMessageOn(request);
					}
				}
			}
		}
	}

	/// <summary>
	/// 동맹 요청 수락
	/// </summary>
	/// <param name="baseRequest"></param>
	private async void AllianceApproveResponseOn(BaseTCPRequest baseRequest)
	{
		AllianceApproveRequest allianceApproveRequest = (AllianceApproveRequest)baseRequest;

		//본인이 오더 플레이어 라면
		if (allianceApproveRequest.orderData.playerEnum == GetMyPlayer())
		{
			AllianceApproveResultCheckOn(allianceApproveRequest);

			myAllianceApproveController.ApprovedOn(allianceApproveRequest.playerEnum, allianceApproveRequest.approveOn);

			if (allianceApproveRequest.approveOn == false)
			{
				myAllianceApproveController.FadeOn();
			}
		}
		else
		{
			bool IsOrderAI = IsAIOn(allianceApproveRequest.orderData.playerEnum);

			//동맹 주체 AI이고 내가 게임 리더라면
			if (IsOrderAI && DataManager.Instance.isOwner)
			{
				AllianceApproveResultCheckOn(allianceApproveRequest);
			}
		}
	}

	/// <summary>
	/// 동맹 수락 상황 체크
	/// </summary>
	/// <param name="allianceApproveRequest"></param>
	private async void AllianceApproveResultCheckOn(AllianceApproveRequest allianceApproveRequest)
	{
		var checkData = allianceRequestCheckDataList.FirstOrDefault(data => data.allianceRequestData.orderData.playerEnum == allianceApproveRequest.orderData.playerEnum);

		if (checkData == null)
			return;

		if (allianceApproveRequest.approveOn == false)
		{
			allianceRequestCheckDataList.Remove(checkData);

			await UniTask.Delay(500, cancellationToken: source.Token);

			DataManager.Instance.SkillCardCountAddOn(allianceApproveRequest.orderData.playerEnum);

			string resultStr = LocalizeManager.Instance.GetStrData(LocalizeStatus.Game, 40);

			PopupManager.Instance.InGameWarningPopupOn(resultStr);
			return;
		}
		

		checkData.allianceApproveRequestList.Add(allianceApproveRequest);

		if (checkData.allianceApproveRequestList.Count == checkData.allianceRequestData.allianceDataList.Count - 1 && checkData.allianceApproveRequestList.All(data => data.approveOn))
		{
			AllianceResultRequest request = new AllianceResultRequest()
			{
				orderData = allianceApproveRequest.orderData,
				allianceDataList = checkData.allianceRequestData.allianceDataList,
			};

			ServerManager.Instance.SendMessageOn(request);
		}
	}

	/// <summary>
	/// 동맹 요청 결과
	/// </summary>
	/// <param name="baseRequest"></param>
	private async void AllianceResultResponseOn(BaseTCPRequest baseRequest)
	{
		AllianceResultRequest allianceResultRequest = (AllianceResultRequest)baseRequest;

		var checkData = allianceRequestCheckDataList.FirstOrDefault(data => data.allianceRequestData.orderData.playerEnum == allianceResultRequest.orderData.playerEnum);

		//이미 동맹인 플레이어가 있는지 체크
		foreach ( var allianceData in allianceResultRequest.allianceDataList)
		{
			if (DataManager.Instance.IsAlliance(allianceData.playerEnum))
			{
				await DataManager.Instance.OrderBetrayOn(allianceData.playerEnum);
			}
		}

		allianceRequestCheckDataList.Remove(checkData);

		mapController.inGameBottomController.nonePlayPanel.AllianceClearOn(allianceResultRequest.allianceDataList);

		mapController.playerIconController.SetAlliancePlayerIcon(allianceResultRequest);

		mapController.playerIconController.SetBundleKeyuAll();

		approveController.ResetApproveData();

		string resultStr = LocalizeManager.Instance.GetStrData(LocalizeStatus.Game, 39);

		PopupManager.Instance.InGameWarningPopupOn(resultStr);

		if (allianceResultRequest.orderData.playerEnum == GetMyPlayer())
		{
			myAllianceApproveController.FadeOn();
		}
	}

	/// <summary>
	/// 동맹 배신
	/// </summary>
	/// <param name="baseRequest"></param>
	private void AllianceBetrayResponseOn(BaseTCPRequest baseRequest)
	{
		AllianceBetrayRequest allianceBetrayRequest = (AllianceBetrayRequest)baseRequest;

		//nonePlayPanel.SetBetrayAllianceList(allianceBetrayRequest.allianceDataList);

		BetrayOn(allianceBetrayRequest.betrayPlayerEnum);

		//내가 배신한것이라면
		if (allianceBetrayRequest.betrayPlayerEnum == GetMyPlayer())
		{
			mapController.inGameBottomController.nonePlayPanel.SkillUseOn();

			int addCoin = allianceBetrayRequest.needBetrayCoin;
			DataManager.Instance.AddCoin(-addCoin);
			DataManager.Instance.myBetrayWaitOn = false;
		}
	}

	private void BetrayOn(PlayerEnum playerEnum)
	{
		var nonePlayPanel = mapController.inGameBottomController.nonePlayPanel;

		DataManager.Instance.BetrayOn(playerEnum);
		mapController.playerIconController.SetBetrayPlayerIcon(playerEnum);

		//남은 동맹이 1명뿐인지 체크
		DataManager.Instance.AllianceClearCheckOn(mapController.playerIconController);

		mapController.playerIconController.SetBundleKeyuAll();

		nonePlayPanel.SetNoneBtn();
		nonePlayPanel.SkillCardPanelActiveOn(nonePlayPanel.skillCardPanelActive);

		approveController.ResetApproveData();
	}

	/// <summary>
	/// 게임 종료
	/// </summary>
	/// <param name="baseRequest"></param>
	private void GameOutResponseOn(BaseTCPRequest baseRequest)
	{
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
	}

	/// <summary>
	/// 영토 강제 변경
	/// </summary>
	/// <param name="baseRequest"></param>
	private void ForceGetAreaResponseOn(BaseTCPRequest baseRequest)
	{
		ForceGetAreaRequest forceGetAreaRequest = (ForceGetAreaRequest)baseRequest;

		AreaData areaData = DataManager.Instance.GetAreaData(forceGetAreaRequest.areaData.id);
		areaData.PlayerChangeOn(forceGetAreaRequest.areaData.player);
		areaData.SetDice(forceGetAreaRequest.areaData.dice);
		mapController.playerIconController.SetBundleKeyuAll();
	}

	private void SkillCardResponseOn(BaseTCPRequest baseRequest)
	{
		SkillCardRequest skillCardRequest = (SkillCardRequest)baseRequest;

		DataManager.Instance.SetPlayerSkillData(skillCardRequest.playerEnum, skillCardRequest.skillCardCount);

		if (skillCardRequest.playerEnum == GetMyPlayer())
		{
			mapController.inGameBottomController.nonePlayPanel.SkillCardReset();
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
		await UniTask.Delay(200, cancellationToken: source.Token);

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

public class allianceRequestCheckData
{
	public AllianceRequest allianceRequestData;
	public List<AllianceApproveRequest> allianceApproveRequestList = new List<AllianceApproveRequest>();
}