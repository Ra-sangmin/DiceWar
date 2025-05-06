using UnityEngine;
using UnityEngine.SceneManagement;
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

    public void DataInit(InGameController inGameController, bool win , int coinCount)
    {
        this.inGameController = inGameController;
        this.win = win;

        titleText.text = win ? "Victory" : "Defeat";

        DataManager.Instance.AddCoin(coinCount);

        coinText.text = $"{coinCount} coin";

        DataManager.Instance.GameDataClearOn();

        ServerManager.Instance.SendMessageOn(new GameEndOnRequest());
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
            inGameController.NewGameOn();
            gameObject.SetActive(false);
        }
    }
}
