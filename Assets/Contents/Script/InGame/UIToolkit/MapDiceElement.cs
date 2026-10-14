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

	/// <summary>
	/// 주사위마다 아주 조금 다른 위치 / 기울기 (2026-10-13).
	/// 자로 잰 듯 똑같이 쌓인 탑이 인위적으로 보여서, 손으로 쌓은 것처럼 1~2px · 1~2도씩 어긋나게 한다.
	/// 값은 고정이라 모든 클라이언트에서 같다.
	/// </summary>
	private static readonly Vector3[] DiceJitter =
	{
		//원본과 같은 고정 배치 (어긋남 없음)
		Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero,
	};

	private readonly VisualElement shadow;

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

		//더미 아래 바닥 그림자 (주사위보다 먼저 = 뒤에 그려짐)
		shadow = new VisualElement();
		shadow.AddToClassList("map-dice__shadow");
		shadow.pickingMode = PickingMode.Ignore;
		shadow.style.display = DisplayStyle.None;
		root.Add(shadow);

		for (int i = 0; i < DiceCenters.Length; i++)
		{
			VisualElement dice = new VisualElement();
			dice.AddToClassList("map-dice__dice");
			dice.pickingMode = PickingMode.Ignore;
			dice.style.width = DiceWidth;
			dice.style.height = DiceHeight;
			dice.style.left = DiceCenters[i].x - (DiceWidth * 0.5f) + DiceJitter[i].x;
			dice.style.top = DiceCenters[i].y - (DiceHeight * 0.5f) + DiceJitter[i].y;
			dice.style.rotate = new UnityEngine.UIElements.Rotate(new UnityEngine.UIElements.Angle(DiceJitter[i].z, UnityEngine.UIElements.AngleUnit.Degree));
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

		//두 번째 줄(4개째 이상)이 있으면 그림자를 넓힌다
		shadow.style.display = dice > 0 ? DisplayStyle.Flex : DisplayStyle.None;
		shadow.EnableInClassList("map-dice__shadow--wide", dice > 3);
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
