using System;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UniRx;

public class GameResultPopup : MonoBehaviour
{
    [SerializeField] Text titleText;
    [SerializeField] Text coinText;
	[SerializeField] PlayCoinBtn playCoinBtn;

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
		DataManager.Instance.userData.myCoin
	        .Subscribe(_ => playCoinBtn.SetNeedCoinCheck())
	        .AddTo(gameObject);
	}

    // Update is called once per frame
    void Update()
    {
        
    }

    public void DataInit(InGameControllerBase inGameController, GameResultEnum gameResultEnum, int coinCount)
    {
        this.inGameController = inGameController;
        this.gameResultEnum = gameResultEnum;

        int key = 0;

        switch (gameResultEnum)
        {
            case GameResultEnum.Win:        key = 16; break;
			case GameResultEnum.Lose:       key = 17; break;
			case GameResultEnum.GiveUp:     key = 11; break;
			case GameResultEnum.LeaveEarly: key = 18; break;
		}

		string titleTextStr = LocalizeManager.Instance.GetStrData(LocalizeStatus.Game, key);

		titleText.text = titleTextStr;

        DataManager.Instance.AddCoin(coinCount);

        coinText.text = $"{coinCount} coin";

        DataManager.Instance.GameDataClearOn();

        Observable
                .Timer(TimeSpan.FromSeconds(1f))
                .Repeat()
                .Subscribe(_ => ServerManager.Instance.GameOutRequestOn())
                .AddTo(this);
	}

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
