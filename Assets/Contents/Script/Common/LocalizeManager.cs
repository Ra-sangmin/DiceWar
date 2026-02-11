using UnityEngine;
using System;
using UniRx;
using System.Linq;

public class LocalizeManager : MonoSingleton<LocalizeManager>
{
	public ReactiveProperty<SystemLanguage> language = new ReactiveProperty<SystemLanguage>( SystemLanguage.English );

	private const string langSaveKey = "langSaveKey";

	public LocalizeDataTable dataTable;

	public override void Init()
	{
		base.Init();

		string langStr = PlayerPrefs.GetString(langSaveKey, string.Empty);

		SystemLanguage tempLang = SystemLanguage.Unknown;

		if (Enum.TryParse(langStr, true, out tempLang) == false)
		{
			LanguageCheck();
		}
		else
		{
			language.Value = tempLang;
		}

		SetData();
	}

	private void SetData()
	{
		dataTable = Resources.Load<LocalizeDataTable>("LocalizeDataTable");
	}

	void LanguageCheck() 
	{
		// 현재 시스템 언어 가져오기
		SystemLanguage tempLang = Application.systemLanguage;

		Debug.LogWarning("현재 시스템 언어: " + tempLang);

		// 특정 언어별 처리 예시
		if (tempLang == SystemLanguage.Korean)
		{
			Debug.LogWarning("한국어 환경입니다!");
		}
		else if (tempLang == SystemLanguage.English)
		{
			Debug.LogWarning("영어 환경입니다!");
		}
		else
		{
			Debug.LogWarning("기타 언어 환경: " + tempLang);
		}

		SaveLang(tempLang);
	}

	public void SaveLang(SystemLanguage lang)
	{
		this.language.Value = lang;
		PlayerPrefs.SetString(langSaveKey, lang.ToString());
		PlayerPrefs.Save();
	}

	/// <summary>
	/// 넘겨 받은 키값에 해당 되는 현지화데이터 취득
	/// </summary>
	/// <param name="key"></param>
	/// <returns></returns>
	public LocalizeData GetData(LocalizeStatus statusEnum, int key)
	{
		LocalizeData result = dataTable.list.FirstOrDefault(data => data.statusEnum == statusEnum && data.key == key);

		if (result == null)
		{
			Debug.LogWarning($"{statusEnum} , {key} 없음 ");
		}

		return result;
	}

	/// <summary>
	/// 넘겨 받은 키값에 해당 되는 언어값 취득
	/// </summary>
	/// <param name="key"></param>
	/// <returns></returns>
	public string GetStrData(LocalizeStatus statusEnum , int key)
	{
		string result = string.Empty;

		LocalizeData data = GetData(statusEnum,key);

		if (data != null)
		{
			switch (language.Value)
			{
				case SystemLanguage.Korean:  result = data.korean; break;
				case SystemLanguage.English: result = data.english; break;
			}
		}
		else
		{
			Debug.LogWarning($"{key} 없음 ");
		}

		return result;
	}

}

[System.Serializable]
/// <summary>
/// 현지화 언어 데이터
/// </summary>
public class LocalizeData
{
	[HideInInspector] public string statusStr = string.Empty;
	public LocalizeStatus statusEnum = LocalizeStatus.None;
	public int key = -1;
	public string korean = string.Empty;
	public string english = string.Empty;

	public void SetStatus()
	{
		Enum.TryParse(statusStr, out statusEnum);
	}

	public void UpdateData(LocalizeData newData)
	{
		statusStr = newData.statusStr;
		statusEnum = newData.statusEnum;
		key = newData.key;
		korean = newData.korean;
		english = newData.english;
		SetStatus();
	}
}

[System.Serializable]
public enum LocalizeStatus
{
	None,
	Common,
	TutorialPopup,
	MainSettingPopup,
	MainInfoPopup,
	BuyCoinPopup,
	PlaySetPopup,
	Loading,
	Game,
	Main
}