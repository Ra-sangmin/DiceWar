using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

/// <summary>
/// 내가 제안한 동맹의 진행 바 (멀티 전용).
/// 2026-09-19 시안: "동맹 제안 | 제안한 플레이어 아이콘 | 보상 : N | 철회"
/// 아이콘에 승인(체크) / 거절(X) 이 찍혀서 누가 수락했는지 바로 보인다.
/// </summary>
public class MyAllianceApproveController : MonoBehaviour
{
	//시안 요청: 결과(O/X)를 충분히 볼 수 있게 2초 기다렸다가 1초 동안 사라진다
	private const float FadeDelay = 2f;
	private const float FadeDuration = 1f;

	private VisualElement panelRoot;
	private VisualElement barRoot;
	private VisualElement iconPanel;
	private Label titleLabel;
	private Label rewardLabel;
	private Label cancelLabel;

	//결과(성립/거절) 상태에서만 보이는 "요청 승인 N/M"
	private Label resultTitleLabel;
	private Label countLabel;

	private UnityEngine.UIElements.Button cancelBtn;

	private readonly List<MyAllianceApproveIconElement> playerIconList = new List<MyAllianceApproveIconElement>();

	private IVisualElementScheduledItem hideSchedule;

	/// <summary> 철회 버튼. InGameControllerMulti 가 연결한다 </summary>
	public UnityAction cancelBtnClickEventOn;

	/// <summary> UI Toolkit 요소 연결 </summary>
	public void InitView(VisualElement root)
	{
		panelRoot = root.Q<VisualElement>("alliance-approve-panel");
		barRoot = root.Q<VisualElement>("alliance-approve-bar");
		iconPanel = root.Q<VisualElement>("alliance-approve-icon-panel");
		titleLabel = root.Q<Label>("alliance-approve-title");
		rewardLabel = root.Q<Label>("alliance-approve-reward");
		cancelLabel = root.Q<Label>("alliance-approve-cancel-label");
		resultTitleLabel = root.Q<Label>("alliance-approve-result-title");
		countLabel = root.Q<Label>("alliance-approve-count");
		cancelBtn = root.Q<UnityEngine.UIElements.Button>("alliance-approve-cancel-btn");

		if (cancelBtn != null)
		{
			cancelBtn.clicked += CancelBtnClickOn;
		}

		LocalizeTextSet();

		if (iconPanel == null)
			return;

		playerIconList.Clear();

		//InitView 가 두 번 불려도 아이콘이 겹쳐 쌓이지 않게 비우고 시작한다
		//주의 : UXML 에 있던 결과 라벨 두 개를 아래에서 다시 붙이므로 자원 반환(RecursiveReleaseResources) 을 쓰면 안 된다 (2026-09-29)
		iconPanel.Clear();

		//원본은 Icon_0 ~ Icon_6 을 미리 배치해 두었다. 같은 순서로 만들어 넣는다
		for (int i = 0; i < 7; i++)
		{
			PlayerEnum playerEnum = (PlayerEnum)i;

			MyAllianceApproveIconElement icon = new MyAllianceApproveIconElement(playerEnum);

			iconPanel.Insert(i, icon.Root);

			playerIconList.Add(icon);
		}

		//Clear 로 같이 떨어져 나간 결과 라벨을 아이콘 뒤에 다시 붙인다
		if (resultTitleLabel != null) iconPanel.Add(resultTitleLabel);
		if (countLabel != null) iconPanel.Add(countLabel);

		Init();
	}

	void LocalizeTextSet()
	{
		LocalizeManager localize = LocalizeManager.Instance;

		if (localize == null)
			return;

		if (titleLabel != null) titleLabel.text = localize.GetStrData(LocalizeStatus.Game, 32);
		if (cancelLabel != null) cancelLabel.text = localize.GetStrData(LocalizeStatus.Game, 57);
		if (resultTitleLabel != null) resultTitleLabel.text = localize.GetStrData(LocalizeStatus.Game, 45);
	}

	private void Init()
	{
		foreach (var icon in playerIconList)
		{
			icon.Init();
			icon.SetActive(false);
		}

		SetResultMode(false);

		SetAlpha(0f, 0f, 0f);
	}

	void SetResultMode(bool resultOn)
	{
		if (barRoot == null)
			return;

		if (resultOn)
		{
			barRoot.AddToClassList("alliance-approve--result");
		}
		else
		{
			barRoot.RemoveFromClassList("alliance-approve--result");
		}
	}

	public void SetData(AllianceRequest allianceRequest)
	{
		if (panelRoot == null)
			return;

		Init();

		LocalizeTextSet();

		var playerEnumList = allianceRequest.allianceDataList.Select(data => data.playerEnum).ToList();
		SetActiveList(playerEnumList);

		ApprovedOn(allianceRequest.orderData.playerEnum, true);

		SetRewardText(allianceRequest.orderData.coinCount);

		SetCancelEnabled(true);

		//DOFade(1, 0.25f)
		SetPanelVisible(true);
		panelRoot.schedule.Execute(() => SetAlpha(1f, 0.25f, 0f)).ExecuteLater(0);
	}

	/// <summary> 시안의 "보상 : N" — 제안자(나)가 받을 몫이다 </summary>
	void SetRewardText(int coinCount)
	{
		if (rewardLabel == null)
			return;

		rewardLabel.text = LocalizeManager.Instance.GetStrData(LocalizeStatus.Game, 55) + " : " + coinCount;
	}

	public void SetActiveList(List<PlayerEnum> playerEnumList)
	{
		foreach (var playerEnum in playerEnumList)
		{
			var icon = playerIconList.FirstOrDefault(data => data.playerEnum == playerEnum);

			if (icon != null)
			{
				icon.SetActive(true);
			}
		}
	}

	public void ApprovedOn(PlayerEnum playerEnum, bool approvedOn)
	{
		var icon = playerIconList.FirstOrDefault(data => data.playerEnum == playerEnum);

		if (icon != null)
		{
			icon.ApproveOn(approvedOn);
		}
	}

	void CancelBtnClickOn()
	{
		if (cancelBtn == null || cancelBtn.enabledSelf == false)
			return;

		SoundManager.Instance.PlaySe(SeEnum.Yes);

		//두 번 눌리지 않게 바로 막는다. 실제 정리는 서버 응답(철회 = 제안자 본인의 거절)에서 한다
		SetCancelEnabled(false);

		if (cancelBtnClickEventOn != null)
		{
			cancelBtnClickEventOn();
		}
	}

	void SetCancelEnabled(bool enableOn)
	{
		if (cancelBtn != null)
		{
			cancelBtn.SetEnabled(enableOn);
		}
	}

	/// <summary>
	/// 성립 / 거절이 확정됐다. 시안처럼 바를 "아이콘 + 요청 승인 N/M" 알약으로 바꾸고 사라진다.
	/// (2026-09-19)
	/// </summary>
	public void SetResultOn()
	{
		if (panelRoot == null || panelRoot.resolvedStyle.display == DisplayStyle.None)
			return;

		SetApprovedCountText();

		SetResultMode(true);

		FadeOn();
	}

	void SetApprovedCountText()
	{
		if (countLabel == null)
			return;

		var activeList = playerIconList.Where(data => data.IsActive()).ToList();

		int currentCount = activeList.Count(data => data.approvedOn);

		countLabel.text = currentCount + "/" + activeList.Count;
	}

	/// <summary> 결과를 보여준 뒤 사라진다 </summary>
	public void FadeOn()
	{
		if (panelRoot == null)
			return;

		SetCancelEnabled(false);

		SetAlpha(0f, FadeDuration, FadeDelay);

		if (hideSchedule != null)
		{
			hideSchedule.Pause();
		}

		hideSchedule = panelRoot.schedule.Execute(() => SetPanelVisible(false))
			.StartingIn((long)((FadeDelay + FadeDuration) * 1000f));
	}

	void SetPanelVisible(bool activeOn)
	{
		if (panelRoot == null)
			return;

		if (activeOn && hideSchedule != null)
		{
			hideSchedule.Pause();
			hideSchedule = null;
		}

		panelRoot.style.display = activeOn ? DisplayStyle.Flex : DisplayStyle.None;
	}

	void SetAlpha(float alpha, float duration, float delay)
	{
		if (panelRoot == null)
			return;

		panelRoot.style.transitionDuration = new List<TimeValue> { new TimeValue(duration, TimeUnit.Second) };
		panelRoot.style.transitionDelay = new List<TimeValue> { new TimeValue(delay, TimeUnit.Second) };
		panelRoot.style.opacity = alpha;
	}
}
