using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class PlayerIconController : MonoBehaviour
{
    [SerializeField] PlayerIcon playerIconPrefab;
    private List<PlayerIcon> playerIconList = new List<PlayerIcon>();

    public UnityAction gameWinOn = () => { };
    public UnityAction gameLoseOn = () => { };

    public UnityAction<PlayerIcon> playerClickOn = data => { };

    private void Awake()
    {
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetPlayerIcon()
    {
        foreach (PlayerIcon playerIcon in playerIconList) 
        {
            playerIcon.gameObject.SetActive(false);
        }

        int numPlayer = InGameDataManager.Instance.num_player;

        if (playerIconList.Count < numPlayer)
        {
            for (int i = playerIconList.Count; i < numPlayer; i++)
            {
                PlayerIcon playerIcon = Instantiate(playerIconPrefab, transform);
                playerIcon.playerClickOn = playerClickOn;
                playerIconList.Add(playerIcon);
            }
        }

        for (int i = 0; i < playerIconList.Count; i++)
        {
            playerIconList[i].gameObject.SetActive(true);
            playerIconList[i].SetPlayer((PlayerEnum)i);
            SetBundleKey(playerIconList[i]);
        }
    }

    public void SetBundleKeyuAll()
    {
        var checkList = GetActiveList();

        for (int i = 0; i < checkList.Count; i++)
        {
            SetBundleKey(checkList[i]);
        }

        PlayerEnum myPlayerEnum = InGameDataManager.Instance.playerData.playerEnum;

        //남은 인원 체크
        var resultList = GetActiveList();

        //내 땅이 모두 없어졌다면
        if (resultList.Any(data => data.playerEnum == myPlayerEnum) == false)
        {
            Debug.LogWarning("내땅 없음");
            gameLoseOn();
            return;
        }

        //남은 인원수가 1명이라면
        if (resultList.Count == 1)
        {
            if (InGameDataManager.Instance.playerData.playerEnum == resultList[0].playerEnum)
            {
                gameWinOn();
            }
            else 
            {
                gameLoseOn();
            }

            //gameEndOn(resultList[0].playerEnum);
        }
        else
        {
            var playerEnumList = resultList.Select(data => data.playerEnum).ToList();
            if (InGameDataManager.Instance.IsAllAlliance(playerEnumList))
            {
                gameWinOn();
                //gameEndOn(InGameDataManager.Instance.playerData.playerEnum);
            }
        }
    }

    public List<PlayerIcon> GetActiveList()
    {
        return playerIconList.Where(data => data.gameObject.activeSelf).ToList();
    }

    //각 플레이어 별로 연결된 영토 확인
    public void SetBundleKey(PlayerIcon checkPlayerIcon)
    {
        PlayerEnum checkEnum = checkPlayerIcon.playerEnum;

        //최대로 연결된 영토 숫자
        int maxCount = InGameDataManager.Instance.SetBundleKey(checkEnum);

        if (maxCount <= 0)
        {
            checkPlayerIcon.gameObject.SetActive(false);
        }
        else 
        {
            checkPlayerIcon.SetConnectedCount(maxCount);
        }
    }

    public List<PlayerIcon> GetPlayerIconList()
    {
        return playerIconList;
    }

    public PlayerIcon GetPlayerIcon(PlayerEnum playerEnum)
    {
        return playerIconList.FirstOrDefault(data => data.playerEnum == playerEnum);
    }
}
