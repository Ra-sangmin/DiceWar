using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 코인이 모자랄 때 화면 중앙에 잠깐 뜨는 안내. (uGUI Canvas → UI Toolkit 이식)
/// 페이드 아웃 처리는 ToastPopup 을 그대로 쓴다.
/// </summary>
public class NeedCoinPopup : ToastPopup
{
	private Label coinLabel;

	protected override void OnUIReady(VisualElement root)
	{
		base.OnUIReady(root);

		coinLabel = root.Q<Label>("need-coin-text");
	}

	public void ActiveOn(int needCoin)
	{
		maxAlphaValue = 1;

		base.ActiveOn();

		//UI 가 준비된 뒤에 글자를 넣는다 (PanelRenderer, 2026-09-29)
		panelUI.Run(() =>
		{
			if (coinLabel != null)
			{
				coinLabel.text = $"{needCoin} Coins";
			}
		});
	}
}
