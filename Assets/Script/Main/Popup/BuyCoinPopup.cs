using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System;

public class BuyCoinPopup : MonoBehaviour
{
    //[SerializeField] Text myCoinText;

    //private void Awake()
    //{
    //    SetEvent();
    //}

    //void SetEvent()
    //{
    //    InGameDataManager.Instance.myCoin.Subscribe(coin => SetCoin());
    //    SetCoin();
    //}

    //void SetCoin()
    //{
    //    myCoinText.text = InGameDataManager.Instance.myCoin.Value.ToString();
    //}

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void BuyCoinOn(int index)
    {
        int addCoin = index == 0 ? 10 : 30;

        DataManager.Instance.AddCoin(addCoin);
    }

    public void WatchAdsOn()
    {
        int addCoin = 10;

        DataManager.Instance.AddCoin(addCoin);
    }
}
