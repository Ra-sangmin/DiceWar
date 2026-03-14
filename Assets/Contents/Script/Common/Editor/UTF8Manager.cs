using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

// AssetPostprocessor를 상속받아 자동 감지 기능 활성화
public class UTF8Manager : AssetPostprocessor
{
	// =========================================================
	// 1. [자동 변환] 새 스크립트가 생성되거나 임포트될 때 실행
	// =========================================================
	static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
	{
		foreach (string asset in importedAssets)
		{
			if (asset.EndsWith(".cs"))
			{
				// false를 넘겨서 개별 자동 변환 로그가 뜨도록 함
				MakeUTF8(asset, isManualBatch: false);
			}
		}
	}

	// =========================================================
	// 2. [수동 변환] 상단 메뉴 버튼 (기존 파일 전체 변환)
	// =========================================================
	[MenuItem("Tools/모든 스크립트 UTF-8로 강제 변환 🚀")]
	public static void ConvertAllScriptsToUTF8()
	{
		// 프로젝트 안의 모든 .cs 파일을 찾습니다.
		string[] files = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);
		int convertedCount = 0;

		foreach (string file in files)
		{
			// true를 넘겨서 파일 하나하나마다 로그가 뜨지 않게 조절
			if (MakeUTF8(file, isManualBatch: true))
			{
				convertedCount++;
			}
		}

		// 유니티 에디터 새로고침
		AssetDatabase.Refresh();
		Debug.Log($"✨ [UTF-8 강제 변환] 전체 검사 완료! 총 {convertedCount}개의 스크립트를 UTF-8로 변환했습니다. (나머지는 이미 정상이었습니다)");
	}

	// =========================================================
	// 3. [핵심 로직] 실제 변환을 담당하는 공통 함수
	// =========================================================
	static bool MakeUTF8(string assetPath, bool isManualBatch)
	{
		string fullPath = Path.GetFullPath(assetPath);
		if (!File.Exists(fullPath)) return false;

		// 파일의 실제 바이트 데이터를 읽어옵니다.
		byte[] bytes = File.ReadAllBytes(fullPath);

		// UTF-8 (BOM 포함) 파일의 지문인 'EF BB BF'가 있는지 확인 (무한 루프 방지)
		if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
		{
			return false; // 이미 완벽한 UTF-8이므로 아무것도 안 함
		}

		// 지문이 없다면 강제로 UTF-8 BOM으로 덮어쓰기!
		string text = File.ReadAllText(fullPath);
		File.WriteAllText(fullPath, text, new UTF8Encoding(true));

		// 자동 생성일 때만 어떤 파일이 변환되었는지 로그를 남김
		if (!isManualBatch)
		{
			Debug.Log($"✨ [UTF-8 자동 변환] '{assetPath}' 파일이 완벽한 UTF-8로 맞춰졌습니다!");
		}

		return true;
	}
}