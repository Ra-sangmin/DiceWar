using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UniRx;

public class MainController : MonoBehaviour
{
    [SerializeField] List<RectTransform> stepPanelList = new List<RectTransform>();

    [SerializeField] Text myCoinText;

    [SerializeField] LoadingPopup loadingPopup;

    private int step = 0;

    private void Awake()
    {
        SetEvent();
    }

    void SetEvent()
    {
        DataManager.Instance.myCoin.
            Subscribe(coin => SetCoin()).
            AddTo(gameObject);
        SetCoin();
    }

    // Start is called before the first frame update
    void Start()
    {
        
        LoginCheck();

        PopupManager.Instance.SetCanvasParant(transform);
    }

    void LoginCheck()
    {
        if (DataManager.Instance.loginId == string.Empty) 
        {
            StepChangeOn(0);
        }
        else
        {
            StepChangeOn(1);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StepChangeOn(int step)
    {
        this.step = step;

        for (int i = 0; i < stepPanelList.Count; i++) 
        {
            bool activeOn = step == i;
            stepPanelList[i].gameObject.SetActive(activeOn);
        }
    }

    public void MultiPlayBtnClickOn()
    {
        StartCoroutine(MultyPlayOn());
    }
    IEnumerator MultyPlayOn()
    {
        ServerManager.Instance.Init();

        yield return new WaitForEndOfFrame();

        DataManager.Instance.SetAILevelEnum(AILevel.Easy);
        DataManager.Instance.SetMapSizeEnum(MapSizeEnum.Large);
        DataManager.Instance.SetPlayerMaxCnt(3);
        DataManager.Instance.SetTurnPosition(0);
        DataManager.Instance.SetOffLinePlayerCnt(0);
        DataManager.Instance.isMultiOn = true;

        yield return new WaitForEndOfFrame();

        loadingPopup.gameObject.SetActive(true);
        loadingPopup.SetData(true);
    }

    void SetCoin()
    {
        myCoinText.text = DataManager.Instance.myCoin.Value.ToString();
    }

    public void PlayBtnClickOn()
    {
        SceneManager.LoadScene("Game");
    }

    public void LoginClickOn()
    {
        DataManager.Instance.loginId = "loginClear";
        StepChangeOn(1);
    }

    public void BuyCoinPopupOpen()
    {
        PopupManager.Instance.BuyCoinPopupOn();
    }
}
