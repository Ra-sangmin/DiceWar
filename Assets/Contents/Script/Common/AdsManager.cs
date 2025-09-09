using UnityEngine;
using UnityEngine.Advertisements;

public class AdsManager : MonoSingleton<AdsManager>, IUnityAdsInitializationListener
{
    private bool _testMode = false;
    private string _gameId = string.Empty;

    public bool InitClear = false;

    private RewardedAdsButton rewardedAdsButton;

    private const int addCoinCount = 10;

    public override void Init()
    {
        base.Init();

        InitializeAds();
    }

    void InitializeAds()
    {
#if UNITY_IOS
        _gameId = "5857052";
#elif UNITY_ANDROID
        _gameId = "5857053";
#endif

        if (!Advertisement.isInitialized && Advertisement.isSupported)
        {
            if (_gameId != string.Empty)
            {
				Advertisement.Initialize(_gameId, _testMode, this);
			}
        }

    }

    public void SetRewardedAdsButton(RewardedAdsButton rewardedAdsButton)
    {
        this.rewardedAdsButton = rewardedAdsButton;

        rewardedAdsButton.showCompletOn = ShowCompletOn;

        if (InitClear)
        {
            rewardedAdsButton.LoadAd();
        }
    }

    void ShowCompletOn(UnityAdsShowCompletionState state)
    {
        //광고 시청을 완료하였다면
        if (state.Equals(UnityAdsShowCompletionState.COMPLETED))
        {
            DataManager.Instance.AddCoin(addCoinCount);
        }
    }

    public void OnInitializationComplete()
    {
        string text = "Unity Ads initialization complete.";

        Debug.Log(text);

        InitClear = true;

        if (rewardedAdsButton != null) 
        {
            rewardedAdsButton.LoadAd();
        }
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        string text = $"Unity Ads Initialization Failed: {error.ToString()} - {message}";

        Debug.Log(text);
    }
}
