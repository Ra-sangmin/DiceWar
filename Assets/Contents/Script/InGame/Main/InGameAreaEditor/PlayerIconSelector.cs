using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 개발용 치트 패널의 플레이어 아이콘 선택기. (uGUI → UI Toolkit 이식)
/// 스프라이트 목록은 씬에 직렬화돼 있으므로 필드 이름을 유지한다.
/// </summary>
public class PlayerIconSelector : MonoBehaviour
{
	[SerializeField] List<Sprite> spriteiconList = new List<Sprite>();

	private VisualElement iconElement;
	private VisualElement myTextElement;

	public PlayerEnum currentPlayerEnum { get; private set; }

	private PlayerEnum myPlayerEnum;

	public void InitView(VisualElement iconElement, VisualElement myTextElement)
	{
		this.iconElement = iconElement;
		this.myTextElement = myTextElement;
	}

	public void SetActiveOn(bool activeOn)
	{
		if (iconElement != null)
			iconElement.style.display = activeOn ? DisplayStyle.Flex : DisplayStyle.None;
	}

	public void SetPlayer(PlayerEnum playerEnum)
	{
		myPlayerEnum = playerEnum;

		this.currentPlayerEnum = playerEnum;
		SetData();
	}

	public void PlayerChangeOn()
	{
		currentPlayerEnum++;

		if (((int)currentPlayerEnum) >= DataManager.Instance.num_player || currentPlayerEnum > PlayerEnum.Player_6) 
		{
			currentPlayerEnum = PlayerEnum.Player_0;
		}

		DataManager.Instance.areaGetPlayerEnum = currentPlayerEnum;

		SetData();
	}

	public void SetData()
	{
		if (iconElement == null)
			return;

		int colorIndex = DataManager.Instance.GetPlayerColorIndex(currentPlayerEnum);

		if (colorIndex >= 0 && colorIndex < spriteiconList.Count)
			iconElement.style.backgroundImage = new StyleBackground(spriteiconList[colorIndex]);

		bool myTextActiveOn = myPlayerEnum == currentPlayerEnum;

		if (myTextElement != null)
			myTextElement.style.display = myTextActiveOn ? DisplayStyle.Flex : DisplayStyle.None;
	}
}
