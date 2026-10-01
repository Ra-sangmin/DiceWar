using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.Advertisements;
using UnityEngine.Events;

public class RewardedAdsButton : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    [SerializeField] UnityEngine.UI.Button _showAdButton;

    /// <summary> UI Toolkit 버튼 (인게임 맵 선택 패널처럼 uGUI 버튼이 없는 경우 사용) </summary>
    UnityEngine.UIElements.Button _showAdUIButton;
    [SerializeField] string _androidAdUnitId = "Rewarded_Android";
    [SerializeField] string _iOSAdUnitId = "Rewarded_iOS";
    string _adUnitId = null; // This will remain null for unsupported platforms

    public UnityAction<UnityAdsShowCompletionState> showCompletOn = data => { };

    /// <summary>
    /// 플랫폼별 광고 유닛 ID.
    ///
    /// 예전에는 Awake 에서만 채웠는데, 이 컴포넌트가 팝업 프리팹의 <b>자식</b> 오브젝트에 붙어 있어서
    /// 루트의 팝업 스크립트(Awake) 가 먼저 돌면 아직 null 인 채로 LoadAd() 가 불렸다.
    /// 그러면 Advertisement.Load(null) 이 되어 "placementId cannot be nil or empty" 가 난다. (2026-09-20)
    /// 그래서 Awake 순서와 무관하게 쓸 수 있도록 여기서 지연 초기화한다.
    /// </summary>
    string AdUnitId
    {
        get
        {
            if (string.IsNullOrEmpty(_adUnitId))
            {
#if UNITY_IOS
                _adUnitId = _iOSAdUnitId;
#elif UNITY_ANDROID
                _adUnitId = _androidAdUnitId;
#endif
            }

            return _adUnitId;
        }
    }

    void Awake()
    {
        //유닛 ID 는 AdUnitId 프로퍼티가 필요할 때 채운다 (위 주석 참고).

        // Disable the button until the ad is ready to show:
        SetInteractable(false);
    }

    /// <summary> UI Toolkit 버튼 연결 (uGUI 버튼 대체). Awake 이후에 호출해도 된다. </summary>
    public void InitView(UnityEngine.UIElements.Button showAdUIButton)
    {
        _showAdUIButton = showAdUIButton;

        if (_showAdUIButton != null)
        {
            _showAdUIButton.clicked -= ShowAd;
            _showAdUIButton.SetEnabled(false);
        }
    }

    void SetInteractable(bool activeOn)
    {
        if (_showAdButton != null)
        {
            _showAdButton.interactable = activeOn;
        }

        if (_showAdUIButton != null)
        {
            _showAdUIButton.SetEnabled(activeOn);
        }
    }

    // Call this public method when you want to get an ad ready to show.
    public void LoadAd()
    {
        //지원하지 않는 플랫폼(에디터의 Windows/Mac 타겟 등)에서는 유닛 ID 가 없다.
        //그대로 Load 를 부르면 "placementId cannot be nil or empty" 에러만 찍히므로 여기서 멈춘다.
        if (string.IsNullOrEmpty(AdUnitId))
        {
            Debug.Log("Skip LoadAd : ad unit id is empty on this platform.");
            return;
        }

        // IMPORTANT! Only load content AFTER initialization (in this example, initialization is handled in a different script).
        Debug.Log("Loading Ad: " + AdUnitId);

        Advertisement.Load(AdUnitId, this);
    }

    // If the ad successfully loads, add a listener to the button and enable it:
    public void OnUnityAdsAdLoaded(string adUnitId)
    {
        Debug.Log("Ad Loaded: " + adUnitId);

        if (adUnitId.Equals(AdUnitId))
        {
            // Configure the button to call the ShowAd() method when clicked:
            if (_showAdButton != null)
            {
                _showAdButton.onClick.AddListener(ShowAd);
            }

            if (_showAdUIButton != null)
            {
                //중복 등록 방지
                _showAdUIButton.clicked -= ShowAd;
                _showAdUIButton.clicked += ShowAd;
            }

            // Enable the button for users to click:
            SetInteractable(true);
        }
    }

    // Implement a method to execute when the user clicks the button:
    public void ShowAd()
    {
        if (string.IsNullOrEmpty(AdUnitId))
            return;

        // Disable the button:
        SetInteractable(false);
        // Then show the ad:
        Advertisement.Show(AdUnitId, this);
    }

    // Implement the Show Listener's OnUnityAdsShowComplete callback method to determine if the user gets a reward:
    public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState showCompletionState)
    {
        if (adUnitId.Equals(AdUnitId) && showCompletionState.Equals(UnityAdsShowCompletionState.COMPLETED))
        {
            Debug.Log("Unity Ads Rewarded Ad Completed");
            // Grant a reward.
        }

        showCompletOn(showCompletionState);

        LoadAd();
    }

    // Implement Load and Show Listener error callbacks:
    public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
    {
        Debug.Log($"Error loading Ad Unit {adUnitId}: {error.ToString()} - {message}");
        // Use the error details to determine whether to try to load another ad.
    }

    public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
    {
        Debug.Log($"Error showing Ad Unit {adUnitId}: {error.ToString()} - {message}");
        // Use the error details to determine whether to try to load another ad.

        LoadAd();
    }

    public void OnUnityAdsShowStart(string adUnitId) { }
    public void OnUnityAdsShowClick(string adUnitId) { }

    void OnDestroy()
    {
        // Clean up the button listeners:
        if (_showAdButton != null)
        {
            _showAdButton.onClick.RemoveAllListeners();
        }

        if (_showAdUIButton != null)
        {
            _showAdUIButton.clicked -= ShowAd;
            _showAdUIButton = null;
        }
    }
}
