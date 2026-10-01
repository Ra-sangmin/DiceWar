using UnityEngine;
using UnityEngine.UIElements;
using UniRx;

/// <summary>
/// 인게임 메뉴 팝업. (uGUI Canvas → UI Toolkit 이식)
/// 원본의 PlayCoinBtn 은 Main 씬 PlaySetPopup 프리팹과 공유하므로 건드리지 않고,
/// 코인 체크 로직만 이 클래스 안으로 옮겼다.
/// </summary>
[RequireComponent(typeof(PanelRenderer))]
public class OptionPopup : MonoBehaviour
{
	[SerializeField] RewardedAdsButton rewardedAdsButton;

	//시안 기준 버튼 간격 (계속하기만 한 칸 더 띄운다)
	private const float BtnSpacing = 16f;
	private const float ContinueSpacing = 70f;

	private PanelUI panelUI;

	private VisualElement btnPanel;
	private VisualElement playGroup;
	private VisualElement needCoinPanel;

	private UnityEngine.UIElements.Button restartBtn;
	private UnityEngine.UIElements.Button adBtn;
	private UnityEngine.UIElements.Button playBtn;

	private Label titleLabel;
	private Label homeLabel;
	private Label restartLabel;
	private Label playLabel;
	private Label adLabel;
	private Label continueLabel;
	private Label needCoinText;
	private Label rewardTitleLabel;
	private Label rewardTextLabel;

	//멀티 전용 - 예상 보상 내역 4줄 (2026-09-19)
	private readonly Label[] rowLabels = new Label[4];
	private readonly Label[] rowValues = new Label[4];
	private bool multiOn;

	private InGameControllerBase inGameController;

	void Awake()
	{
		panelUI = new PanelUI(this, OnUIReady);
	}

	/// <summary> PanelRenderer 의 UI 가 준비되면 한 번 호출된다 (예전 Awake 의 요소 찾기 / 이벤트 등록, 2026-09-29) </summary>
	private void OnUIReady(VisualElement root)
	{
		btnPanel = root.Q<VisualElement>("option-btn-panel");
		playGroup = root.Q<VisualElement>("option-play-group");
		needCoinPanel = root.Q<VisualElement>("option-need-coin-panel");

		restartBtn = root.Q<UnityEngine.UIElements.Button>("option-restart-btn");
		adBtn = root.Q<UnityEngine.UIElements.Button>("option-ad-btn");
		playBtn = root.Q<UnityEngine.UIElements.Button>("option-play-btn");

		titleLabel = root.Q<Label>("option-title");
		homeLabel = root.Q<Label>("option-home-label");
		restartLabel = root.Q<Label>("option-restart-label");
		playLabel = root.Q<Label>("option-play-label");
		adLabel = root.Q<Label>("option-ad-label");
		continueLabel = root.Q<Label>("option-continue-label");
		needCoinText = root.Q<Label>("option-need-coin-text");
		rewardTitleLabel = root.Q<Label>("option-reward-title");
		rewardTextLabel = root.Q<Label>("option-reward-text");

		for (int i = 0; i < rowLabels.Length; i++)
		{
			rowLabels[i] = root.Q<Label>("option-row" + (i + 1) + "-label");
			rowValues[i] = root.Q<Label>("option-row" + (i + 1) + "-value");
		}

		//멀티는 패널이 더 넓고 보상 내역이 표 형태로 들어간다 (USS 의 .option--multi)
		multiOn = DataManager.Instance.isMultiOn;

		VisualElement optionRoot = root.Q<VisualElement>("option-root");

		if (multiOn && optionRoot != null)
		{
			optionRoot.AddToClassList("option--multi");
		}

		UnityEngine.UIElements.Button homeBtn = root.Q<UnityEngine.UIElements.Button>("option-home-btn");
		UnityEngine.UIElements.Button closeBtn = root.Q<UnityEngine.UIElements.Button>("option-close-btn");
		UnityEngine.UIElements.Button continueBtn = root.Q<UnityEngine.UIElements.Button>("option-continue-btn");

		if (homeBtn != null) homeBtn.clicked += () => { PlayClickSe(); GoMainBtnClickOn(); };
		if (restartBtn != null) restartBtn.clicked += () => { PlayClickSe(); RestartBtnClickOn(); };
		if (playBtn != null) playBtn.clicked += () => { PlayClickSe(); NewGameBtnClickOn(); };
		if (continueBtn != null) continueBtn.clicked += () => { PlayClickSe(); CloseBtnClickOn(); };
		if (closeBtn != null) closeBtn.clicked += () => { PlayClickSe(); CloseBtnClickOn(); };

		if (rewardedAdsButton != null)
		{
			rewardedAdsButton.InitView(adBtn);
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
		//코인 구독 / 보상 글자는 UI 가 준비된 뒤에 (PanelRenderer, 2026-09-29)
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

		SetRewardText();
	}

	void PlayClickSe()
	{
		SoundManager.Instance.PlaySe(SeEnum.Yes);
	}

	void LocalizeTextSet()
	{
		LocalizeManager localize = LocalizeManager.Instance;

		if (titleLabel != null) titleLabel.text = localize.GetStrData(LocalizeStatus.Game, 6);
		if (homeLabel != null) homeLabel.text = localize.GetStrData(LocalizeStatus.Game, 46);
		if (restartLabel != null) restartLabel.text = localize.GetStrData(LocalizeStatus.Game, 7);
		if (playLabel != null) playLabel.text = localize.GetStrData(LocalizeStatus.Game, 8);
		if (continueLabel != null) continueLabel.text = localize.GetStrData(LocalizeStatus.Game, 10);
		if (adLabel != null) adLabel.text = localize.GetStrData(LocalizeStatus.Common, 2);
		//싱글은 '승리 보상', 멀티는 '예상 보상'
		if (rewardTitleLabel != null) rewardTitleLabel.text = localize.GetStrData(LocalizeStatus.Game, multiOn ? 49 : 48);

		if (multiOn)
		{
			int[] rowKeys = { 48, 50, 51, 52 };

			for (int i = 0; i < rowLabels.Length; i++)
			{
				if (rowLabels[i] != null) rowLabels[i].text = localize.GetStrData(LocalizeStatus.Game, rowKeys[i]);
			}
		}
	}

	/// <summary>
	/// 보상 표시. 싱글은 '승리 보상' 한 줄, 멀티는 내역 4줄 + 합계다. (2026-09-19)
	/// 멀티 합계 = 승리 보상 + 받은 배신 패널티 - 지불한 배신 패널티.
	/// 표시 전용이다 - 실제 정산은 InGameControllerMulti.GetCoin 이 따로 한다.
	/// </summary>
	void SetRewardText()
	{
		string coinStr = LocalizeManager.Instance.GetStrData(LocalizeStatus.Game, 47);

		int rewardCoin = DataManager.Instance.GetRewardCoin();

		if (multiOn == false)
		{
			if (rewardTextLabel != null) rewardTextLabel.text = rewardCoin + " " + coinStr;

			return;
		}

		//동맹이 있으면 승리 보상 = 동맹 승리 시 내가 받을 몫 (2026-09-28)
		AllianceData myAllianceData = DataManager.Instance.GetMyAllianceData();
		if (myAllianceData != null)
			rewardCoin = myAllianceData.coinCount;

		int receivedCoin = DataManager.Instance.betrayReceivedCoin;
		int paidCoin = DataManager.Instance.betrayPaidCoin;

		//지금 배신할 경우 물어야 할 값. 동맹이 없으면 0 이다.
		int betrayCoin = myAllianceData != null ? DataManager.Instance.GetNeedBetrayCoin() : 0;

		SetRowValue(0, rewardCoin, true);
		SetRowValue(1, receivedCoin, true);
		SetRowValue(2, paidCoin, false);
		SetRowValue(3, betrayCoin, false);

		int totalCoin = rewardCoin + receivedCoin - paidCoin;

		if (rewardTextLabel != null) rewardTextLabel.text = totalCoin + " " + coinStr;
	}

	void SetRowValue(int index, int value, bool plusOn)
	{
		if (rowValues[index] == null)
			return;

		rowValues[index].text = (plusOn ? "+" : "-") + Mathf.Abs(value);
	}

	/// <summary> 원본 PlayCoinBtn.SetNeedCoinCheck 과 동일한 로직 </summary>
	void SetNeedCoinCheck()
	{
		int needCoin = DataManager.Instance.GetNeedCoin();

		bool needCoinOn = DataManager.Instance.userData.myCoin.Value < needCoin;

		if (playBtn != null)
		{
			playBtn.style.display = needCoinOn ? DisplayStyle.None : DisplayStyle.Flex;
		}

		//광고 버튼은 Play 버튼 바로 뒤에 겹쳐 있어서, 안 쓸 때는 숨겨야 테두리로 안 비친다
		if (adBtn != null)
		{
			adBtn.style.display = needCoinOn ? DisplayStyle.Flex : DisplayStyle.None;
		}

		if (needCoinPanel != null)
		{
			bool coinTextOn = DataManager.Instance.aiLevel != AILevel.Easy;

			needCoinPanel.style.display = coinTextOn ? DisplayStyle.Flex : DisplayStyle.None;

			if (coinTextOn && needCoinText != null)
			{
				needCoinText.text = $"-{needCoin}";
			}
		}
	}

	public void DataInit(InGameControllerBase inGameController)
	{
		this.inGameController = inGameController;

		panelUI.Run(() =>
		{
			bool restartOn = !DataManager.Instance.isMultiOn;

			if (restartBtn != null)
			{
				restartBtn.style.display = restartOn ? DisplayStyle.Flex : DisplayStyle.None;
			}

			//원본 VerticalLayoutGroup.spacing 대체 (첫 요소를 제외한 나머지에 위 여백)
			SetSpacing(BtnSpacing);
		});
	}

	void SetSpacing(float spacing)
	{
		if (btnPanel == null)
			return;

		//6.7 gap (2026-09-29) : 숨긴 버튼(display:none)은 gap 계산에서 자동으로 빠지므로
		//예전처럼 버튼마다 표시 여부를 보고 marginTop 을 다시 매길 필요가 없다
		btnPanel.style.rowGap = spacing;

		foreach (VisualElement child in btnPanel.Children())
		{
			//계속하기만 시안처럼 한 칸 더 띄운다 (gap 에 더해지는 만큼만)
			child.style.marginTop = child.name == "option-continue-btn" ? ContinueSpacing - spacing : 0;
		}
	}

	public void SettingPopupOn()
	{
	}

	public void RestartBtnClickOn()
	{
		inGameController.ReStartOn();
		CloseBtnClickOn();
	}

	public void NewGameBtnClickOn()
	{
		if (DataManager.Instance.isMultiOn)
		{
			GiveUpPopupOn(true);
		}
		else
		{
			if (DataManager.Instance.CheckNewGame())
			{
				inGameController.NewGameOn();
				CloseBtnClickOn();
			}
		}
	}

	public void GoMainBtnClickOn()
	{
		if (DataManager.Instance.isMultiOn)
		{
			GiveUpPopupOn(false);
		}
		else
		{
			inGameController.GoMainOn();
		}
	}

	private void GiveUpPopupOn(bool newGameOn)
	{
		AllianceData allianceData = DataManager.Instance.GetMyAllianceData();

		//동맹이 없으면 배신할 것도, 물어낼 것도 없다 — 확인 없이 바로 실행한다 (2026-09-19)
		if (allianceData == null)
		{
			if (newGameOn)
			{
				inGameController.NewGameOn();
			}
			else
			{
				ServerManager.Instance.GameOutRequestOn();
				inGameController.GoMainOn();
			}

			CloseBtnClickOn();

			return;
		}

		PopupManager.Instance.GiveUpPopupOn(inGameController, newGameOn, -allianceData.coinCount);
	}

	public void TutorialBtnClickOn()
	{
		PopupManager.Instance.TutorialPopupOn();
	}

	public void CloseBtnClickOn()
	{
		Destroy(gameObject);
	}
}
