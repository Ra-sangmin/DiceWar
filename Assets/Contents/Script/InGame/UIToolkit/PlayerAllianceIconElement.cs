using UnityEngine.UIElements;

/// <summary>
/// 플레이어 상세 패널에 가로로 쌓이는 동맹 아이콘.
/// 기존 uGUI PlayerAllianceIcon 프리팹(165x120)의 UI Toolkit 버전.
/// </summary>
public class PlayerAllianceIconElement
{
	private readonly VisualElement root;
	private readonly VisualElement fgImage;
	private readonly Label coinText;

	private int fgColorIndex = -1;

	public VisualElement Root => root;

	public PlayerAllianceIconElement()
	{
		root = new VisualElement();
		root.AddToClassList("player-alliance-icon");
		root.pickingMode = PickingMode.Ignore;

		fgImage = new VisualElement();
		fgImage.AddToClassList("player-alliance-icon__fg");
		fgImage.pickingMode = PickingMode.Ignore;
		root.Add(fgImage);

		VisualElement coinIcon = new VisualElement();
		coinIcon.AddToClassList("player-alliance-icon__coin");
		coinIcon.pickingMode = PickingMode.Ignore;
		root.Add(coinIcon);

		coinText = new Label("0");
		coinText.AddToClassList("player-alliance-icon__text");
		coinText.pickingMode = PickingMode.Ignore;
		root.Add(coinText);
	}

	public void SetActive(bool activeOn)
	{
		root.style.display = activeOn ? DisplayStyle.Flex : DisplayStyle.None;
	}

	public void SetPlayerData(AllianceData allianceData)
	{
		int colorIndex = DataManager.Instance.GetPlayerColorIndex(allianceData.playerEnum);

		if (fgColorIndex != colorIndex)
		{
			if (fgColorIndex >= 0)
			{
				fgImage.RemoveFromClassList("player-alliance-icon__fg--" + fgColorIndex);
			}

			fgColorIndex = colorIndex;

			fgImage.AddToClassList("player-alliance-icon__fg--" + fgColorIndex);
		}

		coinText.SetNumber(allianceData.coinCount);
	}
}
