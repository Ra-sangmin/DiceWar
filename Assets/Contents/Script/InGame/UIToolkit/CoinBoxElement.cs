using UnityEngine.UIElements;
using UniRx;

/// <summary>
/// 플레이어 아이콘 아래에 붙는 코인 입력 박스. 기존 uGUI CoinBox 의 UI Toolkit 버전.
/// (uGUI CoinBox 컴포넌트는 Main 씬 프리팹들이 아직 써서 그대로 남겨둔다)
/// </summary>
public class CoinBoxElement
{
	private readonly VisualElement root;
	private readonly VisualElement contens;
	private readonly VisualElement addCoinPanel;
	private readonly Label coinLabel;

	public ReactiveProperty<int> coinCount = new ReactiveProperty<int>(3);

	public VisualElement Root => root;

	public CoinBoxElement(bool betrayStyleOn = false)
	{
		root = new VisualElement();
		root.AddToClassList("coin-box");

		contens = new VisualElement();
		contens.AddToClassList("coin-box__contens");

		if (betrayStyleOn)
		{
			contens.AddToClassList("coin-box__contens--betray");
		}

		root.Add(contens);

		UnityEngine.UIElements.Button currentPanel = new UnityEngine.UIElements.Button();
		currentPanel.AddToClassList("coin-box__current");
		currentPanel.clicked += () => SetActiveAddCoinPanel(true);
		contens.Add(currentPanel);

		VisualElement bg = new VisualElement();
		bg.AddToClassList("coin-box__bg");
		bg.pickingMode = PickingMode.Ignore;
		currentPanel.Add(bg);

		VisualElement fg = new VisualElement();
		fg.AddToClassList("coin-box__fg");
		fg.pickingMode = PickingMode.Ignore;
		currentPanel.Add(fg);

		VisualElement icon = new VisualElement();
		icon.AddToClassList("coin-box__icon");
		icon.pickingMode = PickingMode.Ignore;
		currentPanel.Add(icon);

		coinLabel = new Label("3");
		coinLabel.AddToClassList("coin-box__text");
		coinLabel.pickingMode = PickingMode.Ignore;
		currentPanel.Add(coinLabel);

		addCoinPanel = new VisualElement();
		addCoinPanel.AddToClassList("coin-box__add-panel");
		contens.Add(addCoinPanel);

		UnityEngine.UIElements.Button minusBtn = new UnityEngine.UIElements.Button();
		minusBtn.AddToClassList("coin-box__step-btn");
		minusBtn.AddToClassList("coin-box__step-btn--minus");
		minusBtn.clicked += () => CoinCountChange(false);
		addCoinPanel.Add(minusBtn);

		VisualElement minusIcon = new VisualElement();
		minusIcon.AddToClassList("coin-box__step-icon--minus");
		minusIcon.pickingMode = PickingMode.Ignore;
		minusBtn.Add(minusIcon);

		UnityEngine.UIElements.Button plusBtn = new UnityEngine.UIElements.Button();
		plusBtn.AddToClassList("coin-box__step-btn");
		plusBtn.AddToClassList("coin-box__step-btn--plus");
		plusBtn.clicked += () => CoinCountChange(true);
		addCoinPanel.Add(plusBtn);

		VisualElement plusIcon = new VisualElement();
		plusIcon.AddToClassList("coin-box__step-icon--plus");
		plusIcon.pickingMode = PickingMode.Ignore;
		plusBtn.Add(plusIcon);
	}

	public void SelectOn(bool selectOn, int coinCount = 1)
	{
		this.coinCount.Value = coinCount;

		contens.style.display = selectOn ? DisplayStyle.Flex : DisplayStyle.None;

		coinLabel.SetNumber(coinCount);

		SetActiveAddCoinPanel(false);
	}

	public void SetActiveAddCoinPanel(bool activeOn)
	{
		addCoinPanel.style.display = activeOn ? DisplayStyle.Flex : DisplayStyle.None;
	}

	public void CoinCountChange(bool addOn)
	{
		if (addOn)
		{
			coinCount.Value++;
		}
		else
		{
			coinCount.Value--;

			if (coinCount.Value <= 0)
			{
				coinCount.Value = 1;
			}
		}

		coinLabel.SetNumber(coinCount.Value);
	}

	public void SetCoinCount(int coinCount)
	{
		this.coinCount.Value = coinCount;
		coinLabel.SetNumber(coinCount);
	}
}
