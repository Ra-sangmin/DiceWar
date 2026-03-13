using Google;
using Cysharp.Threading.Tasks;
using System;

public class GoogleAuthService
{
	// [����] ���� ���ο��� ���� ID�� �����մϴ�.
	private const string WebClientId = "176126443286-blbv7kagtc2thpdl9ebfbo4ad29asda5.apps.googleusercontent.com";
	private readonly GoogleSignInConfiguration _configuration;

	public GoogleAuthService()
	{
		// ���� ����� ����Ͽ� ������ �ʱ�ȭ�մϴ�.
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

			// ���� �α����� �����ϰ� ����� ��ٸ��ϴ�.
			var user = await GoogleSignIn.DefaultInstance.SignIn().AsUniTask();

			// ����Ƽ API(UI ��)�� �����ϰ� ���� ���� ���� ������ ���͸� �����մϴ�.
			await UniTask.SwitchToMainThread();

			return user;
		}
		catch (Exception)
		{
			await UniTask.SwitchToMainThread();
			throw; // ������ ����(Controller)�� �����մϴ�.
		}
	}

	public void SignOut()
	{
		GoogleSignIn.DefaultInstance.SignOut();
	}
}