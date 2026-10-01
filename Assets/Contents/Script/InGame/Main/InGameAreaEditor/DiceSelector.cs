using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 개발용 치트 패널의 주사위 개수 선택기. (uGUI → UI Toolkit 이식)
/// </summary>
public class DiceSelector : MonoBehaviour
{
	[SerializeField] List<Sprite> spriteiconList = new List<Sprite>();

	private VisualElement iconElement;

	public int diceCount = 1;

	public void InitView(VisualElement iconElement)
	{
		this.iconElement = iconElement;
		SetIcon();
	}

	public void SetActiveOn(bool activeOn)
	{
		if (iconElement != null)
			iconElement.style.display = activeOn ? DisplayStyle.Flex : DisplayStyle.None;
	}

	public void DiceChangeOn()
	{
		diceCount++;

		if (diceCount > 6)
		{
			diceCount = 1;
		}

		SetData();
	}

	private void SetData()
	{
		SetIcon();

		DataManager.Instance.diceGetCount = diceCount;
	}

	void SetIcon()
	{
		if (iconElement == null)
			return;

		int iconIndex = diceCount - 1;

		if (iconIndex >= 0 && iconIndex < spriteiconList.Count)
			iconElement.style.backgroundImage = new StyleBackground(spriteiconList[iconIndex]);
	}
}
