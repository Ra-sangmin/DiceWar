using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UniRx;

public class MainController : MonoBehaviour
{
    [SerializeField] List<RectTransform> stepPanelList = new List<RectTransform>();

    [SerializeField] List<Toggle> aIToggleList = new List<Toggle>();
    [SerializeField] List<Toggle> playersCountToggleList = new List<Toggle>();
    [SerializeField] List<Toggle> mapSizeToggleList = new List<Toggle>();
    [SerializeField] List<Toggle> turnPositionToggleList = new List<Toggle>();

    [SerializeField] Toggle choosePositionToggle;
    [SerializeField] Toggle mapSelectionToggle;
    [SerializeField] Toggle diceCompensationToggle;

    [SerializeField] Image playerColorImage;

    [SerializeField] RectTransform turnPositionSelectPanel;
    [SerializeField] RectTransform turnPositionCountPanel;
    [SerializeField] Text turnPositionCountText;

    [SerializeField] Text myCoinText;

    private int step = 0;

    public string testJson;

    private void Awake()
    {
        SetEvent();
    }

    void SetEvent()
    {
        SetToggleEvent(aIToggleList,AIToggleChangeOn);
        SetToggleEvent(playersCountToggleList, PlayersCountToggleChangeOn);
        SetToggleEvent(mapSizeToggleList, MapSizeToggleChangeOn);
        SetToggleEvent(turnPositionToggleList, TurnPositionTogglChangeOn);

        choosePositionToggle.onValueChanged.AddListener(ChoosePositionToggleOn);
        mapSelectionToggle.onValueChanged.AddListener(MapSelectionToggle);
        diceCompensationToggle.onValueChanged.AddListener(DiceCompensationToggle);

        PlayerColorChangeOn(0);

        InGameDataManager.Instance.myCoin.Subscribe(coin => SetCoin());
        SetCoin();
    }

    private void SetToggleEvent(List<Toggle> toggleList ,UnityAction<int> toggleEventOn , int defaultIndex = 0)
    {
        for (int i = 0; i < toggleList.Count; i++)
        {
            int index = i;

            toggleList[i].onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                {
                    toggleEventOn(index);
                }
            });
        }

        toggleEventOn(defaultIndex);
    }

    private void AIToggleChangeOn(int index)
    {
        AILevel aiLevel = AILevel.Easy;

        switch (index)
        {
            case 0: aiLevel = AILevel.Easy; break;
            case 1: aiLevel = AILevel.Normal; break;
            case 2: aiLevel = AILevel.Hard; break;
        }

        InGameDataManager.Instance.SetAILevelEnum(aiLevel);
    }

    private void PlayersCountToggleChangeOn(int index)
    {
        int playerMaxCnt = index + 2;
        InGameDataManager.Instance.SetPlayerMaxCnt(playerMaxCnt);
        SetTurnPosition(InGameDataManager.Instance.turnPosition);
    }

    private void MapSizeToggleChangeOn(int index)
    {
        MapSizeEnum mapSizeEnum = MapSizeEnum.Small;

        switch (index)
        {
            case 0: mapSizeEnum = MapSizeEnum.Small; break;
            case 1: mapSizeEnum = MapSizeEnum.Medium; break;
            case 2: mapSizeEnum = MapSizeEnum.Large; break;
        }

        InGameDataManager.Instance.SetMapSizeEnum(mapSizeEnum);

        SetPlayerSelectToggle();
    }

    void SetPlayerSelectToggle() 
    {
        int activeCount = 2;

        MapSizeEnum mapSizeEnum = InGameDataManager.Instance.mapSizeEnum;

        switch (mapSizeEnum)
        {
            case MapSizeEnum.Small: activeCount = 2; break;
            case MapSizeEnum.Medium: activeCount = 4; break;
            case MapSizeEnum.Large: activeCount = 5; break;
        }

        for (int i = 0; i < playersCountToggleList.Count; i++)
        {
            bool interactableOn = i <= activeCount;
            playersCountToggleList[i].interactable = interactableOn;
        }

        int playerMaxCnt = InGameDataManager.Instance.num_player;

        playerMaxCnt -= 2;

        if (playerMaxCnt > activeCount)
        {
            playerMaxCnt = activeCount;
            playersCountToggleList[playerMaxCnt].isOn = true;
        }
    }

    private void TurnPositionTogglChangeOn(int index)
    {
        turnPositionCountPanel.gameObject.SetActive(index == 1);

        if (index ==  0)
        {
            SetTurnPositionText(-1);
        }
        else
        {
            SetTurnPositionText(0);
        }
    }

    private void SetTurnPositionText(int turnPosition)
    {
        InGameDataManager.Instance.SetTurnPosition(turnPosition);
        turnPositionCountText.text = (turnPosition + 1).ToString();
    }
    

    void ChoosePositionToggleOn(bool isOn)
    {
        InGameDataManager.Instance.choosePositionOn = isOn;

        if (isOn == false)
        {
            InGameDataManager.Instance.SetTurnPosition(-1);
        }
        

        turnPositionSelectPanel.gameObject.SetActive(isOn);
    }

    void MapSelectionToggle(bool isOn)
    {
        InGameDataManager.Instance.mapSelectionOn = isOn;
    }

    void DiceCompensationToggle(bool isOn)
    {
        InGameDataManager.Instance.diceCompensationOn = isOn;
    }

    public void ResetToDefaultBtnClickOn() 
    {
        choosePositionToggle.isOn = false;
        mapSelectionToggle.isOn = false;
        diceCompensationToggle.isOn = false;
    }

    public void TurnCountAddBtnClickOn(bool addOn)
    {
        int turnPosition = InGameDataManager.Instance.turnPosition;

        if (addOn) 
        {
            turnPosition++;
        }
        else
        {
            turnPosition--;
        }

        SetTurnPosition(turnPosition);
    }

    private void SetTurnPosition(int turnCount) 
    {
        turnCount = math.clamp(turnCount, 0, InGameDataManager.Instance.num_player - 1);

        SetTurnPositionText(turnCount);
    }

    // Start is called before the first frame update
    void Start()
    {
        
        LoginCheck();

        //MapCreateRequestOn mapCreateRequestOn = 

        //InGameDataManager.Instance.testData = JsonUtility.FromJson<MapCreateRequestOn>(testJson);

        PopupManager.Instance.SetCanvasParant(transform);
    }

    void LoginCheck()
    {
        if (InGameDataManager.Instance.loginId == string.Empty) 
        {
            StepChangeOn(0);
        }
        else
        {
            StepChangeOn(1);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StepChangeOn(int step)
    {
        this.step = step;

        for (int i = 0; i < stepPanelList.Count; i++) 
        {
            bool activeOn = step == i;
            stepPanelList[i].gameObject.SetActive(activeOn);
        }

        if (this.step == 1)
        {
            ToggleInit();

            ChoosePositionToggleOn(InGameDataManager.Instance.choosePositionOn);
        }
    }

    private void ToggleInit()
    {
        aIToggleList[0].isOn = true;
        playersCountToggleList[0].isOn = true;
        mapSizeToggleList[0].isOn = true;
    }

    public void SinglePlayBtnClickOn()
    {
        StepChangeOn(1);
    }

    public void ColorChangeClickOn()
    {
        int index = InGameDataManager.Instance.playerData.colorIndex;

        index = index + 1;

        if (index >= 7) 
        {
            index = 0;
        }


        PlayerColorChangeOn(index);
    }

    public void PlayerColorChangeOn(int index)
    {
        playerColorImage.color = InGameDataManager.Instance.SetCurrentPlayerColor(index);
    }

    public void SettingPopupOn()
    {
        choosePositionToggle.isOn = InGameDataManager.Instance.choosePositionOn;
        mapSelectionToggle.isOn = InGameDataManager.Instance.mapSelectionOn;
        diceCompensationToggle.isOn = InGameDataManager.Instance.diceCompensationOn;
    }

    void SetCoin()
    {
        myCoinText.text = InGameDataManager.Instance.myCoin.Value.ToString();
    }

    public void PlayBtnClickOn()
    {
        SceneManager.LoadScene("Game");
    }

    public void LoginClickOn()
    {
        InGameDataManager.Instance.loginId = "loginClear";
        StepChangeOn(1);
    }

    public void BuyCoinPopupOpen()
    {
        PopupManager.Instance.BuyCoinPopupOn();
    }
}
