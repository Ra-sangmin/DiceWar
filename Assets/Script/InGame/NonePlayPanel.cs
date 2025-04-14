using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NonePlayPanel : MonoBehaviour
{
    [SerializeField] RectTransform singlePanel;
    [SerializeField] RectTransform multiPanel;
    [SerializeField] SkillCard skillCard;

    [SerializeField] List<Button> btnList = new List<Button>();

    private AreaData selectAreaData;

    public enum InGameButtonStatus
    {
        None = -1,
        Ally = 0,
        Sell = 1,
        Buy = 2,
        Betray = 3,
    }

    // Start is called before the first frame update
    void Start()
    {
        SetNoneBtn();
    }

    public void SetAreaData(AreaData areaData , InGameButtonStatus inGameButtonStatus)
    {
        selectAreaData = areaData;
        SetActiveBtn(inGameButtonStatus);
    }

    public void SetNoneBtn()
    {
        selectAreaData = null;

        if (InGameDataManager.Instance.IsAlliance(InGameDataManager.Instance.playerData.playerEnum))
        {
            SetActiveBtn(InGameButtonStatus.Betray);
        }
        else
        {
            SetActiveBtn(InGameButtonStatus.None);
        }        
    }

    public void SetActiveBtn(InGameButtonStatus status)
    {
        foreach (var btn in btnList)
        {
            btn.gameObject.SetActive(false);
        }

        if (status != InGameButtonStatus.None)
        {
            btnList[(int)status].gameObject.SetActive(true);
        }
    }

    // Update is called once per frame
    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Alpha1))
        //{
        //    SetActiveBtn(InGameButtonStatus.Ally);
        //}
        //if (Input.GetKeyDown(KeyCode.Alpha2))
        //{
        //    SetActiveBtn(InGameButtonStatus.Sell);
        //}
        //if (Input.GetKeyDown(KeyCode.Alpha3))
        //{
        //    SetActiveBtn(InGameButtonStatus.Buy);
        //}
        //if (Input.GetKeyDown(KeyCode.Alpha4))
        //{
        //    SetActiveBtn(InGameButtonStatus.Betray);
        //}

        //if (Input.GetKeyDown(KeyCode.Alpha5))
        //{
        //    //SetCountIcon(4);
        //}
    }

    public void SetData()
    {
        singlePanel.gameObject.SetActive(InGameDataManager.Instance.isMultiOn == false);
        multiPanel.gameObject.SetActive(InGameDataManager.Instance.isMultiOn);
    }

    //public void SetCountIcon(int countIndex)
    //{
    //    skillCard.SetCountIcon(countIndex);
    //}

    public void BuyBtnClickOn()
    {
        LandTradePopupOn(true);
    }

    public void SellBtnClickOn()
    {
        LandTradePopupOn(false);
    }

    private void LandTradePopupOn(bool buyOn)
    {
        if (SkillAlreadyUseCheck())
            return;

        LandTradePopup landTradePopup = PopupManager.Instance.LandTradePopupOn();
        landTradePopup.gameObject.SetActive(true);
        landTradePopup.SetDataOn(selectAreaData , buyOn);

        //landTradePopup.tradeClear = TradeClear;
    }

    private void TradeClear(LandTradePopup landTradePopup)
    {
        SkillUseOn();
    }

    public void AllyBtnClickOn()
    {
        if (SkillAlreadyUseCheck())
            return;

        AlliancePopup alliancePopup = PopupManager.Instance.AlliancePopupOn();
        alliancePopup.SetData();
        //alliancePopup.AllianceClearOn = AllianceClearOn;
        Debug.LogWarning("AllyBtnClickOn");
    }

    public void AllianceClearOn(AllianceResultRequest allianceResultRequest)
    {
        List<AllianceData> allianceDataList = allianceResultRequest.allianceDataList;

        InGameDataManager.Instance.SetAllianceList(allianceDataList);

        if (allianceResultRequest.orderData.playerEnum == InGameDataManager.Instance.playerData.playerEnum)
        {
            SkillUseOn();
        }
        

        SetNoneBtn();
    }

    public void BetrayBtnClickOn()
    {
        if (SkillAlreadyUseCheck())
            return;

        BetrayPopup BetrayPopup = PopupManager.Instance.BetrayPopupOn();
        BetrayPopup.SetData();
        //BetrayPopup.BetrayClearOn = () => 
        //{
        //    InGameDataManager.Instance.BetrayOn();

        //    SkillUseOn();

        //    SetNoneBtn();
        //};
    }

    /// <summary>
    /// 스킬 3개 다 사용했는지 체크
    /// </summary>
    /// <returns></returns>
    private bool SkillAlreadyUseCheck()
    {
        return InGameDataManager.Instance.playerData.skillCardCount <= 0;
    }

    public void SkillUseOn()
    {
        InGameDataManager.Instance.playerData.skillCardCount--;
        skillCard.SetCountIcon();
    }
}
