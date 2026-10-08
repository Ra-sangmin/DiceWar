using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UniRx;
using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using System.Threading;
using static Unity.VisualScripting.Member;
using UnityEngine.Assertions.Must;
using static GameResultPopup;

[RequireComponent(typeof(PanelRenderer))]
public class InGameControllerBase : MonoBehaviour
{
	public MapController mapController;
	protected UnityEngine.UIElements.Button endTurnBtn;

	protected PanelUI panelUI;

	/// <summary> UI Toolkit 루트 (인게임 화면 본체). PanelRenderer 가 준비되기 전에는 null </summary>
	protected VisualElement UIRoot => panelUI.Root;

	public InGameAreaEditor areaEditor;

	protected bool gameEndOn = false;

	protected CancellationTokenSource source = new CancellationTokenSource();

	/// <summary> 카운트다운 승리 (싱글 Hard / 멀티, 2026-10-08) </summary>
	protected CountdownManager countdown;

	/// <summary> 타이머 자리의 카운트다운 남은 턴 </summary>
	private Label countdownLabel;

	private bool countdownCheckPosted = false;

	protected virtual void Awake()
	{
		//PanelRenderer 는 UI 를 비동기로 만든다. 화면 구성은 준비된 뒤 StartReady 에서 (2026-09-29)
		panelUI = new PanelUI(this, null);

		countdown = new CountdownManager(() => mapController != null ? mapController.playerIconController : null);
		countdown.popupOn = CountdownPopupOn;
		countdown.countdownWinOn = CountdownWinOn;
		countdown.viewChangedOn = UpdateCountdownView;

		SetEvent();
	}

	protected virtual void OnDestroy()
	{
		panelUI?.Dispose();

		countdown?.Dispose();
	}

	/// <summary> UI Toolkit HUD 버튼 이벤트 연결 (기존 상단 BtnPanel 의 uGUI 버튼 대체) </summary>
	protected virtual void SetHudEvent()
	{
		UnityEngine.UIElements.Button tutorialBtn = UIRoot.Q<UnityEngine.UIElements.Button>("tutorial-btn");
		UnityEngine.UIElements.Button optionBtn = UIRoot.Q<UnityEngine.UIElements.Button>("option-btn");

		endTurnBtn = UIRoot.Q<UnityEngine.UIElements.Button>("end-turn-btn");

		if (endTurnBtn != null)
		{
			endTurnBtn.clicked += () => { PlayClickSe(); EndTurnBtnClickOn(); };
		}

		if (tutorialBtn != null)
		{
			tutorialBtn.clicked += () => { PlayClickSe(); TutorialPopupOn(); };
		}

		if (optionBtn != null)
		{
			optionBtn.clicked += () => { PlayClickSe(); OptionPopupOn(); };
		}

		//맵 선택 바 (원본 yesBtn / ChangeBtn)
		MapSelectPanel mapSelectPanel = mapController.inGameBottomController.mapSelectPanel;

		if (mapSelectPanel != null)
		{
			mapSelectPanel.startClickOn = GameStartOn;
			mapSelectPanel.changeClickOn = NewGameCheckOn;
		}
	}

	/// <summary> 기존 ButtonSound 컴포넌트 역할 </summary>
	protected void PlayClickSe()
	{
		SoundManager.Instance.PlaySe(SeEnum.Yes);
	}

	protected virtual void SetEvent()
	{
		mapController.gameWinOn = GameWinOn;
		mapController.gameLoseOn = GameLoseOn;
		//mapController.turnOffOn = TurnOffOn;
		mapController.selectAreaOn = SelectAreaOn;
		mapController.SetEvent();

		//땅/동맹이 바뀌어 세력이 다시 계산되면 카운트다운 시작 판정 (2026-10-08)
		mapController.playerIconController.bundleKeyUpdatedOn = OnBundleKeyUpdated;
	}

	protected virtual void Start()
	{
		panelUI.Run(StartReady);
	}

	/// <summary> 예전 Start 본문. 인게임 UI 가 준비된 뒤 한 번 실행된다 (PanelRenderer, 2026-09-29) </summary>
	protected virtual void StartReady()
	{
		SoundManager.Instance.PlayBGM(BGMEnum.Game);

		PopupManager.Instance.SetPopupParant();

		mapController.SetMapLayer(UIRoot.Q<VisualElement>("map-layer"));

		mapController.InitView(UIRoot);

		mapController.playerIconController.SetIconPanel(UIRoot.Q<VisualElement>("player-icon-panel"));

		mapController.inGameBottomController.InitView(UIRoot);

		SetHudEvent();

		countdownLabel = UIRoot.Q<Label>("timer-countdown");
		UpdateCountdownView();

		//개발용 치트 패널 (Game_Multi 에만 있다). UI Toolkit 요소를 먼저 물려준 뒤 데이터를 넣는다
		if (areaEditor != null)
		{
			areaEditor.InitView(UIRoot);
			areaEditor.SetData(this);
		}

		mapController.CreateMapInit();

		endTurnBtn.SetEnabled(false);

		if (DataManager.Instance.mapSelectionOn == false)
		{
			GameStartOn();
		}
	}

	void GameWinOn()
	{
		GameEndOn(GameResultEnum.Win);
	}

	void GameLoseOn()
	{
		GameEndOn(GameResultEnum.Lose);
	}

	void GameEndOn(GameResultEnum gameResultEnum)
	{
		if (gameEndOn)
			return;

		gameEndOn = true;

		DataManager.Instance.gameStart.SetValueAndForceNotify(false);

		TokenSourceInit();

		PopupManager.Instance.GameResultPopupOn(this, gameResultEnum, GetCoin(gameResultEnum));
	}

	public void TokenSourceInit()
	{
		source.Cancel();
		source = new CancellationTokenSource();
	}

	protected virtual int GetCoin(GameResultEnum gameResultEnum) { return 0; }

	protected virtual void GameStartOn()
	{
		gameEndOn = false;

		countdown.Clear();

		DataManager.Instance.gameStart.SetValueAndForceNotify(true);

		mapController.inGameBottomController.SetStatus(1);

		//멀티는 첫 TurnRequest 를 받은 뒤 판정한다 (InGameControllerMulti.GameStartOn)
		firstTurnCheckOn = DataManager.Instance.isMultiOn == false;

		TurnCheck();

		DataManager.Instance.leaveEarlyPopupReadyOn = true;
	}

	public void GoMainOn()
	{
		//판을 떠나므로 연패 보너스도 초기화
		DataManager.Instance.loseCount = 0;

		DataManager.Instance.GameDataClearOn();

		if (DataManager.Instance.isMultiOn)
		{
			ServerManager.Instance.GameOutRequestOn();
		}

		SceneManager.LoadScene("Main");
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

	protected virtual void Update()
	{
		//if (DataManager.Instance.isMultiOn == false)
		//{
		//TurnCheckDelayCheck();
		//}   

		if (Input.GetKeyDown(KeyCode.Alpha1))
		{
			//PopupManager.Instance.LandTradeApprovePopupOn();

			//yourTurnPopup.MyTurnActiveOn();

			//LeaveEarlyPopupOn();
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


	/// <summary> 게임이 시작되고 아직 첫 차례를 처리하지 않았다 (Your Color 토스트용) </summary>
	protected bool firstTurnCheckOn = false;

	protected virtual async void TurnCheck()
	{
		DataManager.Instance.playOn = true;

		mapController.playerIconController.SetIconTurnEffect();

		if (gameEndOn)
			return;

		//카운트다운 : holder 의 차례면 남은 턴을 줄이고, 세 번째로 돌아왔으면 판정 (2026-10-08)
		if (countdown.TurnStartOn(DataManager.Instance.currentPlayer))
			return;

		bool myTurn = DataManager.Instance.IsMyTurn();

		if (areaEditor != null)
		{
			areaEditor.ActiveOn(myTurn);
		}

		//첫 차례가 내가 아니면 내 색을 먼저 알려준다 (기획 시트 : 자기 색 구별, 2026-09-26)
		if (firstTurnCheckOn)
		{
			firstTurnCheckOn = false;

			if (myTurn == false)
			{
				PopupManager.Instance.YourColorPopupOn();
			}
		}

		if (myTurn)
		{
			PopupManager.Instance.YourTurnPopupOn();

			mapController.SelectClearOn();

			MyTurnPlayOn();
		}
		else
		{
			await OtherTurnPlayOn();
		}

		//SetEndTurnBtn();
	}

	protected virtual void MyTurnPlayOn(){}

	protected virtual async UniTask OtherTurnPlayOn() 
	{
		//SetEndTurnBtn();
		mapController.playerIconController.SetIconTurnEffect();

		PlayerEnum currentPlayer = DataManager.Instance.currentPlayer;
		PlayerIconElement playerIcon = mapController.playerIconController.GetPlayerIcon(currentPlayer);

		if (playerIcon != null)
		{
			try
			{
				await AIPlayOn(currentPlayer, source);
			}
			catch (OperationCanceledException)
			{
				//Debug.LogWarning("cancleOn");
			}
			
		}
		else
		{
			OtherPlayerNotOn();
		}
	}

	protected virtual void OtherPlayerNotOn(){}

	protected PlayerEnum GetNextPlayer()
	{
		PlayerEnum currentPlayer = DataManager.Instance.currentPlayer;

		PlayerEnum nextPlayer = currentPlayer;

		while (true)
		{
			nextPlayer = (PlayerEnum)(((int)nextPlayer) + 1);

			if ((int)nextPlayer == DataManager.Instance.num_player)
			{
				nextPlayer = PlayerEnum.Player_0;
			}

			PlayerIconElement playerIcon = mapController.playerIconController.GetPlayerIcon(nextPlayer);

			if (playerIcon != null)
			{
				break;
			}
		}
		return nextPlayer;
	}

	protected virtual async UniTask AIPlayOn(PlayerEnum currentPlayer , CancellationTokenSource source)
	{
		await UniTask.Delay(100, cancellationToken: source.Token);
	}

	public void SetEndTurnBtn()
	{
		bool gameStart = DataManager.Instance.gameStart.Value;

		endTurnBtn.SetEnabled(gameStart && DataManager.Instance.IsMyTurn());

		//Debug.LogWarning(DataManager.Instance.IsMyTurn());

	}

	public void EndTurnBtnClickOn()
	{
		EndTurnEventOn();
	}

	protected virtual void EndTurnEventOn()
	{
		endTurnBtn.SetEnabled(false);

		mapController.SelectClearOn();
		TokenSourceInit();
	}

	public void TurnOffOn()
	{
		DataManager.Instance.TurnOffOn();
		mapController.playerIconController.SetIconTurnEffect();
		TurnCheck();
	}

	public void NewGameCheckOn()
	{
		if (DataManager.Instance.CheckNewGame())
		{
			NewGameOn();
		}
	}

	public void NewGameOn()
	{
		NewGameProcessAsync().Forget();
	}
	private async UniTaskVoid NewGameProcessAsync()
	{
		TokenSourceInit();

		DataManager.Instance.gameStart.SetValueAndForceNotify(false);
		DataManager.Instance.playOn = false;
		DataManager.Instance.AddCoin(-DataManager.Instance.GetNeedCoin());

		//다른 판으로 넘어가므로 연패 보너스는 초기화 (같은 판 '다시하기' 는 유지)
		DataManager.Instance.loseCount = 0;

		DataManager.Instance.GameDataClearOn();

		if (DataManager.Instance.isMultiOn)
		{
			// 1. 매너 있게 종료 패킷 발송
			ServerManager.Instance.GameOutRequestOn();

			// 2. 패킷이 서버에 도착할 시간 아주 잠깐(0.5초) 대기
			await UniTask.Delay(100);

			// 3. 🚀 [핵심] 낡은 통로를 아예 박살내고 버립니다. (재연결은 여기서 안 함!)
			if (ServerManager.Instance != null)
			{
				ServerManager.Instance.DisconnectServer();
				ServerManager.Instance.roomDataIndex = -1;
			}

			// 4. 기존 백그라운드 스레드가 확실히 죽고 서버가 청소할 시간 1초 대기
			await UniTask.Delay(300);

			// 5. 로딩 씬 진입 -> 여기서 GameReady를 쏠 때 알아서 '새 통로'가 뚫립니다!
			SceneManager.LoadScene("Loading");
		}
		else
		{
			mapController.NewGameOn();
			if (DataManager.Instance.mapSelectionOn == false)
			{
				GameStartOn();
			}
		}
	}

	public void ReStartOn()
	{
		RestartOnPlay().Forget();
	}

	public async UniTask RestartOnPlay() 
	{
		TokenSourceInit();

		await UniTask.Delay(100);

		mapController.ReStartOn();

		DataManager.Instance.playOn = false;

		GameStartOn();
	}

	public void OptionPopupOn()
	{
		PopupManager.Instance.OptionPopupOn(this);
	}

	public void LeaveEarlyPopupOn()
	{
		PopupManager.Instance.LeaveEarlyPopupOn(this , GetCoin(GameResultEnum.LeaveEarly));
	}

	/// <summary> 조기 종료 시 받는 코인. GiveUpPopup 의 '예상 보상' 표시용이다. (2026-09-19) </summary>
	public int GetLeaveEarlyCoin()
	{
		return GetCoin(GameResultEnum.LeaveEarly);
	}

	public void LeaveEarlyFinishOn(bool finishOn)
	{
		DataManager.Instance.leaveEarlyPopupOpenOn = false;

		if (finishOn)
		{
			GameEndOn(GameResultEnum.LeaveEarly);
		}
	}

	public void TutorialPopupOn()
	{
		PopupManager.Instance.TutorialPopupOn();
	}

	public bool IsAIOn(PlayerEnum playerEnum)
	{
		return DataManager.Instance.GetPlayerData(playerEnum).isAI;
	}
	public PlayerEnum GetMyPlayer()
	{
		return DataManager.Instance.playerData.pe;
	}


	#region 카운트다운 (2026-10-08)

	/// <summary> 세력이 다시 계산될 때마다 오지만, 한 프레임에 몇 번 와도 판정은 다음 프레임에 한 번만 </summary>
	void OnBundleKeyUpdated()
	{
		if (countdownCheckPosted)
			return;

		countdownCheckPosted = true;

		UniTask.Post(() =>
		{
			countdownCheckPosted = false;

			if (this == null || gameEndOn || DataManager.Instance.gameStart.Value == false)
				return;

			countdown.CheckStart();
		});
	}

	void CountdownPopupOn(PlayerEnum holder, string text, bool turnStartOn)
	{
		//차례 시작 때는 Your Turn / Your Color 토스트와 같은 자리라 그게 사라질 즈음 띄운다
		float delay = turnStartOn ? 1.6f : 0f;

		PopupManager.Instance.CountdownPopupOn(holder, text, delay);
	}

	void CountdownWinOn(PlayerEnum holder)
	{
		//holder 와 같은 편(멀티 : 동맹원, 땅이 없어 관전 중인 동맹원 포함)이면 승리
		bool myWinOn = CountdownManager.GetGroupMembers(holder).Contains(GetMyPlayer());

		if (myWinOn)
		{
			GameWinOn();
		}
		else
		{
			GameLoseOn();
		}
	}

	/// <summary> 타이머 자리에 남은 턴 수. 멀티에서 내 차례면 타이머가 보이도록 숨긴다 </summary>
	protected void UpdateCountdownView()
	{
		if (countdownLabel == null)
			return;

		int remainTurns = countdown != null ? countdown.GetDisplayRemainTurns() : 0;

		bool showOn = remainTurns > 0 &&
					  gameEndOn == false &&
					  (DataManager.Instance.isMultiOn == false || DataManager.Instance.IsMyTurn() == false);

		countdownLabel.style.display = showOn ? DisplayStyle.Flex : DisplayStyle.None;

		if (showOn)
		{
			countdownLabel.text = remainTurns.ToString();
		}
	}

	#endregion
}