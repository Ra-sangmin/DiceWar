using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static GameResultPopup;

public class PopupManager : MonoSingleton<PopupManager>
{
	/// <summary>
	/// 팝업들이 붙는 부모. 예전에는 Canvas 밑의 또 다른 Canvas 였지만
	/// 팝업이 전부 UI Toolkit(PanelRenderer) 이라 이제는 그냥 빈 GameObject 다.
	/// </summary>
	public Transform parantTranform = null;

    public bool otherPopupOn = false;

    /// <summary> UI Toolkit 팝업의 겹침 순서 (나중에 뜬 팝업이 위) </summary>
    private int popupSortingOrder = 0;

    public override void Init() 
	{
	}

	/// <summary>
	/// 씬마다 한 번 호출해서 팝업 부모를 새로 만든다.
	/// Canvas / GraphicRaycaster 는 필요 없다 — UI Toolkit 패널은 자기 입력 경로를 따로 갖는다.
	/// </summary>
	public void SetPopupParant()
	{
		if (parantTranform != null)
		{
			Destroy(parantTranform.gameObject);
		}

		GameObject newParantObj = new GameObject("PopupParant");

		parantTranform = newParantObj.transform;

		//씬이 바뀌면 겹침 순서도 0 부터 다시 센다
		popupSortingOrder = 0;
	}

	/// <summary>
	/// 팝업끼리의 겹침 순서.
	/// 같은 PanelSettings 를 공유하는 PanelRenderer 는 sortingOrder 로 순서가 정해진다.
	/// </summary>
	private T PopupOrderOn<T>(T popup) where T : Component
	{
		if (popup == null)
			return popup;

		//PanelRenderer 는 Renderer 라 sortingOrder 가 int 다 (2026-09-29)
		PanelRenderer panelRenderer = popup.GetComponent<PanelRenderer>();

		if (panelRenderer != null)
		{
			popupSortingOrder++;
			panelRenderer.sortingOrder = popupSortingOrder;
		}

		return popup;
	}

    public void MainSettingPopupOn()
    {
		MainSettingPopup mainSettingPopup = PopupOrderOn(Instantiate(Resources.Load<MainSettingPopup>("Popup/Main/MainSettingPopup"), parantTranform));
	}

	public void MainInfoPopupOn()
	{
		MainInfoPopup mainInfoPopup = PopupOrderOn(Instantiate(Resources.Load<MainInfoPopup>("Popup/Main/MainInfoPopup"), parantTranform));
	}

	public void TermsOfConditionsPopupOn()
	{
		TeamOfConditionsPopup teamOfConditionsPopup = PopupOrderOn(Instantiate(Resources.Load<TeamOfConditionsPopup>("Popup/Main/TeamOfConditionsPopup"), parantTranform));
	}

	public void PrivatePolicyPopupOn()
	{
		PrivatePolicyPopup privatePolicyPopup = PopupOrderOn(Instantiate(Resources.Load<PrivatePolicyPopup>("Popup/Main/PrivatePolicyPopup"), parantTranform));
	}

	/// <summary>
	/// 개발용 주사위 개수 설정 팝업. (Main 씬 좌하단 숨김 핫스팟)
	/// 예전에는 Main 씬에 비활성으로 놓여 있었지만 2026-09-20 부터 다른 팝업과 같이 생성한다.
	/// </summary>
	public void DiceSetPopupOn()
	{
		DiceSetPopup diceSetPopup = PopupOrderOn(Instantiate(Resources.Load<DiceSetPopup>("Popup/Main/DiceSetPopup"), parantTranform));
	}

	public void PlaySetPopupOn(bool multiOn)
	{
		PlaySetPopup playSetPopup = PopupOrderOn(Instantiate(Resources.Load<PlaySetPopup>("Popup/Main/PlaySetPopup"), parantTranform));
        playSetPopup.InitOn(multiOn);
	}

	public OkPopup LogOutPopupOn()
	{
		OkPopup okPopup = PopupOrderOn(Instantiate(Resources.Load<OkPopup>("Popup/Common/OkPopup"), parantTranform));
        return okPopup;
	}

	public void YourTurnPopupOn()
	{
		ToastPopup yourTurnPopup = PopupOrderOn(Instantiate(Resources.Load<ToastPopup>("Popup/InGame/YourTurnPopup"), parantTranform));

		if (yourTurnPopup != null)
		{
			yourTurnPopup.ActiveOn();
        }
	}

	/// <summary> 게임 시작 때 첫 차례가 아닌 사람에게 내 색을 알려준다 (Your Turn 과 같은 모양, 2026-09-26) </summary>
	public void YourColorPopupOn()
	{
		YourTurnPopup popup = PopupOrderOn(Instantiate(Resources.Load<YourTurnPopup>("Popup/InGame/YourTurnPopup"), parantTranform));

		if (popup != null)
		{
			popup.SetText(LocalizeManager.Instance.GetStrData(LocalizeStatus.Game, 65));
			popup.ActiveOn();
		}
	}

	/// <summary> 내 동맹원이 배신했을 때 : (배신한 사람 색 육각형) 배신! </summary>
	public void BetrayedPopupOn(PlayerEnum betrayPlayer)
	{
		YourTurnPopup popup = PopupOrderOn(Instantiate(Resources.Load<YourTurnPopup>("Popup/InGame/YourTurnPopup"), parantTranform));

		if (popup != null)
		{
			popup.SetPlayerIcon(betrayPlayer);
			popup.SetText(LocalizeManager.Instance.GetStrData(LocalizeStatus.Game, 66));
			popup.ActiveOn();
		}
	}

    public void TimeOverPopupOn()
    {
		ToastPopup timeOverPopup = PopupOrderOn(Instantiate(Resources.Load<ToastPopup>("Popup/InGame/TimeOverPopup"), parantTranform));

		if (timeOverPopup != null)
        {
            timeOverPopup.ActiveOn();
        }
    }

    public void InGameWarningPopupOn(string text)
	{
		ToastPopup popup = PopupOrderOn(Instantiate(Resources.Load<ToastPopup>("Popup/InGame/InGameWarningPopup"), parantTranform));

		if (popup != null)
		{
            popup.SetText(text);
			popup.ActiveOn();
		}
	}

	public void NeedCoinPopupOn(int needCoin)
	{
		NeedCoinPopup needCoinPopup = PopupOrderOn(Instantiate(Resources.Load<NeedCoinPopup>("Popup/InGame/NeedCoinPopup"), parantTranform));

		if (needCoinPopup != null)
		{
			needCoinPopup.ActiveOn(needCoin);
		}
	}

	public void OptionPopupOn(InGameControllerBase inGameController)
    {
		OptionPopup optionPopup = PopupOrderOn(Instantiate(Resources.Load<OptionPopup>("Popup/InGame/OptionPopup"), parantTranform));

        if (optionPopup != null)
        {
			optionPopup.DataInit(inGameController);
        }
    }

	public void LeaveEarlyPopupOn(InGameControllerBase inGameController, int getCoin)
	{
		LeaveEarlyPopup leaveEarlyPopup = PopupOrderOn(Instantiate(Resources.Load<LeaveEarlyPopup>("Popup/InGame/LeaveEarlyPopup"), parantTranform));

		if (leaveEarlyPopup != null)
		{
			leaveEarlyPopup.DataInit(inGameController, getCoin);
		}
	}

	public void TutorialPopupOn()
    {
		TutorialPopup tutorialPopup = PopupOrderOn(Instantiate(Resources.Load<TutorialPopup>("Popup/InGame/TutorialPopup"), parantTranform));

		if (tutorialPopup != null)
        {
            tutorialPopup.TutorialOn();
        }
    }

    public void GameResultPopupOn(InGameControllerBase inGameController , GameResultEnum gameResultEnum, int coinCount)
    {
        GameResultPopup gameResultPopup = PopupOrderOn(Instantiate(Resources.Load<GameResultPopup>("Popup/InGame/GameResultPopup"), parantTranform));
        gameResultPopup.DataInit(inGameController, gameResultEnum, coinCount);
    }
	/// <param name="penaltyFreeOn"> true : 땅이 0 이 되어 뜬 경우 - 지금 끝내면 배신 패널티 면제 (2026-09-26) </param>
	public void GiveUpPopupOn(InGameControllerBase inGameController, bool newGameOn = false, int coinCount = 0, bool penaltyFreeOn = false)
	{
		GiveUpPopup giveUpPopup = PopupOrderOn(Instantiate(Resources.Load<GiveUpPopup>("Popup/InGame/GiveUpPopup"), parantTranform));
		giveUpPopup.DataInit(inGameController, newGameOn , coinCount, penaltyFreeOn);
	}

	public LandTradePopup LandTradePopupOn()
    {
        LandTradePopup landTradePopup = PopupOrderOn(Instantiate(Resources.Load<LandTradePopup>("Popup/InGame/LandTradePopup"), parantTranform));

        return landTradePopup;
    }

    public AlliancePopup AlliancePopupOn()
    {
        AlliancePopup alliancePopup = PopupOrderOn(Instantiate(Resources.Load<AlliancePopup>("Popup/InGame/AlliancePopup"), parantTranform));

        return alliancePopup;
    }


    public BetrayPopup BetrayPopupOn()
    {
        BetrayPopup betrayPopup = PopupOrderOn(Instantiate(Resources.Load<BetrayPopup>("Popup/InGame/BetrayPopup"), parantTranform));

        return betrayPopup;
    }

	public BuyCoinPopup BuyCoinPopupOn()
    {
        BuyCoinPopup buyCoinPopup = PopupOrderOn(Instantiate(Resources.Load<BuyCoinPopup>("Popup/Common/BuyCoinPopup"), parantTranform));

        return buyCoinPopup;
    }


    //	public OkPopup OkPopupCreate(string contensStr, UnityAction okBtnClickEvent, UnityAction cancelBtnClickEvent = null , bool maskClickDestoryOn = false, bool okBtnOnly = false, string okBtnStr = "예", string cancelBtnStr = "아니오")
    //	{
    //        if (parantTranform == null)
    //			return null;

    //		parantTranform.SetAsLastSibling();

    //		OkPopup okPopup = PopupOrderOn(Instantiate(Resources.Load<OkPopup>("Popup/OkPopup"), parantTranform));
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
    //		WarningPopup warningPopup = PopupOrderOn(Instantiate(Resources.Load<WarningPopup>("Popup/WarningPopup"), parantTranform));
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
