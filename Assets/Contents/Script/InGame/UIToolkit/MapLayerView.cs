using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 인게임 맵(타일 / 경계선 / 주사위)을 담는 UI Toolkit 레이어.
/// 기존에 HexagonPanel > ParantObj > (HexagonObjPanel / HexagonLinePanel / DicePanel) 로 되어있던
/// uGUI 계층을 그대로 옮긴 것이다.
/// </summary>
public class MapLayerView
{
	private readonly VisualElement origin;
	private readonly VisualElement hexLayer;
	private readonly VisualElement lineLayer;
	private readonly VisualElement diceLayer;

	public MapLayerView(VisualElement mapLayer)
	{
		mapLayer.Clear(VisualElementClearOptions.RecursiveReleaseResources); //6.5+ : 다시 만들 요소라 자원까지 바로 반환 (2026-09-29)

		//맵 좌표의 기준점 (원본 ParantObj 의 중심에 해당)
		origin = new VisualElement();
		origin.name = "map-origin";
		origin.AddToClassList("map-origin");
		origin.pickingMode = PickingMode.Ignore;
		mapLayer.Add(origin);

		//그리는 순서 = 원본 계층 순서 (타일 → 경계선 → 주사위)
		hexLayer = CreateSubLayer("hex-layer");
		lineLayer = CreateSubLayer("hex-line-layer");
		diceLayer = CreateSubLayer("dice-layer");
	}

	private VisualElement CreateSubLayer(string name)
	{
		VisualElement layer = new VisualElement();
		layer.name = name;
		layer.AddToClassList("map-sub-layer");
		layer.pickingMode = PickingMode.Ignore;
		origin.Add(layer);
		return layer;
	}

	/// <summary>
	/// 맵 기준점 위치 지정.
	/// uGUI 좌표(위쪽 +y)를 UI Toolkit 좌표(아래쪽 +y)로 뒤집어 적용한다.
	/// </summary>
	public void SetOrigin(Vector2 panelOffset, Vector2 panelPos)
	{
		float x = panelOffset.x + panelPos.x;
		float y = -(panelOffset.y + panelPos.y);

		origin.style.translate = new Translate(x, y);
	}

	public HexagonElement CreateHexagon(Vector2 hexagonSize)
	{
		HexagonElement hexagon = new HexagonElement(hexagonSize);
		hexLayer.Add(hexagon.Root);
		return hexagon;
	}

	public HexagonLineElement CreateHexagonLine(Vector2 hexagonSize)
	{
		HexagonLineElement line = new HexagonLineElement(hexagonSize);
		lineLayer.Add(line.Root);
		return line;
	}

	public MapDiceElement CreateMapDice()
	{
		MapDiceElement dice = new MapDiceElement();
		diceLayer.Add(dice.Root);
		return dice;
	}

	public void Clear()
	{
		hexLayer.Clear(VisualElementClearOptions.RecursiveReleaseResources); //6.5+ : 다시 만들 요소라 자원까지 바로 반환 (2026-09-29)
		lineLayer.Clear(VisualElementClearOptions.RecursiveReleaseResources); //6.5+ : 다시 만들 요소라 자원까지 바로 반환 (2026-09-29)
		diceLayer.Clear(VisualElementClearOptions.RecursiveReleaseResources); //6.5+ : 다시 만들 요소라 자원까지 바로 반환 (2026-09-29)
	}
}
