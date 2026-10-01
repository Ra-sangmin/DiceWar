using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 플레이어 아이콘을 눌렀을 때 왼쪽으로 펼쳐지는 상세 패널. (uGUI Canvas → UI Toolkit 이식)
/// 원본은 선택한 아이콘 밑으로 파고들어 y 를 맞췄는데, 아이콘이 UI Toolkit 요소가 되었으므로
/// 아이콘의 worldBound 를 읽어 같은 위치에 직접 배치한다.
/// </summary>
public class PlayerDataPanel : MonoBehaviour
{
	[SerializeField] SkillCard skillCard;

	/// <summary> 원본 PlayerDataPanel 루트의 x 오프셋 (아이콘 오른쪽 끝 기준 -170) </summary>
	private const float rightOffset = 170f;

	private VisualElement panelRoot;

	private readonly List<PlayerAllianceIconElement> playerAllianceIconList = new List<PlayerAllianceIconElement>();

	private PlayerIconElement selectPlayerIcon;

	private float disableDelay;
	private bool panelActiveOn = false;

	/// <summary> UI Toolkit 요소 연결 </summary>
	public void InitView(VisualElement root)
	{
		panelRoot = root.Q<VisualElement>("player-data-panel");

		VisualElement skillIcon = root.Q<VisualElement>("player-data-skill-icon");

		if (skillCard != null)
		{
			skillCard.InitView(skillIcon);
		}

		SetPanelActive(false);
	}

	public void SetPanelActive(bool activeOn)
	{
		panelActiveOn = activeOn;

		if (panelRoot != null)
		{
			panelRoot.style.display = activeOn ? DisplayStyle.Flex : DisplayStyle.None;
		}
	}

	public void SetData(PlayerIconElement playerIcon)
	{
		if (panelRoot == null)
			return;

		selectPlayerIcon = playerIcon;

		SetPanelActive(true);

		SetPos();

		SetPlayerData();

		skillCard.SetCountIcon(selectPlayerIcon.playerEnum);

		disableDelay = 1;
	}

	/// <summary> 선택한 아이콘과 위/오른쪽 위치를 맞춘다 (원본 : 아이콘 기준 anchoredPosition(-170, 0)) </summary>
	void SetPos()
	{
		Rect iconRect = selectPlayerIcon.Root.worldBound;

		if (float.IsNaN(iconRect.x))
			return;

		float panelWidth = panelRoot.panel.visualTree.worldBound.width;

		panelRoot.style.top = iconRect.y + 0.5f;
		panelRoot.style.right = panelWidth - iconRect.xMax + rightOffset;
	}

	void SetPlayerData()
	{
		List<AllianceData> allianceDataList = DataManager.Instance.GetAllAlliance(selectPlayerIcon.playerEnum).allianceDataList;

		foreach (var allianceIcon in playerAllianceIconList)
		{
			allianceIcon.SetActive(false);
		}

		if (playerAllianceIconList.Count < allianceDataList.Count)
		{
			for (int i = playerAllianceIconList.Count; i < allianceDataList.Count; i++)
			{
				PlayerAllianceIconElement allianceIcon = new PlayerAllianceIconElement();

				//원본 HorizontalLayoutGroup 은 reverseArrangement 라 SkillCardPanel 이 항상 오른쪽 끝이다
				//(USS 에서 flex-direction: row-reverse 로 처리하므로 그냥 뒤에 붙이면 왼쪽으로 쌓인다)
				panelRoot.Add(allianceIcon.Root);

				playerAllianceIconList.Add(allianceIcon);
			}
		}

		for (int i = 0; i < allianceDataList.Count; i++)
		{
			playerAllianceIconList[i].SetActive(true);
			playerAllianceIconList[i].SetPlayerData(allianceDataList[i]);
		}
	}

	void Update()
	{
		DisableDelayCheck();
	}

	void DisableDelayCheck()
	{
		if (panelActiveOn == false)
			return;

		disableDelay -= Time.deltaTime;

		if (disableDelay < 0)
		{
			SetPanelActive(false);
		}
	}
}
