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
    //[SerializeField] Text myCoinText;

    private int step = 0;

    private void Awake()
    {
        //SetEvent();
    }

    //void SetEvent()
    //{
    //    if (DataManager.Instance.userData == null)
    //    {
    //        DataManager.Instance.userData = new UserData();
    //    }

    //    DataManager.Instance.userData.myCoin.
    //        Subscribe(coin => SetCoin()).
    //        AddTo(gameObject);
    //    SetCoin();
    //}

    // Start is called before the first frame update
    void Start()
    {
        //StepChangeOn(1);

        PopupManager.Instance.SetCanvasParant(transform);

        ServerManager.Instance.Init();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	//public void StepChangeOn(int step)
	//{
	//    this.step = step;

	//    for (int i = 0; i < stepPanelList.Count; i++) 
	//    {
	//        bool activeOn = step == i;
	//        stepPanelList[i].gameObject.SetActive(activeOn);
	//    }
	//}


    //void SetCoin()
    //{
    //    myCoinText.text = DataManager.Instance.userData.myCoin.Value.ToString();
    //}

    public void PlayBtnClickOn()
    {
        SceneManager.LoadScene("Game");
    }

    //public void LoginClickOn()
    //{
    //    //DataManager.Instance.loginId = "loginClear";
    //    //StepChangeOn(1);
    //}

    public void SettingPopupOn()
    {
        PopupManager.Instance.MainSettingPopupOn();
    }

	public void InfoPopupOn()
	{
		PopupManager.Instance.MainInfoPopupOn();
	}

	//public void BuyCoinPopupOpen()
 //   {
 //       PopupManager.Instance.BuyCoinPopupOn();
 //   }

	public void SinglePlaySetPopupOn()
	{
		PopupManager.Instance.PlaySetPopupOn(false);
	}

	public void MultiPlaySetPopupOn()
	{
		PopupManager.Instance.PlaySetPopupOn(true);
	}
}
