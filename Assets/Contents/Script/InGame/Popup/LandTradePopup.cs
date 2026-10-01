using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

/// <summary>
/// 영토 거래 제안 팝업. (uGUI Canvas → UI Toolkit 이식)
/// </summary>
[RequireComponent(typeof(PanelRenderer))]
public class LandTradePopup : BasePopup
{
	private PanelUI panelUI;

	private VisualElement iconPanel;

	private readonly List<PlayerToggleIconElement> playerToggleIconList = new List<PlayerToggleIconElement>();

	private AreaData selectAreaData;

	public bool buyOn = false;

	private PlayerToggleIconElement selectPlayer;

	private UnityAction proposeBtnClickEventOn;

	private void Awake()
	{
		panelUI = new PanelUI(this, OnUIReady);
	}

	/// <summary> PanelRenderer 의 UI 가 준비되면 한 번 호출된다 (예전 Awake 의 요소 찾기 / 이벤트 등록, 2026-09-29) </summary>
	private void OnUIReady(VisualElement root)
	{
		iconPanel = root.Q<VisualElement>("land-trade-icon-panel");

		UnityEngine.UIElements.Button backBtn = root.Q<UnityEngine.UIElements.Button>("land-trade-back-btn");
		UnityEngine.UIElements.Button cancelBtn = root.Q<UnityEngine.UIElements.Button>("land-trade-cancel-btn");
		UnityEngine.UIElements.Button proposeBtn = root.Q<UnityEngine.UIElements.Button>("land-trade-propose-btn");

		if (backBtn != null) backBtn.clicked += () => { PlayClickSe(); CloseBtnClickOn(); };
		if (cancelBtn != null) cancelBtn.clicked += () => { PlayClickSe(); CloseBtnClickOn(); };
		if (proposeBtn != null) proposeBtn.clicked += () => { PlayClickSe(); ProposeBtnClickOn(); };

		PopupMyCoinView.Bind(root, gameObject);

		CreatePlayerToggleIcon();
	}

	private void OnDestroy()
	{
		panelUI?.Dispose();
	}

	void PlayClickSe()
	{
		SoundManager.Instance.PlaySe(SeEnum.Yes);
	}

	void CreatePlayerToggleIcon()
	{
		PlayerEnum myPlayerEnum = DataManager.Instance.playerData.pe;

		for (int i = 0; i < DataManager.Instance.num_player; i++)
		{
			PlayerEnum currentPlayerEnum = (PlayerEnum)i;

			if (currentPlayerEnum == myPlayerEnum)
			{
				continue;
			}

			PlayerToggleIconElement playerToggleIcon = new PlayerToggleIconElement();
			playerToggleIcon.SetPlayerData(currentPlayerEnum);

			int index = playerToggleIconList.Count;

			playerToggleIcon.clickOn = () => PlayerSelectOn(index);

			//원본 HorizontalLayoutGroup spacing 142
			if (index > 0)
			{
				playerToggleIcon.Root.style.marginLeft = 142;
			}

			iconPanel.Add(playerToggleIcon.Root);

			playerToggleIconList.Add(playerToggleIcon);
		}
	}

	public void PlayerSelectOn(int selectIndex)
	{
		PlayerToggleIconElement currentSelectPlayer = playerToggleIconList[selectIndex];

		if (selectPlayer != null && selectPlayer == currentSelectPlayer)
		{
			return;
		}

		if (buyOn && selectAreaData.player != currentSelectPlayer.playerEnum)
		{
			return;
		}

		selectPlayer = currentSelectPlayer;

		foreach (var playerToggleIcon in playerToggleIconList)
		{
			bool selectOn = playerToggleIcon.playerEnum == selectPlayer.playerEnum;

			playerToggleIcon.SelectOn(selectOn);
		}
	}

	public void SetDataOn(AreaData selectAreaData, bool buyOn, UnityAction proposeBtnClickEventOn)
	{
		this.selectAreaData = selectAreaData;

		this.buyOn = buyOn;

		this.proposeBtnClickEventOn = proposeBtnClickEventOn;

		if (buyOn)
		{
			//아이콘 목록은 UI 가 준비될 때 만들어진다 (PanelRenderer, 2026-09-29)
			panelUI.Run(() =>
			{
				int index = playerToggleIconList.FindIndex(0, data => data.playerEnum == selectAreaData.player);

				if (index >= 0)
				{
					PlayerSelectOn(index);
				}
			});
		}
	}

	public void ProposeBtnClickOn()
	{
		if (selectPlayer == null)
		{
			return;
		}

		PlayerEnum fromPlayerEnum = DataManager.Instance.playerData.pe;
		PlayerEnum toPlayerEnum = selectPlayer.playerEnum;

		LandTradeRequest request = new LandTradeRequest()
		{
			fromPlayerEnum = fromPlayerEnum,
			toPlayerEnum = toPlayerEnum,
			areaData = selectAreaData,
			coinCount = selectPlayer.coinBox.coinCount.Value,
			buyOn = buyOn,
		};

		ServerManager.Instance.SendMessageOn(request);

		proposeBtnClickEventOn();

		CloseBtnClickOn();
	}
}
