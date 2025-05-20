using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InGameBottomController : MonoBehaviour
{
    [SerializeField] RectTransform mapSelectPanel;
    public NonePlayPanel nonePlayPanel;
    //public RectTransform tutorialPanel;
    //[SerializeField] Image tutorialHexagonImage;
    
    //public DiceWarUIController diceWarUIController;

    // 0 = 맵 선택 , 1 = 튜토리얼 , 2 = 게임 시작
    public int status = 0;

    //bool timerOn = false;

    

    // Start is called before the first frame update
    void Start()
    {
        //tutorialHexagonImage.color = InGameDataManager.Instance.GetPlayerColor();
    }

    

    public void SetStatus(int status)
    {
        this.status = status;

        mapSelectPanel.gameObject.SetActive(false);
        nonePlayPanel.gameObject.SetActive(false);
        //diceWarUIController.gameObject.SetActive(false);

        switch (status)
        {
            case 0:
                mapSelectPanel.gameObject.SetActive(true);
                break;
            case 1:
                nonePlayPanel.gameObject.SetActive(true);
                nonePlayPanel.SetData();
                break;
            //case 2:
            //    diceWarUIController.gameObject.SetActive(true);
            //    break;
        }
    }

    //public void SetTimerOn( bool timerOn) 
    //{
    //    this.timerOn = timerOn;

    //    timerCurrentDelay = timerMaxDelay;
    //}

    // Update is called once per frame
    void Update()
    {
       // TimerCheck();
    }

    //void TimerCheck()
    //{
    //    if (this.timerOn == false)
    //        return;

    //    timerCurrentDelay -= Time.deltaTime;


    //}

}
