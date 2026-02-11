using UnityEditor;
using UnityEngine;

public class LocalizeDataTableEditor : MonoBehaviour
{
	private const string assetPath = "Assets/Resources/LocalizeDataTable.asset";

	[MenuItem("Tools/Localize/Create LocalizeDataTable", false, 1)]
	private static async void CreateData()
	{
		LocalizeDataTable asset = AssetDatabase.LoadAssetAtPath<LocalizeDataTable>(assetPath);

		if (asset == null)
		{
			asset = ScriptableObject.CreateInstance<LocalizeDataTable>();
			AssetDatabase.CreateAsset(asset, assetPath);
			Debug.Log("LocalizeDataTable 새로 생성");
		}

		await asset.UpdateDataFromSheet();

		EditorUtility.SetDirty(asset);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();

		Debug.Log("LocalizeDataTable 데이터 갱신 완료");
	}
}
