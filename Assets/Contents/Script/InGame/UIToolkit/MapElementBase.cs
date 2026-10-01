using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 맵을 구성하는 UI Toolkit 요소들의 공통 베이스.
/// 기존 uGUI HexagonBase 의 역할을 대체한다.
/// (RectTransform.anchoredPosition → VisualElement 의 left/top 으로 변환)
/// </summary>
public abstract class MapElementBase
{
	public int index;
	public Vector2 pos;

	protected VisualElement root;
	protected Vector2 size;

	public VisualElement Root => root;

	protected MapElementBase(Vector2 size)
	{
		this.size = size;
	}

	/// <summary> 셀 인덱스에 맞는 위치로 이동 (기존 HexagonBase.SetPos 와 동일한 계산) </summary>
	public virtual void SetPos(int index)
	{
		this.index = index;
		this.pos = DataManager.Instance.GetPos(index);

		float xValue = size.x * pos.x;
		float yValue = size.y * 0.72f * pos.y;

		if (pos.y % 2 != 0)
		{
			xValue -= size.x * 0.5f;
		}

		SetAnchoredPosition(new Vector2(xValue, yValue));
	}

	/// <summary>
	/// uGUI 기준 좌표(부모 중앙 기준, 위쪽이 +y, 피벗 중앙)를
	/// UI Toolkit 좌표(부모 좌상단 기준, 아래쪽이 +y, 좌상단 기준)로 변환해서 적용한다.
	/// </summary>
	public void SetAnchoredPosition(Vector2 anchoredPosition)
	{
		root.style.left = anchoredPosition.x - (size.x * 0.5f);
		root.style.top = -anchoredPosition.y - (size.y * 0.5f);
	}

	public Vector2 GetAnchoredPosition()
	{
		return new Vector2(root.style.left.value.value + (size.x * 0.5f),
						  -(root.style.top.value.value + (size.y * 0.5f)));
	}

	public void SetActive(bool activeOn)
	{
		root.style.display = activeOn ? DisplayStyle.Flex : DisplayStyle.None;
	}

	public bool IsActive()
	{
		return root.style.display.value == DisplayStyle.Flex;
	}

	public void RemoveFromHierarchy()
	{
		root.RemoveFromHierarchy();
	}
}
