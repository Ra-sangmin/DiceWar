using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 코인 구매 팝업. (uGUI Canvas → UI Toolkit 이식)
/// IAP 는 Unity Purchasing 의 CodelessIAPButton 이 uGUI Button 의 onClick 에 붙어 동작하므로
/// **버튼 GameObject 와 컴포넌트는 그대로 두고**, UI Toolkit 버튼이 그 onClick 을 대신 호출한다.
/// </summary>
[RequireComponent(typeof(PanelRenderer))]
public class BuyCoinPopup : BasePopup
{
	[SerializeField] RewardedAdsButton rewardedAdsButton;

	[Header("IAP (uGUI Button + CodelessIAPButton 유지)")]
	[SerializeField] UnityEngine.UI.Button iapButton0;
	[SerializeField] UnityEngine.UI.Button iapButton1;

	[Header("상품 가격 표기 (원본 고정 문구)")]
	[SerializeField] string priceText0 = "₩ 1000";
	[SerializeField] string priceText1 = "₩ 2000";

	private PanelUI panelUI;

	void Awake()
	{
		panelUI = new PanelUI(this, OnUIReady);
	}

	/// <summary> PanelRenderer 의 UI 가 준비되면 한 번 호출된다 (예전 Awake 의 요소 찾기 / 이벤트 등록, 2026-09-29) </summary>
	private void OnUIReady(VisualElement root)
	{
		UnityEngine.UIElements.Button backBtn = root.Q<UnityEngine.UIElements.Button>("buy-coin-back-btn");
		UnityEngine.UIElements.Button buyBtn0 = root.Q<UnityEngine.UIElements.Button>("buy-coin-btn-0");
		UnityEngine.UIElements.Button buyBtn1 = root.Q<UnityEngine.UIElements.Button>("buy-coin-btn-1");
		UnityEngine.UIElements.Button adsBtn = root.Q<UnityEngine.UIElements.Button>("buy-coin-ads-btn");

		if (backBtn != null) backBtn.clicked += () => { PlayClickSe(); CloseBtnClickOn(); };

		//원본 IAP 버튼의 onClick 을 그대로 호출한다 (CodelessIAPButton 이 붙어 있음)
		if (buyBtn0 != null) buyBtn0.clicked += () => IapClickOn(iapButton0);
		if (buyBtn1 != null) buyBtn1.clicked += () => IapClickOn(iapButton1);

		if (rewardedAdsButton != null)
		{
			rewardedAdsButton.InitView(adsBtn);
		}

		SetPriceText(root, "buy-coin-price-0", priceText0);
		SetPriceText(root, "buy-coin-price-1", priceText1);

		PopupMyCoinView.Bind(root, gameObject);

		LocalizeTextSet(root);
	}

	private void OnDestroy()
	{
		panelUI?.Dispose();
	}

	void Start()
	{
		AdsManager.Instance.SetRewardedAdsButton(rewardedAdsButton);
	}

	void PlayClickSe()
	{
		SoundManager.Instance.PlaySe(SeEnum.Yes);
	}

	void IapClickOn(UnityEngine.UI.Button iapButton)
	{
		if (iapButton == null)
			return;

		iapButton.onClick.Invoke();
	}

	static void SetPriceText(VisualElement root, string name, string text)
	{
		Label label = root.Q<Label>(name);

		if (label != null)
		{
			label.text = text;
		}
	}

	void LocalizeTextSet(VisualElement root)
	{
		LocalizeManager localize = LocalizeManager.Instance;

		SetText(root, "buy-coin-title", localize.GetStrData(LocalizeStatus.BuyCoinPopup, 0));
		SetText(root, "buy-coin-my-coin-label", localize.GetStrData(LocalizeStatus.BuyCoinPopup, 4));
		SetText(root, "buy-coin-card-text-0", localize.GetStrData(LocalizeStatus.BuyCoinPopup, 6));
		SetText(root, "buy-coin-card-text-1", localize.GetStrData(LocalizeStatus.BuyCoinPopup, 7));
		SetText(root, "buy-coin-free-title", localize.GetStrData(LocalizeStatus.BuyCoinPopup, 1));
		SetText(root, "buy-coin-free-text", localize.GetStrData(LocalizeStatus.BuyCoinPopup, 8));
		SetText(root, "buy-coin-time-text", localize.GetStrData(LocalizeStatus.BuyCoinPopup, 5));
		SetText(root, "buy-coin-ads-label", localize.GetStrData(LocalizeStatus.BuyCoinPopup, 2));
	}

	static void SetText(VisualElement root, string name, string text)
	{
		Label label = root.Q<Label>(name);

		if (label != null)
		{
			label.text = text;
		}
	}

	/// <summary>
	/// IAP 버튼을 이용한 자동 인앱 결제후 성공했을때 호출
	/// </summary>
	public void BuyCoinClearOn(int index)
	{
		int addCoin = 0;

		switch ((ProductIdEnum)index)
		{
			case ProductIdEnum.coin_add_10:
				addCoin = 10;
				break;
			case ProductIdEnum.coin_add_30:
				addCoin = 30;
				break;
		}

		DataManager.Instance.AddCoin(0, addCoin);
	}

	public void WatchAdsOn()
	{
		int addCoin = 10;

		DataManager.Instance.AddCoin(addCoin);
	}
}

[System.Serializable]
public enum ProductIdEnum
{
	coin_add_10 = 0,
	coin_add_30 = 1,
}
