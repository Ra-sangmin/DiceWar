using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

[CreateAssetMenu(
	fileName = "LocalizeDataTable",
	menuName = "Data/Localize Data Table"
)]
public class LocalizeDataTable : ScriptableObject
{
	private const string API_KEY = "AIzaSyBMenv11qDFR40UBffWvHxdt7BTNN113nY";
	private const string SPREADSHEET_ID = "1IXRATYPl71Aw_vpmsk8ycaYLqovfXWUdLwI9Ug5Wfw8";

	public List<LocalizeData> list = new();

	public async UniTask UpdateDataFromSheet()
	{
		string sheetName = "App";
		string valueUrl =
			$"https://sheets.googleapis.com/v4/spreadsheets/{SPREADSHEET_ID}/values/{sheetName}?key={API_KEY}";

		string jsonText = await LoadSheet(valueUrl);

		if (string.IsNullOrEmpty(jsonText))
			return;

		SheetResponse sheet = JsonConvert.DeserializeObject<SheetResponse>(jsonText);

		List<LocalizeData> newDataList = ConvertToStageData(sheet);

		foreach (var data in newDataList)
		{
			var oldData =
				list.FirstOrDefault(checkData =>
					checkData.statusEnum == data.statusEnum && checkData.key == data.key);

			if (oldData != null)
			{
				oldData.UpdateData(data);
			}
			else
			{
				list.Add(data);
			}
		}

		list = list.OrderBy(data => data.statusEnum).ThenBy(x => x.key).ToList();
	}

	public async UniTask<string> LoadSheet(string valueUrl)
	{
		using (UnityWebRequest req = UnityWebRequest.Get(valueUrl))
		{
			await req.SendWebRequest();

			if (req.result != UnityWebRequest.Result.Success)
			{
				Debug.LogError(req.error);
				return null;
			}

			return req.downloadHandler.text;
		}
	}

	private List<LocalizeData> ConvertToStageData(SheetResponse sheet)
	{
		List<LocalizeData> list = new List<LocalizeData>();

		// 0번째는 header
		for (int i = 1; i < sheet.values.Count; i++)
		{
			var r = sheet.values[i];

			var data = new LocalizeData
			{
				statusStr = r[0],
				key = int.Parse(r[1]),
				korean = r[2],
				english = r[3],
			};

			data.SetStatus();

			list.Add(data);
		}

		return list;
	}
}

[System.Serializable]
public class SheetResponse
{
	public string range;
	public string majorDimension;
	public List<List<string>> values;
}