using UnityEngine;
using UnityEngine.UI;

public class GameResultPopup : MonoBehaviour
{
    [SerializeField] Text titleText;
    [SerializeField] Text coinText;
    private InGameController inGameController;
    private bool win;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void DataInit(InGameController inGameController, bool win)
    {
        this.inGameController = inGameController;
        this.win = win;

        titleText.text = win ? "Victory" : "Defeat";

        int coinCount = 0;

        if (win) 
        {
            if (DataManager.Instance.isMultiOn)
            {

            }
            else
            {
                int addCoin = 0;

                switch (DataManager.Instance.aiLevel)
                {
                    case AILevel.Easy: addCoin = 1; break;
                    case AILevel.Normal: addCoin = 2; break;
                    case AILevel.Hard: addCoin = 3; break;
                }

                coinCount = addCoin * DataManager.Instance.num_player;
            }
        }

        DataManager.Instance.AddCoin(coinCount);

        coinText.text = $"{coinCount} coin";
    }

    public void GoMainBtnClickOn()
    {
        inGameController.GoMainOn();
        gameObject.SetActive(false);
    }

    public void NewGameBtnClickOn()
    {
        inGameController.NewGameOn();
        gameObject.SetActive(false);
    }
}
