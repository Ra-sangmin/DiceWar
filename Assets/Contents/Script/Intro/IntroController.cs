using AppleAuth.Enums;
using AppleAuth;
using Assets.SimpleSignIn.Google.Scripts;
using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using AppleAuth.Native;

public class IntroController : MonoBehaviour
{
	[SerializeField] Transform reporter;

	[SerializeField] AppleLogin appleLogin;

	public GoogleAuth GoogleAuth;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
		PopupManager.Instance.SetCanvasParant(transform);

		DataManager.Instance.userData = null;

        GoogleAuth = new GoogleAuth();

        SoundManager.Instance.PlayBGM(BGMEnum.Intro);

#if TEST
		reporter.gameObject.SetActive(true);
#endif

		appleLogin.gameObject.SetActive(AppleAuthManager.IsCurrentPlatformSupported);
	}

	public void SignOut()
    {
        GoogleAuth.SignOut(revokeAccessToken: true);
    }


    // Update is called once per frame
    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    SignOut();
        //}   
    }

    public void GetAccessToken()
    {
        if (Application.platform == RuntimePlatform.WindowsPlayer) 
        {
            SceneManager.LoadScene("Main");
        }
        else 
        {
            GoogleAuth.GetTokenResponse(OnGetTokenResponse);
        }
    }

    public void AppleLogin()
    {
		appleLogin.SigninWithApple(data => LoginClear(data));
	}

	private void OnGetTokenResponse(bool success, string error, TokenResponse tokenResponse)
    {
        if (!success) return;

        var jwt = new JWT(tokenResponse.IdToken);

        var userInfo = JsonUtility.FromJson<UserInfo>(jwt.Payload);

        UserData userData = new UserData()
        {
			email = userInfo.email,
			snsType = 0,
		};

		LoginClear(userData);
    }

    private void LoginClear(UserData tempUserData)
    {
		UserDataRequest request = new UserDataRequest()
        {
            requestStatus = 0,
            email = tempUserData.email,
            snsType = tempUserData.snsType,
            successOn = ResultData =>
            {
                UserDataRespons userData = (ResultData as UserDataRespons);

                DataManager.Instance.UserDataSave(userData);

                SceneManager.LoadScene("Main");
            }
        };

        request.RequestOn().Forget();
    }

	public void SettingPopupOn()
	{
		PopupManager.Instance.MainSettingPopupOn();
	}

	public void InfoPopupOn()
	{
		PopupManager.Instance.MainInfoPopupOn();
	}
}