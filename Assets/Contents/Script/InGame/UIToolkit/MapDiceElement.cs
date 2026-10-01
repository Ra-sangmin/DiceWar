using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 영토 위에 표시되는 주사위 더미. 기존 uGUI MapDice(MonoBehaviour) 의 UI Toolkit 버전.
/// 원본은 Horizontal/VerticalLayoutGroup(음수 spacing)으로 겹쳐 쌓았는데,
/// 런타임에서 실제로 계산된 좌표를 그대로 옮겨 고정 배치했다.
/// </summary>
public class MapDiceElement : MapElementBase
{
	private const float DiceWidth = 55f;
	private const float DiceHeight = 59f;

	/// <summary> DiceObj(100x100) 좌상단 기준 각 주사위의 중심 좌표 (원본 런타임 값) </summary>
	private static readonly Vector2[] DiceCenters =
	{
		new Vector2(35.23f, 63.5f),
		new Vector2(35.23f, 34.5f),
		new Vector2(35.23f, 5.5f),
		new Vector2(64.77f, 79.5f),
		new Vector2(64.77f, 50.5f),
		new Vector2(64.77f, 21.5f),
	};

	private readonly List<VisualElement> diceList = new List<VisualElement>();
	private readonly Label areaLabel;
	private readonly Label adjLabel;

	public MapDiceElement() : base(new Vector2(100f, 100f))
	{
		root = new VisualElement();
		root.AddToClassList("map-dice");
		root.style.width = size.x;
		root.style.height = size.y;
		root.pickingMode = PickingMode.Ignore;

		for (int i = 0; i < DiceCenters.Length; i++)
		{
			VisualElement dice = new VisualElement();
			dice.AddToClassList("map-dice__dice");
			dice.pickingMode = PickingMode.Ignore;
			dice.style.width = DiceWidth;
			dice.style.height = DiceHeight;
			dice.style.left = DiceCenters[i].x - (DiceWidth * 0.5f);
			dice.style.top = DiceCenters[i].y - (DiceHeight * 0.5f);
			dice.style.opacity = 0f;

			diceList.Add(dice);
			root.Add(dice);
		}

		areaLabel = new Label(string.Empty);
		areaLabel.AddToClassList("map-dice__area-text");
		areaLabel.pickingMode = PickingMode.Ignore;
		root.Add(areaLabel);

		adjLabel = new Label(string.Empty);
		adjLabel.AddToClassList("map-dice__adj-text");
		adjLabel.pickingMode = PickingMode.Ignore;
		root.Add(adjLabel);
	}

	/// <summary> 중심이 되는 타일 위로 이동 (기존 MapDice.SetPos 과 동일한 결과) </summary>
	public void SetPos(HexagonElement centerHexagon)
	{
		SetAnchoredPosition(centerHexagon.GetAnchoredPosition());
	}

	public void SetDice(int dice)
	{
		for (int i = 0; i < diceList.Count; i++)
		{
			diceList[i].style.opacity = i < dice ? 1f : 0f;
		}
	}

	public void TextInit()
	{
		areaLabel.text = string.Empty;
		adjLabel.text = string.Empty;
	}

	public void AreaTextSet(int index)
	{
		areaLabel.SetNumber(index);
	}

	public void AreaAdjSet(string value)
	{
		adjLabel.text = value;
	}
}
