using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 조기 종료 안내 바. (uGUI Canvas → UI Toolkit 이식)
/// </summary>
[RequireComponent(typeof(PanelRenderer))]
public class LeaveEarlyPopup : MonoBehaviour
{
	private PanelUI panelUI;

	private Label getCoinLabel;
	private Label titleLabel;
	private Label finishLabel;
	private Label continueLabel;

	private InGameControllerBase inGameController;

	void Awake()
	{
		panelUI = new PanelUI(this, OnUIReady);
	}

	/// <summary> PanelRenderer 의 UI 가 준비되면 한 번 호출된다 (예전 Awake 의 요소 찾기 / 이벤트 등록, 2026-09-29) </summary>
	private void OnUIReady(VisualElement root)
	{
		getCoinLabel = root.Q<Label>("leave-early-coin-text");
		titleLabel = root.Q<Label>("leave-early-title");
		finishLabel = root.Q<Label>("leave-early-finish-label");
		continueLabel = root.Q<Label>("leave-early-continue-label");

		UnityEngine.UIElements.Button finishBtn = root.Q<UnityEngine.UIElements.Button>("leave-early-finish-btn");
		UnityEngine.UIElements.Button continueBtn = root.Q<UnityEngine.UIElements.Button>("leave-early-continue-btn");

		if (finishBtn != null)
		{
			finishBtn.clicked += () => { PlayClickSe(); FinishBtnClickOn(true); };
		}

		if (continueBtn != null)
		{
			continueBtn.clicked += () => { PlayClickSe(); FinishBtnClickOn(false); };
		}

		LocalizeTextSet();
	}

	private void OnDestroy()
	{
		panelUI?.Dispose();
	}

	void PlayClickSe()
	{
		SoundManager.Instance.PlaySe(SeEnum.Yes);
	}

	void LocalizeTextSet()
	{
		LocalizeManager localize = LocalizeManager.Instance;

		if (titleLabel != null) titleLabel.text = localize.GetStrData(LocalizeStatus.Game, 19);
		if (finishLabel != null) finishLabel.text = localize.GetStrData(LocalizeStatus.Game, 20);
		if (continueLabel != null) continueLabel.text = localize.GetStrData(LocalizeStatus.Game, 10);
	}

	public void DataInit(InGameControllerBase inGameController, int getCoin)
	{
		this.inGameController = inGameController;

		DataManager.Instance.leaveEarlyPopupReadyOn = false;
		DataManager.Instance.leaveEarlyPopupOpenOn = true;

		//글자는 UI 가 준비된 뒤에 (PanelRenderer, 2026-09-29)
		panelUI.Run(() =>
		{
			if (getCoinLabel != null)
			{
				getCoinLabel.text = $"+{getCoin}";
			}
		});
	}

	public void FinishBtnClickOn()
	{
		inGameController.LeaveEarlyFinishOn(true);
	}

	public void ContinueBtnClickOn()
	{
		inGameController.LeaveEarlyFinishOn(false);
	}

	public void FinishBtnClickOn(bool finish)
	{
		inGameController.LeaveEarlyFinishOn(finish);

		Destroy(gameObject);
	}
}
