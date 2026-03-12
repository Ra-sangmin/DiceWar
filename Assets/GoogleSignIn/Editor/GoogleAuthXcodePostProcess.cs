#if UNITY_IOS
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

public class GoogleAuthXcodePostProcess
{
	// 여기에 본인의 REVERSED_CLIENT_ID를 넣으세요
	private const string REVERSED_CLIENT_ID = "com.googleusercontent.apps.176126443286-8baeh3vp2ppjtt6t0sc2q97fs30n6hul";

	[PostProcessBuild]
	public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
	{
		if (target != BuildTarget.iOS) return;

		// 1. Info.plist 경로 찾기
		string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
		PlistDocument plist = new PlistDocument();
		plist.ReadFromFile(plistPath);

		// 2. URL Types 설정 루틴
		PlistElementDict rootDict = plist.root;
		PlistElementArray urlTypes = rootDict.CreateArray("CFBundleURLTypes");
		PlistElementDict dict = urlTypes.AddDict();

		dict.SetString("CFBundleTypeRole", "Editor");
		dict.SetString("CFBundleURLName", "google_sign_in"); // 구분용 이름
		PlistElementArray urlSchemes = dict.CreateArray("CFBundleURLSchemes");
		urlSchemes.AddString(REVERSED_CLIENT_ID);

		// 3. 파일 저장
		plist.WriteToFile(plistPath);
		Debug.Log($"[XcodePostProcess] URL Scheme ({REVERSED_CLIENT_ID}) 추가 완료!");
	}
}
#endif