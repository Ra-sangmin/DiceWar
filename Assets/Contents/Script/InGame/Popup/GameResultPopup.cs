using System;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameResultPopup : MonoBehaviour
{
    [SerializeField] Text titleText;
    [SerializeField] Text coinText;
    //[SerializeField] RectTransform needCoinTextPanel;
    //[SerializeField] Text needCoinText;

    //[SerializeField] RewardedAdsButton rewardedAdsButton;
    //[SerializeField] RectTransform playButton;

    private InGameControllerBase inGameController;
    private GameResultEnum gameResultEnum;

    public enum GameResultEnum
    {
        Win,
        Lose,
        GiveUp,
        LeaveEarly
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
		//AdsManager.Instance.SetRewardedAdsButton(rewardedAdsButton);
	}

    // Update is called once per frame
    void Update()
    {
        
    }

    public void DataInit(InGameControllerBase inGameController, GameResultEnum gameResultEnum, int coinCount)
    {
        this.inGameController = inGameController;
        this.gameResultEnum = gameResultEnum;

        string titleTextStr = string.Empty;

        switch (gameResultEnum)
        {
            case GameResultEnum.Win: titleTextStr = "You Win"; break;
			case GameResultEnum.Lose: titleTextStr = "You Lose"; break;
			case GameResultEnum.GiveUp: titleTextStr = "Give Up"; break;
			case GameResultEnum.LeaveEarly: titleTextStr = "Leave Early"; break;
		}

        titleText.text = titleTextStr;

        DataManager.Instance.AddCoin(coinCount);

        coinText.text = $"{coinCount} coin";

        DataManager.Instance.GameDataClearOn();

        Observable
                .Timer(TimeSpan.FromSeconds(1f))
                .Repeat()
                .Subscribe(_ => ServerManager.Instance.GameOutRequestOn())
                .AddTo(this);

  //      SetNeedCoinCheck();

		//DataManager.Instance.userData.myCoin
	 //       .Subscribe(_ => SetNeedCoinCheck())
	 //       .AddTo(gameObject);

	}

	//void SetNeedCoinCheck()
	//{
	//	int needCoin = DataManager.Instance.GetNeedCoin();

	//	bool needCoinOn = DataManager.Instance.userData.myCoin.Value < needCoin;

	//	//needCoinBG.gameObject.SetActive(needCoinOn);
	//	playButton.gameObject.SetActive(!needCoinOn);

 //       needCoinText.transform.parent.gameObject.SetActive(needCoinOn);

 //       if (DataManager.Instance.aiLevel == AILevel.Easy)
 //       {
 //           needCoinTextPanel.gameObject.SetActive(false);
 //       }
 //       else
 //       {
 //           needCoinTextPanel.gameObject.SetActive(true);
 //           needCoinText.text = $"-{needCoin}";
 //       }

 //       //needCoinText.text = $"-{needCoin}";
	//}

	public void GoMainBtnClickOn()
    {
        inGameController.GoMainOn();
        gameObject.SetActive(false);
    }

    public void NewGameBtnClickOn()
    {
        if (DataManager.Instance.isMultiOn)
        {
            SceneManager.LoadScene("Loading");
        }
        else 
        {
			if (DataManager.Instance.CheckNewGame())
			{
				inGameController.NewGameOn();
				gameObject.SetActive(false);
			}
        }
    }

	
}
