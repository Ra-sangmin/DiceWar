using System;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/// <summary>
/// 게임 결과 팝업. (uGUI Canvas → UI Toolkit 이식)
/// 원본의 PlayCoinBtn 은 Main 씬 PlaySetPopup 프리팹과 공유하므로 건드리지 않고,
/// 코인 체크 로직만 이 클래스 안으로 옮겼다 (맵 선택 바와 같은 방식).
/// </summary>
[RequireComponent(typeof(PanelRenderer))]
public class GameResultPopup : MonoBehaviour
{
	[SerializeField] RewardedAdsButton rewardedAdsButton;

	private PanelUI panelUI;

	private Label titleLabel;
	private Label coinLabel;
	private Label goMainLabel;
	private Label playLabel;
	private Label adLabel;
	private Label needCoinText;
	private VisualElement needCoinPanel;

	//멀티 전용 보상 내역 (2026-09-19)
	private Label rewardTitleLabel;
	private Label rewardTextLabel;
	private VisualElement rewardCoins;
	private VisualElement loseCoin;
	private readonly VisualElement[] rows = new VisualElement[3];
	private readonly Label[] rowLabels = new Label[3];
	private readonly Label[] rowValues = new Label[3];
	private bool multiOn;

	//싱글 전용 시안 (2026-09-20)
	private Label singleRewardTitle;
	private Label singleRewardText;
	private Label singleHomeLabel;
	private Label singleRetryLabel;
	private Label singlePlayLabel;
	private Label singleAdLabel;
	private Label singleNeedCoinText;
	private Label retryRewardLabel;
	private VisualElement singleCoins;
	private VisualElement singleLoseCoin;
	private VisualElement singleNeedCoinPanel;
	private UnityEngine.UIElements.Button singleAdBtn;
	private UnityEngine.UIElements.Button singlePlayBtn;

	private UnityEngine.UIElements.Button adBtn;
	private UnityEngine.UIElements.Button playBtn;

	private InGameControllerBase inGameController;
	private GameResultEnum gameResultEnum;

	public enum GameResultEnum
	{
		Win,
		Lose,
		GiveUp,
		LeaveEarly
	}

	void Awake()
	{
		panelUI = new PanelUI(this, OnUIReady);
	}

	/// <summary> PanelRenderer 의 UI 가 준비되면 한 번 호출된다 (예전 Awake 의 요소 찾기 / 이벤트 등록, 2026-09-29) </summary>
	private void OnUIReady(VisualElement root)
	{
		titleLabel = root.Q<Label>("game-result-title");
		coinLabel = root.Q<Label>("game-result-coin-text");
		goMainLabel = root.Q<Label>("game-result-go-main-label");
		playLabel = root.Q<Label>("game-result-play-label");
		adLabel = root.Q<Label>("game-result-ad-label");
		needCoinText = root.Q<Label>("game-result-need-coin-text");
		needCoinPanel = root.Q<VisualElement>("game-result-need-coin-panel");

		adBtn = root.Q<UnityEngine.UIElements.Button>("game-result-ad-btn");
		playBtn = root.Q<UnityEngine.UIElements.Button>("game-result-play-btn");

		rewardTitleLabel = root.Q<Label>("game-result-reward-title");
		rewardTextLabel = root.Q<Label>("game-result-reward-text");
		rewardCoins = root.Q<VisualElement>("game-result-reward-coins");
		loseCoin = root.Q<VisualElement>("game-result-lose-coin");

		singleRewardTitle = root.Q<Label>("game-result-single-reward-title");
		singleRewardText = root.Q<Label>("game-result-single-reward-text");
		singleHomeLabel = root.Q<Label>("game-result-single-home-label");
		singleRetryLabel = root.Q<Label>("game-result-single-retry-label");
		singlePlayLabel = root.Q<Label>("game-result-single-play-label");
		singleAdLabel = root.Q<Label>("game-result-single-ad-label");
		singleNeedCoinText = root.Q<Label>("game-result-single-need-coin-text");
		retryRewardLabel = root.Q<Label>("game-result-retry-reward");
		singleCoins = root.Q<VisualElement>("game-result-single-coins");
		singleLoseCoin = root.Q<VisualElement>("game-result-single-lose-coin");
		singleNeedCoinPanel = root.Q<VisualElement>("game-result-single-need-coin-panel");
		singleAdBtn = root.Q<UnityEngine.UIElements.Button>("game-result-single-ad-btn");
		singlePlayBtn = root.Q<UnityEngine.UIElements.Button>("game-result-single-play-btn");

		for (int i = 0; i < rows.Length; i++)
		{
			rows[i] = root.Q<VisualElement>("game-result-row-" + (i + 1));
			rowLabels[i] = root.Q<Label>("game-result-row" + (i + 1) + "-label");
			rowValues[i] = root.Q<Label>("game-result-row" + (i + 1) + "-value");
		}

		//멀티는 시안이 따로 있다 (USS 의 .game-result--multi)
		multiOn = DataManager.Instance.isMultiOn;

		VisualElement resultRoot = root.Q<VisualElement>("game-result-root");

		if (resultRoot != null)
		{
			resultRoot.AddToClassList(multiOn ? "game-result--multi" : "game-result--single");
		}

		UnityEngine.UIElements.Button goMainBtn = root.Q<UnityEngine.UIElements.Button>("game-result-go-main-btn");

		if (goMainBtn != null)
		{
			goMainBtn.clicked += () => { PlayClickSe(); GoMainBtnClickOn(); };
		}

		if (playBtn != null)
		{
			playBtn.clicked += () => { PlayClickSe(); NewGameBtnClickOn(); };
		}

		//싱글 시안 버튼 3개
		UnityEngine.UIElements.Button singleHomeBtn = root.Q<UnityEngine.UIElements.Button>("game-result-single-home-btn");
		UnityEngine.UIElements.Button singleRetryBtn = root.Q<UnityEngine.UIElements.Button>("game-result-single-retry-btn");
		UnityEngine.UIElements.Button closeBtn = root.Q<UnityEngine.UIElements.Button>("game-result-close-btn");

		if (singleHomeBtn != null) singleHomeBtn.clicked += () => { PlayClickSe(); GoMainBtnClickOn(); };
		if (singleRetryBtn != null) singleRetryBtn.clicked += () => { PlayClickSe(); RetryBtnClickOn(); };
		if (singlePlayBtn != null) singlePlayBtn.clicked += () => { PlayClickSe(); NewGameBtnClickOn(); };
		if (closeBtn != null) closeBtn.clicked += () => { PlayClickSe(); CloseBtnClickOn(); };

		if (rewardedAdsButton != null)
		{
			//싱글은 시안의 새로하기 자리(광고 버튼)가 따로 있다
			rewardedAdsButton.InitView(multiOn ? adBtn : singleAdBtn);
			AdsManager.Instance.SetRewardedAdsButton(rewardedAdsButton);
		}

		LocalizeTextSet();
	}

	private void OnDestroy()
	{
		panelUI?.Dispose();
	}

	void Start()
	{
		//코인 구독은 UI 가 준비된 뒤에 (PanelRenderer, 2026-09-29)
		panelUI.Run(StartReady);
	}

	void StartReady()
	{
		if (DataManager.Instance.userData == null)
		{
			DataManager.Instance.userData = new UserData();
		}

		DataManager.Instance.userData.myCoin
			.Subscribe(_ => SetNeedCoinCheck())
			.AddTo(gameObject);
	}

	void PlayClickSe()
	{
		SoundManager.Instance.PlaySe(SeEnum.Yes);
	}

	void LocalizeTextSet()
	{
		LocalizeManager localize = LocalizeManager.Instance;

		if (goMainLabel != null) goMainLabel.text = localize.GetStrData(LocalizeStatus.Game, 13);
		//멀티는 시안대로 '새로하기', 싱글은 기존 '한번 더'
		if (playLabel != null) playLabel.text = localize.GetStrData(LocalizeStatus.Game, multiOn ? 8 : 15);
		if (adLabel != null) adLabel.text = localize.GetStrData(LocalizeStatus.Common, 2);

		if (multiOn == false)
		{
			//싱글 시안 : 홈으로 / 다시하기 / 새로하기
			if (singleHomeLabel != null) singleHomeLabel.text = localize.GetStrData(LocalizeStatus.Game, 13);
			if (singleRetryLabel != null) singleRetryLabel.text = localize.GetStrData(LocalizeStatus.Game, 7);
			if (singlePlayLabel != null) singlePlayLabel.text = localize.GetStrData(LocalizeStatus.Game, 8);
			if (singleAdLabel != null) singleAdLabel.text = localize.GetStrData(LocalizeStatus.Common, 2);
			if (singleRewardTitle != null) singleRewardTitle.text = localize.GetStrData(LocalizeStatus.Game, 55);

			return;
		}

		if (rewardTitleLabel != null) rewardTitleLabel.text = localize.GetStrData(LocalizeStatus.Game, 55);

		int[] rowKeys = { 16, 50, 51 };

		for (int i = 0; i < rowLabels.Length; i++)
		{
			if (rowLabels[i] != null) rowLabels[i].text = localize.GetStrData(LocalizeStatus.Game, rowKeys[i]);
		}
	}

	/// <summary> 원본 PlayCoinBtn.SetNeedCoinCheck 과 동일한 로직 </summary>
	void SetNeedCoinCheck()
	{
		//멀티의 '새로하기' 는 코인도 광고도 쓰지 않는다 (로딩 씬 재진입)
		if (multiOn)
			return;

		int needCoin = DataManager.Instance.GetNeedCoin();

		bool needCoinOn = DataManager.Instance.userData.myCoin.Value < needCoin;

		//광고 버튼은 새로하기 버튼 바로 뒤에 겹쳐 있어서, 안 쓸 때는 숨겨야 테두리로 안 비친다
		if (singlePlayBtn != null)
		{
			singlePlayBtn.style.display = needCoinOn ? DisplayStyle.None : DisplayStyle.Flex;
		}

		if (singleAdBtn != null)
		{
			singleAdBtn.style.display = needCoinOn ? DisplayStyle.Flex : DisplayStyle.None;
		}

		bool coinTextOn = DataManager.Instance.aiLevel != AILevel.Easy;

		if (singleNeedCoinPanel != null)
		{
			singleNeedCoinPanel.style.display = coinTextOn ? DisplayStyle.Flex : DisplayStyle.None;

			if (coinTextOn && singleNeedCoinText != null)
			{
				singleNeedCoinText.text = $"-{needCoin}";
			}
		}

		//구버전 싱글 레이아웃(지금은 숨김) 도 값은 맞춰둔다
		if (playBtn != null)
		{
			playBtn.style.display = needCoinOn ? DisplayStyle.None : DisplayStyle.Flex;
		}

		if (needCoinPanel != null)
		{
			needCoinPanel.style.display = coinTextOn ? DisplayStyle.Flex : DisplayStyle.None;

			if (coinTextOn && needCoinText != null)
			{
				needCoinText.text = $"-{needCoin}";
			}
		}
	}

	public void DataInit(InGameControllerBase inGameController, GameResultEnum gameResultEnum, int coinCount)
	{
		this.inGameController = inGameController;
		this.gameResultEnum = gameResultEnum;

		//지급·집계 초기화·서버 퇴장은 UI 준비와 상관없이 바로 처리한다 (2026-09-29).
		//UI 가 준비되기 전에 팝업이 사라지거나 씬이 바뀌어도 보상이 빠지지 않도록.
		//GameDataClearOn 이 배신 집계를 지우므로 그 전에 읽어 둔다
		bool isMulti = DataManager.Instance.isMultiOn;
		int receivedCoin = DataManager.Instance.betrayReceivedCoin;
		int paidCoin = DataManager.Instance.betrayPaidCoin;

		if (isMulti)
		{
			PayMultiReward(coinCount, receivedCoin);
		}
		else
		{
			PaySingleReward(coinCount);
		}

		DataManager.Instance.GameDataClearOn();

		Observable
				.Timer(TimeSpan.FromSeconds(1f))
				.Repeat()
				.Subscribe(_ => ServerManager.Instance.GameOutRequestOn())
				.AddTo(this);

		//화면 표시만 UI 준비 뒤에 (PanelRenderer)
		panelUI.Run(() => DataInitView(isMulti, coinCount, receivedCoin, paidCoin));
	}

	void DataInitView(bool isMulti, int coinCount, int receivedCoin, int paidCoin)
	{
		int key = 0;

		switch (gameResultEnum)
		{
			case GameResultEnum.Win: key = 16; break;
			case GameResultEnum.Lose: key = 17; break;
			case GameResultEnum.GiveUp: key = 11; break;
			case GameResultEnum.LeaveEarly: key = 18; break;
		}

		if (titleLabel != null)
		{
			titleLabel.text = LocalizeManager.Instance.GetStrData(LocalizeStatus.Game, key);
		}

		if (isMulti)
		{
			SetMultiRewardView(coinCount, receivedCoin, paidCoin);
		}
		else
		{
			SetSingleRewardView(coinCount);
		}
	}

	/// <summary>
	/// 멀티 실제 지급 : '지금 받을 몫(승리 + 받은 패널티)'.
	/// 지불한 패널티는 배신한 순간 이미 차감됐으므로 여기서 또 빼지 않는다.
	/// </summary>
	void PayMultiReward(int coinCount, int receivedCoin)
	{
		bool winOn = gameResultEnum == GameResultEnum.Win;

		DataManager.Instance.AddCoin((winOn ? coinCount : 0) + receivedCoin);
	}

	/// <summary>
	/// 멀티 결과 내역. 승리 / 받은 패널티 / 지불한 패널티 세 줄과 합계를 보여준다. (2026-09-19)
	/// 화면의 합계는 '이번 판의 손익' 이다.
	/// </summary>
	void SetMultiRewardView(int coinCount, int receivedCoin, int paidCoin)
	{
		LocalizeManager localize = LocalizeManager.Instance;

		string coinStr = localize.GetStrData(LocalizeStatus.Game, 47);

		bool winOn = gameResultEnum == GameResultEnum.Win;

		//패배/포기면 승리 줄이 빠지고 나머지가 위로 올라온다 (시안)
		if (rows[0] != null) rows[0].style.display = winOn ? DisplayStyle.Flex : DisplayStyle.None;

		SetRowValue(0, coinCount, coinStr, false);
		SetRowValue(1, receivedCoin, coinStr, false);
		SetRowValue(2, paidCoin, coinStr, true);

		int totalCoin = (winOn ? coinCount : 0) + receivedCoin - paidCoin;

		if (rewardTextLabel != null) rewardTextLabel.text = totalCoin + " " + coinStr;

		//손해를 봤으면 금화 더미 대신 회색 코인 한 장 (시안)
		bool gainOn = totalCoin >= 0;

		if (rewardCoins != null) rewardCoins.style.display = gainOn ? DisplayStyle.Flex : DisplayStyle.None;
		if (loseCoin != null) loseCoin.style.display = gainOn ? DisplayStyle.None : DisplayStyle.Flex;
	}

	/// <summary>
	/// 싱글 실제 지급과 연패 집계.
	/// 연패 보너스 : 패배할 때마다 loseCount 가 오르고, 다시하기로 이기면 보상이 (loseCount+1) 배가 된다.
	/// coinCount 는 GameEndOn 이 loseCount 를 올리기 전에 계산한 값이라 이번 판의 배수가 이미 반영돼 있다.
	/// </summary>
	void PaySingleReward(int coinCount)
	{
		DataManager dataManager = DataManager.Instance;

		dataManager.AddCoin(coinCount);

		if (gameResultEnum == GameResultEnum.Win)
		{
			//이겼으니 연패 보너스는 여기서 끝
			dataManager.loseCount = 0;
		}
		else if (gameResultEnum == GameResultEnum.Lose)
		{
			dataManager.loseCount++;
		}
	}

	/// <summary> 싱글 결과 시안. 왼쪽에 보상(코인 더미 + 금액), 패배면 회색 코인과 '다시하기 보상' 안내. (2026-09-20) </summary>
	void SetSingleRewardView(int coinCount)
	{
		DataManager dataManager = DataManager.Instance;

		string coinStr = LocalizeManager.Instance.GetStrData(LocalizeStatus.Game, 47);

		if (coinLabel != null)
		{
			coinLabel.text = $"{coinCount} coin";
		}

		if (singleRewardText != null)
		{
			singleRewardText.text = coinCount + " " + coinStr;
		}

		//보상이 없으면 금화 더미 대신 회색 코인 한 장 (시안)
		bool gainOn = coinCount > 0;

		if (singleCoins != null) singleCoins.style.display = gainOn ? DisplayStyle.Flex : DisplayStyle.None;
		if (singleLoseCoin != null) singleLoseCoin.style.display = gainOn ? DisplayStyle.None : DisplayStyle.Flex;

		if (gameResultEnum != GameResultEnum.Lose)
			return;

		//다시하기로 이기면 받게 될 보상 (기본 보상 x (연패 + 1)). loseCount 는 PaySingleReward 에서 이미 올렸다
		if (retryRewardLabel != null)
		{
			retryRewardLabel.text = LocalizeManager.Instance.GetStrData(LocalizeStatus.Game, 63)
				+ "   " + dataManager.GetSingleRewardCoin() + " " + coinStr;

			retryRewardLabel.style.display = DisplayStyle.Flex;
		}
	}

	/// <summary> 시안의 '다시하기' : 같은 맵을 처음부터 (코인 소모 없음) </summary>
	public void RetryBtnClickOn()
	{
		inGameController.ReStartOn();
		Destroy(gameObject); //숨기기 대신 삭제 : 숨긴 채 남으면 타이머(서버 퇴장 요청)가 계속 돈다 (2026-09-29)
	}

	public void CloseBtnClickOn()
	{
		Destroy(gameObject); //숨기기 대신 삭제 : 숨긴 채 남으면 타이머(서버 퇴장 요청)가 계속 돈다 (2026-09-29)
	}

	void SetRowValue(int index, int value, string coinStr, bool minusOn)
	{
		if (rowValues[index] == null)
			return;

		int abs = Mathf.Abs(value);

		string sign = abs == 0 ? "" : (minusOn ? "-" : "+");

		rowValues[index].text = sign + abs + " " + coinStr;
	}

	public void GoMainBtnClickOn()
	{
		inGameController.GoMainOn();
		Destroy(gameObject); //숨기기 대신 삭제 : 숨긴 채 남으면 타이머(서버 퇴장 요청)가 계속 돈다 (2026-09-29)
	}

	public void NewGameBtnClickOn()
	{
		if (DataManager.Instance.isMultiOn)
		{
			SceneManager.LoadScene("Loading");
		}
		else
		{
			if (DataManager.Instance.CheckNewGame())
			{
				inGameController.NewGameOn();
				Destroy(gameObject); //숨기기 대신 삭제 : 숨긴 채 남으면 타이머(서버 퇴장 요청)가 계속 돈다 (2026-09-29)
			}
		}
	}
}
