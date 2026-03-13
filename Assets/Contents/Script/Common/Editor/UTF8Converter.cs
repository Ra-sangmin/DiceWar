using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

public class UTF8Converter
{
	// 유니티 상단 메뉴에 Tools 메뉴를 만듭니다.
	[MenuItem("Tools/모든 스크립트 UTF-8로 강제 변환 🚀")]
	public static void ConvertAllScriptsToUTF8()
	{
		// 프로젝트 안의 모든 .cs 파일을 찾습니다.
		string[] files = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);
		int count = 0;

		foreach (string file in files)
		{
			// 파일을 읽어옵니다.
			string text = File.ReadAllText(file);

			// UTF-8 (BOM 포함) 규격으로 다시 덮어씁니다.
			// (true 옵션이 BOM을 포함하겠다는 뜻입니다)
			File.WriteAllText(file, text, new UTF8Encoding(true));
			count++;
		}

		// 유니티 에디터 새로고침
		AssetDatabase.Refresh();
		Debug.Log($"✨ 총 {count}개의 스크립트를 완벽한 UTF-8로 변환 완료했습니다!");
	}
}