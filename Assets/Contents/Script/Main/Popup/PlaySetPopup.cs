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

public class PlaySetPopup : MonoBehaviour
{
    [SerializeField] Image playerImage;
    [SerializeField] List<Sprite> playerSpriteList = new List<Sprite>();

    private int playerColorIndex = 0;

    [SerializeField] List<SMToggle> aIToggleList = new List<SMToggle>();
    [SerializeField] List<SMToggle> mapSizeToggleList = new List<SMToggle>();
    [SerializeField] List<SMToggle> playersCountToggleList = new List<SMToggle>();
	[SerializeField] List<RectTransform> colorPanelList = new List<RectTransform>();

    public bool multiOn = false;

    private void Awake()
    {
        SetEvent();
    }

    void SetEvent()
    {
        SetToggleEvent(aIToggleList, AIToggleChangeOn);
        SetToggleEvent(mapSizeToggleList, MapSizeToggleChangeOn);
        SetToggleEvent(playersCountToggleList, PlayersCountToggleChangeOn);
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

	public void InitOn()
    {
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
        //SetColorData();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void AIToggleChangeOn(int index)
    {
        AILevel aiLevel = (AILevel)(index);

        if (aIToggleList[index].toggleValue.Value == false)
        {
            aIToggleList[index].toggleValue.Value = true;
            return;
		}

        DataManager.Instance.SetAILevelEnum(aiLevel);

        if (multiOn == false)
        {
			SetMapSizeToggle();
		}
        
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
	}

	private void MapSizeToggleChangeOn(int index)
    {
        MapSizeEnum mapSizeEnum = (MapSizeEnum)index;

		if (mapSizeToggleList[index].toggleValue.Value == false)
		{
			mapSizeToggleList[index].toggleValue.Value = true;
			return;
		}

		DataManager.Instance.SetMapSizeEnum(mapSizeEnum);

		if (multiOn == false)
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
        DataManager.Instance.isMultiOn = multiOn;

        SceneManager.LoadScene("Loading");
    }
}
