using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HexagonBase : MonoBehaviour
{
    public int index;
    public Vector2 pos;
    protected RectTransform rectTransform;

    protected virtual void Awake()
    {
        rectTransform = transform as RectTransform;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetPos(int index)
    {
        this.index = index;
        this.pos = InGameDataManager.Instance.GetPos(index);

        Vector2 sizeDelta = rectTransform.sizeDelta;

        float xValue = sizeDelta.x * pos.x;
        float yValue = sizeDelta.y * 0.72f * pos.y;

        if (pos.y % 2 != 0)
        {
            xValue -= sizeDelta.x * 0.5f;
        }

        rectTransform.anchoredPosition3D = new Vector2(xValue, yValue);
    }

    //public void SetPos(int index)
    //{
    //    this.index = index;
    //    this.pos = InGameDataManager.Instance.GetPos(index);

    //    Vector2 sizeDelta = rectTransform.sizeDelta;

    //    float xAddValue = -10;

    //    float xValue = (sizeDelta.x * pos.x);//* 0.95f;
    //    float yValue = sizeDelta.y * 0.742f * pos.y;

    //    if (pos.y % 2 != 0)
    //    {
    //        xValue -= sizeDelta.x * 0.5f;
    //    }

    //    rectTransform.anchoredPosition3D = new Vector2(xValue, yValue);
    //}
}
