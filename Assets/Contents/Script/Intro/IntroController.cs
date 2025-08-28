using Assets.SimpleSignIn.Google.Scripts;
using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class IntroController : MonoBehaviour
{
	[SerializeField] Transform reporter;

	public GoogleAuth GoogleAuth;

    private int snsType = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DataManager.Instance.userData = null;

        GoogleAuth = new GoogleAuth();

        //StartCoroutine(PostRequest());
        //SignOut();

        SoundManager.Instance.PlayBGM(BGMEnum.Intro);

#if TEST
		reporter.gameObject.SetActive(true);
#endif
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

    private void OnGetTokenResponse(bool success, string error, TokenResponse tokenResponse)
    {
        if (!success) return;

        var jwt = new JWT(tokenResponse.IdToken);

        var userInfo = JsonUtility.FromJson<UserInfo>(jwt.Payload);

        LoginClear(userInfo);
    }

    private void LoginClear(UserInfo userInfo)
    {
		UserDataRequest request = new UserDataRequest()
        {
            requestStatus = 0,
            email = userInfo.email,
            snsType = snsType,
            successOn = ResultData =>
            {
                UserDataRespons userData = (ResultData as UserDataRespons);

                DataManager.Instance.UserDataSave(userData);

                SceneManager.LoadScene("Main");
            }
        };

        request.RequestOn().Forget();
    }
}