using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static GameResultPopup;

public class PopupManager : MonoSingleton<PopupManager>
{
	public RectTransform parantTranform = null;
	//public RectTransform parantRectTranform = null;

    public bool otherPopupOn = false;
    
    //private OptionPopup optionPopup;
    //private TutorialPopup tutorialPopup;

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
        playSetPopup.InitOn(multiOn);
	}

	public void YourTurnPopupOn()
	{
		ToastPopup yourTurnPopup = Instantiate(Resources.Load<ToastPopup>("Popup/InGame/YourTurnPopup"), parantTranform);

		if (yourTurnPopup != null)
		{
			yourTurnPopup.ActiveOn();
        }
	}

    public void TimeOverPopupOn()
    {
		ToastPopup timeOverPopup = Instantiate(Resources.Load<ToastPopup>("Popup/InGame/TimeOverPopup"), parantTranform);

		if (timeOverPopup != null)
        {
            timeOverPopup.ActiveOn();
        }
    }

	public void NeedCoinPopupOn(int needCoin)
	{
		NeedCoinPopup needCoinPopup = Instantiate(Resources.Load<NeedCoinPopup>("Popup/InGame/NeedCoinPopup"), parantTranform);

		if (needCoinPopup != null)
		{
			needCoinPopup.ActiveOn(needCoin);
		}
	}

	public void OptionPopupOn(InGameControllerBase inGameController)
    {
		OptionPopup optionPopup = Instantiate(Resources.Load<OptionPopup>("Popup/InGame/OptionPopup"), parantTranform);

        if (optionPopup != null)
        {
			optionPopup.DataInit(inGameController);
        }
    }

	public void LeaveEarlyPopupOn(InGameControllerBase inGameController, int getCoin)
	{
		LeaveEarlyPopup leaveEarlyPopup = Instantiate(Resources.Load<LeaveEarlyPopup>("Popup/InGame/LeaveEarlyPopup"), parantTranform);

		if (leaveEarlyPopup != null)
		{
			leaveEarlyPopup.DataInit(inGameController, getCoin);
		}
	}

	public void TutorialPopupOn()
    {
		TutorialPopup tutorialPopup = Instantiate(Resources.Load<TutorialPopup>("Popup/InGame/TutorialPopup"), parantTranform);

		if (tutorialPopup != null)
        {
            tutorialPopup.TutorialOn();
        }
    }

    public void GameResultPopupOn(InGameControllerBase inGameController , GameResultEnum gameResultEnum, int coinCount)
    {
        GameResultPopup gameResultPopup = Instantiate(Resources.Load<GameResultPopup>("Popup/InGame/GameResultPopup"), parantTranform);
        gameResultPopup.DataInit(inGameController, gameResultEnum, coinCount);
    }
	public void GiveUpPopupOn(InGameControllerBase inGameController, int coinCount = 0)
	{
		GiveUpPopup giveUpPopup = Instantiate(Resources.Load<GiveUpPopup>("Popup/InGame/GiveUpPopup"), parantTranform);
		giveUpPopup.DataInit(inGameController, coinCount);
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

	public ApprovePopup ApprovePopup(ApproveData approveData)
	{
		ApprovePopup approvePopup = Instantiate(Resources.Load<ApprovePopup>("Popup/InGame/ApprovePopup"), parantTranform);
		approvePopup.SetData(approveData);
		return approvePopup;
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
