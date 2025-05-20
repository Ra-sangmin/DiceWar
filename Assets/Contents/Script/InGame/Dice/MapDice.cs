using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapDice : HexagonBase
{
    [SerializeField] List<Image> diceImageList = new List<Image>();

    protected override void Awake()
    {
        base.Awake();
        SetDice(0);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetPos(Hexagon centerHexagon)
    {
        Transform originParant = transform.parent;
        transform.SetParent(centerHexagon.transform);

        RectTransform rectTransform = (transform as RectTransform);
        rectTransform.anchoredPosition3D = Vector2.zero;
        transform.SetParent(originParant);
    }

    public void SetDice(int dice)
    {
        foreach (var diceImage in diceImageList)
        {
            diceImage.CrossFadeAlpha(0, 0,true);
        }

        for (int i = 0; i < dice; i++)
        {
            diceImageList[i].CrossFadeAlpha(1, 0, true);
        }
    }
}
