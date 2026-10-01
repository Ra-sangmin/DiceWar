using UnityEngine.Events;
using UnityEngine.UIElements;

/// <summary>
/// 거래 / 동맹 / 배신 팝업에서 쓰는 플레이어 선택 아이콘 (원본 PlayerToggleIcon 프리팹 120x120).
/// (uGUI PlayerToggleIcon 컴포넌트는 Main 씬 프리팹들이 아직 써서 그대로 남겨둔다)
/// </summary>
public class PlayerToggleIconElement
{
	private readonly VisualElement root;
	private readonly VisualElement bgImage;
	private readonly VisualElement fgImage;
	private readonly Label discriptLabel;

	private int iconColorIndex = -1;

	public PlayerEnum playerEnum = PlayerEnum.Player_None;
	public CoinBoxElement coinBox;

	/// <summary> 원본 Toggle.isOn </summary>
	public bool isOn = false;

	public UnityAction clickOn = () => { };

	public VisualElement Root => root;

	public PlayerToggleIconElement(bool betrayStyleOn = false)
	{
		root = new VisualElement();
		root.AddToClassList("player-toggle");

		bgImage = new VisualElement();
		bgImage.AddToClassList("player-toggle__bg");
		bgImage.pickingMode = PickingMode.Ignore;

		fgImage = new VisualElement();
		fgImage.AddToClassList("player-toggle__fg");
		fgImage.pickingMode = PickingMode.Ignore;

		//원본 계층 순서 (BG → FG)
		root.Add(bgImage);
		root.Add(fgImage);

		discriptLabel = new Label(string.Empty);
		discriptLabel.AddToClassList("player-toggle__discript");
		discriptLabel.pickingMode = PickingMode.Ignore;
		root.Add(discriptLabel);

		coinBox = new CoinBoxElement(betrayStyleOn);
		root.Add(coinBox.Root);

		root.RegisterCallback<ClickEvent>(evt =>
		{
			//코인 박스 안쪽 클릭은 아이콘 선택으로 치지 않는다
			if (evt.target is VisualElement target && target != root && coinBox.Root.Contains(target))
				return;

			clickOn();
		});
	}

	public void SetDiscriptText(string text)
	{
		discriptLabel.text = text;
	}

	public void SetPlayerData(PlayerEnum playerEnum)
	{
		this.playerEnum = playerEnum;

		int colorIndex = DataManager.Instance.GetPlayerColorIndex(playerEnum);

		if (colorIndex < 0 || iconColorIndex == colorIndex)
			return;

		if (iconColorIndex >= 0)
		{
			bgImage.RemoveFromClassList("player-toggle__bg--" + iconColorIndex);
			fgImage.RemoveFromClassList("player-toggle__fg--" + iconColorIndex);
		}

		iconColorIndex = colorIndex;

		bgImage.AddToClassList("player-toggle__bg--" + iconColorIndex);
		fgImage.AddToClassList("player-toggle__fg--" + iconColorIndex);
	}

	public void ToggleOn()
	{
		SelectOn(!isOn);
	}

	public void SelectOn(bool selectOn)
	{
		isOn = selectOn;

		fgImage.style.display = selectOn ? DisplayStyle.Flex : DisplayStyle.None;

		coinBox.SelectOn(selectOn);
	}

	public void SetActive(bool activeOn)
	{
		root.style.display = activeOn ? DisplayStyle.Flex : DisplayStyle.None;
	}
}
