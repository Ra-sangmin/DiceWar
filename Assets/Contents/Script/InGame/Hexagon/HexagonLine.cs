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
            lineImageList[i].gameObject.SetActive(false);
        }
    }
}
