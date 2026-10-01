using UnityEngine;
using UnityEngine.UIElements;
using UniRx;

/// <summary>
/// 팝업 좌하단 "내 코인" 표시. 원본 uGUI MyCoinPanel 과 같은 역할.
/// (uGUI MyCoinPanel 컴포넌트는 Main 씬이 계속 써서 그대로 남겨둔다)
/// </summary>
public static class PopupMyCoinView
{
	/// <summary> my-coin-text / my-coin-plus-btn 을 찾아 연결한다 </summary>
	public static void Bind(VisualElement root, GameObject owner)
	{
		Label coinLabel = root.Q<Label>("my-coin-text");
		UnityEngine.UIElements.Button plusBtn = root.Q<UnityEngine.UIElements.Button>("my-coin-plus-btn");

		DataManager.Instance.CheckUserData();

		if (coinLabel != null)
		{
			DataManager.Instance.userData.myCoin
				.Subscribe(coin => coinLabel.SetNumber(coin))
				.AddTo(owner);
		}

		//코인 충전 페이지는 기획 시트에 따라 삭제 (2026-09-26) - + 버튼을 숨긴다
		if (plusBtn != null)
		{
			plusBtn.style.display = DisplayStyle.None;
			plusBtn.clicked += () =>
			{
				SoundManager.Instance.PlaySe(SeEnum.Yes);
				PopupManager.Instance.BuyCoinPopupOn();
			};
		}
	}
}
