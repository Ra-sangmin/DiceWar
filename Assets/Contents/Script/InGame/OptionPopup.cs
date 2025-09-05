using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using static GameResultPopup;

public class OptionPopup : MonoBehaviour
{
    [SerializeField] Button restartBtn;
	[SerializeField] VerticalLayoutGroup vlg;
	[SerializeField] PlayCoinBtn playCoinBtn;

	private InGameControllerBase inGameController;

    // Start is called before the first frame update
    void Start()
    {
		DataManager.Instance.userData.myCoin
		    .Subscribe(_ => playCoinBtn.SetNeedCoinCheck())
		    .AddTo(gameObject);
	}

    // Update is called once per frame
    void Update()
    {
        
    }

    public void DataInit(InGameControllerBase inGameController)
    {
        this.inGameController = inGameController;

		restartBtn.gameObject.SetActive(!DataManager.Instance.isMultiOn);

		float spacing = DataManager.Instance.isMultiOn ? 30 : 20;
		vlg.spacing = spacing;
	}

	public void SettingPopupOn()
    {
        //PopupManager.Instance.se;
    }

    public void RestartBtnClickOn()
    {
        inGameController.ReStartOn();
        CloseBtnClickOn();
	}

	public void NewGameBtnClickOn()
	{
        if (DataManager.Instance.CheckNewGame())
        {
			inGameController.NewGameOn();
			CloseBtnClickOn();
		}
	}

	public void GoMainBtnClickOn()
	{
        int coinCount = 0;

        if (DataManager.Instance.isMultiOn)
        {
			AllianceData allianceData = DataManager.Instance.GetMyAllianceData();

            if (allianceData != null)
            {
                coinCount = -allianceData.coinCount;
			}

			PopupManager.Instance.GiveUpPopupOn(inGameController, coinCount);
		}
        else 
        {
			inGameController.GoMainOn();
		}
	}

    public void TutorialBtnClickOn()
    {
        PopupManager.Instance.TutorialPopupOn();
    }

    public void CloseBtnClickOn()
    {
        Destroy(gameObject);
    }
}
