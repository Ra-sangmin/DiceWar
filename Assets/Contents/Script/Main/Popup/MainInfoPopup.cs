using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 정보 팝업. (uGUI Canvas → UI Toolkit 이식)
/// 원본은 프리팹의 Button.onClick 에 이 클래스의 메서드를 직접 물려뒀지만,
/// UI Toolkit 에서는 Awake 에서 코드로 연결한다.
/// </summary>
[RequireComponent(typeof(PanelRenderer))]
public class MainInfoPopup : BasePopup
{
	private PanelUI panelUI;

	private void Awake()
	{
		panelUI = new PanelUI(this, OnUIReady);
	}

	/// <summary> PanelRenderer 의 UI 가 준비되면 한 번 호출된다 (예전 Awake 의 요소 찾기 / 이벤트 등록, 2026-09-29) </summary>
	private void OnUIReady(VisualElement root)
	{
		UnityEngine.UIElements.Button contactBtn = root.Q<UnityEngine.UIElements.Button>("main-info-contact-btn");
		UnityEngine.UIElements.Button termsBtn = root.Q<UnityEngine.UIElements.Button>("main-info-terms-btn");
		UnityEngine.UIElements.Button privacyBtn = root.Q<UnityEngine.UIElements.Button>("main-info-privacy-btn");
		UnityEngine.UIElements.Button rateBtn = root.Q<UnityEngine.UIElements.Button>("main-info-rate-btn");
		UnityEngine.UIElements.Button closeBtn = root.Q<UnityEngine.UIElements.Button>("main-info-close-btn");

		//원본 버튼에는 ButtonSound 가 붙어있었다
		if (contactBtn != null) contactBtn.clicked += () => { PlayClickSe(); ContactUsOn(); };
		if (termsBtn != null) termsBtn.clicked += () => { PlayClickSe(); TermsOfConditionsPopupOn(); };
		if (privacyBtn != null) privacyBtn.clicked += () => { PlayClickSe(); PrivatePolicyPopupOn(); };
		if (rateBtn != null) rateBtn.clicked += () => { PlayClickSe(); RateUsOn(); };
		if (closeBtn != null) closeBtn.clicked += () => { PlayClickSe(); CloseBtnClickOn(); };

		LocalizeTextSet(root);
	}

	private void OnDestroy()
	{
		panelUI?.Dispose();
	}

	void PlayClickSe()
	{
		SoundManager.Instance.PlaySe(SeEnum.Yes);
	}

	static string Loc(int key)
	{
		return LocalizeManager.Instance.GetStrData(LocalizeStatus.MainInfoPopup, key);
	}

	void LocalizeTextSet(VisualElement root)
	{
		root.Q<Label>("main-info-title").text = Loc(0);
		root.Q<Label>("main-info-contact-label").text = Loc(1);
		root.Q<Label>("main-info-terms-label").text = Loc(2);
		root.Q<Label>("main-info-privacy-label").text = Loc(3);
		root.Q<Label>("main-info-rate-label").text = Loc(4);
	}

	public void TermsOfConditionsPopupOn()
	{
		PopupManager.Instance.TermsOfConditionsPopupOn();
	}

	public void PrivatePolicyPopupOn()
	{
		PopupManager.Instance.PrivatePolicyPopupOn();
	}

	public void ContactUsOn()
	{
		string url = "mailto:wyeth123@naver.com";
		Application.OpenURL(url);
	}

	public void RateUsOn()
	{
		string url = "https://play.google.com/store/apps/details?id=com.JaedongKim.DiceandDeal";
		Application.OpenURL(url);
	}
}
