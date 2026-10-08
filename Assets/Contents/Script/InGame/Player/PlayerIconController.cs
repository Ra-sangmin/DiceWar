using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class PlayerIconController : MonoBehaviour
{
    private VisualElement iconPanel;
    private List<PlayerIconElement> playerIconList = new List<PlayerIconElement>();

    public UnityAction gameWinOn = () => { };
    public UnityAction gameLoseOn = () => { };

    /// <summary> 땅/동맹 변화로 세력(연결 땅 수)이 다시 계산됐다 (카운트다운 시작 판정용, 2026-10-08) </summary>
    public UnityAction bundleKeyUpdatedOn = () => { };

    /// <summary> 멀티에서 동맹이 있는 채로 내 땅이 0 이 됐다 (퇴장 / 관전 선택 팝업, 2026-09-26) </summary>
    public UnityAction myLandZeroWithAllianceOn = () => { };
    private bool myLandZeroCheckedOn = false;

    public UnityAction<PlayerIconElement> playerClickOn = data => { };

    private void Awake()
    {
    }

    /// <summary> UIDocument 의 player-icon-panel 을 받아 아이콘을 담을 컨테이너로 쓴다 </summary>
    public void SetIconPanel(VisualElement iconPanel)
    {
        this.iconPanel = iconPanel;
        this.iconPanel.Clear(VisualElementClearOptions.RecursiveReleaseResources); //6.5+ : 다시 만들 요소라 자원까지 바로 반환 (2026-09-29)
        playerIconList.Clear();
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
        foreach (PlayerIconElement playerIcon in playerIconList) 
        {
            playerIcon.SetActive(false);
        }

        int numPlayer = DataManager.Instance.num_player;
		//int numPlayer = 6;

		if (playerIconList.Count < numPlayer)
        {
            for (int i = playerIconList.Count; i < numPlayer; i++)
            {
                PlayerIconElement playerIcon = new PlayerIconElement();
                playerIcon.playerClickOn = playerClickOn;
                iconPanel.Add(playerIcon.Root);
                playerIconList.Add(playerIcon);
            }
        }

        for (int i = 0; i < playerIconList.Count; i++)
        {
            playerIconList[i].SetActive(true);
            playerIconList[i].SetPlayer((PlayerEnum)i);
            SetBundleKey(playerIconList[i]);
        }
    }

    public void SetMyEffect()
    {
		for (int i = 0; i < playerIconList.Count; i++)
		{
			playerIconList[i].SetMyTurnEffectOff();
		}

		PlayerEnum myPlayerEnum = DataManager.Instance.playerData.pe;

        PlayerIconElement myPlayerIcon = playerIconList.FirstOrDefault(data => data.playerEnum == myPlayerEnum);

        if (myPlayerIcon != null)
        {
            myPlayerIcon.SetMyTurnEffect(true);
        }
    }

    public void SetIconTurnEffect() 
    {
        for (int i = 0; i < playerIconList.Count; i++)
        {
            playerIconList[i].SetMyTurnEffect();
        }
    }

    public void SetBundleKeyuAll()
    {
        var checkList = GetActiveList();

        for (int i = 0; i < checkList.Count; i++)
        {
            SetBundleKey(checkList[i]);
        }

        bundleKeyUpdatedOn();

		PlayerEnum myPlayerEnum = DataManager.Instance.playerData.pe;

        //남은 인원 체크
        var resultList = GetActiveList();

        //내 땅이 모두 없어졌다면
        if (resultList.Any(data => data.playerEnum == myPlayerEnum) == false)
        {
            //Debug.LogWarning("내땅 없음");
            //기획 시트 : 멀티에서 동맹이 있으면 바로 지는 대신, 패널티 없이 퇴장할지 동맹을 유지한 채 관전할지 고른다 (2026-09-26)
            AllianceData myAllianceData = DataManager.Instance.isMultiOn ? DataManager.Instance.GetMyAllianceData() : null;

            if (myAllianceData == null)
            {
                gameLoseOn();
                return;
            }

            if (myLandZeroCheckedOn == false)
            {
                myLandZeroCheckedOn = true;
                myLandZeroWithAllianceOn();
            }

            //관전 중 : 동맹원이 모두 사라지면 패배, 남은 사람이 전부 내 동맹이면 승리 (승리 시 내 몫 유지)
            List<PlayerEnum> allianceMemberList = DataManager.Instance.GetAllAlliance(myPlayerEnum).allianceDataList
                .Select(data => data.playerEnum).ToList();

            if (resultList.Any(data => allianceMemberList.Contains(data.playerEnum)) == false)
            {
                gameLoseOn();
            }
            else if (resultList.All(data => allianceMemberList.Contains(data.playerEnum)))
            {
                gameWinOn();
            }

            return;
        }

        //남은 인원수가 1명이라면
        if (resultList.Count == 1)
        {
            if (DataManager.Instance.playerData.pe == resultList[0].playerEnum)
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
            int allianceCount = DataManager.Instance.GetAllianceCount();

            //동맹이 1개 이상이라면
            if (allianceCount > 1)
            {
                return;
            }

			var playerEnumList = resultList.Select(data => data.playerEnum).ToList();
            var allianceMemberList = DataManager.Instance.GetAllAlliance(myPlayerEnum).allianceDataList
                .Select(data => data.playerEnum).ToList();

			//남은 인원이 모두 같은 동맹 인원이라면
			//예전에는 '남은 인원 수 == 동맹 인원 수' 로만 봐서, 땅을 모두 잃은(관전 중인) 동맹원이 있으면
			//동맹 밖의 플레이어가 남아 있어도 숫자가 같아져 승리 처리됐다 → 남은 사람이 전부 내 동맹원인지 확인 (2026-10-08)
			if (playerEnumList.All(playerEnum => allianceMemberList.Contains(playerEnum)))
            {
                gameWinOn();
                //gameEndOn(InGameDataManager.Instance.playerData.playerEnum);
            }
        }
    }

    public List<PlayerIconElement> GetActiveList()
    {
        return playerIconList.Where(data => data.IsActive()).ToList();
    }

    public List<PlayerEnum> GetActiveDiceLowerList()
    {
        //연결된 영토가 작은 순으로 취득
		List<PlayerEnum> allActiveList = GetActiveList().OrderBy(data => data.connectedCount).ThenBy(data => data.allAreaCount).Select(data => data.playerEnum).ToList();

        //하위 순위권 플레이어 숫자 취득
        int listCount = (int)Math.Round(allActiveList.Count / 2.0f);

        List<PlayerEnum> resultList = new List<PlayerEnum>();

        for (int i = 0; i < listCount; i++)
        {
            resultList.Add(allActiveList[i]);
		}

		return resultList;
	}

	public List<PlayerIconElement> GetActiveDiceHigherList(List<PlayerToggleIcon>  playerList)
	{
        List<PlayerIconElement> newCheckList = new List<PlayerIconElement>();

        foreach (var item in GetActiveList())
        {
            if (playerList.Any(data => data.playerEnum == item.playerEnum))
            {
                newCheckList.Add(item);
			}
		}

		//연결된 영토가 큰 순으로 취득
		List<PlayerIconElement> resultList = newCheckList.OrderByDescending(data => data.connectedCount).ToList();

		return resultList;
	}

	public List<PlayerIconElement> GetActiveDiceHigherList(List<AllianceData> playerList)
	{
		List<PlayerIconElement> newCheckList = new List<PlayerIconElement>();

		foreach (var item in GetActiveList())
		{
			if (playerList.Any(data => data.playerEnum == item.playerEnum))
			{
				newCheckList.Add(item);
			}
		}

		//연결된 영토가 큰 순으로 취득
		List<PlayerIconElement> resultList = newCheckList.OrderByDescending(data => data.connectedCount).ToList();

		return resultList;
	}

	

	//각 플레이어 별로 연결된 영토 확인
	public void SetBundleKey(PlayerIconElement checkPlayerIcon)
    {
        PlayerEnum checkEnum = checkPlayerIcon.playerEnum;

        //최대로 연결된 영토 숫자
        int maxCount = DataManager.Instance.SetBundleKey(checkEnum).Count;

        if (maxCount <= 0)
        {
			bool isAI = DataManager.Instance.GetPlayerData(checkPlayerIcon.playerEnum).isAI;

			// AI는 동맹 여부와 상관없이 땅이 없어지면 바로 퇴장
			if (DataManager.Instance.isMultiOn && isAI && DataManager.Instance.IsAlliance(checkPlayerIcon.playerEnum))
			{
				AllianceAllData allianceAllData = DataManager.Instance.BetrayOn(checkPlayerIcon.playerEnum);

				SetBetrayPlayerIcon(checkPlayerIcon.playerEnum);
				ResetAlliancePlayerIcon(allianceAllData);
				DataManager.Instance.AllianceClearCheckOn(this);

				checkPlayerIcon.SetActive(false);
			}
			// 넘겨 받은 플레이어가 동맹 상태가 아니라면
			else if (DataManager.Instance.IsAlliance(checkPlayerIcon.playerEnum) == false)
            {
				checkPlayerIcon.SetActive(false);
            }
            else
            {
                //나머지 동맹원들이 모두 영토가 0인지 체크
                var allianceAllData = DataManager.Instance.GetAllAlliance(checkEnum);

				bool allLoseOn = allianceAllData.allianceDataList.All(data => DataManager.Instance.SetBundleKey(data.playerEnum).Count == 0);

                if (allLoseOn) 
                {
                    DataManager.Instance.AllianceRemoveOn(allianceAllData);

                    foreach (var allianceData in allianceAllData.allianceDataList)
                    {
                        var playerIcon = GetPlayerIcon(allianceData.playerEnum);

                        if (playerIcon != null)
						{
							playerIcon.SetActive(false);
						}
					}
				}
			}
		}

		checkPlayerIcon.SetConnectedCount(maxCount);
	}

    public List<PlayerIconElement> GetPlayerIconList()
    {
        return playerIconList;
    }

    public PlayerIconElement GetPlayerIcon(PlayerEnum playerEnum)
    {
        return GetActiveList().FirstOrDefault(data => data.playerEnum == playerEnum);
    }

    public List<PlayerIconElement> CheckAIAllianceOn(PlayerEnum fromAIEnum)
    {
		int maxAreaCount = DataManager.Instance.areaDataList.Count;

		maxAreaCount = Mathf.RoundToInt(maxAreaCount / 3.0f);

		List<PlayerIconElement> resultList = new List<PlayerIconElement>();

		//세력이 전체 땅의 1/3 이상인 플레이어 or 동맹이 있는가? (AI 알고리즘 수정-260927.pptx, 2026-09-28)
		//예전엔 플레이어는 1/3 '초과', 동맹은 모든 동맹의 합으로 봤다 → 플레이어/동맹 각각 1/3 '이상'
		if (IsOverThirdPowerOn() == false)
        {
            return resultList;
		}

        //Debug.LogWarning($"동맹 세력 합 = {allianceSumCount} , {maxAreaCount}");

        //foreach (var overPlayer in overPlayerList)
        //{
        //    Debug.LogWarning($"큰 세력 존재 {overPlayer.playerEnum} , {maxAreaCount} , {overPlayer.connectedCount}");
        //}

		int connectedCount = GetPlayerIcon(fromAIEnum).connectedCount;

        //내 세력이 최대 영토 보다 적다면
        if (connectedCount < maxAreaCount)
        {
            List<PlayerIconElement> checkList = GetActiveList()
                                            .Where(data => data.playerEnum != fromAIEnum && //체크 상대가 당사자가 아니고
                                                           !DataManager.Instance.IsAlliance(data.playerEnum) && // 동맹이 없다면
														   GetPlayerIcon(data.playerEnum).connectedCount < maxAreaCount) // 세력수가 최대 영토수보다 작다면
											.ToList();


            foreach (var playerIcon in checkList)
            {
				// AI 라면 (임시로 AI만 적용)
				//if (DataManager.Instance.GetPlayerData(playerIcon.playerEnum).isAI)
				{
					resultList.Add(playerIcon);
				}
			}
		}

        return resultList;
	}


	/// <summary> 세력(연결 땅 수)이 전체 땅의 1/3 이상인 플레이어 또는 동맹(동맹원 합)이 있는가 </summary>
	bool IsOverThirdPowerOn()
	{
		int allAreaCount = DataManager.Instance.areaDataList.Count;

		List<PlayerEnum> countedList = new List<PlayerEnum>();

		foreach (var playerIcon in GetActiveList())
		{
			if (countedList.Contains(playerIcon.playerEnum))
				continue;

			int power = playerIcon.connectedCount;

			countedList.Add(playerIcon.playerEnum);

			if (DataManager.Instance.IsAlliance(playerIcon.playerEnum))
			{
				var memberList = DataManager.Instance.GetAllAlliance(playerIcon.playerEnum).allianceDataList.Select(data => data.playerEnum).ToList();

				power = memberList.Sum(playerEnum => GetPlayerIcon(playerEnum)?.connectedCount ?? 0);

				countedList.AddRange(memberList);
			}

			if (power * 3 >= allAreaCount)
				return true;
		}

		return false;
	}

	public void SetAlliancePlayerIcon(AllianceResultRequest allianceResultRequest)
    {
        AllianceAllData allianceAllData = new AllianceAllData() { orderData = allianceResultRequest.orderData , allianceDataList = allianceResultRequest.allianceDataList };

        ResetAlliancePlayerIcon(allianceAllData);
    }
	public void ResetAlliancePlayerIcon(AllianceAllData allianceAllData)
	{
		PlayerEnum orderPlayer = allianceAllData.orderData.playerEnum;

		foreach (var allianceData in allianceAllData.allianceDataList)
		{
			PlayerIconElement playerIcon = GetActiveList().FirstOrDefault(data => data.playerEnum == allianceData.playerEnum);

			if (playerIcon != null)
			{
				playerIcon.SetAllianceColor(orderPlayer);
			}
		}
	}

	public void SetBetrayPlayerIcon(PlayerEnum targetPlayerEnum)
    {
        PlayerIconElement playerIcon = GetActiveList().FirstOrDefault(data => data.playerEnum == targetPlayerEnum);

        if (playerIcon != null)
        {
            playerIcon.SetAllianceColor(PlayerEnum.Player_None);
        }
    }

    public PlayerEnum BestBigPlayer()
    {
        PlayerEnum playerEnum = PlayerEnum.Player_None;
        int maxCount = 0;

        foreach (var playerIcon in playerIconList)
        {
            if (maxCount < playerIcon.connectedCount )
            {
                playerEnum = playerIcon.playerEnum;
				maxCount = playerIcon.connectedCount;
			}
        }

		return playerEnum;
    }
}
