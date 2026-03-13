#if UNITY_IOS
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

public class GoogleAuthXcodePostProcess
{
    // 빌드가 끝난 직후(999번째 순서로) 자동으로 실행되는 함수입니다.
    [PostProcessBuild(999)]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string path)
    {
        if (buildTarget == BuildTarget.iOS)
        {
            Debug.Log("🚀 [Google Sign-In] Xcode 자동 수술을 시작합니다...");

			// 1. Info.plist 수정 (URL Scheme 및 HTTP 허용 자동 추가)
			string plistPath = Path.Combine(path, "Info.plist");
			PlistDocument plist = new PlistDocument();
			plist.ReadFromString(File.ReadAllText(plistPath));

			// --- (기존에 넣었던 URL Scheme 코드) ---
			string reversedClientId = "com.googleusercontent.apps.미쿠짱의아이디를여기에넣으세요";
			PlistElementArray urlTypes = plist.root.values.ContainsKey("CFBundleURLTypes")
				? plist.root["CFBundleURLTypes"].AsArray()
				: plist.root.CreateArray("CFBundleURLTypes");
			PlistElementDict urlDict = urlTypes.AddDict();
			urlDict.SetString("CFBundleTypeRole", "Editor");
			urlDict.SetString("CFBundleURLName", "google_sign_in");
			urlDict.CreateArray("CFBundleURLSchemes").AddString(reversedClientId);

			// 💥 [여기에 HTTP 통신 허용 코드 추가!] 💥
			PlistElementDict atsDict = plist.root.values.ContainsKey("NSAppTransportSecurity")
				? plist.root["NSAppTransportSecurity"].AsDict()
				: plist.root.CreateDict("NSAppTransportSecurity");
			atsDict.SetBoolean("NSAllowsArbitraryLoads", true); // http 통신 강제 허용!

			File.WriteAllText(plistPath, plist.WriteToString());

			// 2. Podfile 수정 (Firebase 자동 추가)
			string podfilePath = Path.Combine(path, "Podfile");
            if (File.Exists(podfilePath))
            {
                string podfileContent = File.ReadAllText(podfilePath);
                if (!podfileContent.Contains("Firebase/Auth"))
                {
                    podfileContent = podfileContent.Replace("target 'UnityFramework' do", 
                        "target 'UnityFramework' do\n  pod 'Firebase/Auth'\n  pod 'Firebase/Core'");
                    File.WriteAllText(podfilePath, podfileContent);
                }
            }

            // 3. C++ 코드 자동 수술
            // 프로젝트 내의 모든 .mm 파일을 뒤져서 해당 파일을 찾아냅니다.
            string[] allFiles = Directory.GetFiles(path, "*.mm", SearchOption.AllDirectories);
            foreach (string filePath in allFiles)
            {
                // [수술 1] GoogleSignInAppController.mm
                if (filePath.EndsWith("GoogleSignInAppController.mm"))
                {
                    string code = File.ReadAllText(filePath);
                    
                    // 옛날 코드 주석 처리 및 삭제
                    code = code.Replace("<GIDSignInDelegate, GIDSignInUIDelegate>", "<GIDSignInDelegate>");
                    code = code.Replace("signIn.uiDelegate = gsiHandler;", "// signIn.uiDelegate = gsiHandler;");
                    
                    // init 추가
                    code = code.Replace("gsiHandler = [GoogleSignInHandler alloc];", "gsiHandler = [[GoogleSignInHandler alloc] init];");

                    // 바닥(화면) 지정 및 사파리 직통 알림망 설치
                    if (!code.Contains("kUnityOnOpenURL"))
                    {
                        string injection = @"signIn.delegate = gsiHandler;
  signIn.presentingViewController = UnityGetGLViewController();

  [[NSNotificationCenter defaultCenter] addObserverForName:@""kUnityOnOpenURL""
                                                    object:nil
                                                     queue:[NSOperationQueue mainQueue]
                                                usingBlock:^(NSNotification * _Nonnull note) {
      NSURL *url = note.userInfo[@""url""];
      if (url) { [[GIDSignIn sharedInstance] handleURL:url]; }
  }];";
                        code = code.Replace("signIn.delegate = gsiHandler;", injection);
                    }

                    // handleURL 매개변수 축소
                    code = code.Replace("return [[GIDSignIn sharedInstance] handleURL:url\n                             sourceApplication:sourceApplication\n                                    annotation:annotation];", "return [[GIDSignIn sharedInstance] handleURL:url];");
                    
                    File.WriteAllText(filePath, code);
                    Debug.Log("💉 GoogleSignInAppController.mm 수술 완료!");
                }

                // [수술 2] GoogleSignIn.mm
                if (filePath.EndsWith("GoogleSignIn.mm"))
                {
                    string code = File.ReadAllText(filePath);
                    
                    // 이름 바뀐 함수 적용
                    code = code.Replace("[[GIDSignIn sharedInstance] signInSilently];", "[[GIDSignIn sharedInstance] restorePreviousSignIn];");
                    
                    // signIn 호출 전 바탕화면 강제 지정
                    if (!code.Contains("keyWindow.rootViewController"))
                    {
                        code = code.Replace("[[GIDSignIn sharedInstance] signIn];", 
                            "[GIDSignIn sharedInstance].presentingViewController = [UIApplication sharedApplication].keyWindow.rootViewController;\n    [[GIDSignIn sharedInstance] signIn];");
                    }

                    // 없는 에러코드 주석처리
                    code = code.Replace("case kGIDSignInErrorCodeNoSignInHandlersInstalled:", "// case kGIDSignInErrorCodeNoSignInHandlersInstalled:");

                    File.WriteAllText(filePath, code);
                    Debug.Log("💉 GoogleSignIn.mm 수술 완료!");
                }
            }
            
            Debug.Log("✨ [Google Sign-In] Xcode 자동화 세팅이 완벽하게 끝났습니다!");
        }
    }
}
#endif