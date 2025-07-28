using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class DiceCountManager : MonoSingleton<DiceCountManager>
{
	public DiceCountListData diceCountListData;

	private string diceCountListDataKey = "diceCountListData";

	public override void Init()
	{
		base.Init();

		string jsonValue = PlayerPrefs.GetString(diceCountListDataKey, string.Empty);

		if (string.IsNullOrEmpty(jsonValue))
		{
			DiceCountListDataInit();
		}
		else
		{
			diceCountListData = JsonUtility.FromJson<DiceCountListData>(jsonValue);
		}
	}

	void DiceCountListDataInit()
	{
		diceCountListData = new DiceCountListData();

		diceCountListData.diceCountDataList = new List<DiceCountData>()
		{
			new DiceCountData(false, AILevel.Easy,3,4),
			new DiceCountData(false, AILevel.Normal,2,4),
			new DiceCountData(false, AILevel.Hard,1,3),
			new DiceCountData(true,  AILevel.Easy,1,3),
			new DiceCountData(true,  AILevel.Normal,2,4),
			new DiceCountData(true,  AILevel.Hard,3,5),
		};
	}

	public DiceCountData GetData(bool isAI, AILevel aiLevel)
	{
		return diceCountListData.diceCountDataList.FirstOrDefault(data => data.isAI == isAI && data.aiLevel == aiLevel);
	}

	public void SetData(DiceCountData diceCountData)
	{
		//Debug.LogWarning(diceCountData.isAI + " , " + diceCountData.aiLevel);
		foreach (var data in diceCountListData.diceCountDataList)
		{
			//Debug.LogWarning(data.isAI + " , " + data.aiLevel);

			if (data.isAI == diceCountData.isAI && data.aiLevel == diceCountData.aiLevel)
			{
				data.minCount = diceCountData.minCount;
				data.maxCount = diceCountData.maxCount;

			//	Debug.LogWarning(data.minCount);
				//Debug.LogWarning(data.maxCount);
			}
		}

		SaveData();

		//AssetDatabase.SaveAssets();
	}

	void SaveData()
	{
		string saveData = JsonUtility.ToJson(diceCountListData);
		PlayerPrefs.SetString(diceCountListDataKey,saveData);
		PlayerPrefs.Save();
	}
}

[Serializable]
public class DiceCountListData
{
	public List<DiceCountData> diceCountDataList = new List<DiceCountData>();
}