using UnityEngine;
using System;
using UniRx;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Linq;

public class LocalizeManager : MonoSingleton<LocalizeManager>
{
	//public SystemLanguage lang;
	public ReactiveProperty<SystemLanguage> language = new ReactiveProperty<SystemLanguage>( SystemLanguage.English );

	private const string langSaveKey = "langSaveKey";

	/// <summary> 각 나라별 언어를 저장할 문자열 딕셔너리 선언 </summary>
	//public Dictionary<string, LocalizeData> data_Meum_Dic = new Dictionary<string, LocalizeData>();

	private List<LocalizeData> dataList = new List<LocalizeData>();

	public bool initOn = false;

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
	}

	public async void SetLangData()
	{
		if (initOn)
			return;
		
		initOn = true;

		string csvData = await GetCsvData();

		Debug.LogWarning(csvData);

		//CSV 데이터 List 형식으로 취득
		dataList = CSVReader.ReadAutoData<LocalizeData>(csvData);

		//Enum 설정
		dataList.ForEach(data => data.SetStatus());

		//Enum이 None값인 항목 제거
		dataList = dataList.Where(data => data.statusEnum != LocalizeStatus.None).ToList();

		language.SetValueAndForceNotify(language.Value);
	}

	public async UniTask<string> GetCsvData()
	{
		//각 나라별 언어가 정리된 구글 시트 Page key 값
		//string pageKey = "1JrlEk5tY5zHKJsZIlWmRBiIM4fd1YB-Ex6T-UGyhDKI";
		string pageKey = "1IXRATYPl71Aw_vpmsk8ycaYLqovfXWUdLwI9Ug5Wfw8";

		string sheetName = "App";

		string allURL = $"https://docs.google.com/spreadsheets/d/{pageKey}/gviz/tq?tqx=out:csv&sheet={sheetName}";

		string resultText = string.Empty;

		using (UnityWebRequest www = UnityWebRequest.Get(allURL))
		{
			await www.SendWebRequest();  // UniTask가 코루틴처럼 대기

			if (www.result != UnityWebRequest.Result.Success)
			{
				Debug.LogError(www.error);
				return null;
			}

			resultText = www.downloadHandler.text;
		}

		return resultText;
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
		LocalizeData result = dataList.FirstOrDefault(data => data.statusEnum == statusEnum && data.key == key);

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

		if (initOn == false)
		{
			SetLangData();

			return result;
		}

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
	public string statusStr = string.Empty;
	public LocalizeStatus statusEnum = LocalizeStatus.None;
	public int key = -1;
	public string korean = string.Empty;
	public string english = string.Empty;

	public void SetStatus()
	{
		Enum.TryParse(statusStr, out statusEnum);
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