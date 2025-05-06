using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CoinBox : MonoBehaviour
{
    [SerializeField] RectTransform tradeCoinPanel;
    [SerializeField] Image activeImage;
    [SerializeField] RectTransform addCoinPanel;
    [SerializeField] Text coinText;
    public int coinCount = 3;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SelectOn(bool selectOn , int coinCount = 3)
    {
        //toggle.isOn = selectOn;

        this.coinCount = coinCount;

        tradeCoinPanel.gameObject.SetActive(selectOn);

        //coinCount = 3;
        coinText.text = coinCount.ToString();

        SetActiveAddCoinPanel(false);
    }
    public void SetActiveAddCoinPanel(bool activeOn)
    {
        return;

        addCoinPanel.gameObject.SetActive(activeOn);
    }

    public void SetCoinBtnActiveOn(bool activeOn)
    {
        activeImage.gameObject.SetActive(activeOn);
    }
    public void CoinCountChange(bool addOn)
    {
        if (addOn)
        {
            coinCount++;
        }
        else
        {
            coinCount--;
        }

        coinText.text = coinCount.ToString();
    }

    public void SetCoinCount(int coinCount) 
    {
        this.coinCount = coinCount;
        coinText.text = coinCount.ToString();
    }

}
