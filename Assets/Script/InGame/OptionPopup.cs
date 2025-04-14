using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OptionPopup : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SettingPopupOn()
    {
        //PopupManager.Instance.se;
    }

    public void RestartBtnClickOn()
    {
        FindFirstObjectByType<InGameController>().ReStartOn();
    }

    public void GoMainBtnClickOn()
    {
        FindFirstObjectByType<InGameController>().GoMainOn();
    }

    public void TutorialBtnClickOn()
    {
        PopupManager.Instance.TutorialPopupOn();
    }
}
