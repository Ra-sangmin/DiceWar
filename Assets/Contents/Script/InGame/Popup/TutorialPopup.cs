using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UniRx;

/// <summary>
/// 튜토리얼 팝업 (전체 화면). 2026-09-18 Figma 시안에 맞춰 재작성.
///
/// 구성 : 상단(제목 + 공통룰/멀티플레이 룰 탭 + Skip + 닫기)
///        가운데(페이지 이미지) / 하단(제목 + 본문 + n/N + 다음 버튼)
///
/// 페이지 문구는 시안 그대로 코드에 넣었다(현지화 테이블에 없는 새 문구).
/// 이미지는 프리팹의 Sprite 리스트에서 페이지 순서대로 가져온다.
/// </summary>
[RequireComponent(typeof(PanelRenderer))]
public class TutorialPopup : MonoBehaviour
{
	/// <summary> 한 페이지 = 제목 + 본문 + 이미지 </summary>
	class Page
	{
		public readonly string Title;
		public readonly string Body;

		public Page(string title, string body)
		{
			Title = title;
			Body = body;
		}
	}

	//인스펙터에서 페이지 순서대로 넣는다 (공통룰 / 멀티플레이 룰)
	[SerializeField] List<Sprite> commonSpriteList = new List<Sprite>();
	[SerializeField] List<Sprite> multiSpriteList = new List<Sprite>();

	//시안 문구 (공통룰)
	static readonly Page[] commonPages =
	{
		new Page("주사위 랜덤 공격 규칙",
			"내 영토와 닿아있는 상대편의 영토를 공격할 때\n" +
			"랜덤으로 나온 주사위의 총합이 내가 공격하고 싶은 땅의 주사위 총합 보다 크면\n" +
			"영토를 빼앗을 수 있어요."),

		new Page("내 영토 주사위 개수",
			"내 공격이 끝난 후, (연결된 영토 갯수 -1)개의 주사위가 내 영토내에 랜덤으로 분배되어요.\n" +
			"\n" +
			"한 영토당 최대 6개의 주사위가 존재하고, 모든 영토에 6개가 분배된 후\n" +
			"초과된 주사위는 저장소에 저장되어요"),

		new Page("플레이어 패널",
			"내 영토는 패널 외곽에 들어와 있어요. 영토 패널에서는 연결되어 있는 영토 갯수를 확인 할수 있어요.\n" +
			"\n" +
			"게임 종료된 시점에서 연결되어 있는 영토가 제일 많은 유저가 승리해요.\n" +
			"현재 공격 중인 유저는 패널 옆에 화살표로 표기되어요."),

		new Page("영역 선택과 공격",
			"내 영토를 선택하면 공격할 수 있는 상대 영토가 함께 표시되어요.\n" +
			"\n" +
			"주사위가 2개 이상인 영토만 공격할 수 있고,\n" +
			"공격할 상대 영토를 누르면 양쪽 주사위를 굴려 승자를 정해요."),

		new Page("맵 선택과 게임 시작",
			"게임을 시작하기 전에 마음에 드는 맵이 나올 때까지 맵을 바꿀 수 있어요.\n" +
			"\n" +
			"'시작하기'를 누르면 내 색으로 게임이 시작되고,\n" +
			"모든 영토를 정복하면 승리해요."),
	};

	//시안에 없는 탭. 기존 현지화 문구(TutorialPopup 9~13)를 정리해서 넣었다.
	static readonly Page[] multiPages =
	{
		new Page("스킬 카드",
			"멀티 플레이에서는 하단에 '카드'와 '타이머'가 추가되어요.\n" +
			"\n" +
			"타이머가 끝나기 전에 내 차례를 마쳐야 하고,\n" +
			"거래를 하려면 카드를 누르세요."),

		new Page("동맹 제안",
			"플레이어를 선택하고 보상을 설정한 다음 제안해요.\n" +
			"\n" +
			"오른쪽 하단 숫자는 여분의 코인 또는 부족한 코인을 나타내요."),

		new Page("토지 거래",
			"토지를 선택하고, 플레이어를 선택한 다음 가격을 정해서 제안해요."),

		new Page("제안 수락 / 거절",
			"거래가 제안되면 수락하거나 거절할 수 있어요."),

		new Page("배신",
			"동맹을 탈퇴하려면 카드를 사용해 '배신'을 누르세요.\n" +
			"\n" +
			"벌금은 내 동맹 지분과 같아요.\n" +
			"멀티 플레이에서는 나 또는 내 동맹이 모든 맵을 정복하면 승리해요."),
	};

	private PanelUI panelUI;

	private VisualElement pageImage;
	private Label titleLabel;
	private Label pageTitleLabel;
	private Label pageBodyLabel;
	private Label pageIndexLabel;
	private Label skipLabel;

	private readonly List<SMToggleElement> tabToggleList = new List<SMToggleElement>();

	private int tabIndex = 0;
	private int pageIndex = 0;

	private Page[] CurrentPages => tabIndex == 0 ? commonPages : multiPages;
	private List<Sprite> CurrentSprites => tabIndex == 0 ? commonSpriteList : multiSpriteList;

	void Awake()
	{
		panelUI = new PanelUI(this, OnUIReady);
	}

	/// <summary> PanelRenderer 의 UI 가 준비되면 한 번 호출된다 (예전 Awake 의 요소 찾기 / 이벤트 등록, 2026-09-29) </summary>
	private void OnUIReady(VisualElement root)
	{
		pageImage = root.Q<VisualElement>("tutorial-image");
		titleLabel = root.Q<Label>("tutorial-title");
		pageTitleLabel = root.Q<Label>("tutorial-page-title");
		pageBodyLabel = root.Q<Label>("tutorial-page-body");
		pageIndexLabel = root.Q<Label>("tutorial-page-index");
		skipLabel = root.Q<Label>("tutorial-skip-label");

		UnityEngine.UIElements.Button afterBtn = root.Q<UnityEngine.UIElements.Button>("tutorial-after-btn");
		UnityEngine.UIElements.Button skipBtn = root.Q<UnityEngine.UIElements.Button>("tutorial-skip-btn");
		UnityEngine.UIElements.Button closeBtn = root.Q<UnityEngine.UIElements.Button>("tutorial-close-btn");

		if (afterBtn != null) afterBtn.clicked += () => { PlayClickSe(); NextPageOn(); };
		if (skipBtn != null) skipBtn.clicked += () => { PlayClickSe(); CloseBtnClickOn(); };
		if (closeBtn != null) closeBtn.clicked += () => { PlayClickSe(); CloseBtnClickOn(); };

		CreateTabs(root);

		LocalizeTextSet();
	}

	private void OnDestroy()
	{
		panelUI?.Dispose();
	}

	void PlayClickSe()
	{
		SoundManager.Instance.PlaySe(SeEnum.Yes);
	}

	void CreateTabs(VisualElement root)
	{
		VisualElement tabs = root.Q<VisualElement>("tutorial-tabs");

		if (tabs == null)
			return;

		SMToggleGroupElement group = new SMToggleGroupElement();

		string[] texts = { "공통룰", "멀티플레이 룰" };

		for (int i = 0; i < texts.Length; i++)
		{
			int index = i;

			//폭 0 = USS 의 width:auto (글자 길이에 맞춤)
			SMToggleElement toggle = new SMToggleElement(texts[i], 0f, "sm-toggle--auto", "tutorial__tab");
			toggle.SetGroup(group);

			toggle.toggleValue.Subscribe(isOn =>
			{
				if (isOn)
				{
					TabChangeOn(index);
				}
			}).AddTo(gameObject);

			tabs.Add(toggle.Root);
			tabToggleList.Add(toggle);
		}
	}

	void LocalizeTextSet()
	{
		LocalizeManager localize = LocalizeManager.Instance;

		if (titleLabel != null) titleLabel.text = localize.GetStrData(LocalizeStatus.TutorialPopup, 15);
		if (skipLabel != null) skipLabel.text = "Skip";
	}

	/// <summary> 팝업이 열릴 때 호출된다 (PopupManager.TutorialPopupOn) </summary>
	public void TutorialOn()
	{
		//만들자마자 불리므로 UI 가 준비된 뒤에 (PanelRenderer, 2026-09-29)
		panelUI.Run(TutorialOnReady);
	}

	void TutorialOnReady()
	{
		tabIndex = 0;
		pageIndex = 0;

		if (tabToggleList.Count > 0)
		{
			tabToggleList[0].SetToggleValue(true);
		}

		SetData();
	}

	void TabChangeOn(int index)
	{
		tabIndex = index;
		pageIndex = 0;

		SetData();
	}

	void NextPageOn()
	{
		//마지막 페이지에서 다음을 누르면 다음 탭으로, 마지막 탭이면 닫는다
		if (pageIndex >= CurrentPages.Length - 1)
		{
			if (tabIndex < tabToggleList.Count - 1)
			{
				tabToggleList[tabIndex + 1].SetToggleValue(true);
			}
			else
			{
				CloseBtnClickOn();
			}

			return;
		}

		pageIndex++;

		SetData();
	}

	void SetData()
	{
		Page[] pages = CurrentPages;

		pageIndex = Mathf.Clamp(pageIndex, 0, pages.Length - 1);

		Page page = pages[pageIndex];

		if (pageTitleLabel != null) pageTitleLabel.text = page.Title;
		if (pageBodyLabel != null) pageBodyLabel.text = page.Body;
		if (pageIndexLabel != null) pageIndexLabel.text = (pageIndex + 1) + "/" + pages.Length;

		List<Sprite> sprites = CurrentSprites;

		if (pageImage != null)
		{
			if (sprites != null && pageIndex < sprites.Count && sprites[pageIndex] != null)
			{
				pageImage.style.backgroundImage = new StyleBackground(sprites[pageIndex]);
			}
			else
			{
				pageImage.style.backgroundImage = StyleKeyword.None;
			}
		}
	}

	public void CloseBtnClickOn()
	{
		Destroy(gameObject);
	}
}
