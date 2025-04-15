using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Experimental.AI;
using UnityEngine.UI;

public class Hexagon : HexagonBase
{
    [SerializeField] Text areaText;
    //[SerializeField] List<Image> diceImageList = new List<Image>();
    [SerializeField] Image centerImage;
    [SerializeField] List<Sprite> hexagonSpriteList = new List<Sprite>();
    [SerializeField] List<LineRenderer> lineRendererList = new List<LineRenderer>();

    public int area;
    public PlayerEnum playerEnum = PlayerEnum.Player_None;
    private bool choisOn = false;

    public Join join;

    public UnityAction<int> clickOn = data => { };

    protected override void Awake()
    {
        base.Awake();

        //foreach (var diceImage in diceImageList)
        //{
        //    diceImage.gameObject.SetActive(false);
        //}
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetJoin(Join join)
    {
        this.join = join;
    }

    public bool AllCenterCheck()
    {
        bool allCenterOn = true;

        //주변이 모두 같은 땅으로 되어있는지 체크
        for (int i = 0; i < join.dir.Length; i++)
        {
            int index = join.dir[i];

            if (index != -1 )
            {
                int dirData = DataManager.Instance.GetCelData(index);

                if (dirData != area)
                {
                    allCenterOn = false;
                }
            }
            else 
            {
                allCenterOn = false;
            }
        }

        return allCenterOn;
    }

    public void SetArea(int area)
    {
        this.area = area;
    }

    public void SetPlayer(PlayerEnum playerEnum , bool choisOn = false)
    {
        this.playerEnum = playerEnum;
        this.choisOn = choisOn;

        SetColor();
    }

    public void SetColor()
    {
        Color color = Color.black;

        if (choisOn)
        {
            color = Color.gray;
        }
        else
        {
            color = DataManager.Instance.GetPlayerColor(playerEnum);
        }

        centerImage.color = color;
    }

    public void HexagonClickOn()
    {
        clickOn(index);
    }

    public void DrawLineOnClear()
    {
        for (int i = 0; i < lineRendererList.Count; ++i)
        {
            List<Vector2> dotPosList = GetDotPos(i);

            lineRendererList[i].positionCount = dotPosList.Count;

            lineRendererList[i].SetPosition(0, dotPosList[0]);
            lineRendererList[i].SetPosition(1, dotPosList[1]);

            lineRendererList[i].gameObject.SetActive(false);
        }
        SetLineColor();
    }

    public void SetLineColor()
    {
        Color color = Color.gray;

        for (int i = 0; i < lineRendererList.Count; ++i)
        {
            lineRendererList[i].startColor = color;
        }
    }

    public void DrawLineOn(int lineIndex)
    {
        lineRendererList[lineIndex].gameObject.SetActive(true);
    }

    public List<Vector2> GetDotPos(int index) 
    {
        Vector2 sizeHalfValue = rectTransform.sizeDelta * 0.5f;

        List<Vector2> dotList = new List<Vector2>();

        switch (index)
        {
            case 0: dotList = new List<Vector2>() 
                    {
                        new Vector2(0, sizeHalfValue.y),
                        new Vector2(sizeHalfValue.x, sizeHalfValue.y * 0.5f)
                    };    break;
            case 4: dotList = new List<Vector2>()
                    {
                        new Vector2(sizeHalfValue.x, sizeHalfValue.y * 0.5f),
                        new Vector2(sizeHalfValue.x, -sizeHalfValue.y * 0.5f)
                    }; break;

            case 2: dotList = new List<Vector2>()
                    {
                        new Vector2(sizeHalfValue.x, -sizeHalfValue.y * 0.5f),
                        new Vector2(0, -sizeHalfValue.y)
                    }; break;

            case 3: dotList = new List<Vector2>()
                    {
                        new Vector2(0, -sizeHalfValue.y),
                        new Vector2(-sizeHalfValue.x, -sizeHalfValue.y * 0.5f)
                    }; break;

            case 5: dotList = new List<Vector2>()
                    {
                        new Vector2(-sizeHalfValue.x, -sizeHalfValue.y * 0.5f),
                        new Vector2(-sizeHalfValue.x, sizeHalfValue.y * 0.5f)
                    }; break;

            case 1: dotList = new List<Vector2>()
                    {
                        new Vector2(-sizeHalfValue.x, sizeHalfValue.y * 0.5f),
                        new Vector2(0, sizeHalfValue.y)
                    }; break;
        }

        return dotList;
    }
}
