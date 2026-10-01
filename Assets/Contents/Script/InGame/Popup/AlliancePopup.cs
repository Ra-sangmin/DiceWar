using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using UniRx;

/// <summary>
/// 동맹 제안 팝업. (uGUI Canvas → UI Toolkit 이식)
/// </summary>
[RequireComponent(typeof(PanelRenderer))]
public class AlliancePopup : MonoBehaviour
{
	private PanelUI panelUI;

	private VisualElement iconPanel;
	private Label leftCoinCount;
	private UnityEngine.UIElements.Button propossBtn;

	//좌하단 요약 줄 (2026-09-19 시안)
	private VisualElement selectGroupPanel;
	private VisualElement existGroupPanel;
	private Label needCoinText;

	private readonly List<PlayerToggleIconElement> playerToggleIconList = new List<PlayerToggleIconElement>();

	public UnityAction<AllianceRequestData> AllianceClearOn = data => { };

	private int maxCoinCount;

	private PlayerIconController playerIconController;

	private void Awake()
	{
		panelUI = new PanelUI(this, OnUIReady);
	}

	/// <summary> PanelRenderer 의 UI 가 준비되면 한 번 호출된다 (예전 Awake 의 요소 찾기 / 이벤트 등록, 2026-09-29) </summary>
	private void OnUIReady(VisualElement root)
	{
		iconPanel = root.Q<VisualElement>("alliance-icon-panel");
		leftCoinCount = root.Q<Label>("alliance-left-coin-text");
		propossBtn = root.Q<UnityEngine.UIElements.Button>("alliance-propose-btn");

		selectGroupPanel = root.Q<VisualElement>("alliance-select-group");
		existGroupPanel = root.Q<VisualElement>("alliance-exist-group-panel");
		needCoinText = root.Q<Label>("alliance-need-coin-text");

		UnityEngine.UIElements.Button backBtn = root.Q<UnityEngine.UIElements.Button>("alliance-back-btn");
		UnityEngine.UIElements.Button cancelBtn = root.Q<UnityEngine.UIElements.Button>("alliance-cancel-btn");

		if (backBtn != null) backBtn.clicked += () => { PlayClickSe(); CloseOn(); };
		if (cancelBtn != null) cancelBtn.clicked += () => { PlayClickSe(); CloseOn(); };
		if (propossBtn != null) propossBtn.clicked += () => { PlayClickSe(); ProposeBtnClickOn(); };

		PopupMyCoinView.Bind(root, gameObject);

		LocalizeTextSet(root);

		playerIconController = FindFirstObjectByType<PlayerIconController>();

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

	void LocalizeTextSet(VisualElement root)
	{
		LocalizeManager localize = LocalizeManager.Instance;

		Label title = root.Q<Label>("alliance-title");
		Label subTitle = root.Q<Label>("alliance-sub-title");
		Label proposeLabel = root.Q<Label>("alliance-propose-label");
		Label cancelLabel = root.Q<Label>("alliance-cancel-label");

		if (title != null) title.text = localize.GetStrData(LocalizeStatus.Game, 32);
		if (subTitle != null) subTitle.text = localize.GetStrData(LocalizeStatus.Game, 33);
		if (proposeLabel != null) proposeLabel.text = localize.GetStrData(LocalizeStatus.Game, 34);
		if (cancelLabel != null) cancelLabel.text = localize.GetStrData(LocalizeStatus.Common, 1);

		Label needCoinLabel = root.Q<Label>("alliance-need-coin-label");
		Label leftCoinLabel = root.Q<Label>("alliance-left-coin-label");

		if (needCoinLabel != null) needCoinLabel.text = localize.GetStrData(LocalizeStatus.Game, 60);
		if (leftCoinLabel != null) leftCoinLabel.text = localize.GetStrData(LocalizeStatus.Game, 61) + " :";
	}

	void CreatePlayerToggleIcon()
	{
		for (int i = 0; i < DataManager.Instance.num_player; i++)
		{
			PlayerToggleIconElement playerToggleIcon = new PlayerToggleIconElement();
			playerToggleIcon.SetPlayerData((PlayerEnum)i);

			int index = playerToggleIconList.Count;

			playerToggleIcon.clickOn = () => PlayerSelectOn(index);

			playerToggleIcon.coinBox.coinCount
				.Subscribe(_ => SetLeftCoint())
				.AddTo(gameObject);

			//원본 HorizontalLayoutGroup spacing 142
			if (index > 0)
			{
				playerToggleIcon.Root.style.marginLeft = 142;
			}

			iconPanel.Add(playerToggleIcon.Root);

			playerToggleIconList.Add(playerToggleIcon);
		}
	}

	public void SetData()
	{
		//만들자마자 불리므로 UI 가 준비된 뒤에 채운다 (PanelRenderer, 2026-09-29)
		panelUI.Run(SetDataReady);
	}

	void SetDataReady()
	{
		PlayerEnum myPlayerEnum = DataManager.Instance.playerData.pe;

		foreach (var playerToggleIcon in playerToggleIconList)
		{
			playerToggleIcon.SelectOn(playerToggleIcon.playerEnum == myPlayerEnum);
		}

		maxCoinCount = DataManager.Instance.GetMultiRewardCoin();

		SetDiscriptText();

		SetExistAllianceGroup();

		SetCointCount();
	}

	/// <summary>
	/// 아이콘 밑 설명 — 나는 "me", 이미 동맹이 있는 플레이어는 "기존 보상 : N" (2026-09-19 시안)
	/// </summary>
	void SetDiscriptText()
	{
		PlayerEnum myPlayerEnum = DataManager.Instance.playerData.pe;

		string rewardStr = LocalizeManager.Instance.GetStrData(LocalizeStatus.Game, 62);

		foreach (var playerToggleIcon in playerToggleIconList)
		{
			if (playerToggleIcon.playerEnum == myPlayerEnum)
			{
				playerToggleIcon.SetDiscriptText("me");

				continue;
			}

			AllianceData allianceData = DataManager.Instance.GetAllianceData(playerToggleIcon.playerEnum);

			playerToggleIcon.SetDiscriptText(allianceData != null ? rewardStr + " : " + allianceData.coinCount : string.Empty);
		}
	}

	/// <summary> 좌하단 — 이미 맺어져 있는 동맹 묶음들 </summary>
	void SetExistAllianceGroup()
	{
		if (existGroupPanel == null)
			return;

		existGroupPanel.Clear(VisualElementClearOptions.RecursiveReleaseResources); //6.5+ : 다시 만들 요소라 자원까지 바로 반환 (2026-09-29)

		foreach (var allianceAllData in DataManager.Instance.GetAllianceAllDataList())
		{
			if (allianceAllData.allianceDataList.Count <= 1)
				continue;

			VisualElement group = new VisualElement();
			group.AddToClassList("alliance__group");
			group.pickingMode = PickingMode.Ignore;

			foreach (var allianceData in allianceAllData.allianceDataList)
			{
				group.Add(CreateGroupIcon(allianceData.playerEnum));
			}

			existGroupPanel.Add(group);
		}
	}

	static VisualElement CreateGroupIcon(PlayerEnum playerEnum)
	{
		VisualElement icon = new VisualElement();
		icon.AddToClassList("alliance__group-icon");
		icon.pickingMode = PickingMode.Ignore;

		int colorIndex = DataManager.Instance.GetPlayerColorIndex(playerEnum);

		if (colorIndex >= 0)
		{
			icon.AddToClassList("alliance__group-icon--" + colorIndex);
		}

		return icon;
	}

	public void PlayerSelectOn(int selectIndex)
	{
		PlayerToggleIconElement currentSelectPlayer = playerToggleIconList[selectIndex];

		PlayerEnum myPlayerEnum = DataManager.Instance.playerData.pe;

		if (currentSelectPlayer.playerEnum == myPlayerEnum)
		{
			return;
		}

		currentSelectPlayer.ToggleOn();

		SetCointCount();
	}

	/// <summary> 토글이 On 된 플레이어들에게 코인 배분 </summary>
	private void SetCointCount()
	{
		PlayerEnum myPlayerEnum = DataManager.Instance.playerData.pe;

		List<PlayerToggleIconElement> toggleOnPlayerList = playerToggleIconList.Where(data => data.isOn).ToList();

		List<PlayerEnum> playerEnumList = toggleOnPlayerList.Select(data => data.playerEnum).ToList();

		List<AllianceData> allianceDataList = DataManager.Instance.GetAllianceDefaultData(myPlayerEnum, playerEnumList, playerIconController);

		foreach (var toggleOnPlayer in toggleOnPlayerList)
		{
			var data = allianceDataList.FirstOrDefault(data => data.playerEnum == toggleOnPlayer.playerEnum);

			if (data != null)
			{
				toggleOnPlayer.playerEnum = data.playerEnum;
				toggleOnPlayer.coinBox.SetCoinCount(data.coinCount);
			}
		}

		SetLeftCoint();
	}

	/// <summary> 전체 코인에서 현재 분배된 코인을 뺀 나머지 코인수 </summary>
	void SetLeftCoint()
	{
		int leftCoin = maxCoinCount;

		foreach (var playerToggleIcon in playerToggleIconList)
		{
			if (playerToggleIcon.isOn == false)
			{
				continue;
			}

			leftCoin -= playerToggleIcon.coinBox.coinCount.Value;
		}

		if (leftCoinCount != null)
		{
			leftCoinCount.SetNumber(leftCoin);
		}

		List<PlayerToggleIconElement> selectList = playerToggleIconList.Where(data => data.isOn).ToList();

		SetSelectGroup(selectList, maxCoinCount - leftCoin);

		bool activeOn = selectList.Count > 1 && leftCoin == 0;

		if (propossBtn != null)
		{
			propossBtn.SetEnabled(activeOn);
		}
	}

	/// <summary> 좌하단 — 지금 고른 플레이어 묶음과 배분된 총 코인 </summary>
	void SetSelectGroup(List<PlayerToggleIconElement> selectList, int usedCoin)
	{
		if (needCoinText != null)
		{
			needCoinText.SetNumber(usedCoin);
		}

		if (selectGroupPanel == null)
			return;

		selectGroupPanel.Clear(VisualElementClearOptions.RecursiveReleaseResources); //6.5+ : 다시 만들 요소라 자원까지 바로 반환 (2026-09-29)

		foreach (var playerToggleIcon in selectList)
		{
			selectGroupPanel.Add(CreateGroupIcon(playerToggleIcon.playerEnum));
		}
	}

	public void ProposeBtnClickOn()
	{
		List<PlayerToggleIconElement> selectList = playerToggleIconList.Where(data => data.isOn).ToList();

		if (selectList.Count <= 1)
		{
			return;
		}

		List<AllianceData> allianceDataList = new List<AllianceData>();

		PlayerEnum myPlayerEnum = DataManager.Instance.playerData.pe;
		int myCoinCount = 0;

		foreach (var selectItem in selectList)
		{
			AllianceData allianceData = new AllianceData(selectItem.playerEnum, selectItem.coinBox.coinCount.Value);
			allianceDataList.Add(allianceData);

			if (selectItem.playerEnum == myPlayerEnum)
			{
				myCoinCount = selectItem.coinBox.coinCount.Value;
			}
		}

		AllianceData orderData = new AllianceData()
		{
			playerEnum = myPlayerEnum,
			coinCount = myCoinCount
		};

		AllianceRequest request = new AllianceRequest()
		{
			orderData = orderData,
			allianceDataList = allianceDataList,
		};

		ServerManager.Instance.SendMessageOn(request);

		CloseOn();
	}

	public void AllianceRequestOn(AllianceRequestData allianceRequestData)
	{
		AllianceClear(allianceRequestData);
	}

	public void AllianceClear(AllianceRequestData allianceRequestData)
	{
		CloseOn();
	}

	void CloseOn()
	{
		Destroy(gameObject); //숨기기 대신 삭제 (2026-09-29)
	}
}

public class AllianceRequestData
{
	public List<AllianceData> allianceDataList = new List<AllianceData>() { };
}
