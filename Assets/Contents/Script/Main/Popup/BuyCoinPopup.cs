using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System;
using UnityEngine.Purchasing;

public class BuyCoinPopup : MonoBehaviour
{
    [SerializeField] RewardedAdsButton rewardedAdsButton;

    // Start is called before the first frame update
    void Start()
    {
        AdsManager.Instance.SetRewardedAdsButton(rewardedAdsButton);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// IAPManager를 이용한 직접적인 인앱 구매 ( 삭제 예정 )
    /// </summary>
    /// <param name="index"></param>
    public void BuyCoinOn(int index)
    {
        if (index == 0) 
        {
            IAPManager.Instance.Purchase(IAPManager.Instance.productId_test_id);
        }
        else 
        {
            IAPManager.Instance.Purchase(IAPManager.Instance.productId_test_id2);
        }
    }

    /// <summary>
    /// IAP 버튼을 이용한 자동 인앱 결제후 성공했을때 호출
    /// </summary>
    /// <param name="index"></param>
    public void BuyCoinClearOn(int index)
    {
        int addCoin = index == 0 ? 10 : 30;

        DataManager.Instance.AddCoin(addCoin);
    }

    public void WatchAdsOn()
    {
        Debug.LogWarning("WatchAdsOn");

        int addCoin = 10;

        DataManager.Instance.AddCoin(addCoin);
    }
}
