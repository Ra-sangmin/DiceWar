using System;
using System.Collections.Generic;
using System.Linq;
//using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "DiceCount", menuName = "Scriptable Objects/DiceCount")]
public class DiceCount : ScriptableObject
{
    public List<DiceCountData> diceCountDataList = new List<DiceCountData>();

    public DiceCountData GetData(bool isAI , AILevel aiLevel)
    {
        return diceCountDataList.FirstOrDefault(data => data.isAI == isAI && data.aiLevel == aiLevel);
	}

    public void SetData(DiceCountData diceCountData)
    {
        Debug.LogWarning(diceCountData.isAI + " , " + diceCountData.aiLevel);
        foreach (var data in diceCountDataList) 
        {
			//Debug.LogWarning(data.isAI + " , " + data.aiLevel);

			if (data.isAI == diceCountData.isAI && data.aiLevel == diceCountData.aiLevel )
            {
                data.minCount = diceCountData.minCount;
				data.maxCount = diceCountData.maxCount;

                Debug.LogWarning(data.minCount);
				Debug.LogWarning(data.maxCount);
			}
		}

		//AssetDatabase.SaveAssets();
	}
}

[Serializable]
public class DiceCountData
{
    public bool isAI = false;
    public AILevel aiLevel = AILevel.Easy;
    public int minCount = 0;
	public int maxCount = 0;

    public DiceCountData() { }
	public DiceCountData(bool isAI, AILevel aiLevel, int minCount,int  maxCount ) 
    {
        this.isAI = isAI;
		this.aiLevel = aiLevel;
		this.minCount = minCount;
		this.maxCount = maxCount;
	}
}