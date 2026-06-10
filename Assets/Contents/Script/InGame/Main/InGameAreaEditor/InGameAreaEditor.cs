using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class InGameAreaEditor : MonoBehaviour
{
	[SerializeField] private Button editorPanelOnBtn;
	[SerializeField] private RectTransform editorPanel;
	[SerializeField] private Toggle getAreaToggle;
	[SerializeField] private PlayerIconSelector playerIconSelector;
	[SerializeField] private Toggle getDiceToggle;
	[SerializeField] private DiceSelector diceSelector;

	private InGameControllerBase inGameControllerBase;

	private Timer timer;
	private NonePlayPanel nonePlayPanel;

	//[SerializeField] private Timer timer;

	private void Awake()
	{
		editorPanelOnBtn.gameObject.SetActive(false);
		editorPanel.gameObject.SetActive(false);
		playerIconSelector.gameObject.SetActive(false);
		diceSelector.gameObject.SetActive(false);

		Init();
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

		getAreaToggle.OnValueChangedAsObservable()
			.Subscribe(isOn => 
			{
				PlayerEnum playerEnum = isOn ? playerIconSelector.currentPlayerEnum : PlayerEnum.Player_None;
				DataManager.Instance.areaGetPlayerEnum = playerEnum;
				playerIconSelector.gameObject.SetActive(isOn);
			})
			.AddTo(this);

		getDiceToggle.OnValueChangedAsObservable()
			.Subscribe(isOn =>
			{
				int diceCount = isOn ? diceSelector.diceCount : 0;
				DataManager.Instance.diceGetCount = diceCount;

				diceSelector.gameObject.SetActive(isOn);
			})
			.AddTo(this);

		playerIconSelector.SetPlayer(DataManager.Instance.playerData.pe);
	}

	[Conditional("UNITY_EDITOR")]
	[Conditional("UNITY_STANDALONE_WIN")]
	public void ActiveOn(bool activeOn)
    {
		editorPanelOnBtn.gameObject.SetActive(activeOn);

		if (activeOn == false)
		{
			DataManager.Instance.areaGetPlayerEnum = PlayerEnum.Player_None;
			DataManager.Instance.diceGetCount = 0;
			getAreaToggle.isOn = false;
			getDiceToggle.isOn = false;
			editorPanel.gameObject.SetActive(false);
		}
	}

	public void EditorPanelOnBtnClickOn()
    {
        bool activeOn = editorPanel.gameObject.activeSelf;

		editorPanel.gameObject.SetActive(!activeOn);

		if (!activeOn == false)
		{
			DataManager.Instance.areaGetPlayerEnum = PlayerEnum.Player_None;
			DataManager.Instance.diceGetCount = 0;
			getAreaToggle.isOn = false;
			getDiceToggle.isOn = false;
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
