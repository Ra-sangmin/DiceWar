using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;

public class PlayerDataPanel : MonoBehaviour
{
    [SerializeField] PlayerAllianceIcon playerAllianceIconPrefab;
    [SerializeField] RectTransform playerAllianceIconParant;
    [SerializeField] SkillCard skillCard;

    private List<PlayerAllianceIcon> playerAllianceIconList = new List<PlayerAllianceIcon>();

    private PlayerIcon selectPlayerIcon;

    private float disableDelay;

    private void Awake()
    {
        //CreatePlayerToggleIcon();
    }

    public void SetData(PlayerIcon playerIcon)
    {
        selectPlayerIcon = playerIcon;

        SetPos();

        SetPlayerData();

        skillCard.SetCountIcon(selectPlayerIcon.playerEnum);

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

        foreach (var playerToggleIcon in playerAllianceIconList)
        {
            playerToggleIcon.gameObject.SetActive(false);
        }

        if (playerAllianceIconList.Count < allianceDataList.Count)
        {
            for (int i = playerAllianceIconList.Count; i < allianceDataList.Count; i++)
            {
                PlayerAllianceIcon playerToggleIcon = Instantiate(playerAllianceIconPrefab, playerAllianceIconParant);
                playerAllianceIconList.Add(playerToggleIcon);
            }
        }

        for (int i = 0; i < allianceDataList.Count; i++)
        {
            playerAllianceIconList[i].gameObject.SetActive(true);
            playerAllianceIconList[i].SetPlayerData(allianceDataList[i]);
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
