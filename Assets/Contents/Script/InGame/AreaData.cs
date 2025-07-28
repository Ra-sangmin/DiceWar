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
    private MapDice mapDice;

    public PlayerEnum player = PlayerEnum.Player_None;
    public bool choisOn { get; set; }

    public List<int> adj = new List<int>();
    private List<Hexagon> hexagonList = new List<Hexagon>();

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
            if (adj.player == player) 
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


	public void AddHexagon(Hexagon hexagon)
    {
        if (hexagonList == null)
        {
            hexagonList = new List<Hexagon>();
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

    public Hexagon SetCenterHexagon(MapDice mapDice , int diceCnt) 
    {
        this.mapDice = mapDice;

        SetDice(diceCnt);

        if (hexagonList == null)
        {
            hexagonList = new List<Hexagon> { };
        }

        if (hexagonList.Count == 0)
        {
            mapDice.gameObject.SetActive(false);
            return null;
        }

        int left =   (int)hexagonList.Select(data => data.pos.x).Min();
        int right =  (int)hexagonList.Select(data => data.pos.x).Max();
        int bottom = (int)hexagonList.Select(data => data.pos.y).Min();
        int top = (int)hexagonList.Select(data => data.pos.y).Max();

        int cx = (left + right) / 2;
        int cy = (top + bottom) / 2;

        Vector2 centerValue = new Vector2(cx, cy);

        var checkList = hexagonList.Where(data => data.AllCenterCheck()).ToList();

        if (checkList.Count == 0) 
        {
            checkList = hexagonList;
        }

        return SetCenterHexagon(checkList , centerValue);
    }

    public Hexagon SetCenterHexagon(List<Hexagon> checkList , Vector2 centerValue)
    {
        Hexagon resultHexagon = null;

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
