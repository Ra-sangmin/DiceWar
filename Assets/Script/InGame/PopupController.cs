using UnityEngine;

public class PopupController : MonoBehaviour
{
    //[SerializeField] ToastPopup yourTurnPopup;
    //[SerializeField] ToastPopup timerOverPopup;
    //[SerializeField] RectTransform settingPopup;
    [SerializeField] TutorialPopup tutorialPopup;
    [SerializeField] RectTransform victoryPopup;
    [SerializeField] RectTransform defeatPopup;

    // Start is called before the first frame update
    void Start()
    {
        PopupAllInActive();
        //yourTurnPopup.SetImageColor();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SettingOn()
    {
        PopupAllInActive();
    }

    //public ToastPopup GetYourTurnPopup()
    //{
    //    return yourTurnPopup;
    //}

    //public ToastPopup GetTimeOverPopup()
    //{
    //    return timerOverPopup;
    //}

    public void TutorialOn()
    {
        PopupAllInActive();

        tutorialPopup.gameObject.SetActive(true);
        tutorialPopup.TutorialOn();
    }

    public void GameResultPopupOn(bool win)
    {
        if (win)
        {
            victoryPopup.gameObject.SetActive(true);
        }
        else 
        {
            defeatPopup.gameObject.SetActive(true);
        }
    }

    public void PopupAllInActive()
    {
        //settingPopup.gameObject.SetActive(false);
        //victoryPopup.gameObject.SetActive(false);
        //defeatPopup.gameObject.SetActive(false);
        //tutorialPopup.gameObject.SetActive(false);
    }
}
