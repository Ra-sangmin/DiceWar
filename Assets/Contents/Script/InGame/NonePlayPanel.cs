using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using UniRx;
using UnityEngine.UIElements;

public class NonePlayPanel : MonoBehaviour
{
    [SerializeField] SkillCard skillCard;
    [SerializeField] DiceWarUIController diceWarUIController;

    private VisualElement panelRoot;
    private Label introText;
    private VisualElement skillBtnPanel;
    private VisualElement stashPanel;
    private Label stashText;
    private VisualElement skillCardIcon;
    private Button skillCardBtn;
    private List<Button> btnList = new List<Button>();

	private AreaData selectAreaData;

    /// <summary> 영토 교환 : 먼저 고른 '내가 줄 땅' (2026-09-26) </summary>
    private AreaData exchangeGiveAreaData;

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

    /// <summary> UI Toolkit 요소 연결 (기존 uGUI 하단 바 대체) </summary>
    public void InitView(VisualElement root)
    {
        panelRoot = root.Q<VisualElement>("none-play-panel");

        introText = root.Q<Label>("intro-text");
        skillBtnPanel = root.Q<VisualElement>("skill-btn-panel");
        stashPanel = root.Q<VisualElement>("stash-panel");
        stashText = root.Q<Label>("stash-text");
        skillCardIcon = root.Q<VisualElement>("skill-card-icon");

        //btnList 순서는 InGameButtonStatus (Ally, Sell, Buy, Betray, Cancel) 와 같아야 한다
        btnList = new List<Button>
        {
            root.Q<Button>("skill-btn-ally"),
            root.Q<Button>("skill-btn-sell"),
            root.Q<Button>("skill-btn-buy"),
            root.Q<Button>("skill-btn-betray"),
            root.Q<Button>("skill-btn-cancel"),
        };

        btnList[(int)InGameButtonStatus.Ally].clicked += AllyBtnClickOn;
        btnList[(int)InGameButtonStatus.Sell].clicked += SellBtnClickOn;
        btnList[(int)InGameButtonStatus.Buy].clicked += BuyBtnClickOn;
        btnList[(int)InGameButtonStatus.Betray].clicked += BetrayBtnClickOn;
        btnList[(int)InGameButtonStatus.Cancel].clicked += CancelBtnClickOn;

        skillCardBtn = root.Q<Button>("skill-btn");
        skillCardBtn.clicked += SkillCardPanelToggleOn;

        skillCard.InitView(skillCardIcon);

        diceWarUIController.InitView(root, "dice-row-my", "dice-result-my", "dice-row-enemy", "dice-result-enemy");

        LocalizeTextSet();

        LocalizeManager.Instance.language
            .Subscribe(_ => LocalizeTextSet())
            .AddTo(gameObject);
    }

    /// <summary>
    /// 문구 현지화 (원본 LocalizeText 컴포넌트 역할).
    /// 스킬 메뉴 버튼들은 원본도 현지화 없이 영문 고정이라 UXML 문구를 그대로 쓴다.
    /// </summary>
    void LocalizeTextSet()
    {
        LocalizeManager localize = LocalizeManager.Instance;

        //안내 문구 (Game/2), 턴 종료 버튼 (Game/3)
        SetIntroText();

        //스킬 메뉴 (시안 : 동맹·배신 / 교환). 구매·판매는 교환 하나로 합쳤다 (2026-09-26)
        SetBtnLabel("skill-btn-ally-label", localize.GetStrData(LocalizeStatus.Game, 68));
        SetBtnLabel("skill-btn-betray-label", localize.GetStrData(LocalizeStatus.Game, 28));
        SetBtnLabel("skill-btn-buy-label", localize.GetStrData(LocalizeStatus.Game, 67));
        SetBtnLabel("skill-btn-cancel-label", localize.GetStrData(LocalizeStatus.Game, 69));

        Label endTurnLabel = panelRoot.Q<Label>("end-turn-label");

        if (endTurnLabel != null)
        {
            endTurnLabel.text = localize.GetStrData(LocalizeStatus.Game, 3);
        }
    }

    void SetBtnLabel(string name, string text)
    {
        Label label = panelRoot.Q<Label>(name);

        if (label != null)
            label.text = text;
    }

    /// <summary> 하단 안내 문구. 교환 중이면 단계별 안내를 보여준다 </summary>
    void SetIntroText()
    {
        if (introText == null)
            return;

        int key = 2;

        if (exchangeModeOn)
        {
            key = exchangeGiveAreaData == null ? 70 : 71;
        }

        introText.text = LocalizeManager.Instance.GetStrData(LocalizeStatus.Game, key);
        introText.style.display = DisplayStyle.Flex;
    }

    private bool exchangeModeOn = false;

    void ExchangeClear()
    {
        if (exchangeGiveAreaData != null)
        {
            exchangeGiveAreaData.ChoisEventOn(false);
            exchangeGiveAreaData = null;
        }

        exchangeModeOn = false;
        SetIntroText();
    }

    public void SetPanelActive(bool activeOn)
    {
        if (panelRoot != null)
        {
            panelRoot.style.display = activeOn ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    public void Init()
    {
		SetNoneBtn();

        DataManager.Instance.SetStashCount(DataManager.Instance.playerData.pe, 0);
		SetStashText();

        diceWarUIController.DiceClear();

		introText.style.display = DisplayStyle.Flex;
	}

    public void SetAreaData(AreaData areaData , InGameButtonStatus inGameButtonStatus)
    {
        PlayerEnum myPlayerEnum = DataManager.Instance.GetMyPlayerData().pe;

		//영토 교환 (2026-09-26) : 내 땅 → 상대 땅 순서로 고르면 바로 제안한다
		if (inGameButtonStatus == InGameButtonStatus.Buy)
        {
            ExchangeAreaSelectOn(areaData, myPlayerEnum);
        }
        else if (inGameButtonStatus == InGameButtonStatus.Sell && areaData.player == myPlayerEnum)
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
            btn.style.display = DisplayStyle.None;
        }

        if (status != InGameButtonStatus.None)
        {
            btnList[(int)status].style.display = DisplayStyle.Flex;
        }
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void SetData()
    {
        //스킬 카드는 멀티플레이에서만 사용 (원본은 SkillCard 오브젝트째로 껐다)
        bool activeOn = DataManager.Instance.isMultiOn;

        skillCard.SetActive(activeOn);

        skillCardBtn.style.display = activeOn ? DisplayStyle.Flex : DisplayStyle.None;

        if (activeOn == false)
        {
            SkillCardPanelActiveOn(false);
        }
    }

    public void SkillCardPanelToggleOn()
    {
        SkillCardPanelActiveOn(!skillCardPanelActive);
	}

	public void SkillCardPanelActiveOn(bool activeOn)
    {
        this.skillCardPanelActive = activeOn;

		skillBtnPanel.style.display = activeOn ? DisplayStyle.Flex : DisplayStyle.None;

        foreach (var btn in btnList)
        {
            btn.style.display = DisplayStyle.None;
        }

        if (activeOn)
        {
			//if (SkillAlreadyUseCheck() || DataManager.Instance.IsMyTurn() == false) 
			if (SkillAlreadyUseCheck())
				return;
            
            bool allianceOn = DataManager.Instance.IsAlliance(DataManager.Instance.playerData.pe);

            if (allianceOn) 
            {
                btnList[(int)InGameButtonStatus.Betray].style.display = DisplayStyle.Flex;
            }
            else 
            {
                btnList[(int)InGameButtonStatus.Ally].style.display = DisplayStyle.Flex;
            }
            
            //판매는 교환으로 합쳐져 쓰지 않는다 (Buy 버튼 = 교환)
            btnList[(int)InGameButtonStatus.Buy].style.display = DisplayStyle.Flex;
        }
    }

    public void CancelBtnClickOn()
    {
        ExchangeClear();

        skillBtnClickEventOn(InGameButtonStatus.Cancel);
        SkillCardPanelActiveOn(false);
    }

    public void BuyBtnClickOn()
    {
        skillBtnClickEventOn(InGameButtonStatus.Buy);
        SetActiveBtn(InGameButtonStatus.Cancel);

        //교환 모드 시작 : 내 땅부터 고른다
        exchangeModeOn = true;
        SetIntroText();
    }

    public void SellBtnClickOn()
    {
        skillBtnClickEventOn(InGameButtonStatus.Sell);
        SetActiveBtn(InGameButtonStatus.Cancel);
    }

    void ExchangeAreaSelectOn(AreaData areaData, PlayerEnum myPlayerEnum)
    {
        if (SkillAlreadyUseCheck())
            return;

        //내 땅을 고르면 '줄 땅' 으로 (다시 고르면 바꾼다)
        if (areaData.player == myPlayerEnum)
        {
            if (exchangeGiveAreaData != null)
                exchangeGiveAreaData.ChoisEventOn(false);

            exchangeGiveAreaData = areaData;
            exchangeGiveAreaData.ChoisEventOn(true);

            SetIntroText();
            return;
        }

        //줄 땅을 먼저 골라야 한다
        if (exchangeGiveAreaData == null || areaData.player == PlayerEnum.Player_None)
            return;

        //LandTradeRequest 필드를 그대로 쓴다 (서버는 중계만 하므로 새 필드를 만들지 않는다)
        //  areaData  = 내가 받을 상대 땅,  coinCount = 내가 줄 땅의 id,  buyOn = true (교환)
        LandTradeRequest request = new LandTradeRequest()
        {
            fromPlayerEnum = myPlayerEnum,
            toPlayerEnum = areaData.player,
            areaData = areaData,
            coinCount = exchangeGiveAreaData.id,
            buyOn = true,
        };

        ServerManager.Instance.SendMessageOn(request);

        CancelBtnClickOn();
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
		AllianceAllData allianceAllData = new AllianceAllData() { orderData = allianceResultRequest.orderData, allianceDataList = allianceResultRequest.allianceDataList };

		DataManager.Instance.SetAllianceList(allianceAllData);

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

    public void SkillCardAddOn(List<PlayerEnum> playerList)
    {
        foreach (PlayerEnum playerEnum in playerList)
        {
			DataManager.Instance.SkillCardCountAddOn(playerEnum);
		}
	}

    public void SkillUseOn(int addCount = -1)
    {
        DataManager.Instance.SkillCardCountAdd(addCount);

        SkillCardReset();
	}

    public void SkillCardReset()
    {
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
            stashPanel.style.opacity = 0;
        }
        else
        {
			stashPanel.style.opacity = 1;

			string stashTextStr = LocalizeManager.Instance.GetStrData( LocalizeStatus.Game, 38);

			stashText.text = $"{stashTextStr} : {stashCount}";
        }
    }

    public async UniTask AttackOn(DiceWarData myDiceWarData, DiceWarData enemyDiceWarData , CancellationTokenSource source)
    {
		introText.style.display = DisplayStyle.None;

		await diceWarUIController.AttackOn(myDiceWarData, enemyDiceWarData , source);
    }
}
