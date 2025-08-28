using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static UnityEngine.UI.Button;

public class PlayCoinBtn : MonoBehaviour
{
	[SerializeField] RectTransform needCoinTextPanel;
	[SerializeField] Text needCoinText;
	[SerializeField] RewardedAdsButton rewardedAdsButton;
	[SerializeField] RectTransform playButton;
	[Space(10)]
	[SerializeField] ButtonClickedEvent m_OnClick = new ButtonClickedEvent();

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
		if (rewardedAdsButton != null)
		{
			AdsManager.Instance.SetRewardedAdsButton(rewardedAdsButton);
		}
		
		SetNeedCoinCheck();
	}

    // Update is called once per frame
    void Update()
    {
        
    }

	public bool SetNeedCoinCheck()
	{
		int needCoin = DataManager.Instance.GetNeedCoin();

		if (DataManager.Instance.userData == null)
		{
			DataManager.Instance.userData = new UserData();
		}

		bool needCoinOn = DataManager.Instance.userData.myCoin.Value < needCoin;

		if (rewardedAdsButton != null)
		{
			playButton.gameObject.SetActive(!needCoinOn);
		}

		if (DataManager.Instance.aiLevel == AILevel.Easy)
		{
			needCoinTextPanel.gameObject.SetActive(false);
		}
		else
		{
			needCoinTextPanel.gameObject.SetActive(true);
			needCoinText.text = $"-{needCoin}";
		}

		return needCoinOn;
	}

	public void PlayBtnClickOn()
	{
		m_OnClick.Invoke();
	}
}
