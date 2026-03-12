using AppleAuth;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroController : MonoBehaviour
{
	[SerializeField] Transform reporter;

	[SerializeField] AppleLogin appleLogin;

	// 서비스 클래스 참조 (ID를 넘길 필요가 없어졌습니다.)
	private GoogleAuthService _authService;

	void Awake()
	{
		_authService = new GoogleAuthService();
	}

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
		PopupManager.Instance.SetCanvasParant(transform);

		DataManager.Instance.userData = null;

        SoundManager.Instance.PlayBGM(BGMEnum.Intro);

#if TEST
		reporter.gameObject.SetActive(true);
#endif

		appleLogin.gameObject.SetActive(AppleAuthManager.IsCurrentPlatformSupported);
	}

    // Update is called once per frame
    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    SignOut();
        //}   
    }

    public void GoogleSignInBtnClick()
    {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
		UserData userData = new UserData()
		{
			email = "fktkdals1@gmail.com",
			snsType = 0
		};

		LoginClear(userData);
#else
        HandleSignIn().Forget();
#endif
	}

	private async UniTaskVoid HandleSignIn()
	{
		// 서비스를 통해 유저 정보만 쏙 가져옵니다.
		var googleUser = await _authService.SignInAsync();

		if (googleUser != null)
		{
			UserData userData = new UserData()
			{
				email = googleUser.Email,
				snsType = 0
			};

			LoginClear(userData);
		}
	}

	public void AppleLogin()
	{
		appleLogin.SigninWithApple(data => LoginClear(data));
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