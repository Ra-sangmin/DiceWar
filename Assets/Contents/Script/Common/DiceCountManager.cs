using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DiceCountManager : MonoSingleton<DiceCountManager>
{
	public DiceCountListData diceCountListData;

	private string diceCountListDataKey = "diceCountListData";

	public override void Init()
	{
		base.Init();

		string jsonValue = PlayerPrefs.GetString(diceCountListDataKey, string.Empty);

		//Dice Setting 팝업에서 저장한 값을 그대로 쓴다 (예전의 '임시 초기화' 줄 제거, 2026-09-26)
		if (string.IsNullOrEmpty(jsonValue) == false)
		{
			diceCountListData = JsonUtility.FromJson<DiceCountListData>(jsonValue);
		}

		//저장값이 없거나 깨졌으면(6개 설정이 다 없으면) 기본값으로
		if (diceCountListData == null ||
			diceCountListData.diceCountDataList == null ||
			diceCountListData.diceCountDataList.Count < 6)
		{
			DiceCountListDataInit();
		}
	}

	void DiceCountListDataInit()
	{
		diceCountListData = new DiceCountListData();

		diceCountListData.diceCountDataList = new List<DiceCountData>()
		{
			//2026-09-26 사용자 지정값
			new DiceCountData(false, AILevel.Easy,	1,3),
			new DiceCountData(false, AILevel.Normal,1,3),
			new DiceCountData(false, AILevel.Hard,	1,3),
			new DiceCountData(true,  AILevel.Easy,	1,3),
			new DiceCountData(true,  AILevel.Normal,1,4),
			new DiceCountData(true,  AILevel.Hard,	1,3),
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