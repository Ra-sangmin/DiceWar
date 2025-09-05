using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class NonePlayPanel : MonoBehaviour
{
    [SerializeField] Text introText;
    [SerializeField] SkillCard skillCard;
    [SerializeField] RectTransform skillBtnPanel;
	[SerializeField] CanvasGroup stashCanvasGroup;
	[SerializeField] Text stashText;
    [SerializeField] List<Button> btnList = new List<Button>();
    [SerializeField] DiceWarUIController diceWarUIController;

	private AreaData selectAreaData;

    public UnityAction<InGameButtonStatus> skillBtnClickEventOn = data => { };

    public bool skillCardPanelActive = false;


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

        DataManager.Instance.SetStashCount(DataManager.Instance.playerData.pe, 0);
		SetStashText();

        diceWarUIController.DiceClear();

		introText.gameObject.SetActive(true);
	}

    public void SetAreaData(AreaData areaData , InGameButtonStatus inGameButtonStatus)
    {
        if (inGameButtonStatus == InGameButtonStatus.Buy && areaData.player != DataManager.Instance.GetMyPlayerData().pe) 
        {
			selectAreaData = areaData;
			LandTradePopupOn(true);
        }
        else if (inGameButtonStatus == InGameButtonStatus.Sell && areaData.player == DataManager.Instance.GetMyPlayerData().pe)
        {
			selectAreaData = areaData;
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

    public void SkillCardPanelToggleOn()
    {
        SkillCardPanelActiveOn(!skillCardPanelActive);
	}

	public void SkillCardPanelActiveOn(bool activeOn)
    {
        this.skillCardPanelActive = activeOn;

		skillBtnPanel.gameObject.SetActive(activeOn);

        foreach (var btn in btnList)
        {
            btn.gameObject.SetActive(false);
        }

        if (activeOn)
        {
			//if (SkillAlreadyUseCheck() || DataManager.Instance.IsMyTurn() == false) 
			if (SkillAlreadyUseCheck())
				return;
            
            bool allianceOn = DataManager.Instance.IsAlliance(DataManager.Instance.playerData.pe);

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

    public void AllianceClearOn(List<AllianceData> allianceDataList)
    {
        DataManager.Instance.SetAllianceList(allianceDataList);

        SetNoneBtn();

		SkillCardPanelActiveOn(skillCardPanelActive);
	}

    public void BetrayBtnClickOn()
    {
        if (SkillAlreadyUseCheck())
            return;

        BetrayPopup BetrayPopup = PopupManager.Instance.BetrayPopupOn();
        BetrayPopup.SetData();

		SkillCardPanelActiveOn(false);

	}

    /// <summary>
    /// 스킬 3개 다 사용했는지 체크
    /// </summary>
    /// <returns></returns>
    private bool SkillAlreadyUseCheck()
    {
        return DataManager.Instance.GetMyPlayerData().sc <= 0;
    }

    public void SkillUseOn(int addCount = -1)
    {
        DataManager.Instance.SkillCardCountAdd(addCount);

        skillCard.SetCountIcon(DataManager.Instance.GetMyPlayerData().pe);

        if (SkillAlreadyUseCheck())
        {
            SkillCardPanelActiveOn(false);
        }
    }

    public void SetStashText()
    {
        int stashCount = DataManager.Instance.GetStashCount(DataManager.Instance.playerData.pe);

        if (stashCount <= 0)
        {
            stashCanvasGroup.alpha = 0;
        }
        else
        {
			stashCanvasGroup.alpha = 1;

			string stashTextStr = LocalizeManager.Instance.GetStrData( LocalizeStatus.Game, 38);

			stashText.text = $"{stashTextStr} : {stashCount}";
        }
    }

    public async UniTask AttackOn(DiceWarData myDiceWarData, DiceWarData enemyDiceWarData , CancellationTokenSource source)
    {
		introText.gameObject.SetActive(false);

		await diceWarUIController.AttackOn(myDiceWarData, enemyDiceWarData , source);
    }
}
