using AppleAuth;
using AppleAuth.Interfaces;
using AppleAuth.Native;
using Assets.SimpleSignIn.Google.Scripts;
using UnityEngine;
using UnityEngine.Events;

public class AppleLogin : MonoBehaviour
{
	private AppleAuthManager appleAuthManager;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
		// If the current platform is supported
		if (AppleAuthManager.IsCurrentPlatformSupported)
		{
			// Creates a default JSON deserializer, to transform JSON Native responses to C# instances
			var deserializer = new PayloadDeserializer();
			// Creates an Apple Authentication manager with the deserializer
			this.appleAuthManager = new AppleAuthManager(deserializer);
		}
	}

    // Update is called once per frame
    void Update()
    {
		if (appleAuthManager != null)
			appleAuthManager.Update();
	}

	public void SigninWithApple(UnityAction<UserData> loginClear)
	{
		var loginArgs = new AppleAuthLoginArgs(AppleAuth.Enums.LoginOptions.IncludeEmail | AppleAuth.Enums.LoginOptions.IncludeFullName);

		appleAuthManager.LoginWithAppleId(
			loginArgs,
			credential =>
			{
				var appleIdCredential = credential as IAppleIDCredential;
				if (appleIdCredential != null)
				{
					UserData userData = new UserData()
					{
						email = appleIdCredential.Email,
						snsType = 1,
					};

					if (loginClear != null)
					{
						loginClear(userData);
					}

					//sonPortraitLoginController.SetApple(appleIdCredential);
					Debug.Log("apple signin");
				}
			},
			error =>
			{
				Debug.Log("Apple Signin Error");
			});


	}
}
