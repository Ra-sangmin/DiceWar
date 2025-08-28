using UnityEngine;
using UnityEngine.UI;

public class NeedCoinPopup : ToastPopup
{
	[SerializeField] Text coinText;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
		
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	public void ActiveOn(int needCoin)
	{
		maxAlphaValue = 1;

		base.ActiveOn();

		coinText.text = $"{needCoin} Coins";
	}
}
