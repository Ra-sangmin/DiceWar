using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InGameBottomController : MonoBehaviour
{
    [SerializeField] RectTransform mapSelectPanel;
	[SerializeField] Text needCoinText;
	public NonePlayPanel nonePlayPanel;

    // 0 = 맵 선택 , 1 = 튜토리얼 , 2 = 게임 시작
    public int status = 0;

    // Start is called before the first frame update
    void Start()
    {   
	}

	public void SetStatus(int status)
    {
        this.status = status;

        mapSelectPanel.gameObject.SetActive(false);
        nonePlayPanel.gameObject.SetActive(false);

        switch (status)
        {
            case 0:
                mapSelectPanel.gameObject.SetActive(true);
                break;
            case 1:
                nonePlayPanel.gameObject.SetActive(true);
                nonePlayPanel.SetData();
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
