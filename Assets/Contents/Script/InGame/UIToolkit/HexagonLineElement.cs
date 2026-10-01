using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 영토 경계선. 기존 uGUI HexagonLine(MonoBehaviour) 의 UI Toolkit 버전.
/// 육각형 6방향의 선 조각을 들고 있다가 필요한 방향만 켠다.
///
/// 원본(프리팹)은 6개 조각의 회전값이 27.5도로 고정돼 있고 길이도 전부 size.x*0.6 이었다.
/// 맵 크기(Small 65x70 / Medium 50x50 / Large 40x40)에 따라 실제 변의 각도와 길이가 달라지므로
/// 여기서는 타일 육각형의 꼭짓점을 직접 계산해서 각 변에 정확히 올린다.
/// </summary>
public class HexagonLineElement : MapElementBase
{
	/// <summary>
	/// 선 두께(px). 원본 HexagonLine.GetSize 의 y 값과 동일.
	/// 회전 후에도 변과 수직인 방향의 두께라서 어느 방향이든 같은 굵기가 된다.
	/// </summary>
	private const float Thickness = 5f;

	/// <summary>
	/// 좌우 꼭짓점이 중심에서 세로로 얼마나 떨어져 있는지 (halfY 대비 비율).
	/// hexagon.png(45x45) 를 실측한 값으로, 정육각형이면 0.5 가 된다.
	/// </summary>
	private const float WaistRatio = 0.475f;

	/// <summary>
	/// 꼭짓점에서 두 조각이 만날 때 생기는 빈 틈을 메우기 위해 변 양끝으로 늘리는 길이.
	/// 내각 120도 마이터 조인 = (두께/2) / tan(60도) 를 양쪽 합친 값.
	/// </summary>
	private const float JoinExtend = 0.577f;

	private readonly List<VisualElement> lineList = new List<VisualElement>();

	public HexagonLineElement(Vector2 size) : base(size)
	{
		root = new VisualElement();
		root.AddToClassList("hexagon-line");
		root.style.width = size.x;
		root.style.height = size.y;
		root.pickingMode = PickingMode.Ignore;

		for (int i = 0; i < 6; i++)
		{
			VisualElement line = new VisualElement();
			line.AddToClassList("hexagon-line__seg");
			line.pickingMode = PickingMode.Ignore;
			line.style.display = DisplayStyle.None;

			lineList.Add(line);
			root.Add(line);
		}
	}

	public void DrawLineOn(int lineIndex)
	{
		lineList[lineIndex].style.display = DisplayStyle.Flex;
	}

	/// <summary> 선 위치/크기/각도를 타일 크기에 맞춰 재계산하고 전부 끈다 (기존 DrawLineOnClear 과 동일) </summary>
	public void DrawLineOnClear()
	{
		for (int i = 0; i < lineList.Count; i++)
		{
			VisualElement line = lineList[i];

			Vector2 from;
			Vector2 to;
			GetEdge(i, out from, out to);

			Vector2 mid = (from + to) * 0.5f;
			Vector2 dir = to - from;

			float length = dir.magnitude + (Thickness * JoinExtend);

			line.style.width = length;
			line.style.height = Thickness;

			//부모(타일) 중앙 기준 좌표 → 좌상단 기준 좌표. 타일 좌표는 위쪽이 +y 라 y 는 부호를 뒤집는다.
			line.style.left = (size.x * 0.5f) + mid.x - (length * 0.5f);
			line.style.top = (size.y * 0.5f) - mid.y - (Thickness * 0.5f);

			//UI Toolkit 은 아래쪽이 +y 라 시계 방향이 + 가 된다
			float degree = Mathf.Atan2(-dir.y, dir.x) * Mathf.Rad2Deg;
			if (degree > 90f) degree -= 180f;
			else if (degree <= -90f) degree += 180f;

			//Unity-Logs-Viewer 플러그인에 전역 Rotate(MonoBehaviour) 클래스가 있어서
			//using 만으로는 UIElements 쪽 Rotate 가 가려진다. 전체 이름으로 써야 한다.
			line.style.rotate = new UnityEngine.UIElements.Rotate(
				new UnityEngine.UIElements.Angle(degree, UnityEngine.UIElements.AngleUnit.Degree));

			line.style.display = DisplayStyle.None;
		}
	}

	/// <summary>
	/// 육각형 변 하나의 양 끝점 (타일 중심 기준, 위쪽이 +y).
	/// 인덱스는 원본과 동일하다 — 0 오른위 / 1 왼위 / 2 오른아래 / 3 왼아래 / 4 오른쪽 / 5 왼쪽.
	/// </summary>
	public void GetEdge(int index, out Vector2 from, out Vector2 to)
	{
		//가로는 타일 간격(size.x)을 기준으로 잡는다.
		//타일 이미지는 좌우로 2px 씩 넓어서 이웃과 겹치는데, 그 겹침의 한가운데가 실제로 보이는 경계다.
		float halfX = size.x * 0.5f;
		float halfY = size.y * 0.5f;
		float waistY = halfY * WaistRatio;

		Vector2 top = new Vector2(0f, halfY);
		Vector2 bottom = new Vector2(0f, -halfY);
		Vector2 upRight = new Vector2(halfX, waistY);
		Vector2 upLeft = new Vector2(-halfX, waistY);
		Vector2 downRight = new Vector2(halfX, -waistY);
		Vector2 downLeft = new Vector2(-halfX, -waistY);

		switch (index)
		{
			case 0: from = top; to = upRight; break;
			case 1: from = top; to = upLeft; break;
			case 2: from = bottom; to = downRight; break;
			case 3: from = bottom; to = downLeft; break;
			case 4: from = upRight; to = downRight; break;
			default: from = upLeft; to = downLeft; break;
		}
	}

	/// <summary> 변의 중심 좌표 (기존 GetPos 호환) </summary>
	public Vector2 GetPos(int index)
	{
		Vector2 from;
		Vector2 to;
		GetEdge(index, out from, out to);
		return (from + to) * 0.5f;
	}

	/// <summary> 변 하나를 덮는 선 조각의 크기 (회전 전 기준) </summary>
	public Vector2 GetSize(int index)
	{
		Vector2 from;
		Vector2 to;
		GetEdge(index, out from, out to);
		return new Vector2((to - from).magnitude + (Thickness * JoinExtend), Thickness);
	}
}
