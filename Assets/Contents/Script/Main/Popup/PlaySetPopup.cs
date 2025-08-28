using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UniRx;
using Unity.VisualScripting;
using Unity.Mathematics;
using UnityEngine.SceneManagement;
using System;
using System.Linq;
using System.Reflection;
using static Unity.Burst.Intrinsics.Arm;
using static GameResultPopup;

public class PlaySetPopup : MonoBehaviour
{
    [SerializeField] Image playerImage;
    [SerializeField] List<Sprite> playerSpriteList = new List<Sprite>();
    [SerializeField] List<SMToggle> aIToggleList = new List<SMToggle>();
    [SerializeField] List<SMToggle> mapSizeToggleList = new List<SMToggle>();
    [SerializeField] List<SMToggle> playersCountToggleList = new List<SMToggle>();
	[SerializeField] List<RectTransform> colorPanelList = new List<RectTransform>();
    [SerializeField] PlayCoinBtn playCoinBtn;
	[SerializeField] RectTransform needCoinBG;
	[SerializeField] MyCoinPanel myCoinPanel;
	[SerializeField] Text victoryRewardText;
	private int playerColorIndex = 0;

	private void Awake()
    {
        SetEvent();
    }

    void SetEvent()
    {
        SetToggleEvent(aIToggleList, AIToggleChangeOn);
        SetToggleEvent(mapSizeToggleList, MapSizeToggleChangeOn);
        SetToggleEvent(playersCountToggleList, PlayersCountToggleChangeOn);

        DataManager.Instance.userData.myCoin
            .Subscribe(_ => SetNeedCoinCheck())
            .AddTo(gameObject);

        //playCoinBtn.playBtnClickOn = PlayBtnClickOn;
	}

    private void SetToggleEvent(List<SMToggle> toggleList, UnityAction<int> toggleEventOn, int defaultIndex = 0)
    {
        for (int i = 0; i < toggleList.Count; i++)
        {
            int index = i;

            toggleList[i].toggleValue.Subscribe(isOn =>
            {
                if (isOn)
                {
                    toggleEventOn(index);
                }
            }).AddTo(gameObject);
        }
    }

	public void InitOn(bool multiOn)
    {
        DataManager.Instance.isMultiOn = multiOn;

		colorPanelList[0].gameObject.SetActive(!multiOn);
		colorPanelList[1].gameObject.SetActive(multiOn);

		if (multiOn)
		{
            SetMultiToggle();
		}
        else
        {
			SetColorData();
		}
	}

    void SetMultiToggle()
    {
        SetToggleActive(aIToggleList, 2);
        SetToggleActive(mapSizeToggleList, 2);
        SetToggleActive(playersCountToggleList, 5);

        SetNeedCoinCheck();
	}

    public void SetToggleActive(List<SMToggle> toggleList, int activeIndex)
    {
		for (int i = 0; i < toggleList.Count; i++)
		{
			bool interactableOn = i >= activeIndex;
			toggleList[i].interactable.Value = interactableOn;
		}

		toggleList[activeIndex].toggleValue.Value = true;
    }

	// Start is called before the first frame update
	void Start()
    {
		//AdsManager.Instance.SetRewardedAdsButton(rewardedAdsButton);
	}

    // Update is called once per frame
    void Update()
    {
        
    }
    private void AIToggleChangeOn(int index)
    {
		SoundManager.Instance.PlaySe(SeEnum.Yes);

		AILevel aiLevel = (AILevel)(index);

        if (aIToggleList[index].toggleValue.Value == false)
        {
            aIToggleList[index].toggleValue.Value = true;
            return;
		}

        DataManager.Instance.SetAILevelEnum(aiLevel);

        if (DataManager.Instance.isMultiOn == false)
        {
			SetMapSizeToggle();
		}

		SetVictoryRewardText();
	}

	void SetMapSizeToggle()
	{
		int activeCount = 0;

		AILevel aiLevel = DataManager.Instance.aiLevel;

        switch (aiLevel)
        {
            case AILevel.Easy: activeCount = 0;
				break;
			case AILevel.Normal: activeCount = 1;
				break;
			case AILevel.Hard: activeCount = 2;
				break;
		}

		for (int i = 0; i < mapSizeToggleList.Count; i++)
		{
			bool interactableOn = i <= activeCount;
			mapSizeToggleList[i].interactable.Value = interactableOn;
		}

		if ((int)DataManager.Instance.mapSizeEnum > activeCount)
		{
			MapSizeToggleChangeOn(activeCount);
		}

		SetNeedCoinCheck();
	}

	private void MapSizeToggleChangeOn(int index)
    {
		SoundManager.Instance.PlaySe(SeEnum.Yes);

		MapSizeEnum mapSizeEnum = (MapSizeEnum)index;

		if (mapSizeToggleList[index].toggleValue.Value == false)
		{
			mapSizeToggleList[index].toggleValue.Value = true;
			return;
		}

		DataManager.Instance.SetMapSizeEnum(mapSizeEnum);

		if (DataManager.Instance.isMultiOn == false)
		{
			SetPlayerSelectToggle();
		}
    }

    void SetPlayerSelectToggle()
    {
        int activeCount = DataManager.Instance.ActivePlayerCount();

        for (int i = 0; i < playersCountToggleList.Count; i++)
        {
            bool interactableOn = i <= activeCount;
            playersCountToggleList[i].interactable.SetValueAndForceNotify(interactableOn);
        }

        int playerMaxCnt = DataManager.Instance.num_player;

        playerMaxCnt -= 2;

        if (playerMaxCnt > activeCount)
        {
            PlayersCountToggleChangeOn(activeCount);
        }
    }

    private void PlayersCountToggleChangeOn(int index)
    {
		SoundManager.Instance.PlaySe(SeEnum.Yes);

		int playerMaxCnt = index + 2;

		if (playersCountToggleList[index].toggleValue.Value == false)
		{
			playersCountToggleList[index].toggleValue.Value = true;
			return;
		}


		DataManager.Instance.SetPlayerMaxCnt(playerMaxCnt);
        SetTurnPosition(DataManager.Instance.turnPosition);

        if (playerColorIndex >= DataManager.Instance.num_player)
        {
            playerColorIndex = DataManager.Instance.num_player - 1;
			SetColorData();
		}

        SetVictoryRewardText();
	}

    void SetVictoryRewardText()
	{
        int rewardCoin = DataManager.Instance.GetRewardCoin();
        victoryRewardText.text = $"{rewardCoin} coins";
	}

    void SetNeedCoinCheck()
    {
        bool needCoinOn = playCoinBtn.SetNeedCoinCheck();
		needCoinBG.gameObject.SetActive(needCoinOn);
	}

	private void SetTurnPosition(int turnCount)
    {
        turnCount = math.clamp(turnCount, 0, DataManager.Instance.num_player - 1);

        SetTurnPositionText(turnCount);
    }

    private void SetTurnPositionText(int turnPosition)
    {
        DataManager.Instance.SetTurnPosition(turnPosition);
    }

    public void PlayerColorSetBtnOn(bool nextOn)
    {
        if (nextOn)
        {
            playerColorIndex++;

			//if (playerColorIndex >= playerSpriteList.Count)
			if (playerColorIndex >= DataManager.Instance.num_player)
			{
                playerColorIndex = 0;
            }
        }
        else
        {
            playerColorIndex--;

            if (playerColorIndex < 0)
            {
                playerColorIndex = DataManager.Instance.num_player - 1;
            }
        }

        SetColorData();
    }

    void SetColorData()
    {
        playerImage.sprite = playerSpriteList[playerColorIndex];
        playerImage.SetNativeSize();

        DataManager.Instance.SetCurrentPlayerColor(playerColorIndex);
    }

    public void PlayBtnClickOn()
    {
		DataManager.Instance.AddCoin(-DataManager.Instance.GetNeedCoin());
		SceneManager.LoadScene("Loading");
    }

    public void AdPlayBtnClickOn()
    {
		int addCoin = 10;

		DataManager.Instance.AddCoin(addCoin);

		SetNeedCoinCheck();
	}

    public void CloseBtnClickOn()
    {
        Destroy(gameObject);
    }
}
