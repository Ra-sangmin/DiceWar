using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

public class MyCoinPanel : MonoBehaviour
{
    [SerializeField] Text myCoinText;

    private void Awake()
    {
        SetEvent();
    }

    void SetEvent()
    {
        DataManager.Instance.CheckUserData();

		DataManager.Instance.userData.myCoin.
            Subscribe(coin => SetCoin())
            .AddTo(gameObject);
        SetCoin();
    }

    void SetCoin()
    {
        myCoinText.text = DataManager.Instance.userData.myCoin.Value.ToString();
    }


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void BuyCoinPopupOpen()
    {
        PopupManager.Instance.BuyCoinPopupOn();
    }
}
