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

public class PlaySetPopup : MonoBehaviour
{
    [SerializeField] Image playerImage;
    [SerializeField] List<Sprite> playerSpriteList = new List<Sprite>();

    private int playerColorIndex = 0;

    [SerializeField] List<SMToggle> aIToggleList = new List<SMToggle>();
    [SerializeField] List<SMToggle> mapSizeToggleList = new List<SMToggle>();
    [SerializeField] List<SMToggle> playersCountToggleList = new List<SMToggle>();
    [SerializeField] List<SMToggle> offlineUsersToggleList = new List<SMToggle>();
    [SerializeField] LoadingPopup loadingPopup;

    private bool loadingOn;

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

        for (int i = 0; i < offlineUsersToggleList.Count; i++)
        {
            int index = i;

            offlineUsersToggleList[i].toggleValue.Subscribe(isOn =>
            {
                if (isOn)
                {
                    OfflineUsersToggleChangeOn(index);
                }
                else
                {
                    if (offlineUsersToggleList.All(data => data.toggleValue.Value == false))
                    {
                        OfflineUsersToggleChangeOn(-1);
                    }
                }
            }).AddTo(gameObject);
        }

        //SetToggleEvent(offlineUsersToggleList, OfflineUsersToggleChangeOn);


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

        toggleEventOn(defaultIndex);
    }

    // Start is called before the first frame update
    void Start()
    {
        if (multiOn)
        {
            ServerManager.Instance.Init();
        }

    }

    // Update is called once per frame
    void Update()
    {
        
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
            playersCountToggleList[i].interactable.SetValueAndForceNotify(interactableOn);

            if (multiOn)
            {
                bool interactableOn2 = i <= activeCount - 1;
                offlineUsersToggleList[i].interactable.SetValueAndForceNotify(interactableOn2);
            }
        }

        int playerMaxCnt = InGameDataManager.Instance.num_player;

        playerMaxCnt -= 2;

        if (playerMaxCnt > activeCount)
        {
            playerMaxCnt = activeCount;
            playersCountToggleList[playerMaxCnt].toggleValue.SetValueAndForceNotify(true);
        }

        if (multiOn)
        {
            int offLineCnt = InGameDataManager.Instance.off_line_num_player - 3;

            if (offLineCnt >= activeCount - 1)
            {
                offlineUsersToggleList[activeCount - 1].toggleValue.SetValueAndForceNotify(true);
            }
        }
    }

    private void PlayersCountToggleChangeOn(int index)
    {
        int playerMaxCnt = index + 2;
        InGameDataManager.Instance.SetPlayerMaxCnt(playerMaxCnt);
        SetTurnPosition(InGameDataManager.Instance.turnPosition);
    }

    private void OfflineUsersToggleChangeOn(int index)
    {
        int playerMaxCnt = index + 1;
        InGameDataManager.Instance.SetOffLinePlayerCnt(playerMaxCnt);
        //SetTurnPosition(InGameDataManager.Instance.turnPosition);
    }

    private void SetTurnPosition(int turnCount)
    {
        turnCount = math.clamp(turnCount, 0, InGameDataManager.Instance.num_player - 1);

        SetTurnPositionText(turnCount);
    }

    private void SetTurnPositionText(int turnPosition)
    {
        InGameDataManager.Instance.SetTurnPosition(turnPosition);
        //turnPositionCountText.text = (turnPosition + 1).ToString();
    }

    public void PlayerColorSetBtnOn(bool nextOn)
    {
        if (nextOn)
        {
            playerColorIndex++;

            if (playerColorIndex >= playerSpriteList.Count)
            {
                playerColorIndex = 0;
            }
        }
        else
        {
            playerColorIndex--;

            if (playerColorIndex < 0)
            {
                playerColorIndex = playerSpriteList.Count -1;
            }
        }

        playerImage.sprite = playerSpriteList[playerColorIndex];
        playerImage.SetNativeSize();

        InGameDataManager.Instance.SetCurrentPlayerColor(playerColorIndex);
    }

    public void PlayBtnClickOn()
    {
        loadingOn = true;

        InGameDataManager.Instance.isMultiOn = multiOn;

        loadingPopup.gameObject.SetActive(true);
        loadingPopup.SetData(multiOn);

        //SceneManager.LoadScene("Game");
    }
}
