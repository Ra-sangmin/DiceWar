using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// "나의 턴" 토스트. 좌측에 내 플레이어 아이콘이 붙는다. (uGUI Canvas → UI Toolkit 이식)
/// </summary>
public class YourTurnPopup : ToastPopup
{
	protected override void OnUIReady(VisualElement root)
	{
		base.OnUIReady(root);

		SetPlayerIcon();
	}

	void SetPlayerIcon()
	{
		if (iconElement == null || textLabel == null)
			return;

		textLabel.AddToClassList("toast-popup__text--with-icon");

		DataManager dataManager = DataManager.Instance;

		PlayerData myData = dataManager.GetMyPlayerData();

		int colorIndex = myData != null
			? dataManager.GetPlayerColorIndex(myData.pe)
			: dataManager.GetPlayerColorIndex(dataManager.playerData.pe);

		//색 목록이 아직 안 채워졌다면 내가 고른 색을 그대로 쓴다 (아이콘이 통째로 안 나오는 것보다 낫다)
		if (colorIndex < 0)
			colorIndex = dataManager.playerData.ci;

		if (colorIndex < 0)
			return;

		SetIconColorIndex(colorIndex);
	}

	/// <summary> 아이콘을 다른 플레이어 색으로 바꾼다 (배신 알림처럼 내가 아닌 사람을 보여줄 때, 2026-09-26) </summary>
	public void SetPlayerIcon(PlayerEnum playerEnum)
	{
		int colorIndex = DataManager.Instance.GetPlayerColorIndex(playerEnum);

		if (colorIndex < 0)
			colorIndex = (int)playerEnum;

		//준비되면 기본 아이콘(내 색)을 그린 다음에 덮어쓰도록 순서대로 실행된다
		panelUI.Run(() => SetIconColorIndex(colorIndex));
	}

	void SetIconColorIndex(int colorIndex)
	{
		if (iconElement == null)
			return;

		for (int i = 0; i < 7; i++)
		{
			iconElement.RemoveFromClassList("popup-player-icon--" + i);
		}

		iconElement.AddToClassList("popup-player-icon--" + colorIndex);
		iconElement.style.display = DisplayStyle.Flex;
	}
}
