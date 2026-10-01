using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UniRx;

[RequireComponent(typeof(PanelRenderer))]
public class MainController : MonoBehaviour
{
	private PanelUI panelUI;

	private Button singlePlayBtn;
	private Button multiPlayBtn;
	private Button tutorialBtn;
	private Button getCoinBtn;
	private Button infoBtn;
	private Button settingBtn;
	private Button diceSettingBtn;

	private Label singlePlayLabel;
	private Label multiPlayLabel;
	private Label tutorialLabel;
	private Label getCoinLabel;
	private Label coinLabel;

	private void Awake()
	{
		panelUI = new PanelUI(this, OnUIReady);
	}

	/// <summary> PanelRenderer 의 UI 가 준비되면 한 번 호출된다 (2026-09-29) </summary>
	void OnUIReady(VisualElement root)
	{
		SetUI(root);

		SetEvent();
	}

	private void OnDestroy()
	{
		panelUI?.Dispose();
	}

	// Start is called before the first frame update
	void Start()
	{

		//멀티 중 앱을 벗어나 퇴장 처리된 경우 안내
		if (string.IsNullOrEmpty(DataManager.Instance.mainToastText) == false)
		{
			PopupManager.Instance.InGameWarningPopupOn(DataManager.Instance.mainToastText);
			DataManager.Instance.mainToastText = string.Empty;
		}

		SoundManager.Instance.PlayBGM(BGMEnum.Intro);

		PopupManager.Instance.SetPopupParant();

		ServerManager.Instance.Init();

		//SetUI / SetEvent 는 UI 가 준비되면 OnUIReady 에서 (PanelRenderer, 2026-09-29)
	}

	void SetUI(VisualElement root)
	{

		singlePlayBtn = root.Q<Button>("single-play-btn");
		multiPlayBtn = root.Q<Button>("multi-play-btn");
		tutorialBtn = root.Q<Button>("tutorial-btn");
		getCoinBtn = root.Q<Button>("get-coin-btn");
		infoBtn = root.Q<Button>("info-btn");
		settingBtn = root.Q<Button>("setting-btn");
		diceSettingBtn = root.Q<Button>("dice-setting-btn");

		singlePlayLabel = root.Q<Label>("single-play-label");
		multiPlayLabel = root.Q<Label>("multi-play-label");
		tutorialLabel = root.Q<Label>("tutorial-label");
		getCoinLabel = root.Q<Label>("get-coin-label");
		coinLabel = root.Q<Label>("coin-text");

		singlePlayBtn.clicked += () => { PlayClickSe(); SinglePlaySetPopupOn(); };
		multiPlayBtn.clicked += () => { PlayClickSe(); MultiPlaySetPopupOn(); };
		tutorialBtn.clicked += () => { PlayClickSe(); TutorialPopupOn(); };
		//Get Coin 버튼은 기획 시트에 따라 삭제 (2026-09-26) - UXML 에 없으면 null
		if (getCoinBtn != null)
			getCoinBtn.clicked += () => { PlayClickSe(); BuyCoinPopupOpen(); };
		infoBtn.clicked += () => { PlayClickSe(); InfoPopupOn(); };
		settingBtn.clicked += () => { PlayClickSe(); SettingPopupOn(); };
		diceSettingBtn.clicked += DiceSetPopupOn;
	}

	void SetEvent()
	{
		//언어 변경 시 버튼 문구 갱신 (기존 LocalizeText 컴포넌트 역할)
		LocalizeManager.Instance.language
			.Subscribe(_ => LocalizeTextSet())
			.AddTo(gameObject);

		//보유 코인 갱신 (기존 MyCoinPanel 역할)
		DataManager.Instance.CheckUserData();

		DataManager.Instance.userData.myCoin
			.Subscribe(_ => SetCoin())
			.AddTo(gameObject);

		SetCoin();
	}

	void LocalizeTextSet()
	{
		LocalizeManager localize = LocalizeManager.Instance;

		singlePlayLabel.text = localize.GetStrData(LocalizeStatus.Main, 0);
		multiPlayLabel.text = localize.GetStrData(LocalizeStatus.Main, 1);
		tutorialLabel.text = localize.GetStrData(LocalizeStatus.Main, 2);
		if (getCoinLabel != null)
			getCoinLabel.text = localize.GetStrData(LocalizeStatus.BuyCoinPopup, 3);
	}

	void SetCoin()
	{
		coinLabel.SetNumber(DataManager.Instance.userData.myCoin.Value);
	}

	void PlayClickSe()
	{
		SoundManager.Instance.PlaySe(SeEnum.Yes);
	}

	// Update is called once per frame
	void Update()
	{

	}

	public void PlayBtnClickOn()
	{
		SceneManager.LoadScene("Game");
	}

	public void SettingPopupOn()
	{
		PopupManager.Instance.MainSettingPopupOn();
	}

	public void InfoPopupOn()
	{
		PopupManager.Instance.MainInfoPopupOn();
	}

	public void BuyCoinPopupOpen()
	{
		PopupManager.Instance.BuyCoinPopupOn();
	}

	public void SinglePlaySetPopupOn()
	{
		PopupManager.Instance.PlaySetPopupOn(false);
	}

	public void MultiPlaySetPopupOn()
	{
		PopupManager.Instance.PlaySetPopupOn(true);
	}

	/// <summary> 튜토리얼 버튼 : 인게임과 같은 튜토리얼 팝업을 연다 (2026-09-18) </summary>
	public void TutorialPopupOn()
	{
		PopupManager.Instance.TutorialPopupOn();
	}

	/// <summary> 좌하단 숨김 핫스팟 : 개발용 주사위 설정 팝업 (2026-09-20 부터 런타임 생성) </summary>
	public void DiceSetPopupOn()
	{
		PopupManager.Instance.DiceSetPopupOn();
	}
}
