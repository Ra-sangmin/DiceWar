using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

/// <summary>
/// 동맹 / 영토 거래 승인 요청 바 (멀티 전용). (uGUI Canvas → UI Toolkit 이식)
/// 원본의 PlayerToggleIcon 은 Resources 팝업들과 공유하므로 여기서는 쓰지 않고
/// 같은 이미지(player_none / player_select)를 그리는 요소로 대체했다.
/// </summary>
public class ApprovePopup : MonoBehaviour
{
	private VisualElement popupRoot;
	private VisualElement tradePanel;

	private VisualElement tradeIconBG;
	private VisualElement tradeIconFG;

	private Label tradeTitleLabel;
	private Label coinLabel;

	private UnityEngine.UIElements.Button refuseBtn;
	private UnityEngine.UIElements.Button acceptBtn;

	//동맹 제안 받기 바 (2026-09-19 시안) — 제안자 쪽 바와 같은 모양이다
	private VisualElement allianceBarRoot;
	private VisualElement allianceIconPanel;
	private Label allianceTitleLabel;
	private Label allianceRewardLabel;
	private Label allianceRefuseLabel;
	private Label allianceAcceptLabel;
	private Label allianceBetrayCoinText;

	private VisualElement allianceBetrayCoinPanel;

	private UnityEngine.UIElements.Button allianceRefuseBtn;
	private UnityEngine.UIElements.Button allianceAcceptBtn;

	private readonly List<MyAllianceApproveIconElement> playerIconList = new List<MyAllianceApproveIconElement>();

	private int tradeIconColorIndex = -1;

	//영토 교환 제안 받기 바 (2026-09-26)
	private VisualElement tradeBarRoot;
	private VisualElement tradeBarIcon;
	private Label tradeBarTitle;
	private int tradeBarColorIndex = -1;

	public ApproveData approveData;

	public UnityAction refuseBtnClickEventOn = () => { };
	public UnityAction acceptBtnClickEventOn = () => { };

	/// <summary> UI Toolkit 요소 연결 </summary>
	public void InitView(VisualElement root)
	{
		popupRoot = root.Q<VisualElement>("approve-popup");

		tradePanel = root.Q<VisualElement>("approve-trade-panel");

		tradeIconBG = root.Q<VisualElement>("approve-trade-icon-bg");
		tradeIconFG = root.Q<VisualElement>("approve-trade-icon-fg");

		tradeTitleLabel = root.Q<Label>("approve-trade-title");
		coinLabel = root.Q<Label>("approve-coin-text");

		refuseBtn = root.Q<UnityEngine.UIElements.Button>("approve-refuse-btn");
		acceptBtn = root.Q<UnityEngine.UIElements.Button>("approve-accept-btn");

		allianceBarRoot = root.Q<VisualElement>("approve-alliance-bar");
		allianceIconPanel = root.Q<VisualElement>("approve-alliance-bar-icons");
		allianceTitleLabel = root.Q<Label>("approve-alliance-bar-title");
		allianceRewardLabel = root.Q<Label>("approve-alliance-bar-reward");
		allianceRefuseLabel = root.Q<Label>("approve-alliance-refuse-label");
		allianceAcceptLabel = root.Q<Label>("approve-alliance-accept-label");
		allianceBetrayCoinText = root.Q<Label>("approve-alliance-betray-coin-text");
		allianceBetrayCoinPanel = root.Q<VisualElement>("approve-alliance-betray-coin");

		allianceRefuseBtn = root.Q<UnityEngine.UIElements.Button>("approve-alliance-refuse-btn");

		tradeBarRoot = root.Q<VisualElement>("approve-trade-bar");
		tradeBarIcon = root.Q<VisualElement>("approve-trade-bar-icon");
		tradeBarTitle = root.Q<Label>("approve-trade-bar-title");

		UnityEngine.UIElements.Button tradeRefuseBtn = root.Q<UnityEngine.UIElements.Button>("approve-trade-refuse-btn");
		UnityEngine.UIElements.Button tradeAcceptBtn = root.Q<UnityEngine.UIElements.Button>("approve-trade-accept-btn");

		if (tradeRefuseBtn != null) tradeRefuseBtn.clicked += () => { PlayClickSe(); RefuseBtnClickOn(); };
		if (tradeAcceptBtn != null) tradeAcceptBtn.clicked += () => { PlayClickSe(); AcceptBtnClickOn(); };
		allianceAcceptBtn = root.Q<UnityEngine.UIElements.Button>("approve-alliance-accept-btn");

		if (refuseBtn != null) refuseBtn.clicked += () => { PlayClickSe(); RefuseBtnClickOn(); };
		if (acceptBtn != null) acceptBtn.clicked += () => { PlayClickSe(); AcceptBtnClickOn(); };
		if (allianceRefuseBtn != null) allianceRefuseBtn.clicked += () => { PlayClickSe(); RefuseBtnClickOn(); };
		if (allianceAcceptBtn != null) allianceAcceptBtn.clicked += () => { PlayClickSe(); AcceptBtnClickOn(); };

		LocalizeTextSet();

		//제안자 쪽 바와 같은 아이콘 요소를 쓴다 (체크 / X 표시가 딸려 온다)
		playerIconList.Clear();

		if (allianceIconPanel != null)
		{
			allianceIconPanel.Clear(VisualElementClearOptions.RecursiveReleaseResources); //6.5+ : 다시 만들 요소라 자원까지 바로 반환 (2026-09-29)

			for (int i = 0; i < 7; i++)
			{
				MyAllianceApproveIconElement icon = new MyAllianceApproveIconElement((PlayerEnum)i);

				allianceIconPanel.Insert(i, icon.Root);

				icon.SetActive(false);

				playerIconList.Add(icon);
			}
		}

		SetPanelActive(false);
	}

	void LocalizeTextSet()
	{
		LocalizeManager localize = LocalizeManager.Instance;

		if (localize == null)
			return;

		if (allianceTitleLabel != null) allianceTitleLabel.text = localize.GetStrData(LocalizeStatus.Game, 32);
		if (allianceRefuseLabel != null) allianceRefuseLabel.text = localize.GetStrData(LocalizeStatus.Game, 58);

		if (tradeBarTitle != null) tradeBarTitle.text = localize.GetStrData(LocalizeStatus.Game, 72);

		if (tradeBarRoot != null)
		{
			Label tradeRefuseLabel = tradeBarRoot.Q<Label>("approve-trade-refuse-label");
			Label tradeAcceptLabel = tradeBarRoot.Q<Label>("approve-trade-accept-label");

			if (tradeRefuseLabel != null) tradeRefuseLabel.text = localize.GetStrData(LocalizeStatus.Game, 58);
			if (tradeAcceptLabel != null) tradeAcceptLabel.text = localize.GetStrData(LocalizeStatus.Game, 59);
		}
	}

	/// <summary> 원본 ButtonSound 컴포넌트 역할 </summary>
	void PlayClickSe()
	{
		SoundManager.Instance.PlaySe(SeEnum.Yes);
	}

	/// <summary> 둘 다 끄거나, SetData 가 둘 중 하나를 켠다 </summary>
	public void SetPanelActive(bool activeOn)
	{
		if (activeOn == false)
		{
			SetVisible(popupRoot, false);
			SetVisible(allianceBarRoot, false);
			SetVisible(tradeBarRoot, false);
		}
	}

	static void SetVisible(VisualElement element, bool activeOn)
	{
		if (element != null)
		{
			element.style.display = activeOn ? DisplayStyle.Flex : DisplayStyle.None;
		}
	}

	public void SetData(ApproveData approveData)
	{
		this.approveData = approveData;

		if (popupRoot == null)
			return;

		//동맹 / 영토 교환 모두 바 형태 (예전 영토 거래 팝업은 쓰지 않는다, 2026-09-26)
		SetVisible(popupRoot, false);
		SetVisible(allianceBarRoot, approveData.isAlliance);
		SetVisible(tradeBarRoot, approveData.isAlliance == false);

		if (approveData.isAlliance == false)
		{
			LandTradeSetData(approveData.landTradeRequest);
		}
		else
		{
			AllianceSetData(approveData);
		}
	}

	/// <summary> 영토 교환 제안 : (제안자 색 육각형) 영토 교환 제안 [거절][수락]. 맵에서 오가는 두 땅을 표시한다 </summary>
	public void LandTradeSetData(LandTradeRequest landTradeRequest)
	{
		LocalizeTextSet();

		int colorIndex = DataManager.Instance.GetPlayerColorIndex(landTradeRequest.fromPlayerEnum);

		if (tradeBarIcon != null && colorIndex >= 0 && colorIndex != tradeBarColorIndex)
		{
			if (tradeBarColorIndex >= 0)
				tradeBarIcon.RemoveFromClassList("player-icon__icon--" + tradeBarColorIndex);

			tradeBarColorIndex = colorIndex;
			tradeBarIcon.AddToClassList("player-icon__icon--" + tradeBarColorIndex);
		}

		TradeEventOn(true);
	}

	/// <summary> 교환 대상 두 땅 (내가 줄 땅 = areaData, 내가 받을 땅 = coinCount 의 id) 을 맵에서 강조 </summary>
	public void TradeEventOn(bool tradeOn)
	{
		if (approveData == null || approveData.landTradeRequest == null)
			return;

		AreaData giveArea = DataManager.Instance.GetAreaData(approveData.landTradeRequest.areaData.id);
		AreaData receiveArea = DataManager.Instance.GetAreaData(approveData.landTradeRequest.coinCount);

		if (giveArea != null) giveArea.TradeEventOn(tradeOn);
		if (receiveArea != null) receiveArea.TradeEventOn(tradeOn);
	}

	public void AllianceSetData(ApproveData approveData)
	{
		AllianceRequest allianceRequest = approveData.allianceRequest;

		LocalizeTextSet();

		AllianceData myAllianceData = allianceRequest.allianceDataList
			.FirstOrDefault(data => data.playerEnum == DataManager.Instance.playerData.pe);

		foreach (var icon in playerIconList)
		{
			icon.Init();
			icon.SetActive(false);
		}

		//제안에 포함된 플레이어만 켠다
		foreach (var allianceData in allianceRequest.allianceDataList)
		{
			var icon = playerIconList.FirstOrDefault(data => data.playerEnum == allianceData.playerEnum);

			if (icon != null)
			{
				icon.SetActive(true);
			}
		}

		//제안자는 당연히 수락한 상태다
		SetAllianceAnswer(allianceRequest.orderData.playerEnum, true);

		//이미 답한 사람들의 체크 / X 를 되살린다 (다른 요청을 보다가 돌아왔을 때)
		foreach (var pair in approveData.answerDic)
		{
			SetAllianceAnswer(pair.Key, pair.Value);
		}

		//시안: "보상 : N" — 내가 받을 몫
		if (allianceRewardLabel != null)
		{
			int coinCount = myAllianceData != null ? myAllianceData.coinCount : 0;

			allianceRewardLabel.text = LocalizeManager.Instance.GetStrData(LocalizeStatus.Game, 55) + " : " + coinCount;
		}

		SetAcceptBtnMode();

		SetAllianceAcceptEnabled(approveData);
	}

	/// <summary> 아이콘에 체크(수락) / X(거절) 를 찍는다 </summary>
	public void SetAllianceAnswer(PlayerEnum playerEnum, bool approveOn)
	{
		var icon = playerIconList.FirstOrDefault(data => data.playerEnum == playerEnum);

		if (icon != null && icon.IsActive())
		{
			icon.ApproveOn(approveOn);
		}
	}

	/// <summary>
	/// 이미 동맹 중이면 수락 버튼이 "배신 -N🪙" 이 된다 (2026-09-19 시안).
	/// 받아들이면 기존 동맹을 깨는 것이므로 배신 패널티를 미리 보여준다.
	/// </summary>
	void SetAcceptBtnMode()
	{
		bool betrayOn = DataManager.Instance.IsAlliance(DataManager.Instance.playerData.pe);

		LocalizeManager localize = LocalizeManager.Instance;

		if (allianceAcceptLabel != null)
		{
			allianceAcceptLabel.text = localize.GetStrData(LocalizeStatus.Game, betrayOn ? 28 : 59);
		}

		if (allianceAcceptBtn != null)
		{
			allianceAcceptBtn.EnableInClassList("approve-alliance__accept-btn--betray", betrayOn);
		}

		if (allianceBetrayCoinPanel != null)
		{
			allianceBetrayCoinPanel.style.display = betrayOn ? DisplayStyle.Flex : DisplayStyle.None;
		}

		if (betrayOn && allianceBetrayCoinText != null)
		{
			allianceBetrayCoinText.text = "-" + DataManager.Instance.GetNeedBetrayCoin();
		}
	}

	/// <summary> 누군가 거절해 깨진 제안이면 수락(배신)을 막는다 </summary>
	public void SetAllianceAcceptEnabled(ApproveData approveData)
	{
		if (allianceAcceptBtn == null)
			return;

		bool refusedOn = approveData.answerDic.Any(pair => pair.Value == false);

		allianceAcceptBtn.SetEnabled(refusedOn == false);
	}

	int SetPlayerIcon(VisualElement bgImage, VisualElement fgImage, PlayerEnum playerEnum, int currentColorIndex)
	{
		int colorIndex = DataManager.Instance.GetPlayerColorIndex(playerEnum);

		if (colorIndex < 0 || currentColorIndex == colorIndex)
			return currentColorIndex;

		if (currentColorIndex >= 0)
		{
			bgImage.RemoveFromClassList("approve__player-icon-bg--" + currentColorIndex);
			fgImage.RemoveFromClassList("approve__player-icon-fg--" + currentColorIndex);
		}

		bgImage.AddToClassList("approve__player-icon-bg--" + colorIndex);
		fgImage.AddToClassList("approve__player-icon-fg--" + colorIndex);

		return colorIndex;
	}

	public void RefuseBtnClickOn()
	{
		refuseBtnClickEventOn();
	}

	public void AcceptBtnClickOn()
	{
		//이미 동맹 중이어도 막지 않는다 — 버튼이 "배신" 으로 바뀌어 있고,
		//동맹이 성립되면 AllianceResultResponseOn 이 기존 동맹을 깨면서 패널티를 물린다 (2026-09-19)
		acceptBtnClickEventOn();
	}
}

public class ApproveData
{
	public LandTradeRequest landTradeRequest;
	public AllianceRequest allianceRequest;
	public bool isAlliance = false;

	/// <summary> 동맹 제안에 누가 수락/거절했는지. 바의 체크 / X 표시용이다 (2026-09-19) </summary>
	public readonly Dictionary<PlayerEnum, bool> answerDic = new Dictionary<PlayerEnum, bool>();
}
