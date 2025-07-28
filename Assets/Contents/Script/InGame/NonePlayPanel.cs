using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class NonePlayPanel : MonoBehaviour
{
    [SerializeField] Text introText;
    [SerializeField] SkillCard skillCard;
    [SerializeField] RectTransform skillBtnPanel;
    [SerializeField] Text stashText;
    [SerializeField] List<Button> btnList = new List<Button>();
    [SerializeField] DiceWarUIController diceWarUIController;

    private AreaData selectAreaData;

    public UnityAction<InGameButtonStatus> skillBtnClickEventOn = data => { };

    public enum InGameButtonStatus
    {
        None = -1,
        Ally = 0,
        Sell = 1,
        Buy = 2,
        Betray = 3,
        Cancel = 4,
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void Init()
    {
		SetNoneBtn();

        DataManager.Instance.SetStashCount(DataManager.Instance.playerData.playerEnum, 0);
		SetStashText();

        diceWarUIController.DiceClear();

		introText.gameObject.SetActive(true);
	}

    public void SetAreaData(AreaData areaData , InGameButtonStatus inGameButtonStatus)
    {
        selectAreaData = areaData;

        if (inGameButtonStatus == InGameButtonStatus.Buy) 
        {
            LandTradePopupOn(true);
        }
        else if (inGameButtonStatus == InGameButtonStatus.Sell)
        {
            LandTradePopupOn(false);
        }
    }

    public void SetNoneBtn()
    {
        selectAreaData = null;    
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
    }

    public void SetData()
    {
        skillCard.gameObject.SetActive(DataManager.Instance.isMultiOn);
    }

    public void SkillCardPanelActiveOn(bool activeOn)
    {
        skillBtnPanel.gameObject.SetActive(activeOn);

        foreach (var btn in btnList)
        {
            btn.gameObject.SetActive(false);
        }

        if (activeOn)
        {
            if (SkillAlreadyUseCheck() || DataManager.Instance.IsMyTurn() == false) 
                return;
            
            bool allianceOn = DataManager.Instance.IsAlliance(DataManager.Instance.playerData.playerEnum);

            if (allianceOn) 
            {
                btnList[(int)InGameButtonStatus.Betray].gameObject.SetActive(true);
            }
            else 
            {
                btnList[(int)InGameButtonStatus.Ally].gameObject.SetActive(true);
            }
            
            btnList[(int)InGameButtonStatus.Sell].gameObject.SetActive(true);
            btnList[(int)InGameButtonStatus.Buy].gameObject.SetActive(true);
        }
    }

    public void CancelBtnClickOn()
    {
        skillBtnClickEventOn(InGameButtonStatus.Cancel);
        SkillCardPanelActiveOn(false);
    }

    public void BuyBtnClickOn()
    {
        skillBtnClickEventOn(InGameButtonStatus.Buy);
        SetActiveBtn(InGameButtonStatus.Cancel);
    }

    public void SellBtnClickOn()
    {
        skillBtnClickEventOn(InGameButtonStatus.Sell);
        SetActiveBtn(InGameButtonStatus.Cancel);
    }

    private void LandTradePopupOn(bool buyOn)
    {
        if (SkillAlreadyUseCheck())
            return;

        LandTradePopup landTradePopup = PopupManager.Instance.LandTradePopupOn();
        landTradePopup.gameObject.SetActive(true);
        landTradePopup.SetDataOn(selectAreaData , buyOn , ProposeBtnClickEventOn);
    }

    private void ProposeBtnClickEventOn()
    {
        CancelBtnClickOn();
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

        SkillCardPanelActiveOn(false);
    }

    public void AllianceClearOn(AllianceResultRequest allianceResultRequest)
    {
        List<AllianceData> allianceDataList = allianceResultRequest.allianceDataList;

        DataManager.Instance.SetAllianceList(allianceDataList);

        if (allianceResultRequest.orderData.playerEnum == DataManager.Instance.playerData.playerEnum)
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
    }

    /// <summary>
    /// 스킬 3개 다 사용했는지 체크
    /// </summary>
    /// <returns></returns>
    private bool SkillAlreadyUseCheck()
    {
        return DataManager.Instance.playerData.skillCardCount <= 0;
    }

    public void SkillUseOn(int addCount = -1)
    {
        DataManager.Instance.SkillCardCountAdd(addCount);

        skillCard.SetCountIcon(DataManager.Instance.playerData.playerEnum);

        if (SkillAlreadyUseCheck())
        {
            SkillCardPanelActiveOn(false);
        }
    }

    public void SetStashText()
    {
        int stashCount = DataManager.Instance.GetStashCount(DataManager.Instance.playerData.playerEnum);

        if (stashCount <= 0)
        {
            stashText.gameObject.SetActive(false);
        }
        else
        {
            stashText.gameObject.SetActive(true);
            stashText.text = $"stash : {stashCount}";
        }
    }

    public async UniTask AttackOn(DiceWarData myDiceWarData, DiceWarData enemyDiceWarData)
    {
		introText.gameObject.SetActive(false);

		await diceWarUIController.AttackOn(myDiceWarData, enemyDiceWarData);
    }
}
