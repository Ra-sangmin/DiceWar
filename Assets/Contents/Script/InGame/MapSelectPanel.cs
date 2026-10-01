using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using UniRx;

/// <summary>
/// 게임 시작 전 하단 맵 선택 바. (uGUI Canvas → UI Toolkit 이식)
/// 원본의 PlayCoinBtn 은 Main 씬 PlaySetPopup 프리팹과 공유하므로 건드리지 않고,
/// 코인 체크 로직만 이 클래스 안에 동일하게 옮겼다.
/// </summary>
public class MapSelectPanel : MonoBehaviour
{
	[SerializeField] RewardedAdsButton rewardedAdsButton;

	private VisualElement panelRoot;
	private VisualElement playerIconImage;
	private Label playerLabel;
	private Label adLabel;
	private Label playLabel;
	private Label yesLabel;
	private Label needCoinText;
	private VisualElement needCoinPanel;
	private UnityEngine.UIElements.Button adBtn;
	private UnityEngine.UIElements.Button playBtn;
	private UnityEngine.UIElements.Button yesBtn;

	private int iconColorIndex = -1;

	/// <summary> 시작하기 버튼 (원본 yesBtn → InGameControllerSingle.GameStartOn) </summary>
	public UnityAction startClickOn = () => { };

	/// <summary> 맵 변경 버튼 (원본 PlayBtn → PlayCoinBtn → InGameControllerSingle.NewGameCheckOn) </summary>
	public UnityAction changeClickOn = () => { };

	/// <summary> UI Toolkit 요소 연결 </summary>
	public void InitView(VisualElement root)
	{
		panelRoot = root.Q<VisualElement>("map-select-panel");

		playerIconImage = root.Q<VisualElement>("map-select-player-icon");
		playerLabel = root.Q<Label>("map-select-player-label");
		adLabel = root.Q<Label>("map-select-ad-label");
		playLabel = root.Q<Label>("map-select-play-label");
		yesLabel = root.Q<Label>("map-select-yes-label");
		needCoinText = root.Q<Label>("map-select-need-coin-text");
		needCoinPanel = root.Q<VisualElement>("map-select-need-coin-panel");

		adBtn = root.Q<UnityEngine.UIElements.Button>("map-select-ad-btn");
		playBtn = root.Q<UnityEngine.UIElements.Button>("map-select-play-btn");
		yesBtn = root.Q<UnityEngine.UIElements.Button>("map-select-yes-btn");

		if (yesBtn != null)
		{
			yesBtn.clicked += () => { PlayClickSe(); startClickOn(); };
		}

		if (playBtn != null)
		{
			playBtn.clicked += () => { PlayClickSe(); changeClickOn(); };
		}

		//광고 버튼은 RewardedAdsButton 이 직접 클릭 이벤트를 붙인다
		if (rewardedAdsButton != null)
		{
			rewardedAdsButton.InitView(adBtn);
			AdsManager.Instance.SetRewardedAdsButton(rewardedAdsButton);
		}

		LocalizeTextSet();

		LocalizeManager.Instance.language
			.Subscribe(_ => LocalizeTextSet())
			.AddTo(gameObject);

		if (DataManager.Instance.userData == null)
		{
			DataManager.Instance.userData = new UserData();
		}

		DataManager.Instance.userData.myCoin
			.Subscribe(_ => SetData())
			.AddTo(gameObject);
	}

	/// <summary> 원본 ButtonSound 컴포넌트 역할 </summary>
	void PlayClickSe()
	{
		SoundManager.Instance.PlaySe(SeEnum.Yes);
	}

	/// <summary> 문구 현지화 (원본 LocalizeText 컴포넌트 역할) </summary>
	void LocalizeTextSet()
	{
		LocalizeManager localize = LocalizeManager.Instance;

		if (playerLabel != null)
		{
			playerLabel.text = localize.GetStrData(LocalizeStatus.Game, 0);
		}

		if (playLabel != null)
		{
			playLabel.text = localize.GetStrData(LocalizeStatus.Game, 1);
		}

		if (yesLabel != null)
		{
			yesLabel.text = localize.GetStrData(LocalizeStatus.Common, 0);
		}

		if (adLabel != null)
		{
			adLabel.text = localize.GetStrData(LocalizeStatus.Common, 2);
		}
	}

	public void SetPanelActive(bool activeOn)
	{
		if (panelRoot != null)
		{
			panelRoot.style.display = activeOn ? DisplayStyle.Flex : DisplayStyle.None;
		}
	}

	public void SetData()
	{
		SetNeedCoinCheck();
		SetPlayerIcon();
	}

	/// <summary> 원본 PlayCoinBtn.SetNeedCoinCheck 과 동일한 로직 </summary>
	public bool SetNeedCoinCheck()
	{
		if (DataManager.Instance.userData == null)
		{
			DataManager.Instance.userData = new UserData();
		}

		int needCoin = DataManager.Instance.GetNeedCoin();

		bool needCoinOn = DataManager.Instance.userData.myCoin.Value < needCoin;

		//코인이 모자라면 PlayBtn 을 숨겨 아래의 광고 버튼이 드러나게 한다
		if (playBtn != null)
		{
			playBtn.style.display = needCoinOn ? DisplayStyle.None : DisplayStyle.Flex;
		}

		//광고 버튼은 PlayBtn 바로 뒤에 겹쳐 있다.
		//눌렀을 때 PlayBtn 이 :active 로 0.97 배 줄어들면 뒤의 밝은 btn_bg4 가
		//테두리처럼 비쳐 보이므로, 쓰지 않을 때는 아예 숨긴다 (2026-09-18)
		if (adBtn != null)
		{
			adBtn.style.display = needCoinOn ? DisplayStyle.Flex : DisplayStyle.None;
		}

		if (needCoinPanel != null)
		{
			bool coinTextOn = DataManager.Instance.aiLevel != AILevel.Easy;

			needCoinPanel.style.display = coinTextOn ? DisplayStyle.Flex : DisplayStyle.None;

			if (coinTextOn && needCoinText != null)
			{
				needCoinText.text = $"-{needCoin}";
			}
		}

		return needCoinOn;
	}

	void SetPlayerIcon()
	{
		if (playerIconImage == null)
			return;

		PlayerData playerData = DataManager.Instance.GetMyPlayerData();

		if (playerData == null)
			return;

		int colorIndex = DataManager.Instance.GetPlayerColorIndex(playerData.pe);

		if (iconColorIndex == colorIndex)
			return;

		if (iconColorIndex >= 0)
		{
			playerIconImage.RemoveFromClassList("map-select__player-icon--" + iconColorIndex);
		}

		iconColorIndex = colorIndex;

		playerIconImage.AddToClassList("map-select__player-icon--" + iconColorIndex);
	}
}
