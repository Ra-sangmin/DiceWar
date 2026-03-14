#if UNITY_IOS
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;
using System.Text.RegularExpressions;

public class GoogleAuthXcodePostProcess
{
    [PostProcessBuild(999)]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string path)
    {
        if (buildTarget == BuildTarget.iOS)
        {
            Debug.Log("🚀 [Google Sign-In] Xcode 100% 자동 수술 및 파일 배달을 시작합니다...");

            // --- [추가] 0. GoogleService-Info.plist 강제 등록 로직 ---
            string googleConfigName = "GoogleService-Info.plist";
            // 💥 주의: 현재 유니티 프로젝트 내 실제 파일 이름이 .plist가 없다면 
            // 아래 sourcePath의 파일명을 실제 파일명과 똑같이 맞춰주세요.
            string sourcePath = Path.Combine(Application.dataPath, "Plugins/iOS/GoogleService-Info"); 
            
            // 만약 파일명을 이미 .plist로 바꾸셨다면 위 줄 대신 아래 줄을 쓰세요.
            if (!File.Exists(sourcePath)) sourcePath += ".plist";

            string destPath = Path.Combine(path, googleConfigName);

            if (File.Exists(sourcePath))
            {
                // Xcode 빌드 폴더로 파일 복사
                File.Copy(sourcePath, destPath, true);

                // 프로젝트 설정 열기
                string projPath = PBXProject.GetPBXProjectPath(path);
                PBXProject proj = new PBXProject();
                proj.ReadFromString(File.ReadAllText(projPath));

                // 빌드 타겟(Unity-iPhone) 가져오기
                string targetGuid = proj.GetUnityMainTargetGuid();

                // 파일 등록 및 빌드 타겟에 추가
                string fileGuid = proj.AddFile(googleConfigName, googleConfigName, PBXSourceTree.Source);
                proj.AddFileToBuild(targetGuid, fileGuid);

                proj.WriteToFile(projPath);
                Debug.Log("✅ [Google Config] GoogleService-Info.plist 배달 완료!");
            }
            else
            {
                Debug.LogError($"❌ [Google Config] 파일을 찾을 수 없습니다! 경로 확인 필수: {sourcePath}");
            }

            // --- 1. Info.plist 수정 ---
            string plistPath = Path.Combine(path, "Info.plist");
            PlistDocument plist = new PlistDocument();
            plist.ReadFromString(File.ReadAllText(plistPath));

            string reversedClientId = "com.googleusercontent.apps.176126443286-8baeh3vp2ppjtt6t0sc2q97fs30n6hul";
            PlistElementArray urlTypes = plist.root.values.ContainsKey("CFBundleURLTypes")
                ? plist.root["CFBundleURLTypes"].AsArray()
                : plist.root.CreateArray("CFBundleURLTypes");
            PlistElementDict urlDict = urlTypes.AddDict();
            urlDict.SetString("CFBundleTypeRole", "Editor");
            urlDict.SetString("CFBundleURLName", "google_sign_in");
            urlDict.CreateArray("CFBundleURLSchemes").AddString(reversedClientId);

            PlistElementDict atsDict = plist.root.values.ContainsKey("NSAppTransportSecurity")
                ? plist.root["NSAppTransportSecurity"].AsDict()
                : plist.root.CreateDict("NSAppTransportSecurity");
            atsDict.SetBoolean("NSAllowsArbitraryLoads", true); 

            File.WriteAllText(plistPath, plist.WriteToString());

            // --- 2. Podfile 수정 ---
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

            // --- 3. C++ 및 헤더 코드 자동 수술 ---
            string[] allFiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            foreach (string filePath in allFiles)
            {
                if (!filePath.EndsWith(".mm") && !filePath.EndsWith(".h")) continue;

                string code = File.ReadAllText(filePath);
                bool isChanged = false;

                if (Regex.IsMatch(code, @"\,\s*GIDSignInUIDelegate"))
                {
                    code = Regex.Replace(code, @"\,\s*GIDSignInUIDelegate", "");
                    isChanged = true;
                }
                if (Regex.IsMatch(code, @"signIn\.uiDelegate\s*=\s*gsiHandler;"))
                {
                    code = Regex.Replace(code, @"signIn\.uiDelegate\s*=\s*gsiHandler;", "// signIn.uiDelegate = gsiHandler;");
                    isChanged = true;
                }

                if (filePath.EndsWith("GoogleSignInAppController.mm"))
                {
                    if (code.Contains("gsiHandler = [GoogleSignInHandler alloc];")) {
                        code = code.Replace("gsiHandler = [GoogleSignInHandler alloc];", "gsiHandler = [[GoogleSignInHandler alloc] init];");
                        isChanged = true;
                    }

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
                        isChanged = true;
                    }

                    string pattern1 = @"handleURL:url\s*sourceApplication:sourceApplication\s*annotation:annotation";
                    if (Regex.IsMatch(code, pattern1, RegexOptions.Singleline)) {
                        code = Regex.Replace(code, pattern1, "handleURL:url", RegexOptions.Singleline);
                        isChanged = true;
                    }

                    string pattern2 = @"handleURL:url\s*sourceApplication:\s*options\[UIApplicationOpenURLOptionsSourceApplicationKey\]\s*annotation:\s*options\[UIApplicationOpenURLOptionsAnnotationKey\]";
                    if (Regex.IsMatch(code, pattern2, RegexOptions.Singleline)) {
                        code = Regex.Replace(code, pattern2, "handleURL:url", RegexOptions.Singleline);
                        isChanged = true;
                    }
                }

                if (filePath.EndsWith("GoogleSignIn.mm"))
                {
                    if (code.Contains("[[GIDSignIn sharedInstance] signInSilently];")) {
                        code = code.Replace("[[GIDSignIn sharedInstance] signInSilently];", "[[GIDSignIn sharedInstance] restorePreviousSignIn];");
                        isChanged = true;
                    }
                    
                    if (!code.Contains("keyWindow.rootViewController") && code.Contains("[[GIDSignIn sharedInstance] signIn];"))
                    {
                        code = code.Replace("[[GIDSignIn sharedInstance] signIn];", 
                            "[GIDSignIn sharedInstance].presentingViewController = [UIApplication sharedApplication].keyWindow.rootViewController;\n    [[GIDSignIn sharedInstance] signIn];");
                        isChanged = true;
                    }

                    if (code.Contains("case kGIDSignInErrorCodeNoSignInHandlersInstalled:")) {
                        code = code.Replace("case kGIDSignInErrorCodeNoSignInHandlersInstalled:", "// case kGIDSignInErrorCodeNoSignInHandlersInstalled:");
                        isChanged = true;
                    }
                }

                if (isChanged)
                {
                    File.WriteAllText(filePath, code);
                }
            }
            
            Debug.Log("✨ [Google Sign-In] 모든 자동화 작업이 성공적으로 끝났습니다!");
        }
    }
}
#endif