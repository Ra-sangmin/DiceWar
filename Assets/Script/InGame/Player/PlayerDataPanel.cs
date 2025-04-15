using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;

public class PlayerDataPanel : MonoBehaviour
{
    [SerializeField] PlayerToggleIcon playerToggleIconPrefab;

    private List<PlayerToggleIcon> playerToggleIconList = new List<PlayerToggleIcon>();

    private PlayerIcon selectPlayerIcon;

    private float disableDelay;

    private void Awake()
    {
        //CreatePlayerToggleIcon();
    }

    //void CreatePlayerToggleIcon() 
    //{
    //    for (int i = 0; i < 7; i++) 
    //    {
    //        PlayerToggleIcon playerToggleIcon = Instantiate(playerToggleIconPrefab, playerToggleIconPrefab.transform.parent);
    //        playerToggleIconList.Add(playerToggleIcon);
    //    }
    //}

    public void SetData(PlayerIcon playerIcon)
    {
        selectPlayerIcon = playerIcon;

        SetPos();

        SetPlayerData();

        disableDelay = 1;
    }

    void SetPos()
    {
        Transform originParant = transform.parent;

        transform.SetParent(selectPlayerIcon.transform);

        RectTransform rectTransform = transform as RectTransform;

        Vector3 pos = rectTransform.anchoredPosition3D;
        pos.y = 0;
        rectTransform.anchoredPosition3D = pos;

        transform.SetParent(originParant);
    }

    void SetPlayerData()
    {
        List<AllianceData> allianceDataList = DataManager.Instance.GetAllAlliance(selectPlayerIcon.playerEnum);

        foreach (var playerToggleIcon in playerToggleIconList)
        {
            playerToggleIcon.gameObject.SetActive(false);
        }

        if (playerToggleIconList.Count < allianceDataList.Count)
        {
            for (int i = playerToggleIconList.Count; i < allianceDataList.Count; i++)
            {
                PlayerToggleIcon playerToggleIcon = Instantiate(playerToggleIconPrefab, playerToggleIconPrefab.transform.parent);
                playerToggleIconList.Add(playerToggleIcon);
            }
        }

        for (int i = 0; i < allianceDataList.Count; i++)
        {
            playerToggleIconList[i].gameObject.SetActive(true);
            playerToggleIconList[i].SetPlayerData(allianceDataList[i].playerEnum);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.LogWarning("d");
        }

        DisableDelayCheck();
    }

    void DisableDelayCheck()
    {
        disableDelay -= Time.deltaTime;

        if (disableDelay < 0)
        {
            gameObject.SetActive(false);
        }
    }
}
