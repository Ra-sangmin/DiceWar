using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System;

public class GiveUpPopup : MonoBehaviour
{
	[SerializeField] Text coinText;

	private int addCoin;
	private InGameControllerBase inGameController;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	public void DataInit(InGameControllerBase inGameController, int addCoin)
	{
		this.inGameController = inGameController;
		this.addCoin = addCoin;

		coinText.text = $"{addCoin} coin";
	}

	public void GoMainBtnClickOn()
	{
		DataManager.Instance.AddCoin(addCoin);

		if (DataManager.Instance.isMultiOn)
		{
			ServerManager.Instance.GameOutRequestOn();
		}
		
		inGameController.GoMainOn();
	}

	public void ReturnBtnClickOn()
	{
		Destroy(gameObject);
	}
}
