using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class AreaData
{
    public int id;
    public int dice = 0;     // 주사위 수
    private MapDiceElement mapDice;

    public PlayerEnum player = PlayerEnum.Player_None;
    public bool choisOn { get; set; }
	public bool tradeOn { get; set; }

	public List<int> adj = new List<int>();
    private List<HexagonElement> hexagonList = new List<HexagonElement>();

    public List<int> cel = new List<int>();

    public string bundleKey { get; set;}

    public AreaData() {}

    public AreaData(int areaIndex)
    {
        this.id = areaIndex;
    }

    public AreaData(AreaData areaData)
    {
        this.id = areaData.id;

        this.dice = areaData.dice;
        this.mapDice = areaData.mapDice;

        this.player = areaData.player;
        this.adj = areaData.adj;
        this.hexagonList = areaData.hexagonList;
        this.cel = areaData.cel;
    }

    public void AddCel(int cel)
    {
        this.cel.Add(cel);
    }

    public void ClearCel()
    {
        cel.Clear();
    }

    public bool IsHaveCelData(int celIndex)
    {
        return cel.Contains(celIndex);
    }

    public void SetAdj(List<int> adj)
    {
        this.adj = adj;
    }

    public List<int> GetAdj()
    {
        return adj;
    }

	public List<AreaData> GetAdjList()
	{
        List<AreaData> resultList = new List<AreaData>();

        foreach (var targetAreaId in adj)
        {
			AreaData checkAreaData = DataManager.Instance.GetAreaData(targetAreaId);

            if (checkAreaData != null)
            {
                resultList.Add(checkAreaData);
			}
		}

		return resultList;
	}

	public bool AttackAreaOn(AreaData attackArea, AreaData checkArea)
	{
		bool attackOn = true;

		if (attackArea.dice == 1 || //주사위가 1 이라면
			checkArea.player == attackArea.player ||  //공격 Area와 방어 Area 가 같은 플레이라면
			DataManager.Instance.IsAllAlliance(new List<PlayerEnum>() { attackArea.player, checkArea.player })) // 동맹 플레이어 라면
		{
			attackOn = false;
		}

		return attackOn;
	}

	public AreaData GetAttackAreaList(int status)
	{
		AreaData fromAreaData = DataManager.Instance.GetAreaData(id);

        List<AreaData> checkAreaDataList = GetAdjList().Where(checkArea => AttackAreaOn(fromAreaData, checkArea)).ToList();

        AreaData checkAreaData = null;

        switch (status)
        {
            case 0: checkAreaData = checkAreaDataList.FirstOrDefault(checkArea => fromAreaData.dice > checkArea.dice);
				break;
			case 1: checkAreaData = checkAreaDataList.FirstOrDefault(checkArea => fromAreaData.dice >= checkArea.dice);
				break;
		}

		return checkAreaData;
	}

	public AreaData GetAttackAreaListToPlayer(int status , PlayerEnum playerEnum)
	{
		AreaData areaData = GetAttackAreaList(status);

        if (areaData != null && areaData.player == playerEnum) 
        {
            return areaData;
		}

        return null;
	}

	public List<AreaData> CheckAdj(PlayerEnum playerEnum)
	{
		List<AreaData> checkList = GetAdjList();

		return checkList.Where(data => data.player == playerEnum).ToList();
	}

    /// <summary>
    /// 주변 영토중에서 넘겨받은 영토와 일치하는 영토 취득
    /// </summary>
    /// <param name="checkList"></param>
    /// <returns></returns>
	public List<AreaData> CheckAdjAttakList(List<AreaData> checkList)
	{
		List<AreaData> adjList = GetAdjList();

		List<AreaData> resultList = new List<AreaData>();

        foreach (var adj in adjList)
        {
            if (checkList.Any(data => data.id == adj.id ) &&
				resultList.Any(data => data.id == adj.id) == false)
            {
                resultList.Add(adj);
			}
		}

		return resultList;
	}

	public List<AreaData> CheckAdjAttakList2(List<AreaData> checkList)
	{
		List<AreaData> adjList = GetAdjList();

		List<AreaData> resultList = new List<AreaData>();

		foreach (var adj in adjList)
		{
            if (adj.player == player ||
				DataManager.Instance.IsAllAlliance(new List<PlayerEnum>() { adj.player, player })) 
                continue;
            
            List<AreaData> targetAdjList = adj.GetAdjList();

            foreach (var check in checkList)
            {
                if (targetAdjList.Any(data => data.id == check.id))
                {
                    //Debug.LogWarning("target = " + adj.id);
                    resultList.Add(adj);
				}
            }   

			//targetAdjList.Any(data => data.id == checkList)



			//if (checkList.Any(data => data.id == adj.id))
			//{
			//	resultList.Add(adj);
			//}
		}

		return resultList;
	}


	public void AddHexagon(HexagonElement hexagon)
    {
        if (hexagonList == null)
        {
            hexagonList = new List<HexagonElement>();
        }

        this.hexagonList.Add(hexagon);
    }

    public bool PlayerChangeOn(PlayerEnum playerEnum)
    {
        this.player = playerEnum;
        this.choisOn = false;
        SetColor();

        return false;
    }

    public int GetDiece() 
    {
        return dice;
    }

    public HexagonElement SetCenterHexagon(MapDiceElement mapDice , int diceCnt) 
    {
        this.mapDice = mapDice;

        SetDice(diceCnt);

        if (hexagonList == null)
        {
            hexagonList = new List<HexagonElement> { };
        }

        if (hexagonList.Count == 0)
        {
            mapDice.SetActive(false);
            return null;
        }

        //주사위를 놓을 칸 고르기 (2026-10-13)
        //예전 : 격자 좌표 사각형의 가운데에 가장 가까운 '주변이 모두 같은 땅' 칸
        //       → 격자 좌표는 홀수 줄이 반 칸 밀려 있고, ㄱ / C 자 땅은 사각형 가운데가 땅 밖이라 가장자리에 붙는 일이 많았다.
        //지금 : 실제 화면 위치의 무게중심에 가깝고, 경계에서 안쪽으로 들어간 칸일수록 좋게 본다.
        HexagonElement bestHexagon = FindDiceHexagon();

        return SetCenterHexagon(new List<HexagonElement> { bestHexagon }, bestHexagon.pos);
    }

    /// <summary>
    /// 영토 중앙에 가장 가까운 칸.
    /// 점수 = (경계에서 떨어진 칸 수, 최대 2) x 칸 너비
    ///      + 위쪽 칸 보너스 (주사위 더미가 위로 그려지므로 윗줄 2칸 / 두 줄 위 칸이 같은 땅이면 +, 아니면 -)
    ///      - 무게중심까지 거리 → 가장 큰 칸.
    /// 같은 점수면 인덱스가 작은 칸 (모든 클라이언트에서 같은 결과).
    /// </summary>
    HexagonElement FindDiceHexagon()
    {
        Dictionary<int, HexagonElement> hexagonMap = new Dictionary<int, HexagonElement>();

        foreach (var hexagon in hexagonList)
        {
            hexagonMap[hexagon.index] = hexagon;
        }

        //1. 화면 위치 무게중심
        Vector2 centroid = Vector2.zero;

        foreach (var hexagon in hexagonList)
        {
            centroid += hexagon.GetAnchoredPosition();
        }

        centroid /= hexagonList.Count;

        //2. 경계에서의 깊이 (경계 칸 = 0, 그 안쪽 = 1, ...) : 경계 칸들에서 시작하는 BFS
        Dictionary<int, int> depthMap = new Dictionary<int, int>();
        Queue<HexagonElement> queue = new Queue<HexagonElement>();

        foreach (var hexagon in hexagonList)
        {
            if (hexagon.AllCenterCheck() == false)
            {
                depthMap[hexagon.index] = 0;
                queue.Enqueue(hexagon);
            }
        }

        while (queue.Count > 0)
        {
            HexagonElement current = queue.Dequeue();
            int nextDepth = depthMap[current.index] + 1;

            foreach (int neighborIndex in current.join.dir)
            {
                if (neighborIndex == -1 || depthMap.ContainsKey(neighborIndex))
                    continue;

                if (hexagonMap.TryGetValue(neighborIndex, out HexagonElement neighbor))
                {
                    depthMap[neighborIndex] = nextDepth;
                    queue.Enqueue(neighbor);
                }
            }
        }

        //3. 칸 너비 (이웃 칸 사이 거리) : 가장 가까운 두 칸의 거리
        float cellWidth = 0f;

        if (hexagonList.Count > 1)
        {
            Vector2 first = hexagonList[0].GetAnchoredPosition();
            cellWidth = hexagonList.Skip(1).Min(data => Vector2.Distance(first, data.GetAnchoredPosition()));
        }

        //4. 점수
        HexagonElement result = null;
        float bestScore = float.MinValue;

        //주사위 더미는 칸 중심에서 위로 1~2 줄 높이까지 그려진다 → 위쪽 칸들이 같은 땅이어야 경계를 넘어 보이지 않는다
        int mapWidth = (int)DataManager.Instance.GetMapSize().x;

        foreach (var hexagon in hexagonList.OrderBy(data => data.index))
        {
            int depth = depthMap.TryGetValue(hexagon.index, out int value) ? value : 2;

            //바로 윗줄 이웃 2칸 (격자 y 가 1 큰 칸 = 화면에서 위)
            int upCount = 0;
            int upTotal = 0;

            foreach (int neighborIndex in hexagon.join.dir)
            {
                if (neighborIndex == -1)
                    continue;

                if ((int)DataManager.Instance.GetPos(neighborIndex).y != (int)hexagon.pos.y + 1)
                    continue;

                upTotal++;

                if (hexagonMap.ContainsKey(neighborIndex))
                    upCount++;
            }

            bool upAllOn = upTotal == 2 && upCount == 2;

            //두 줄 위 (같은 x) 칸
            bool up2On = hexagonMap.ContainsKey(hexagon.index + (mapWidth * 2));

            float score = Mathf.Min(depth, 2) * cellWidth
                        + (upAllOn ? 1.5f : -1.5f * (2 - upCount)) * cellWidth
                        + (up2On ? 0.8f : 0f) * cellWidth
                        - Vector2.Distance(centroid, hexagon.GetAnchoredPosition());

            if (score > bestScore + 0.01f)
            {
                bestScore = score;
                result = hexagon;
            }
        }

        return result ?? hexagonList[0];
    }

    public HexagonElement SetCenterHexagon(List<HexagonElement> checkList , Vector2 centerValue)
    {
        HexagonElement resultHexagon = null;

        float checkValue = 9999;

        var newCheckList = checkList.Where( data => data.pos.x > 0 && 
                                            data.pos.x < DataManager.Instance.GetMapSizeValue().x && 
                                            data.pos.y > 0).ToList();

        if (newCheckList.Count == 0)
        {
            newCheckList = checkList;
        }


        foreach (var hexagon in newCheckList)
        {
            float distance = Vector2.Distance(centerValue, hexagon.pos);

            if (checkValue > distance)
            {
                checkValue = distance;

                resultHexagon = hexagon;
            }
        }

        if (mapDice != null && resultHexagon != null)
        {
            mapDice.SetPos(resultHexagon);

            mapDice.TextInit();


			if (DataManager.Instance.inGameEditOn)
            {
				mapDice.AreaTextSet(id);

				string value = string.Empty;

				for (int i = 0; i < adj.Count; i++)
				{
					if (i != 0)
					{
						value += ",";
					}
					value += adj[i];
				}
				mapDice.AreaAdjSet(value);
			}
		}

        return resultHexagon;
    }

    public bool IsDiceMax()
    {
        return dice == 6;
    }

    public void DiceAddOn()
    {
        SetDice(dice + 1);
    }

    public int SetDice(int dice)
    {
        this.dice = dice;

        if (mapDice != null)
        {
            mapDice.SetDice(dice);
        }

        return dice;
    }

    public void ChoisEventOn(bool choisOn)
    {
        this.choisOn = choisOn;
        SetColor();
    }

	public void TradeEventOn(bool tradeOn)
	{
		this.tradeOn = tradeOn;

		for (int i = 0; i < hexagonList.Count; i++)
		{
			hexagonList[i].TradeOn(tradeOn);
		}
	}

	void SetColor()
    {
        for (int i = 0; i < hexagonList.Count; i++)
        {
            hexagonList[i].SetPlayer(player, choisOn);
        }
    }

    public void SetBundleKey(List<AreaData> adat, PlayerEnum playerEnum, string bundleKey)
    {
        this.bundleKey = bundleKey;

        foreach (int areaIndex in adj)
        {
            AreaData areaData = adat.FirstOrDefault(data => data.id == areaIndex);

            if (areaData != null &&  areaData.player == playerEnum && areaData.bundleKey == string.Empty)
            {
                areaData.SetBundleKey(adat, playerEnum, bundleKey);
            }
        }
    }

    public bool GetIsOnConnectedArea(int checkAreaIndex)
    {
        return adj.Contains(checkAreaIndex);
    }
}
