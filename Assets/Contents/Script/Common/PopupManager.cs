using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PopupManager : MonoSingleton<PopupManager>
{
	public RectTransform parantTranform = null;
	//public RectTransform parantRectTranform = null;

    public bool otherPopupOn = false;
    
	private ToastPopup yourTurnPopup;
    private ToastPopup timeOverPopup;
    private OptionPopup optionPopup;
    private TutorialPopup tutorialPopup;
    //private VictoryPopup victoryPopup;
    //private DefeatPopup defeatPopup;

    public override void Init() 
	{
		//ParantSet(transform);
	}

	public void SetCanvasParant(Transform target)
	{
		Canvas canvas = target.GetComponentInParent<Canvas>();
		ParantSet(canvas);
	}

	public void ParantSet(Canvas canvas)
	{
		GameObject newParantObj = new GameObject("PopupParant");
		newParantObj.transform.SetParent(canvas.transform);
        RectTransform rectTransform = newParantObj.AddComponent<RectTransform>();

        parantTranform = rectTransform;
        rectTransform.anchoredPosition3D = new Vector3(0, 0, 0);
		rectTransform.sizeDelta = (canvas.transform as RectTransform).sizeDelta;

        Canvas newCanvas = parantTranform.AddComponent<Canvas>();
        newCanvas.overrideSorting = true;
        newCanvas.sortingOrder = 10;

        parantTranform.AddComponent<GraphicRaycaster>();

        rectTransform.transform.localScale = Vector3.one;
	}

    public void MainSettingPopupOn()
    {
		MainSettingPopup mainSettingPopup = Instantiate(Resources.Load<MainSettingPopup>("Popup/Main/MainSettingPopup"), parantTranform);
	}

	public void MainInfoPopupOn()
	{
		MainInfoPopup mainInfoPopup = Instantiate(Resources.Load<MainInfoPopup>("Popup/Main/MainInfoPopup"), parantTranform);
	}

	public void TermsOfConditionsPopupOn()
	{
		TeamOfConditionsPopup teamOfConditionsPopup = Instantiate(Resources.Load<TeamOfConditionsPopup>("Popup/Main/TeamOfConditionsPopup"), parantTranform);
	}

	public void PrivatePolicyPopupOn()
	{
		PrivatePolicyPopup privatePolicyPopup = Instantiate(Resources.Load<PrivatePolicyPopup>("Popup/Main/PrivatePolicyPopup"), parantTranform);
	}

	public void PlaySetPopupOn(bool multiOn)
	{
		PlaySetPopup playSetPopup = Instantiate(Resources.Load<PlaySetPopup>("Popup/Main/PlaySetPopup"), parantTranform);
		playSetPopup.multiOn = multiOn;
        playSetPopup.InitOn();
	}

	public void YourTurnPopupOn()
	{
        if (yourTurnPopup == null)
		{
            yourTurnPopup = Instantiate(Resources.Load<ToastPopup>("Popup/InGame/YourTurnPopup"), parantTranform);
        }

        if (yourTurnPopup != null)
		{
			yourTurnPopup.ActiveOn();
        }
	}

    public void TimeOverPopupOn()
    {
        if (timeOverPopup == null)
        {
            timeOverPopup = Instantiate(Resources.Load<ToastPopup>("Popup/InGame/TimeOverPopup"), parantTranform);
        }

        if (timeOverPopup != null)
        {
            timeOverPopup.ActiveOn();
        }
    }

    public void OptionPopupOn(InGameController inGameController)
    {
        if (optionPopup == null)
        {
            optionPopup = Instantiate(Resources.Load<OptionPopup>("Popup/InGame/OptionPopup"), parantTranform);
            optionPopup.DataInit(inGameController);
        }

        if (optionPopup != null)
        {
            optionPopup.gameObject.SetActive(true);
        }
    }

    public void TutorialPopupOn()
    {
        if (tutorialPopup == null)
        {
            tutorialPopup = Instantiate(Resources.Load<TutorialPopup>("Popup/InGame/TutorialPopup"), parantTranform);
        }

        if (tutorialPopup != null)
        {
            tutorialPopup.gameObject.SetActive(true);
            tutorialPopup.TutorialOn();
        }
    }

    public void GameResultPopupOn(InGameController inGameController ,  bool win , int coinCount)
    {
        GameResultPopup gameResultPopup = Instantiate(Resources.Load<GameResultPopup>("Popup/InGame/GameResultPopup"), parantTranform);
        gameResultPopup.DataInit(inGameController, win , coinCount);

        //if (win)
        //{
        //    VictoryPopup victoryPopup = Instantiate(Resources.Load<VictoryPopup>("Popup/InGame/VictoryPopup"), parantTranform);
        //}
        //else
        //{
        //    DefeatPopup defeatPopup = Instantiate(Resources.Load<DefeatPopup>("Popup/InGame/DefeatPopup"), parantTranform);
        //}
    }

    public LandTradePopup LandTradePopupOn()
    {
        LandTradePopup landTradePopup = Instantiate(Resources.Load<LandTradePopup>("Popup/InGame/LandTradePopup"), parantTranform);

        return landTradePopup;
    }

    public AlliancePopup AlliancePopupOn()
    {
        AlliancePopup alliancePopup = Instantiate(Resources.Load<AlliancePopup>("Popup/InGame/AlliancePopup"), parantTranform);

        return alliancePopup;
    }


    public AllianceApprovePopup AllianceApprovePopupOn(AllianceRequest AllianceRequest)
    {
        AllianceApprovePopup allianceApprovePopup = Instantiate(Resources.Load<AllianceApprovePopup>("Popup/InGame/AllianceApprovePopup"), parantTranform);
        allianceApprovePopup.SetData(AllianceRequest);
        return allianceApprovePopup;
    }

    public BetrayPopup BetrayPopupOn()
    {
        BetrayPopup betrayPopup = Instantiate(Resources.Load<BetrayPopup>("Popup/InGame/BetrayPopup"), parantTranform);

        return betrayPopup;
    }

    public LandTradeApprovePopup LandTradeApprovePopupOn(LandTradeRequest landTradeRequest)
    {
        LandTradeApprovePopup landTradeApprovePopup = Instantiate(Resources.Load<LandTradeApprovePopup>("Popup/InGame/LandTradeApprovePopup"), parantTranform);
        landTradeApprovePopup.SetData(landTradeRequest);
        return landTradeApprovePopup;
    }

    public BuyCoinPopup BuyCoinPopupOn()
    {
        BuyCoinPopup buyCoinPopup = Instantiate(Resources.Load<BuyCoinPopup>("Popup/Common/BuyCoinPopup"), parantTranform);

        return buyCoinPopup;
    }


    //	public OkPopup OkPopupCreate(string contensStr, UnityAction okBtnClickEvent, UnityAction cancelBtnClickEvent = null , bool maskClickDestoryOn = false, bool okBtnOnly = false, string okBtnStr = "예", string cancelBtnStr = "아니오")
    //	{
    //        if (parantTranform == null)
    //			return null;

    //		parantTranform.SetAsLastSibling();

    //		OkPopup okPopup = Instantiate(Resources.Load<OkPopup>("Popup/OkPopup"), parantTranform);
    //#if UNITY_WEBGL || UNITY_STANDALONE
    //#else
    //		okPopup.transform.localScale = Vector3.one * 1.5f;
    //#endif
    //		okPopup.DataSet(contensStr, okBtnClickEvent , cancelBtnClickEvent , okBtnOnly,maskClickDestoryOn,  okBtnStr, cancelBtnStr);

    //		return okPopup;
    //	}

    //	public WarningPopup WarningPopupCreate(string contensStr)
    //	{
    //		if (parantTranform == null)
    //			return null;

    //		parantTranform.SetAsLastSibling();
    //		WarningPopup warningPopup = Instantiate(Resources.Load<WarningPopup>("Popup/WarningPopup"), parantTranform);
    //		warningPopup.DataSet(contensStr);

    //		Vector3 scale = Vector3.one;

    //		switch (SceneManager.GetActiveScene().name)
    //		{
    //			case "Search":
    //			case "Intro": scale *= 2f; break;
    //			case "Login": scale *= 1.5f; break;
    //		}

    //		warningPopup.transform.localScale = scale;

    //		return warningPopup;
    //	}
}
