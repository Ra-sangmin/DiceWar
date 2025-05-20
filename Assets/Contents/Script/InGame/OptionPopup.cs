using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OptionPopup : MonoBehaviour
{
    private InGameController inGameController;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void DataInit(InGameController inGameController)
    {
        this.inGameController = inGameController;
    }

    public void SettingPopupOn()
    {
        //PopupManager.Instance.se;
    }

    public void RestartBtnClickOn()
    {
        inGameController.ReStartOn();
        gameObject.SetActive(false);
    }

    public void GoMainBtnClickOn()
    {
        inGameController.GoMainOn();
    }

    public void TutorialBtnClickOn()
    {
        PopupManager.Instance.TutorialPopupOn();
    }
}
