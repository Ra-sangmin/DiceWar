using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 약관 / 개인정보처리방침 팝업의 공통 본체. (uGUI Canvas → UI Toolkit 이식)
/// TeamOfConditionsPopup / PrivatePolicyPopup 이 그대로 상속해서 쓴다.
///
/// 본문은 `Assets/Contents/MDFile` 의 마크다운(final_terms_* / final_privacy_*)을
/// 리치텍스트로 바꿔서 그대로 보여준다. 언어 설정에 따라 kr / en 을 고른다.
/// 원본은 TextMeshProUGUI + TMPLinkClickHandler 로 <link> 클릭을 받았는데,
/// UI Toolkit 도 PointerUpLinkTagEvent 로 같은 <link> 태그를 잡을 수 있다.
/// </summary>
[RequireComponent(typeof(PanelRenderer))]
public class InfoPopup : BasePopup
{
	// Assets/Contents/MDFile 의 마크다운을 드래그해서 연결한다. (원본 직렬화 필드 이름 그대로)
	[SerializeField] protected TextAsset krTextAsset;
	[SerializeField] protected TextAsset enTextAsset;

	/// <summary> 제목에 쓸 현지화 키 (MainInfoPopup: 2=이용약관, 3=개인정보처리방침) </summary>
	[SerializeField] protected int titleLocalizeKey = 2;

	private PanelUI panelUI;
	/// <summary> 본문 문서가 들어갈 자리 (InfoDocumentBuilder 가 블록별 요소로 채운다, 2026-09-29) </summary>
	private VisualElement contentsRoot;

	/// <summary> 마우스로 끌어서 스크롤 (터치는 ScrollView 기본 동작, 2026-09-29) </summary>
	private ScrollViewDragScroller dragScroller;

	protected virtual void Awake()
	{
		panelUI = new PanelUI(this, OnUIReady);
	}

	/// <summary> PanelRenderer 의 UI 가 준비되면 한 번 호출된다 (예전 Awake 의 요소 찾기 / 이벤트 등록, 2026-09-29) </summary>
	protected virtual void OnUIReady(VisualElement root)
	{
		ScrollView scroll = root.Q<ScrollView>("info-scroll");
		if (scroll != null)
		{
			scroll.mode = ScrollViewMode.Vertical;
			//오른쪽 세로 스크롤 바 (모양은 PopupView.uss 의 .info__scroll .unity-scroller--vertical, 2026-09-29)
			scroll.verticalScrollerVisibility = ScrollerVisibility.Auto;
			scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

			//원본 Content 의 VerticalLayoutGroup (padding 48/50, 가운데 정렬)
			scroll.contentContainer.style.paddingTop = 48;
			scroll.contentContainer.style.paddingBottom = 50;
			scroll.contentContainer.style.alignItems = Align.Center;

			//휠뿐 아니라 마우스 드래그로도 스크롤 (2026-09-29)
			dragScroller = new ScrollViewDragScroller(scroll);
		}

		root.Q<Label>("info-title").text =
			LocalizeManager.Instance.GetStrData(LocalizeStatus.MainInfoPopup, titleLocalizeKey);

		contentsRoot = root.Q<VisualElement>("info-contents");

		UnityEngine.UIElements.Button backBtn = root.Q<UnityEngine.UIElements.Button>("info-back-btn");
		if (backBtn != null)
		{
			backBtn.clicked += () => { SoundManager.Instance.PlaySe(SeEnum.Yes); CloseBtnClickOn(); };
		}

		SetContentsText();
	}

	protected virtual void OnDestroy()
	{
		panelUI?.Dispose();
	}

	void LinkClickOn(UnityEngine.UIElements.Experimental.PointerUpLinkTagEvent evt)
	{
		//끌어서 스크롤하다 링크 위에서 놓은 경우는 클릭이 아니다
		if (dragScroller != null && dragScroller.WasDragged)
			return;

		if (!string.IsNullOrEmpty(evt.linkID))
		{
			Application.OpenURL(evt.linkID);
		}
	}

	public void SetContentsText()
	{
		if (krTextAsset == null || enTextAsset == null || contentsRoot == null)
		{
			Debug.LogError("약관 TextAsset 또는 본문 Label 이 연결되지 않았습니다.");
			return;
		}

		// 언어 설정에 따라 MDFile 의 마크다운을 통째로 불러온다.
		string text = LocalizeManager.Instance.language.Value == SystemLanguage.Korean
			? krTextAsset.text
			: enTextAsset.text;

		//제목·문단·목록·문의를 각각 요소로 만들어 읽기 좋게 (링크는 원본 TMPLinkClickHandler 와 같은 동작)
		InfoDocumentBuilder.Build(contentsRoot, text, LinkClickOn);
	}
}
