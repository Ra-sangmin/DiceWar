using UnityEngine;
using UnityEngine.UIElements;
using Cysharp.Threading.Tasks;

/// <summary>
/// 멀티 전용 '동맹을 배신 하시겠습니까?' 확인 팝업. (2026-09-19 Figma 시안으로 개편)
/// 메뉴에서 그만하기 / 새로하기를 눌렀을 때, 동맹이 있으면 이 팝업이 뜬다.
/// 동맹이 없으면 OptionPopup 이 팝업 없이 바로 실행한다.
/// </summary>
[RequireComponent(typeof(PanelRenderer))]
public class GiveUpPopup : MonoBehaviour
{
	private PanelUI panelUI;

	private Label titleLabel;
	private Label subTitleLabel;
	private Label rewardTitleLabel;
	private Label rewardTextLabel;
	private Label needCoinText;
	private Label goMainLabel;
	private Label newGameLabel;
	private Label returnLabel;

	private readonly Label[] rowLabels = new Label[3];
	private readonly Label[] rowValues = new Label[3];

	private int addCoin;
	private bool newGameOn = false;

	/// <summary> 땅이 0 이 되어 뜬 팝업이면 배신 패널티 면제 (2026-09-26). 메뉴에서 연 경우는 패널티를 문다 </summary>
	private bool penaltyFreeOn = false;
	private InGameControllerBase inGameController;

	private PlayerIconController playerIconController;

	void Awake()
	{
		panelUI = new PanelUI(this, OnUIReady);
	}

	/// <summary> PanelRenderer 의 UI 가 준비되면 한 번 호출된다 (예전 Awake 의 요소 찾기 / 이벤트 등록, 2026-09-29) </summary>
	private void OnUIReady(VisualElement root)
	{
		titleLabel = root.Q<Label>("give-up-title");
		subTitleLabel = root.Q<Label>("give-up-sub-title");
		rewardTitleLabel = root.Q<Label>("give-up-reward-title");
		rewardTextLabel = root.Q<Label>("give-up-reward-text");
		needCoinText = root.Q<Label>("give-up-need-coin-text");
		goMainLabel = root.Q<Label>("give-up-go-main-label");
		newGameLabel = root.Q<Label>("give-up-new-game-label");
		returnLabel = root.Q<Label>("give-up-return-label");

		for (int i = 0; i < rowLabels.Length; i++)
		{
			rowLabels[i] = root.Q<Label>("give-up-row" + (i + 1) + "-label");
			rowValues[i] = root.Q<Label>("give-up-row" + (i + 1) + "-value");
		}

		UnityEngine.UIElements.Button goMainBtn = root.Q<UnityEngine.UIElements.Button>("give-up-go-main-btn");
		UnityEngine.UIElements.Button newGameBtn = root.Q<UnityEngine.UIElements.Button>("give-up-new-game-btn");
		UnityEngine.UIElements.Button returnBtn = root.Q<UnityEngine.UIElements.Button>("give-up-return-btn");
		UnityEngine.UIElements.Button closeBtn = root.Q<UnityEngine.UIElements.Button>("give-up-close-btn");

		if (goMainBtn != null) goMainBtn.clicked += () => { PlayClickSe(); GoMainBtnClickOn(); };
		if (newGameBtn != null) newGameBtn.clicked += () => { PlayClickSe(); NewGameBtnClickOn(); };
		if (returnBtn != null) returnBtn.clicked += () => { PlayClickSe(); ReturnBtnClickOn(); };
		if (closeBtn != null) closeBtn.clicked += () => { PlayClickSe(); ReturnBtnClickOn(); };

		LocalizeTextSet();
	}

	private void OnDestroy()
	{
		panelUI?.Dispose();
	}

	void Start()
	{
		playerIconController = FindFirstObjectByType<PlayerIconController>();
	}

	void PlayClickSe()
	{
		SoundManager.Instance.PlaySe(SeEnum.Yes);
	}

	void LocalizeTextSet()
	{
		LocalizeManager localize = LocalizeManager.Instance;

		if (titleLabel != null) titleLabel.text = localize.GetStrData(LocalizeStatus.Game, 53);
		if (subTitleLabel != null) subTitleLabel.text = localize.GetStrData(LocalizeStatus.Game, 54);
		if (rewardTitleLabel != null) rewardTitleLabel.text = localize.GetStrData(LocalizeStatus.Game, 55);

		if (goMainLabel != null) goMainLabel.text = localize.GetStrData(LocalizeStatus.Game, 46);
		if (newGameLabel != null) newGameLabel.text = localize.GetStrData(LocalizeStatus.Game, 8);
		if (returnLabel != null) returnLabel.text = localize.GetStrData(LocalizeStatus.Game, 10);

		int[] rowKeys = { 50, 51, 52 };

		for (int i = 0; i < rowLabels.Length; i++)
		{
			if (rowLabels[i] != null) rowLabels[i].text = localize.GetStrData(LocalizeStatus.Game, rowKeys[i]);
		}

		if (needCoinText != null) needCoinText.text = $"-{DataManager.Instance.GetNeedCoin()}";
	}

	public void DataInit(InGameControllerBase inGameController, bool newGameOn, int addCoin, bool penaltyFreeOn = false)
	{
		this.inGameController = inGameController;
		this.addCoin = addCoin;
		this.newGameOn = newGameOn;
		this.penaltyFreeOn = penaltyFreeOn;

		//표시는 UI 가 준비된 뒤에 (PanelRenderer, 2026-09-29)
		panelUI.Run(() =>
		{
			//메뉴에서 연 경우에는 면제가 아니므로 '지금 끝내면 면제' 안내를 숨긴다
			if (subTitleLabel != null)
				subTitleLabel.style.display = penaltyFreeOn ? DisplayStyle.Flex : DisplayStyle.None;

			SetRewardText();
		});
	}

	/// <summary>
	/// 표시 전용 보상 내역. 합계 = 조기종료 보상 + 받은 패널티 - 지불한 패널티.
	/// 지금 끝내면 배신 패널티는 면제라서 0 으로 친다. (2026-09-19)
	/// </summary>
	void SetRewardText()
	{
		LocalizeManager localize = LocalizeManager.Instance;

		string coinStr = localize.GetStrData(LocalizeStatus.Game, 47);

		int receivedCoin = DataManager.Instance.betrayReceivedCoin;
		int paidCoin = DataManager.Instance.betrayPaidCoin;
		int leaveEarlyCoin = inGameController != null ? inGameController.GetLeaveEarlyCoin() : 0;

		SetRowValue(0, $"+{Mathf.Abs(receivedCoin)} {coinStr}");
		SetRowValue(1, $"-{Mathf.Abs(paidCoin)} {coinStr}");
		//배신 패널티 : 면제 / 또는 지금 물어야 할 금액
		int betrayCoin = 0;

		if (penaltyFreeOn)
		{
			SetRowValue(2, localize.GetStrData(LocalizeStatus.Game, 56));
		}
		else
		{
			AllianceData myAllianceData = DataManager.Instance.GetMyAllianceData();
			betrayCoin = myAllianceData != null ? DataManager.Instance.GetNeedBetrayCoin() : 0;
			SetRowValue(2, $"-{betrayCoin} {coinStr}");
		}

		if (rowValues[2] != null)
		{
			rowValues[2].EnableInClassList("give-up__reward-value--free", penaltyFreeOn);
			rowValues[2].EnableInClassList("give-up__reward-value--minus", penaltyFreeOn == false);
		}

		int totalCoin = leaveEarlyCoin + receivedCoin - paidCoin - betrayCoin;

		if (rewardTextLabel != null) rewardTextLabel.text = totalCoin + " " + coinStr;
	}

	void SetRowValue(int index, string text)
	{
		if (rowValues[index] != null) rowValues[index].text = text;
	}

	public void GoMainBtnClickOn()
	{
		GoMainOn().Forget();
	}

	private async UniTask GoMainOn()
	{
		if (DataManager.Instance.isMultiOn)
		{
			AllianceData allianceData = DataManager.Instance.GetMyAllianceData();

			//땅이 0 이 되어 뜬 팝업이면 패널티 없이 나간다 (2026-09-26)
			if (allianceData != null && penaltyFreeOn == false)
			{
				await DataManager.Instance.MyBetrayOn(playerIconController);
			}

			ServerManager.Instance.GameOutRequestOn();
		}

		inGameController.GoMainOn();
	}

	public void NewGameBtnClickOn()
	{
		NewGameOn().Forget();
	}

	private async UniTask NewGameOn()
	{
		if (DataManager.Instance.isMultiOn == false)
			return;

		AllianceData allianceData = DataManager.Instance.GetMyAllianceData();

		//땅이 0 이 되어 뜬 팝업이면 패널티 없이 나간다
		if (allianceData != null && penaltyFreeOn == false)
		{
			await DataManager.Instance.MyBetrayOn(playerIconController);
		}

		inGameController.NewGameOn();
	}

	public void ReturnBtnClickOn()
	{
		Destroy(gameObject);
	}
}
