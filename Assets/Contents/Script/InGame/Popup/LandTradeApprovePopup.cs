using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LandTradeApprovePopup : MonoBehaviour
{
    [SerializeField] PlayerToggleIcon PlayerToggleIcon;
    [SerializeField] Text tradeText;
    [SerializeField] CoinBox coinBox;
    private LandTradeRequest landTradeRequest;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetData(LandTradeRequest landTradeRequest)
    {
        this.landTradeRequest = landTradeRequest;

        PlayerToggleIcon.SetPlayerData(this.landTradeRequest.fromPlayerEnum);

        string tradeTextValue = landTradeRequest.buyOn ? "Buyer" : "Seller";
        tradeText.text = tradeTextValue;

        coinBox.SetCoinCount(landTradeRequest.coinCount);
    }

    public void RefuseBtnClickOn()
    {
        DataRequestOn(false);
    }

    public void AcceptBtnClickOn() 
    {
        DataRequestOn(true);
    }

    void DataRequestOn(bool approveOn)
    {
        LandTradeApproveRequest request = new LandTradeApproveRequest()
        {
            landTradeRequest = landTradeRequest,
            approveOn = approveOn,
        };

        ServerManager.Instance.SendMessageOn(request);
        gameObject.SetActive(false);
    }
}
