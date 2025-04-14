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
    public int coinCount = 5;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Alpha1))
        //{
        //    SetCoinBtnActiveOn(false);
        //    //activeImage.gameObject.SetActive(false);
        //    //Debug.LogWarning("d");
        //}
        //if (Input.GetKeyDown(KeyCode.Alpha2))
        //{
        //    SetCoinBtnActiveOn(true);
        //    //activeImage.gameObject.SetActive(true);
        //    //Debug.LogWarning("3");
        //}
    }

    public void SelectOn(bool selectOn)
    {
        //toggle.isOn = selectOn;

        tradeCoinPanel.gameObject.SetActive(selectOn);

        coinCount = 5;
        coinText.text = coinCount.ToString();

        SetActiveAddCoinPanel(false);
    }
    public void SetActiveAddCoinPanel(bool activeOn)
    {
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
