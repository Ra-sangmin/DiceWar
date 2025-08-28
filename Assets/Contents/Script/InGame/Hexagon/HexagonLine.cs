using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HexagonLine : HexagonBase
{
    [SerializeField] List<Image> lineImageList = new List<Image>();

    public void DrawLineOn(int lineIndex)
    {
        lineImageList[lineIndex].gameObject.SetActive(true);
    }

    public void DrawLineOnClear()
    {
        for (int i = 0; i < lineImageList.Count; ++i)
        {
			RectTransform rect = lineImageList[i].rectTransform;

			rect.anchoredPosition3D = GetPos(i);

			rect.sizeDelta = GetSize(i);

			rect.gameObject.SetActive(false);
        }
    }

	public Vector2 GetPos(int index)
	{
		Vector2 sizeHalfValue = rectTransform.sizeDelta * 0.5f;

		Vector2 dotData = Vector3.zero;

		switch (index)
		{
			case 0: dotData = new Vector2(sizeHalfValue.x * 0.5f, sizeHalfValue.y * 0.75f); break;
			case 1: dotData = new Vector2(-sizeHalfValue.x * 0.5f, sizeHalfValue.y * 0.75f); break;
			case 2: dotData = new Vector2(sizeHalfValue.x * 0.5f, -sizeHalfValue.y * 0.75f); break;
			case 3: dotData = new Vector2(-sizeHalfValue.x * 0.5f, -sizeHalfValue.y * 0.75f); break;
			case 4: dotData = new Vector2(sizeHalfValue.x, 0); break;
			case 5: dotData = new Vector2(-sizeHalfValue.x, 0); break;
		}

		return dotData;
	}
	public Vector2 GetSize(int index)
	{
		float sizeHalfValue = rectTransform.sizeDelta.x * 0.5f;

		Vector2 size = size = new Vector2(sizeHalfValue * 1.2f, 5);

		return size;
	}
}
