using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DiceSelector : MonoBehaviour
{
	[SerializeField] List<Sprite> spriteiconList = new List<Sprite>();
	[SerializeField] Image iconImage;

	public int diceCount = 1;

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
		int iconIndex = diceCount - 1;
		iconImage.sprite = spriteiconList[iconIndex];

		DataManager.Instance.diceGetCount = diceCount;
	}
}
