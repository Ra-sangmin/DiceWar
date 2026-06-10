using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System;
using Cysharp.Threading.Tasks;

public class GiveUpPopup : MonoBehaviour
{
	[SerializeField] Text coinText;

	[SerializeField] Button goMainBtn;
	[SerializeField] Button newGameBtn;

	private int addCoin;
	private bool newGameOn = false;
	private InGameControllerBase inGameController;

	private PlayerIconController playerIconController;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
		playerIconController = FindFirstObjectByType<PlayerIconController>();
	}

    // Update is called once per frame
    void Update()
    {
        
    }

	public void DataInit(InGameControllerBase inGameController, bool newGameOn, int addCoin)
	{
		this.inGameController = inGameController;
		this.addCoin = addCoin;
		this.newGameOn = newGameOn;

		goMainBtn.gameObject.SetActive(!newGameOn);
		newGameBtn.gameObject.SetActive(newGameOn);

		coinText.text = $"{addCoin} coin";
	}

	public void GoMainBtnClickOn()
	{
		GoMainOn().Forget();
	}

	private async UniTask GoMainOn()
	{
		if (DataManager.Instance.isMultiOn)
		{
			AllianceData allianceData = DataManager.Instance.GetMyAllianceData();

			if (allianceData != null)
			{
				await DataManager.Instance.MyBetrayOn(playerIconController);
			}

			ServerManager.Instance.GameOutRequestOn();
		}

		inGameController.GoMainOn();
	}

	public void NewGameBtnClickOn()
	{
		NewGameOn().Forget();
	}

	private async UniTask NewGameOn()
	{
		if (DataManager.Instance.isMultiOn == false)
			return;
		
		AllianceData allianceData = DataManager.Instance.GetMyAllianceData();

		if (allianceData != null)
		{
			await DataManager.Instance.MyBetrayOn(playerIconController);
		}

		inGameController.NewGameOn();
	}

	public void ReturnBtnClickOn()
	{
		Destroy(gameObject);
	}
}
