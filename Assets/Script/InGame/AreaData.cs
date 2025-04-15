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

    private List<int> adj = new List<int>();
    private List<Hexagon> hexagonList = new List<Hexagon>();

    public List<int> cel = new List<int>();

    public string bundleKey { get; set;}

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

    public void SetAdj(Join[] join)
    {
        adj = new List<int>();

        foreach (int cel in cel) 
        {
            for (int z = 0; z < 6; z++)
            {
                int pos = join[cel].dir[z];

                int currentAreaData = DataManager.Instance.GetCelData(pos);

                if (currentAreaData != -1 && currentAreaData != id && adj.Contains(currentAreaData) == false)
                {
                    adj.Add(currentAreaData);
                }
            }
        }
    }

    public List<int> GetAdj()
    {
        return adj;
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
        }

        return resultHexagon;
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
