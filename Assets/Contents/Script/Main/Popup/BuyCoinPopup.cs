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

    /// <summary>
    /// IAP 버튼을 이용한 자동 인앱 결제후 성공했을때 호출
    /// </summary>
    /// <param name="index"></param>
    public void BuyCoinClearOn(int index)
    {
        int addCoin = 0;

        switch ((ProductIdEnum)index)
        {
            case ProductIdEnum.coin_add_10: addCoin = 10; 
                break;
			case ProductIdEnum.coin_add_30: addCoin = 30; 
                break;
		}

        DataManager.Instance.AddCoin(0,addCoin);
    }

    public void WatchAdsOn()
    {
        int addCoin = 10;

        DataManager.Instance.AddCoin(addCoin);
    }
}

[System.Serializable]
public enum ProductIdEnum
{
	coin_add_10 = 0,
	coin_add_30 = 1,
}
