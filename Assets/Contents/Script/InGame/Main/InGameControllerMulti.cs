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

	//앱 이탈 처리 (기획 시트 : 게임 화면을 3초 이상 벗어나면 그 판은 퇴장, 동맹이 있으면 배신 패널티) - 2026-09-26
	private const double BackgroundOutSeconds = 3.0;
	private DateTime pauseStartTime;
	private bool pausedOn = false;
	private bool backgroundOutOn = false;

	private bool isFirstMyTurn = true;

	private Dictionary<PlayerEnum, HashSet<PlayerEnum>> allianceRejectedDic = new Dictionary<PlayerEnum, HashSet<PlayerEnum>>();

	//카드를 가진 플레이어 중 하나만 동맹 제안을 진행하도록 관리하는 현재 제안자 (퇴장 시 다른 AI가 이어받음)
	private PlayerEnum currentAllianceProposer = PlayerEnum.Player_None;

	//거절/타임아웃 이후 재제안까지 기다려야 하는 턴 수 (자신의 턴이 2번째 돌아올 때까지 대기)
	private Dictionary<PlayerEnum, int> allianceRetryCooldownDic = new Dictionary<PlayerEnum, int>();

	//AI 가 받은 동맹 제안 대기 목록 — 리더(방장)만 관리. AI 턴 시작 때 한 바퀴만 판단한다 (2026-09-28)
	private List<AIAllianceOfferData> aiAllianceOfferList = new List<AIAllianceOfferData>();

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
		//인게임 UI 가 준비될 때까지(보통 1 프레임) 받은 패킷을 큐에 붙잡아 둔다.
		//receiveDataOn 을 StartReady 에서 연결하므로, 그 전에 처리되면 패킷이 사라진다 (PanelRenderer, 2026-09-29)
		ServerManager.Instance.receiveOn = false;

		base.Start();
	}

	/// <summary> Start 에서 멈춘 패킷 처리를 다시 켰는지 </summary>
	private bool receiveResumed = false;

	protected override void StartReady()
	{
		try
		{
			base.StartReady();

			timer.InitView(UIRoot);

			myAllianceApproveController.InitView(UIRoot);
			myAllianceApproveController.cancelBtnClickEventOn = AllianceCancelRequestOn;

			approveController.InitView(UIRoot);

			//동맹이 있는 채로 내 땅이 0 이 되면 : 계속하기(관전) / 새로하기 / 그만하기 - 배신 패널티 면제
			mapController.playerIconController.myLandZeroWithAllianceOn = () =>
				PopupManager.Instance.GiveUpPopupOn(this, false, 0, true);
		}
		finally
		{
			//위에서 예외가 나도 패킷 수신이 멈춘 채로 남지 않게 한다 (2026-09-29)
			ResumeReceive();
		}

		//DataManager.Instance.playerData.skillCardCount = 1;

		GameStartOn();
	}

	void ResumeReceive()
	{
		if (receiveResumed)
			return;

		receiveResumed = true;

		ServerManager.Instance.receiveDataOn += ReceiveDataOn;

		//붙잡아 둔 패킷 처리 재개
		ServerManager.Instance.receiveOn = true;
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
		//동맹 수락 판단 → 동맹 제안 → 공격 순서 (2026-09-28)
		await AIAllianceApproveCheckOn(currentPlayer);

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

		//거절/타임아웃 이후 재제안 대기 중이라면 (자신의 턴이 2번째 돌아올 때까지 대기)
		if (allianceRetryCooldownDic.ContainsKey(playerEnum))
		{
			return;
		}

		//카드를 가진 플레이어 중 하나만 제안하도록, 이미 다른 AI가 동맹 제안을 진행중이라면 대기 (중복 제안 방지)
		if (IsAllianceProposerAvailableOn(playerEnum) == false)
		{
			return;
		}

		//새로 제안을 시작할 때는 카드를 가진 AI 중 '가장 약한 AI' 만 제안한다 (2026-09-28)
		if (currentAllianceProposer != playerEnum && IsWeakestAIProposerOn(playerEnum) == false)
		{
			return;
		}

		List<PlayerIconElement> playerList = mapController.playerIconController.CheckAIAllianceOn(playerEnum)
			.Where(data => IsAllianceRejectedOn(playerEnum, data.playerEnum) == false)
			.ToList();

		// 동맹을 맺을 플레이어가 없다면
		if (playerList.Count == 0)
		{
			//더 이상 제안할 대상이 없다면 제안자 역할을 반납해서 다른 AI가 이어받을 수 있도록 처리
			if (currentAllianceProposer == playerEnum)
			{
				currentAllianceProposer = PlayerEnum.Player_None;
			}

			return;
		}
		else
		{
			currentAllianceProposer = playerEnum;

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

	protected override void OnDestroy()
	{
		base.OnDestroy();

		// [수정] ServerManager 인스턴스가 파괴되지 않고 살아있을 때만 이벤트를 해제합니다.
		if (ServerManager.Instance != null)
		{
			ServerManager.Instance.receiveDataOn -= ReceiveDataOn;

			//UI 가 준비되기 전에 씬을 떠났으면 Start 에서 멈춘 수신을 되돌려 놓는다 (2026-09-29)
			if (receiveResumed == false)
			{
				ServerManager.Instance.receiveOn = true;
			}
		}
	}

	protected override void GameStartOn()
	{
		base.GameStartOn();

		isFirstMyTurn = true;

		//멀티는 서버의 첫 TurnRequest 가 와야 진짜 첫 차례가 정해진다 - 그때 Your Color 판정
		firstTurnCheckOn = true;

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

			AllianceTurnPassCheckOn(currentPlayer);

			PlayerIconElement playerIcon = mapController.playerIconController.GetPlayerIcon(currentPlayer);

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
				timer.SetTimerOn(true, playerIcon.connectedCount == 0, isFirstMyTurn);
				isFirstMyTurn = false;
				endTurnBtn.SetEnabled(true);
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
			bool approveOn = AILandExchangeApproveCheck(landTradeRequest);

			LandTradeApproveRequest request = new LandTradeApproveRequest()
			{
				landTradeRequest = landTradeRequest,
				approveOn = approveOn,
			};

			ServerManager.Instance.SendMessageOn(request);
		}
	}

	/// <summary>
	/// AI 의 영토 교환 수락 판단 (AI 알고리즘 수정.pptx - 영토 교환 수락 알고리즘, 2026-09-26)
	///  - 동맹이 있으면 동맹에게서 온 제안만 검토한다
	///  - 받을 땅이 내 가장 큰 덩어리와 붙어 있고,
	///  - 줄 땅이 내 가장 큰 덩어리에 속해 있지 않고, 그 덩어리와 땅 하나를 사이에 두고 있지도 않으면 수락
	/// </summary>
	bool AILandExchangeApproveCheck(LandTradeRequest tradeData)
	{
		PlayerEnum aiPlayer = tradeData.toPlayerEnum;

		AreaData receiveArea = DataManager.Instance.GetAreaData(tradeData.coinCount);        //AI 가 받을 땅 (제안자의 땅)
		AreaData giveArea = DataManager.Instance.GetAreaData(tradeData.areaData.id);          //AI 가 줄 땅

		if (receiveArea == null || giveArea == null ||
			receiveArea.player != tradeData.fromPlayerEnum || giveArea.player != aiPlayer)
			return false;

		//동맹이 있는가? → 동맹에게서 온 제안인가?
		if (DataManager.Instance.IsAlliance(aiPlayer) &&
			DataManager.Instance.IsAllAlliance(new List<PlayerEnum>() { tradeData.fromPlayerEnum, aiPlayer }) == false)
			return false;

		List<AreaData> bigAreaList = DataManager.Instance.SetBundleKey(aiPlayer);
		HashSet<int> bigIdSet = new HashSet<int>(bigAreaList.Select(data => data.id));

		//받을 땅이, 내 가장 큰 덩어리와 붙어 있는가?
		if (receiveArea.GetAdjList().Any(data => bigIdSet.Contains(data.id)) == false)
			return false;

		//줄 땅이 내 가장 큰 덩어리에 속해 있는가?
		if (bigIdSet.Contains(giveArea.id))
			return false;

		//줄 땅이 내 가장 큰 덩어리와 땅 하나를 사이에 두고 있는가? (줄 땅의 이웃 중 덩어리에 붙어 있는 땅이 있는가)
		bool oneGapOn = giveArea.GetAdjList()
			.Where(data => bigIdSet.Contains(data.id) == false)
			.Any(middle => middle.GetAdjList().Any(data => bigIdSet.Contains(data.id)));

		return oneGapOn == false;
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

		//상대가 거절한 것인지 (거절이면 카드 환불 없음, 2026-09-28)
		bool rejectedOn = approveOn == false;

		//영토 교환 (2026-09-26) : areaData = 제안자가 받을 땅, coinCount = 제안자가 줄 땅의 id. 코인은 오가지 않는다
		AreaData receiveArea = DataManager.Instance.GetAreaData(tradeData.areaData.id);
		AreaData giveArea = DataManager.Instance.GetAreaData(tradeData.coinCount);

		//그 사이에 땅 주인이 바뀌었다면 성립하지 않는다 (모든 클라이언트가 같은 판단을 한다)
		if (approveOn && (receiveArea == null || giveArea == null ||
			receiveArea.player != tradeData.toPlayerEnum || giveArea.player != tradeData.fromPlayerEnum))
		{
			approveOn = false;
		}

		if (tradeData.fromPlayerEnum == GetMyPlayer())
		{
			LandTradeWarningPopupOn(tradeData , approveOn);
		}

		if (approveOn)
		{
			receiveArea.PlayerChangeOn(tradeData.fromPlayerEnum);
			giveArea.PlayerChangeOn(tradeData.toPlayerEnum);
			mapController.playerIconController.SetBundleKeyuAll();
		}
		else
		{
			//내가 제안해서 실패했을 경우 — 거절은 카드 소모, 수락했는데 땅 주인이 바뀌어 불성립이면 환불
			if (tradeData.fromPlayerEnum == GetMyPlayer() && rejectedOn == false)
			{
				DataManager.Instance.SkillCardCountAdd(1);
			}
		}
	}

	private void LandTradeWarningPopupOn(LandTradeRequest tradeData , bool approveOn)
	{
		//영토 교환 성사 / 거절 (2026-09-26)
		int localizeKey = approveOn ? 73 : 74;

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

		//내가 현재 게임의 리더라면 — AI 가 받은 제안은 바로 답하지 않고 대기 목록에 넣는다.
		//AI 는 자기 턴 시작 때(주사위 공격 전) AIAllianceApproveCheckOn 으로 한 바퀴만 판단한다 (AI 알고리즘 수정-260927.pptx, 2026-09-28)
		if (DataManager.Instance.isOwner)
		{
			PlayerEnum orderPlayerEnum = allianceRequest.orderData.playerEnum;

			//같은 제안자의 예전 제안은 새 제안으로 대체된다
			RemoveAIAllianceOfferOn(orderPlayerEnum);

			bool IsOrderAI = IsAIOn(orderPlayerEnum);

			//동맹 요청 주최가 AI라면, 리더가 대신 수락 현황을 관리한다
			if (IsOrderAI)
			{
				allianceRequestCheckData checkData = new allianceRequestCheckData();
				checkData.allianceRequestData = allianceRequest;
				checkData.allianceApproveRequestList.Clear();

				allianceRequestCheckDataList.Add(checkData);
			}

			foreach (var allianceData in allianceRequest.allianceDataList)
			{
				if (allianceData.playerEnum == orderPlayerEnum || !IsAIOn(allianceData.playerEnum))
					continue;

				aiAllianceOfferList.Add(new AIAllianceOfferData()
				{
					aiPlayer = allianceData.playerEnum,
					allianceRequest = allianceRequest,
				});
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

		//거절(또는 철회)이 하나라도 오면 그 제안은 끝난다 — AI 대기 목록에서도 지운다 (2026-09-28)
		if (allianceApproveRequest.approveOn == false && DataManager.Instance.isOwner)
		{
			RemoveAIAllianceOfferOn(allianceApproveRequest.orderData.playerEnum);
		}

		//제안자 본인이 '거절' 로 보낸 것 = 철회다 (2026-09-19)
		//따로 프로토콜을 만들지 않고 기존 거절 패킷을 그대로 쓴다 — 서버는 중계만 한다
		if (allianceApproveRequest.approveOn == false &&
			allianceApproveRequest.playerEnum == allianceApproveRequest.orderData.playerEnum)
		{
			AllianceCancelResponseOn(allianceApproveRequest);

			return;
		}

		//본인이 오더 플레이어 라면
		if (allianceApproveRequest.orderData.playerEnum == GetMyPlayer())
		{
			AllianceApproveResultCheckOn(allianceApproveRequest);

			myAllianceApproveController.ApprovedOn(allianceApproveRequest.playerEnum, allianceApproveRequest.approveOn);

			if (allianceApproveRequest.approveOn == false)
			{
				//거절 확정 — 바를 "요청 승인 N/M" 결과 알약으로 바꾼다 (2026-09-19)
				myAllianceApproveController.SetResultOn();
			}
		}
		else
		{
			//내가 제안받은 쪽이라면, 다른 사람들의 수락/거절을 바에 표시한다 (2026-09-19)
			approveController.AllianceApprovedOn(allianceApproveRequest.orderData.playerEnum,
				allianceApproveRequest.playerEnum, allianceApproveRequest.approveOn);

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
	/// <summary>
	/// 철회 — 내가 낸 동맹 제안을 거두어들인다. (2026-09-19)
	/// 아직 답하지 않은 사람에게도 "이 제안은 끝났다" 를 알려야 하므로
	/// 제안자 본인이 보낸 거절 패킷을 뿌린다.
	/// </summary>
	public void AllianceCancelRequestOn()
	{
		var checkData = allianceRequestCheckDataList
			.FirstOrDefault(data => data.allianceRequestData.orderData.playerEnum == GetMyPlayer());

		if (checkData == null)
			return;

		AllianceApproveRequest request = new AllianceApproveRequest()
		{
			orderData = checkData.allianceRequestData.orderData,
			playerEnum = GetMyPlayer(),
			approveOn = false,
		};

		ServerManager.Instance.SendMessageOn(request);
	}

	/// <summary>
	/// 철회 수신. 제안자 쪽은 대기 기록을, 제안받은 쪽은 승인 대기 줄을 지운다.
	/// 스킬 카드는 돌려주지 않는다 — 거절/시간초과와 달리 철회는 본인 선택이다.
	/// </summary>
	private void AllianceCancelResponseOn(AllianceApproveRequest allianceApproveRequest)
	{
		PlayerEnum orderPlayerEnum = allianceApproveRequest.orderData.playerEnum;

		var checkData = allianceRequestCheckDataList
			.FirstOrDefault(data => data.allianceRequestData.orderData.playerEnum == orderPlayerEnum);

		if (checkData != null)
		{
			allianceRequestCheckDataList.Remove(checkData);
		}

		approveController.RemoveAllianceOn(orderPlayerEnum);

		if (currentAllianceProposer == orderPlayerEnum)
		{
			currentAllianceProposer = PlayerEnum.Player_None;
		}

		if (orderPlayerEnum == GetMyPlayer())
		{
			myAllianceApproveController.FadeOn();
		}
	}

	private async void AllianceApproveResultCheckOn(AllianceApproveRequest allianceApproveRequest)
	{
		var checkData = allianceRequestCheckDataList.FirstOrDefault(data => data.allianceRequestData.orderData.playerEnum == allianceApproveRequest.orderData.playerEnum);

		if (checkData == null)
			return;

		if (allianceApproveRequest.approveOn == false)
		{
			PlayerEnum orderPlayer = allianceApproveRequest.orderData.playerEnum;
			PlayerEnum rejectPlayer = allianceApproveRequest.playerEnum;

			AddAllianceRejectOn(orderPlayer, rejectPlayer);

			allianceRequestCheckDataList.Remove(checkData);

			await UniTask.Delay(500, cancellationToken: source.Token);

			//제안이 거절당해도 카드는 소모한 것으로 한다 — 환불하지 않는다 (AI 알고리즘 수정-260927.pptx, 2026-09-28)

			string resultStr = LocalizeManager.Instance.GetStrData(LocalizeStatus.Game, 40);

			PopupManager.Instance.InGameWarningPopupOn(resultStr);

			//오더 플레이어가 AI이고, 내가 게임 리더라면 재제안 여부를 판단
			if (IsAIOn(orderPlayer) && DataManager.Instance.isOwner)
			{
				List<PlayerEnum> remainingTargetList = checkData.allianceRequestData.allianceDataList
					.Select(data => data.playerEnum)
					.Where(playerEnum => playerEnum != orderPlayer && playerEnum != rejectPlayer)
					.ToList();

				PlayerIconElement orderPlayerIcon = mapController.playerIconController.GetPlayerIcon(orderPlayer);

				int myAllianceCount = (orderPlayerIcon != null ? orderPlayerIcon.connectedCount : 0) +
					remainingTargetList.Sum(playerEnum => mapController.playerIconController.GetPlayerIcon(playerEnum)?.connectedCount ?? 0);

				int topPowerCount = GetTopPowerCount(orderPlayer, remainingTargetList);

				//거절당한 플레이어를 제외해도 세력 1위인 플레이어/동맹보다 세력이 크다면, 그 플레이어를 제외하고 다시 제안
				//카드가 환불되지 않으므로 다시 제안하려면 카드가 남아 있어야 한다 (2026-09-28)
				bool cardOn = DataManager.Instance.GetPlayerData(orderPlayer).sc > 0;

				if (cardOn && remainingTargetList.Count > 0 && myAllianceCount > topPowerCount)
				{
					List<PlayerEnum> retryPlayerEnumList = new List<PlayerEnum>(remainingTargetList);

					retryPlayerEnumList.Insert(0, orderPlayer);

					await AllianceRequestOn(orderPlayer, retryPlayerEnumList);
				}
				else
				{
					//아니면, 자신의 턴이 2번째 돌아올 때까지 기다렸다가 다시 제안
					allianceRetryCooldownDic[orderPlayer] = 2;
				}
			}

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
	/// 제안자의 차례가 올 때마다 진행중인 동맹 제안의 미응답 여부를 체크하고, 두 번째로 차례가 돌아올 때까지 응답이 없다면 거절 처리합니다
	/// </summary>
	private void AllianceTurnPassCheckOn(PlayerEnum currentPlayer)
	{
		List<allianceRequestCheckData> checkDataList = allianceRequestCheckDataList
			.Where(data => data.allianceRequestData.orderData.playerEnum == currentPlayer)
			.ToList();

		foreach (var checkData in checkDataList)
		{
			checkData.turnStartCount++;

			if (checkData.turnStartCount >= 2)
			{
				AllianceTimeOutOn(currentPlayer);
			}
		}

		//AI 대기 목록의 제안도 제안자의 차례가 두 번째로 돌아오면 시간 초과로 버린다 (2026-09-28)
		foreach (var offer in aiAllianceOfferList.Where(data => data.allianceRequest.orderData.playerEnum == currentPlayer).ToList())
		{
			offer.turnStartCount++;

			if (offer.turnStartCount >= 2)
			{
				aiAllianceOfferList.Remove(offer);
			}
		}

		//거절/타임아웃 이후 재제안 대기 중이라면, 자신의 턴이 돌아올 때마다 대기 횟수를 차감
		if (allianceRetryCooldownDic.TryGetValue(currentPlayer, out int cooldownCount))
		{
			cooldownCount--;

			if (cooldownCount <= 0)
			{
				allianceRetryCooldownDic.Remove(currentPlayer);
			}
			else
			{
				allianceRetryCooldownDic[currentPlayer] = cooldownCount;
			}
		}
	}

	/// <summary>
	/// 시간 초과로 동맹 제안을 거절 처리합니다 (미응답 플레이어는 이후 동일 제안자의 재제안 대상에서 제외됩니다)
	/// </summary>
	private void AllianceTimeOutOn(PlayerEnum orderPlayer)
	{
		var checkData = allianceRequestCheckDataList.FirstOrDefault(data => data.allianceRequestData.orderData.playerEnum == orderPlayer);

		if (checkData == null)
			return;

		List<PlayerEnum> approvedPlayerList = checkData.allianceApproveRequestList.Select(data => data.playerEnum).ToList();

		List<PlayerEnum> notRespondPlayerList = checkData.allianceRequestData.allianceDataList
			.Select(data => data.playerEnum)
			.Where(playerEnum => playerEnum != orderPlayer && approvedPlayerList.Contains(playerEnum) == false)
			.ToList();

		foreach (var notRespondPlayer in notRespondPlayerList)
		{
			AddAllianceRejectOn(orderPlayer, notRespondPlayer);
		}

		allianceRequestCheckDataList.Remove(checkData);

		//시간 초과도 거절이다 — 카드는 환불하지 않는다 (2026-09-28)

		//AI 라면, 자신의 턴이 2번째 돌아올 때까지 기다렸다가 다시 제안하도록 대기 상태로 전환
		if (IsAIOn(orderPlayer))
		{
			allianceRetryCooldownDic[orderPlayer] = 2;
		}

		if (orderPlayer == GetMyPlayer())
		{
			myAllianceApproveController.SetResultOn();
		}

		string resultStr = LocalizeManager.Instance.GetStrData(LocalizeStatus.Game, 40);

		PopupManager.Instance.InGameWarningPopupOn(resultStr);
	}

	void AddAllianceRejectOn(PlayerEnum orderPlayer, PlayerEnum rejectPlayer)
	{
		if (!allianceRejectedDic.TryGetValue(orderPlayer, out var rejectPlayerSet))
		{
			rejectPlayerSet = new HashSet<PlayerEnum>();
			allianceRejectedDic[orderPlayer] = rejectPlayerSet;
		}

		rejectPlayerSet.Add(rejectPlayer);
	}

	bool IsAllianceRejectedOn(PlayerEnum orderPlayer, PlayerEnum targetPlayer)
	{
		return allianceRejectedDic.TryGetValue(orderPlayer, out var rejectPlayerSet) && rejectPlayerSet.Contains(targetPlayer);
	}

	/// <summary>
	/// 카드를 가진 플레이어 중 하나만 동맹 제안을 진행하도록 합니다 (중복 제안 방지).
	/// 제안을 진행중인 AI가 없거나, 자기 자신이 제안자이거나, 기존 제안자가 퇴장(제거)되었을 때만 새로 제안자가 될 수 있습니다.
	/// </summary>
	bool IsAllianceProposerAvailableOn(PlayerEnum playerEnum)
	{
		if (currentAllianceProposer == PlayerEnum.Player_None || currentAllianceProposer == playerEnum)
		{
			return true;
		}

		//기존 제안자가 이미 동맹을 맺었다면 역할 반납
		if (DataManager.Instance.IsAlliance(currentAllianceProposer))
		{
			currentAllianceProposer = PlayerEnum.Player_None;
			return true;
		}

		//기존 제안자가 퇴장(제거)되었는지 체크
		bool proposerAliveOn = mapController.playerIconController.GetPlayerIcon(currentAllianceProposer) != null;

		if (proposerAliveOn == false)
		{
			//퇴장한 제안자가 진행중이던 제안 정보를 정리하고, 다른 AI가 역할을 이어받을 수 있도록 반납
			allianceRequestCheckDataList.RemoveAll(data => data.allianceRequestData.orderData.playerEnum == currentAllianceProposer);
			allianceRetryCooldownDic.Remove(currentAllianceProposer);

			currentAllianceProposer = PlayerEnum.Player_None;

			return true;
		}

		return false;
	}

	/// <summary>
	/// 제안자 자신(+제안 대상 후보) 세력을 제외한, 현재 세력 1위인 플레이어/동맹의 연결 영토 합을 반환합니다.
	/// </summary>
	int GetTopPowerCount(PlayerEnum orderPlayer, List<PlayerEnum> allyCandidateList)
	{
		List<PlayerEnum> excludeList = new List<PlayerEnum>(allyCandidateList) { orderPlayer };

		List<PlayerIconElement> checkList = mapController.playerIconController.GetActiveList()
			.Where(data => excludeList.Contains(data.playerEnum) == false)
			.ToList();

		List<PlayerEnum> countedAllianceList = new List<PlayerEnum>();

		int topPowerCount = 0;

		foreach (var playerIcon in checkList)
		{
			if (countedAllianceList.Contains(playerIcon.playerEnum))
				continue;

			int powerCount = playerIcon.connectedCount;

			if (DataManager.Instance.IsAlliance(playerIcon.playerEnum))
			{
				var allianceDataList = DataManager.Instance.GetAllAlliance(playerIcon.playerEnum).allianceDataList;

				powerCount = allianceDataList.Sum(data => mapController.playerIconController.GetPlayerIcon(data.playerEnum)?.connectedCount ?? 0);

				countedAllianceList.AddRange(allianceDataList.Select(data => data.playerEnum));
			}

			topPowerCount = Mathf.Max(topPowerCount, powerCount);
		}

		return topPowerCount;
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

		mapController.inGameBottomController.nonePlayPanel.AllianceClearOn(allianceResultRequest);

		mapController.playerIconController.SetAlliancePlayerIcon(allianceResultRequest);

		mapController.playerIconController.SetBundleKeyuAll();

		approveController.ResetApproveData();

		//동맹 제안이 성사되었다면, 제안자 역할과 재제안 대기 상태를 정리 (다른 AI가 새로 제안자가 될 수 있도록)
		PlayerEnum orderPlayerEnum = allianceResultRequest.orderData.playerEnum;

		if (currentAllianceProposer == orderPlayerEnum)
		{
			currentAllianceProposer = PlayerEnum.Player_None;
		}

		allianceRetryCooldownDic.Remove(orderPlayerEnum);

		RemoveAIAllianceOfferOn(orderPlayerEnum);

		string resultStr = LocalizeManager.Instance.GetStrData(LocalizeStatus.Game, 39);

		PopupManager.Instance.InGameWarningPopupOn(resultStr);

		if (allianceResultRequest.orderData.playerEnum == GetMyPlayer())
		{
			myAllianceApproveController.SetResultOn();
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

		//BetrayOn 이 동맹 목록에서 지우기 전에, 나와 같은 동맹이었는지 먼저 확인한다 (2026-09-19)
		bool myAllianceBetrayOn = false;

		if (allianceBetrayRequest.betrayPlayerEnum != GetMyPlayer())
		{
			AllianceAllData myAllianceAllData = DataManager.Instance.GetAllAlliance(GetMyPlayer());

			if (myAllianceAllData != null)
			{
				myAllianceBetrayOn = myAllianceAllData.allianceDataList
					.Any(data => data.playerEnum == allianceBetrayRequest.betrayPlayerEnum);
			}
		}

		BetrayOn(allianceBetrayRequest.betrayPlayerEnum);

		//내가 배신한것이라면
		if (allianceBetrayRequest.betrayPlayerEnum == GetMyPlayer())
		{
			mapController.inGameBottomController.nonePlayPanel.SkillUseOn();

			int addCoin = allianceBetrayRequest.needBetrayCoin;
			DataManager.Instance.AddCoin(-addCoin);
			DataManager.Instance.betrayPaidCoin += addCoin;
			DataManager.Instance.myBetrayWaitOn = false;
		}
		else if (myAllianceBetrayOn)
		{
			//내 동맹원이 배신했다 — 표시용 집계 + 알림 (기획 시트 : 배신을 당했을 때 알려줘야 함, 2026-09-26)
			DataManager.Instance.betrayReceivedCoin += allianceBetrayRequest.needBetrayCoin;

			PopupManager.Instance.BetrayedPopupOn(allianceBetrayRequest.betrayPlayerEnum);
		}
	}

	private void BetrayOn(PlayerEnum playerEnum)
	{
		var nonePlayPanel = mapController.inGameBottomController.nonePlayPanel;

		AllianceAllData allianceAllData = DataManager.Instance.BetrayOn(playerEnum);
		mapController.playerIconController.SetBetrayPlayerIcon(playerEnum);
		mapController.playerIconController.ResetAlliancePlayerIcon(allianceAllData);

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
	/// <summary>
	/// 다른 앱으로 갔다가 돌아왔을 때. 백그라운드에서는 소켓이 끊기고 턴도 진행되지 않으므로
	/// 3초 이상 벗어났다면 그 판에서 퇴장시킨다 (나는 GameOut 으로 다른 사람에게 AI 로 바뀐다)
	/// </summary>
	private void OnApplicationPause(bool pauseStatus)
	{
		if (pauseStatus)
		{
			pausedOn = true;
			pauseStartTime = DateTime.UtcNow;
			return;
		}

		if (pausedOn == false)
			return;

		pausedOn = false;

		if ((DateTime.UtcNow - pauseStartTime).TotalSeconds < BackgroundOutSeconds)
			return;

		//이미 결과창이 뜬 판이면 결과창의 버튼으로 나가게 둔다
		if (gameEndOn)
			return;

		BackgroundOutOn().Forget();
	}

	private async UniTask BackgroundOutOn()
	{
		if (backgroundOutOn)
			return;

		backgroundOutOn = true;

		//ServerManager 도 같은 콜백에서 소켓을 다시 연결하므로 한 프레임 기다린다
		await UniTask.Yield();

		DataManager dataManager = DataManager.Instance;

		//동맹이 있으면 배신 패널티를 내고 나간다 (포기 팝업의 '홈으로' 와 같은 처리)
		if (dataManager.GetMyAllianceData() != null)
		{
			int needBetrayCoin = dataManager.GetNeedBetrayCoin();

			await dataManager.MyBetrayOn(mapController.playerIconController);

			//재연결된 소켓으로는 내 배신 패킷이 돌아오지 않을 수 있다 - 그러면 패널티를 여기서 직접 차감
			if (dataManager.myBetrayWaitOn)
			{
				dataManager.myBetrayWaitOn = false;
				dataManager.AddCoin(-needBetrayCoin);
				dataManager.betrayPaidCoin += needBetrayCoin;
			}
		}

		dataManager.mainToastText = LocalizeManager.Instance.GetStrData(LocalizeStatus.Game, 64);

		//GoMainOn 안에서 GameOutRequest 전송 + 메인 씬 이동
		GoMainOn();
	}

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

	#region AI 동맹 수락 / 제안 규칙 (AI 알고리즘 수정-260927.pptx, 2026-09-28)

	/// <summary>
	/// AI 동맹 수락 알고리즘. AI 턴 시작 때(주사위 공격 전) 한 바퀴만 돈다.
	/// 1. AI 제안이 있으면 그 제안 수락 (나머지는 모두 거절)
	/// 2. 내 몫이 '1/N 몫'(유저 동맹 팝업 기본 배분) 미만인 제안 모두 거절
	/// 3. 남은 제안이 하나면 수락
	/// 4. 여럿이면 : 동맹 성립 시 세력 1위가 되는 제안과 안 되는 제안이 섞여 있으면 안 되는 쪽을 모두 거절,
	///    아니면(전부 1위 / 전부 1위 아님) 내 몫이 가장 적은 제안 하나만 거절. 나머지는 다음 턴에 다시 본다
	/// </summary>
	async UniTask AIAllianceApproveCheckOn(PlayerEnum aiPlayer)
	{
		if (DataManager.Instance.isOwner == false)
			return;

		//제안자가 퇴장했으면 버린다
		aiAllianceOfferList.RemoveAll(data => mapController.playerIconController.GetPlayerIcon(data.allianceRequest.orderData.playerEnum) == null);

		List<AIAllianceOfferData> offerList = aiAllianceOfferList.Where(data => data.aiPlayer == aiPlayer).ToList();

		if (offerList.Count == 0)
			return;

		//1. AI 플레이어의 제안이 있는가 → 수락
		AIAllianceOfferData aiOffer = offerList.FirstOrDefault(data => IsAIOn(data.allianceRequest.orderData.playerEnum));

		if (aiOffer != null)
		{
			await AIAllianceOfferAcceptOn(aiOffer, offerList);
			return;
		}

		//2. 내 몫이 1/N 몫 이상이 아닌 제안 모두 거절
		List<AIAllianceOfferData> remainList = new List<AIAllianceOfferData>();

		foreach (var offer in offerList)
		{
			if (IsAIAllianceShareEnoughOn(offer))
			{
				remainList.Add(offer);
			}
			else
			{
				await AIAllianceOfferReplyOn(offer, false);
			}
		}

		if (remainList.Count == 0)
			return;

		//3. 복수의 제안이 아니면 수락
		if (remainList.Count == 1)
		{
			await AIAllianceOfferAcceptOn(remainList[0], remainList);
			return;
		}

		//4. 복수의 제안
		List<AIAllianceOfferData> topList = remainList.Where(data => IsAllianceTopPowerOn(data.allianceRequest)).ToList();

		if (topList.Count > 0 && topList.Count < remainList.Count)
		{
			//세력 1위가 되지 '않는' 제안을 모두 거절
			foreach (var offer in remainList.Except(topList).ToList())
			{
				await AIAllianceOfferReplyOn(offer, false);
			}
		}
		else
		{
			//자신의 몫이 가장 적은 제안 '하나만' 거절
			AIAllianceOfferData minOffer = remainList.OrderBy(data => GetAIAllianceShare(data)).First();

			await AIAllianceOfferReplyOn(minOffer, false);
		}
	}

	/// <summary> 수락 시 나머지 제안은 모두 거절 </summary>
	async UniTask AIAllianceOfferAcceptOn(AIAllianceOfferData acceptOffer, List<AIAllianceOfferData> offerList)
	{
		await AIAllianceOfferReplyOn(acceptOffer, true);

		foreach (var offer in offerList)
		{
			if (offer == acceptOffer || aiAllianceOfferList.Contains(offer) == false)
				continue;

			await AIAllianceOfferReplyOn(offer, false);
		}
	}

	async UniTask AIAllianceOfferReplyOn(AIAllianceOfferData offer, bool approveOn)
	{
		aiAllianceOfferList.Remove(offer);

		await UniTask.Delay(200, cancellationToken: source.Token);

		AllianceApproveRequest request = new AllianceApproveRequest()
		{
			orderData = offer.allianceRequest.orderData,
			playerEnum = offer.aiPlayer,
			approveOn = approveOn,
		};

		ServerManager.Instance.SendMessageOn(request);
	}

	void RemoveAIAllianceOfferOn(PlayerEnum orderPlayer)
	{
		aiAllianceOfferList.RemoveAll(data => data.allianceRequest.orderData.playerEnum == orderPlayer);
	}

	int GetAIAllianceShare(AIAllianceOfferData offer)
	{
		return offer.allianceRequest.allianceDataList.FirstOrDefault(data => data.playerEnum == offer.aiPlayer)?.coinCount ?? 0;
	}

	/// <summary>
	/// 내 몫이 '1/N 몫'(유저가 동맹 제안을 설정할 때 기본으로 나오는 배분) 이상인가.
	/// 이미 다른 동맹에 들어가 있으면 기존 몫의 1.5배 이상이어야 한다 (기존 규칙 유지 — pptx 에는 없음)
	/// </summary>
	bool IsAIAllianceShareEnoughOn(AIAllianceOfferData offer)
	{
		AllianceRequest allianceRequest = offer.allianceRequest;

		List<PlayerEnum> playerEnumList = allianceRequest.allianceDataList.Select(data => data.playerEnum).ToList();

		List<AllianceData> defaultDataList = DataManager.Instance.GetAllianceDefaultData(allianceRequest.orderData.playerEnum, playerEnumList, mapController.playerIconController);

		float needCoin = defaultDataList.FirstOrDefault(data => data.playerEnum == offer.aiPlayer)?.coinCount ?? 0;

		var allReadyData = DataManager.Instance.GetAllianceData(offer.aiPlayer);

		if (allReadyData != null)
		{
			needCoin = Mathf.Max(needCoin, allReadyData.coinCount * 1.5f);
		}

		return GetAIAllianceShare(offer) >= needCoin;
	}

	/// <summary>
	/// 동맹 성립 시 세력 1위가 되는가 — 동맹원 연결 땅 합이 나머지 플레이어/동맹 누구보다 크다.
	/// 다른 동맹에서 빠져나오는 동맹원은 그 동맹의 세력에서 뺀다
	/// </summary>
	bool IsAllianceTopPowerOn(AllianceRequest allianceRequest)
	{
		PlayerIconController playerIconController = mapController.playerIconController;

		List<PlayerEnum> memberList = allianceRequest.allianceDataList.Select(data => data.playerEnum).ToList();

		int myPower = memberList.Sum(playerEnum => playerIconController.GetPlayerIcon(playerEnum)?.connectedCount ?? 0);

		List<PlayerEnum> countedList = new List<PlayerEnum>(memberList);

		int topPower = 0;

		foreach (var playerIcon in playerIconController.GetActiveList())
		{
			if (countedList.Contains(playerIcon.playerEnum))
				continue;

			int power = playerIcon.connectedCount;

			countedList.Add(playerIcon.playerEnum);

			if (DataManager.Instance.IsAlliance(playerIcon.playerEnum))
			{
				var otherMemberList = DataManager.Instance.GetAllAlliance(playerIcon.playerEnum).allianceDataList
					.Select(data => data.playerEnum)
					.Where(playerEnum => memberList.Contains(playerEnum) == false)
					.ToList();

				power = otherMemberList.Sum(playerEnum => playerIconController.GetPlayerIcon(playerEnum)?.connectedCount ?? 0);

				countedList.AddRange(otherMemberList);
			}

			topPower = Mathf.Max(topPower, power);
		}

		return myPower > topPower;
	}

	/// <summary>
	/// 카드를 가진 AI 중 '가장 약한'(연결 땅 수가 가장 적은) AI 인가. 같으면 플레이어 순서가 빠른 쪽.
	/// 동맹 중이거나, 재제안 대기 중이거나, 제안할 대상이 없는 AI 는 후보에서 뺀다
	/// </summary>
	bool IsWeakestAIProposerOn(PlayerEnum playerEnum)
	{
		PlayerIconController playerIconController = mapController.playerIconController;

		PlayerIconElement weakest = playerIconController.GetActiveList()
			.Where(data => IsAIOn(data.playerEnum) &&
						   DataManager.Instance.GetPlayerData(data.playerEnum).sc > 0 &&
						   DataManager.Instance.IsAlliance(data.playerEnum) == false &&
						   allianceRetryCooldownDic.ContainsKey(data.playerEnum) == false &&
						   playerIconController.CheckAIAllianceOn(data.playerEnum).Any(target => IsAllianceRejectedOn(data.playerEnum, target.playerEnum) == false))
			.OrderBy(data => data.connectedCount)
			.ThenBy(data => (int)data.playerEnum)
			.FirstOrDefault();

		return weakest != null && weakest.playerEnum == playerEnum;
	}

	#endregion

	protected override int GetCoin(GameResultEnum gameResultEnum)
	{
		int coinCount = 0;

		if (gameResultEnum == GameResultEnum.Win)
		{
			int resultPlayCount = mapController.playerIconController.GetActiveList().Count;

			coinCount = DataManager.Instance.GetMultiRewardCoin(resultPlayCount);

			//동맹으로 이겼으면 전체 보상이 아니라 동맹에서 정한 내 몫만 받는다 (2026-09-28)
			//메뉴 예상 보상(OptionPopup.SetRewardText)과 같은 기준
			AllianceData myAllianceData = DataManager.Instance.GetMyAllianceData();
			if (myAllianceData != null)
				coinCount = myAllianceData.coinCount;
		}

		return coinCount;
	}

}

public class AIAllianceOfferData
{
	public PlayerEnum aiPlayer;
	public AllianceRequest allianceRequest;
	public int turnStartCount = 0;
}

public class allianceRequestCheckData
{
	public AllianceRequest allianceRequestData;
	public List<AllianceApproveRequest> allianceApproveRequestList = new List<AllianceApproveRequest>();
	public int turnStartCount = 0;
}