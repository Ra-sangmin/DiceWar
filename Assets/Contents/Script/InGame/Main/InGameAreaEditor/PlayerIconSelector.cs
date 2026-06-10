using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerIconSelector : MonoBehaviour
{
	[SerializeField] List<Sprite> spriteiconList = new List<Sprite>();
	[SerializeField] Image iconImage;
	[SerializeField] Text myText;
	public PlayerEnum currentPlayerEnum { get; private set; }

	private PlayerEnum myPlayerEnum;

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
		int colorIndex = DataManager.Instance.GetPlayerColorIndex(currentPlayerEnum);
		iconImage.sprite = spriteiconList[colorIndex];

		bool myTextActiveOn = myText != null && myPlayerEnum == currentPlayerEnum;
		myText.gameObject.SetActive(myTextActiveOn);
	}
}
