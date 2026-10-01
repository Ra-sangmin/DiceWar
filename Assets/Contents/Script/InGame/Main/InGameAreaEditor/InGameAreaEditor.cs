using UnityEngine;
using UnityEngine.UIElements;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

/// <summary>
/// 개발용 치트 패널. (uGUI Canvas → UI Toolkit 이식)
/// 화면 요소는 InGameView.uxml 의 area-editor-* 이고, 이 클래스는 그 요소들을 조작한다.
/// 원본과 같이 [Conditional] 로 에디터 / 윈도우 빌드에서만 동작한다.
/// </summary>
public class InGameAreaEditor : MonoBehaviour
{
	/// <summary> 토글이 켜졌을 때 붙는 클래스 (원본 ActiveImage 대신) </summary>
	const string ToggleOnClass = "area-editor__btn--on";

	[SerializeField] private PlayerIconSelector playerIconSelector;
	[SerializeField] private DiceSelector diceSelector;

	private UnityEngine.UIElements.Button editorPanelOnBtn;
	private VisualElement editorPanel;
	private UnityEngine.UIElements.Button getAreaToggle;
	private UnityEngine.UIElements.Button getDiceToggle;

	private bool getAreaOn = false;
	private bool getDiceOn = false;

	private InGameControllerBase inGameControllerBase;

	private Timer timer;
	private NonePlayPanel nonePlayPanel;

	/// <summary> 인게임 UIDocument 루트를 받아 요소를 찾는다. InGameControllerBase.Start 에서 호출한다. </summary>
	public void InitView(VisualElement root)
	{
		if (root == null)
			return;

		editorPanelOnBtn = root.Q<UnityEngine.UIElements.Button>("area-editor-on-btn");
		editorPanel = root.Q<VisualElement>("area-editor-panel");
		getAreaToggle = root.Q<UnityEngine.UIElements.Button>("area-editor-get-area-toggle");
		getDiceToggle = root.Q<UnityEngine.UIElements.Button>("area-editor-get-dice-toggle");

		if (playerIconSelector != null)
		{
			playerIconSelector.InitView(root.Q<VisualElement>("area-editor-player-icon"),
									   root.Q<VisualElement>("area-editor-player-my-text"));
		}

		if (diceSelector != null)
		{
			diceSelector.InitView(root.Q<VisualElement>("area-editor-dice-icon"));
		}

		UnityEngine.UIElements.Button addTimeBtn = root.Q<UnityEngine.UIElements.Button>("area-editor-add-time-btn");
		UnityEngine.UIElements.Button addSkillBtn = root.Q<UnityEngine.UIElements.Button>("area-editor-add-skill-btn");
		UnityEngine.UIElements.Button playerIconBtn = root.Q<UnityEngine.UIElements.Button>("area-editor-player-icon");
		UnityEngine.UIElements.Button diceIconBtn = root.Q<UnityEngine.UIElements.Button>("area-editor-dice-icon");

		if (editorPanelOnBtn != null) editorPanelOnBtn.clicked += EditorPanelOnBtnClickOn;
		if (addTimeBtn != null) addTimeBtn.clicked += AddTimeBtnClickOn;
		if (addSkillBtn != null) addSkillBtn.clicked += AddSkillCardBtnClickOn;
		if (getAreaToggle != null) getAreaToggle.clicked += () => GetAreaToggleOn(!getAreaOn);
		if (getDiceToggle != null) getDiceToggle.clicked += () => GetDiceToggleOn(!getDiceOn);
		if (playerIconBtn != null) playerIconBtn.clicked += () => { if (playerIconSelector != null) playerIconSelector.PlayerChangeOn(); };
		if (diceIconBtn != null) diceIconBtn.clicked += () => { if (diceSelector != null) diceSelector.DiceChangeOn(); };

		SetActive(editorPanelOnBtn, false);
		SetActive(editorPanel, false);

		if (playerIconSelector != null) playerIconSelector.SetActiveOn(false);
		if (diceSelector != null) diceSelector.SetActiveOn(false);

		Init();
	}

	static void SetActive(VisualElement element, bool activeOn)
	{
		if (element != null)
			element.style.display = activeOn ? DisplayStyle.Flex : DisplayStyle.None;
	}

	static bool IsActive(VisualElement element)
	{
		return element != null && element.resolvedStyle.display == DisplayStyle.Flex;
	}

	public void SetData(InGameControllerBase inGameControllerBase)
	{
		this.inGameControllerBase = inGameControllerBase;

		if (this.inGameControllerBase != null)
		{
			timer = inGameControllerBase.mapController.timer;

			nonePlayPanel = inGameControllerBase.mapController.inGameBottomController.nonePlayPanel;
		}
	}

	[Conditional("UNITY_EDITOR")]
	[Conditional("UNITY_STANDALONE_WIN")]
	private void Init()
	{
		DataManager.Instance.areaGetPlayerEnum = PlayerEnum.Player_None;

		if (playerIconSelector != null)
			playerIconSelector.SetPlayer(DataManager.Instance.playerData.pe);
	}

	void GetAreaToggleOn(bool isOn)
	{
		getAreaOn = isOn;

		if (getAreaToggle != null)
			getAreaToggle.EnableInClassList(ToggleOnClass, isOn);

		PlayerEnum playerEnum = isOn && playerIconSelector != null ? playerIconSelector.currentPlayerEnum : PlayerEnum.Player_None;
		DataManager.Instance.areaGetPlayerEnum = playerEnum;

		if (playerIconSelector != null)
			playerIconSelector.SetActiveOn(isOn);
	}

	void GetDiceToggleOn(bool isOn)
	{
		getDiceOn = isOn;

		if (getDiceToggle != null)
			getDiceToggle.EnableInClassList(ToggleOnClass, isOn);

		int diceCount = isOn && diceSelector != null ? diceSelector.diceCount : 0;
		DataManager.Instance.diceGetCount = diceCount;

		if (diceSelector != null)
			diceSelector.SetActiveOn(isOn);
	}

	[Conditional("UNITY_EDITOR")]
	[Conditional("UNITY_STANDALONE_WIN")]
	public void ActiveOn(bool activeOn)
	{
		SetActive(editorPanelOnBtn, activeOn);

		if (activeOn == false)
		{
			DataManager.Instance.areaGetPlayerEnum = PlayerEnum.Player_None;
			DataManager.Instance.diceGetCount = 0;
			GetAreaToggleOn(false);
			GetDiceToggleOn(false);
			SetActive(editorPanel, false);
		}
	}

	public void EditorPanelOnBtnClickOn()
	{
		bool activeOn = IsActive(editorPanel);

		SetActive(editorPanel, !activeOn);

		if (!activeOn == false)
		{
			DataManager.Instance.areaGetPlayerEnum = PlayerEnum.Player_None;
			DataManager.Instance.diceGetCount = 0;
			GetAreaToggleOn(false);
			GetDiceToggleOn(false);
		}
	}

	public void AddTimeBtnClickOn()
	{
		if (timer == null)
			return;

		timer.timerCurrentDelay += 10;
	}

	public void AddSkillCardBtnClickOn()
	{
		if (nonePlayPanel == null)
			return;

		nonePlayPanel.SkillUseOn(1);
	}
}
