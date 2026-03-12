using Google;
using Cysharp.Threading.Tasks;
using System;

public class GoogleAuthService
{
	// [변경] 서비스 내부에서 직접 ID를 관리합니다.
	private const string WebClientId = "176126443286-blbv7kagtc2thpdl9ebfbo4ad29asda5.apps.googleusercontent.com";
	private readonly GoogleSignInConfiguration _configuration;

	public GoogleAuthService()
	{
		// 내부 상수를 사용하여 설정을 초기화합니다.
		_configuration = new GoogleSignInConfiguration
		{
			WebClientId = WebClientId,
			RequestEmail = true,
			RequestIdToken = true,
			RequestProfile = true
		};
	}

	public async UniTask<GoogleSignInUser> SignInAsync()
	{
		try
		{
			GoogleSignIn.Configuration = _configuration;

			// 구글 로그인을 실행하고 결과를 기다립니다.
			var user = await GoogleSignIn.DefaultInstance.SignIn().AsUniTask();

			// 유니티 API(UI 등)를 안전하게 쓰기 위해 메인 스레드 복귀를 보장합니다.
			await UniTask.SwitchToMainThread();

			return user;
		}
		catch (Exception)
		{
			await UniTask.SwitchToMainThread();
			throw; // 에러를 상위(Controller)로 전달합니다.
		}
	}

	public void SignOut()
	{
		GoogleSignIn.DefaultInstance.SignOut();
	}
}