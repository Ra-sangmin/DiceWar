using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class InGameBottomController : MonoBehaviour
{
    public MapSelectPanel mapSelectPanel;
	public NonePlayPanel nonePlayPanel;
	
	// 0 = 맵 선택 , 1 = 튜토리얼 , 2 = 게임 시작
	public int status = 0;

    // Start is called before the first frame update
    void Start()
    {   
	}

	/// <summary> UI Toolkit 요소 연결 </summary>
	public void InitView(VisualElement root)
	{
		nonePlayPanel.InitView(root);
		mapSelectPanel.InitView(root);
	}

	public void SetStatus(int status)
    {
        this.status = status;

        mapSelectPanel.SetPanelActive(false);
        nonePlayPanel.SetPanelActive(false);

        switch (status)
        {
            case 0:
                mapSelectPanel.SetPanelActive(true);
				mapSelectPanel.SetData();
				break;
            case 1:
                nonePlayPanel.SetPanelActive(true);
                nonePlayPanel.SetData();
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
