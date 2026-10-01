using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

/// <summary>
/// 동맹 배신 확인 팝업. (uGUI Canvas → UI Toolkit 이식)
/// </summary>
[RequireComponent(typeof(PanelRenderer))]
public class BetrayPopup : BasePopup
{
	private PanelUI panelUI;

	private PlayerToggleIconElement playerToggleIcon;

	public UnityAction BetrayClearOn = () => { };

	private PlayerIconController playerIconController;

	void Awake()
	{
		panelUI = new PanelUI(this, OnUIReady);
	}

	/// <summary> PanelRenderer 의 UI 가 준비되면 한 번 호출된다 (예전 Awake 의 요소 찾기 / 이벤트 등록, 2026-09-29) </summary>
	private void OnUIReady(VisualElement root)
	{
		VisualElement iconHolder = root.Q<VisualElement>("betray-player-toggle");

		playerToggleIcon = new PlayerToggleIconElement(true);
		playerToggleIcon.Root.style.position = Position.Absolute;
		playerToggleIcon.Root.style.left = 0;
		playerToggleIcon.Root.style.top = 0;
		iconHolder.Add(playerToggleIcon.Root);

		UnityEngine.UIElements.Button cancelBtn = root.Q<UnityEngine.UIElements.Button>("betray-cancel-btn");
		UnityEngine.UIElements.Button okBtn = root.Q<UnityEngine.UIElements.Button>("betray-ok-btn");
		UnityEngine.UIElements.Button backBtn = root.Q<UnityEngine.UIElements.Button>("betray-back-btn");

		if (cancelBtn != null) cancelBtn.clicked += () => { PlayClickSe(); CloseBtnClickOn(); };
		if (backBtn != null) backBtn.clicked += () => { PlayClickSe(); CloseBtnClickOn(); };
		if (okBtn != null) okBtn.clicked += () => { PlayClickSe(); BetrayBtnClickOn(); };

		PopupMyCoinView.Bind(root, gameObject);

		LocalizeTextSet(root);
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

	void LocalizeTextSet(VisualElement root)
	{
		LocalizeManager localize = LocalizeManager.Instance;

		Label title = root.Q<Label>("betray-title");
		Label subText = root.Q<Label>("betray-sub-text");
		Label cancelLabel = root.Q<Label>("betray-cancel-label");
		Label okLabel = root.Q<Label>("betray-ok-label");

		if (title != null) title.text = localize.GetStrData(LocalizeStatus.Game, 28);
		if (subText != null) subText.text = localize.GetStrData(LocalizeStatus.Game, 35);
		if (cancelLabel != null) cancelLabel.text = localize.GetStrData(LocalizeStatus.Common, 1);
		if (okLabel != null) okLabel.text = localize.GetStrData(LocalizeStatus.Game, 36);

		playerToggleIcon.SetDiscriptText(localize.GetStrData(LocalizeStatus.Game, 37));
	}

	public void SetData()
	{
		int addCoin = DataManager.Instance.GetNeedBetrayCoin();

		//만들자마자 불리므로 UI 가 준비된 뒤에 채운다 (PanelRenderer, 2026-09-29)
		panelUI.Run(() =>
		{
			playerToggleIcon.SetPlayerData(DataManager.Instance.playerData.pe);
			playerToggleIcon.SelectOn(true);
			playerToggleIcon.coinBox.SetCoinCount(addCoin);
		});
	}

	public void BetrayBtnClickOn()
	{
		DataManager.Instance.MyBetrayOn(playerIconController);

		Destroy(gameObject);
	}
}
