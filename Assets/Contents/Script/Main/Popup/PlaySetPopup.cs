using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using UniRx;
using Unity.Mathematics;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 설정 / 플레이 시작 팝업. (uGUI Canvas → UI Toolkit 이식)
/// 원본의 SMToggle / PlayCoinBtn / MyCoinPanel 은 그대로 두고 UI Toolkit 대체본을 쓴다.
/// </summary>
[RequireComponent(typeof(PanelRenderer))]
public class PlaySetPopup : BasePopup
{
	[SerializeField] Sprite playerRandomSprite;
	[SerializeField] List<Sprite> playerSpriteList = new List<Sprite>();
	[SerializeField] RewardedAdsButton rewardedAdsButton;

	private PanelUI panelUI;

	private VisualElement playerImage;
	private VisualElement selectPanel;
	private VisualElement matchingCountRow;
	private VisualElement singleColorPanel;
	private VisualElement multiColorPanel;
	private VisualElement needCoinBG;
	private VisualElement needCoinPanel;

	private Label playerRandomText;
	private Label victoryRewardText;
	private Label needCoinText;
	private Label playLabel;

	private UnityEngine.UIElements.Button playBtn;
	private UnityEngine.UIElements.Button adBtn;

	private readonly List<SMToggleElement> aIToggleList = new List<SMToggleElement>();
	private readonly List<SMToggleElement> mapSizeToggleList = new List<SMToggleElement>();
	private readonly List<SMToggleElement> playersCountToggleList = new List<SMToggleElement>();
	private readonly List<SMToggleElement> matchingCountToggleList = new List<SMToggleElement>();

	private int playerColorIndex = 0;

	//원본 실측 좌표 (토글 묶음의 시작 X)
	//글자 토글은 폭을 글자 길이에 맞춰 자동으로 잡고(USS .sm-toggle--auto), 숫자 토글만 98 고정.
	const float aiGroupLeft = 456.4f;
	const float mapGroupLeft = 457.7f;
	const float countGroupLeft = 458f;
	const float countWidth = 98f;

	private void Awake()
	{
		panelUI = new PanelUI(this, OnUIReady);
	}

	/// <summary> PanelRenderer 의 UI 가 준비되면 한 번 호출된다 (예전 Awake 의 요소 찾기 / 이벤트 등록, 2026-09-29) </summary>
	private void OnUIReady(VisualElement root)
	{
		playerImage = root.Q<VisualElement>("play-set-player-icon");
		selectPanel = root.Q<VisualElement>("play-set-select-panel");
		singleColorPanel = root.Q<VisualElement>("play-set-single-color-panel");
		multiColorPanel = root.Q<VisualElement>("play-set-multi-color-panel");
		needCoinBG = root.Q<VisualElement>("play-set-need-coin-bg");
		needCoinPanel = root.Q<VisualElement>("play-set-need-coin-panel");

		playerRandomText = root.Q<Label>("play-set-random-text");
		victoryRewardText = root.Q<Label>("play-set-reward-value");
		needCoinText = root.Q<Label>("play-set-need-coin-text");
		playLabel = root.Q<Label>("play-set-play-label");

		playBtn = root.Q<UnityEngine.UIElements.Button>("play-set-play-btn");
		adBtn = root.Q<UnityEngine.UIElements.Button>("play-set-ad-btn");

		UnityEngine.UIElements.Button backBtn = root.Q<UnityEngine.UIElements.Button>("play-set-back-btn");
		UnityEngine.UIElements.Button nextBtn = root.Q<UnityEngine.UIElements.Button>("play-set-next-btn");
		UnityEngine.UIElements.Button beforeBtn = root.Q<UnityEngine.UIElements.Button>("play-set-before-btn");

		if (backBtn != null) backBtn.clicked += () => { PlayClickSe(); CloseBtnClickOn(); };
		if (playBtn != null) playBtn.clicked += () => { PlayClickSe(); PlayBtnClickOn(); };
		if (nextBtn != null) nextBtn.clicked += () => { PlayClickSe(); PlayerColorSetBtnOn(true); };
		if (beforeBtn != null) beforeBtn.clicked += () => { PlayClickSe(); PlayerColorSetBtnOn(false); };

		if (rewardedAdsButton != null)
		{
			rewardedAdsButton.InitView(adBtn);
			AdsManager.Instance.SetRewardedAdsButton(rewardedAdsButton);
		}

		PopupMyCoinView.Bind(root, gameObject);

		CreateRows(root);

		SetEvent();
	}

	private void OnDestroy()
	{
		panelUI?.Dispose();
	}

	void PlayClickSe()
	{
		SoundManager.Instance.PlaySe(SeEnum.Yes);
	}

	string Loc(LocalizeStatus status, int key)
	{
		return LocalizeManager.Instance.GetStrData(status, key);
	}

	void CreateRows(VisualElement root)
	{
		root.Q<Label>("play-set-title").text = Loc(LocalizeStatus.PlaySetPopup, 0);
		root.Q<Label>("play-set-color-title").text = Loc(LocalizeStatus.PlaySetPopup, 11);
		root.Q<Label>("play-set-reward-label").text = Loc(LocalizeStatus.PlaySetPopup, 4);
		root.Q<Label>("play-set-ad-label").text = Loc(LocalizeStatus.Common, 2);
		playLabel.text = Loc(LocalizeStatus.Common, 0);

		//원본도 현지화 없이 고정 문구
		string randomStr = Loc(LocalizeStatus.PlaySetPopup, 12);
		playerRandomText.text = randomStr;
		root.Q<Label>("play-set-multi-random-text").text = randomStr;

		string[] aiTexts = { Loc(LocalizeStatus.PlaySetPopup, 5), Loc(LocalizeStatus.PlaySetPopup, 6), Loc(LocalizeStatus.PlaySetPopup, 7) };
		string[] mapTexts = { Loc(LocalizeStatus.PlaySetPopup, 8), Loc(LocalizeStatus.PlaySetPopup, 9), Loc(LocalizeStatus.PlaySetPopup, 10) };
		string[] countTexts = { "2", "3", "4", "5", "6", "7" };

		AddRow(Loc(LocalizeStatus.PlaySetPopup, 1), aiTexts, aiGroupLeft, aIToggleList, true, false);
		AddRow(Loc(LocalizeStatus.PlaySetPopup, 2), mapTexts, mapGroupLeft, mapSizeToggleList, false, false);
		AddRow(Loc(LocalizeStatus.PlaySetPopup, 3), countTexts, countGroupLeft, playersCountToggleList, false, true);
		matchingCountRow = AddRow("매칭 유저 수", countTexts, countGroupLeft, matchingCountToggleList, false, true);
	}

	VisualElement AddRow(string labelText, string[] texts, float groupLeft, List<SMToggleElement> list, bool aiRowOn, bool countRowOn)
	{
		VisualElement row = new VisualElement();
		row.AddToClassList("play-set__row");

		if (selectPanel.childCount == 0)
		{
			row.AddToClassList("play-set__row--first");
		}

		Label label = new Label(labelText);
		label.AddToClassList("play-set__row-label");

		if (aiRowOn)
		{
			label.AddToClassList("play-set__row-label--ai");
		}

		label.pickingMode = PickingMode.Ignore;
		row.Add(label);

		//토글은 가로로 이어 붙인다 (폭/간격은 USS 가 잡는다)
		VisualElement toggleGroup = new VisualElement();
		toggleGroup.AddToClassList("play-set__toggle-group");

		if (countRowOn)
		{
			toggleGroup.AddToClassList("play-set__toggle-group--count");
		}

		toggleGroup.style.left = groupLeft;
		row.Add(toggleGroup);

		SMToggleGroupElement group = new SMToggleGroupElement();

		for (int i = 0; i < texts.Length; i++)
		{
			//숫자 토글만 폭 고정, 글자 토글은 0 을 넘겨 USS 의 width:auto 를 쓴다
			float width = countRowOn ? countWidth : 0f;

			SMToggleElement toggle = new SMToggleElement(texts[i], width, "sm-toggle--auto");

			toggle.SetGroup(group);

			toggleGroup.Add(toggle.Root);
			list.Add(toggle);
		}

		selectPanel.Add(row);

		return row;
	}

	void SetEvent()
	{
		SetToggleEvent(aIToggleList, AIToggleChangeOn);
		SetToggleEvent(mapSizeToggleList, MapSizeToggleChangeOn);
		SetToggleEvent(playersCountToggleList, PlayersCountToggleChangeOn);
		SetToggleEvent(matchingCountToggleList, MatchingCountToggleChangeOn);

		DataManager.Instance.CheckUserData();

		DataManager.Instance.userData.myCoin
			.Subscribe(_ => SetNeedCoinCheck())
			.AddTo(gameObject);
	}

	private void SetToggleEvent(List<SMToggleElement> toggleList, UnityAction<int> toggleEventOn)
	{
		for (int i = 0; i < toggleList.Count; i++)
		{
			int index = i;

			toggleList[i].toggleValue.Subscribe(isOn =>
			{
				if (isOn)
				{
					toggleEventOn(index);
				}
			}).AddTo(gameObject);
		}
	}

	public void InitOn(bool multiOn)
	{
		DataManager.Instance.isMultiOn = multiOn;

		//화면 구성은 UI 가 준비된 뒤에 (PanelRenderer, 2026-09-29)
		panelUI.Run(() => InitOnReady(multiOn));
	}

	void InitOnReady(bool multiOn)
	{

		SetActive(singleColorPanel, !multiOn);
		SetActive(multiColorPanel, multiOn);
		//매칭 유저 수 줄은 숨긴다 (2026-09-28). 값은 SetMatchingCountToggle 이 플레이어 수와 같게 자동으로 맞춘다
		SetActive(matchingCountRow, false);

		playerColorIndex = -1;
		DataManager.Instance.randomPositionOn = false;
		playerImage.style.backgroundImage = new StyleBackground(playerRandomSprite);

		//멀티에서는 MultiColorPanel 쪽 안내문구만 쓴다
		SetActive(playerRandomText, false);

		//원본은 프리팹에 저장된 SMToggle 값으로 초기 선택이 정해졌다.
		//UI Toolkit 판은 요소를 런타임에 만들므로 현재 설정값으로 직접 맞춘다.
		SetToggleFromData();

		if (multiOn)
		{
			SetMultiToggle();
		}
		else
		{
			SetColorData();
		}
	}

	/// <summary> 현재 DataManager 설정값에 맞춰 토글 선택 상태를 맞춘다 </summary>
	void SetToggleFromData()
	{
		SelectToggle(aIToggleList, (int)DataManager.Instance.aiLevel);
		SelectToggle(mapSizeToggleList, (int)DataManager.Instance.mapSizeEnum);
		SelectToggle(playersCountToggleList, DataManager.Instance.num_player - 2);

		SetVictoryRewardText();
		SetNeedCoinCheck();
	}

	static void SelectToggle(List<SMToggleElement> list, int index)
	{
		if (list.Count == 0)
			return;

		index = Mathf.Clamp(index, 0, list.Count - 1);

		list[index].toggleValue.SetValueAndForceNotify(true);
	}

	static void SetActive(VisualElement element, bool activeOn)
	{
		if (element == null)
			return;

		element.style.display = activeOn ? DisplayStyle.Flex : DisplayStyle.None;
	}

	void SetMultiToggle()
	{
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || DEV
		SetToggleActive(aIToggleList, 0);
		SetToggleActive(mapSizeToggleList, 0);
		SetToggleActive(playersCountToggleList, 0);
		SetToggleActive(matchingCountToggleList, 0);
#endif
		SetMatchingCountToggle();
		SetNeedCoinCheck();
	}

	public void SetToggleActive(List<SMToggleElement> toggleList, int activeIndex)
	{
		for (int i = 0; i < toggleList.Count; i++)
		{
			bool interactableOn = i >= activeIndex;
			toggleList[i].interactable.Value = interactableOn;
		}

		toggleList[activeIndex].toggleValue.Value = true;
	}

	private void AIToggleChangeOn(int index)
	{
		AILevel aiLevel = (AILevel)(index);

		if (aIToggleList[index].toggleValue.Value == false)
		{
			aIToggleList[index].toggleValue.Value = true;
			return;
		}

		DataManager.Instance.SetAILevelEnum(aiLevel);

		if (DataManager.Instance.isMultiOn == false)
		{
			SetMapSizeToggle();
		}

		SetVictoryRewardText();
	}

	void SetMapSizeToggle()
	{
		int activeCount = 0;

		AILevel aiLevel = DataManager.Instance.aiLevel;

		switch (aiLevel)
		{
			case AILevel.Easy: activeCount = 1; break;
			case AILevel.Normal: activeCount = 1; break;
			case AILevel.Hard: activeCount = 2; break;
		}

		for (int i = 0; i < mapSizeToggleList.Count; i++)
		{
			bool interactableOn = i <= activeCount;
			mapSizeToggleList[i].interactable.Value = interactableOn;
		}

		if ((int)DataManager.Instance.mapSizeEnum > activeCount)
		{
			MapSizeToggleChangeOn(activeCount);
		}

		SetNeedCoinCheck();
	}

	private void MapSizeToggleChangeOn(int index)
	{
		MapSizeEnum mapSizeEnum = (MapSizeEnum)index;

		if (mapSizeToggleList[index].toggleValue.Value == false)
		{
			mapSizeToggleList[index].toggleValue.Value = true;
			return;
		}

		DataManager.Instance.SetMapSizeEnum(mapSizeEnum);

		if (DataManager.Instance.isMultiOn == false)
		{
			SetPlayerSelectToggle();
		}
	}

	void SetPlayerSelectToggle()
	{
		int activeCount = DataManager.Instance.ActivePlayerCount();

		for (int i = 0; i < playersCountToggleList.Count; i++)
		{
			bool interactableOn = i <= activeCount;
			playersCountToggleList[i].interactable.SetValueAndForceNotify(interactableOn);
		}

		int playerMaxCnt = DataManager.Instance.num_player;

		playerMaxCnt -= 2;

		if (playerMaxCnt > activeCount)
		{
			PlayersCountToggleChangeOn(activeCount);
		}
	}

	private void PlayersCountToggleChangeOn(int index)
	{
		int playerMaxCnt = index + 2;

		if (playersCountToggleList[index].toggleValue.Value == false)
		{
			playersCountToggleList[index].toggleValue.Value = true;
			return;
		}

		DataManager.Instance.SetPlayerMaxCnt(playerMaxCnt);

		if (DataManager.Instance.isMultiOn)
		{
			SetMatchingCountToggle();
		}

		if (DataManager.Instance.randomPositionOn == false)
		{
			SetTurnPosition(DataManager.Instance.turnPosition);

			if (playerColorIndex >= DataManager.Instance.num_player)
			{
				playerColorIndex = DataManager.Instance.num_player - 1;
				SetColorData();
			}
		}

		SetVictoryRewardText();
	}

	private void SetMatchingCountToggle()
	{
		int activeIndex = DataManager.Instance.num_player - 2;

		for (int i = 0; i < matchingCountToggleList.Count; i++)
		{
			bool interactableOn = i <= activeIndex;
			matchingCountToggleList[i].interactable.SetValueAndForceNotify(interactableOn);
		}

		MatchingCountToggleChangeOn(activeIndex);
	}

	private void MatchingCountToggleChangeOn(int index)
	{
		int matchingUserCnt = index + 2;

		if (matchingCountToggleList[index].toggleValue.Value == false)
		{
			matchingCountToggleList[index].toggleValue.Value = true;
			return;
		}

		DataManager.Instance.SetMatchingUserCnt(matchingUserCnt);
	}

	void SetVictoryRewardText()
	{
		int rewardCoin = DataManager.Instance.GetRewardCoin();
		//뒤에 코인 아이콘(play-set-reward-coin)이 붙으므로 숫자만 넣는다
		victoryRewardText.SetNumber(rewardCoin);
	}

	/// <summary> 원본 PlayCoinBtn.SetNeedCoinCheck 과 동일한 로직 </summary>
	/// <summary> 원본 PlayCoinBtn.SetNeedCoinCheck 과 동일한 로직 </summary>
	void SetNeedCoinCheck()
	{
		int needCoin = DataManager.Instance.GetNeedCoin();

		bool needCoinOn = DataManager.Instance.userData.myCoin.Value < needCoin;

		if (playBtn != null)
		{
			playBtn.style.display = needCoinOn ? DisplayStyle.None : DisplayStyle.Flex;
		}

		//광고 버튼은 시작하기 버튼 바로 뒤에 겹쳐 있다.
		//눌렀을 때 시작하기 버튼이 :active 로 0.97 배 줄어들면 뒤의 밝은 btn_bg4 가
		//테두리처럼 비쳐 보이므로, 쓰지 않을 때는 아예 숨긴다 (2026-09-18)
		if (adBtn != null)
		{
			adBtn.style.display = needCoinOn ? DisplayStyle.Flex : DisplayStyle.None;
		}

		//쉬움 난이도는 무료라 가격 배지를 숨긴다.
		bool coinTextOn = DataManager.Instance.aiLevel != AILevel.Easy;

		if (needCoinPanel != null)
		{
			needCoinPanel.style.display = coinTextOn ? DisplayStyle.Flex : DisplayStyle.None;

			if (coinTextOn && needCoinText != null)
			{
				needCoinText.text = $"-{needCoin}";
			}
		}

		//배지가 있을 때는 배지를 피해 왼쪽으로 치우쳐 있어야 하고,
		//배지가 없으면 버튼 전체 기준으로 가운데 와야 한다.
		if (playLabel != null)
		{
			playLabel.EnableInClassList("play-set__play-label--full", !coinTextOn);
		}

		SetActive(needCoinBG, needCoinOn);
	}

	private void SetTurnPosition(int turnCount)
	{
		turnCount = math.clamp(turnCount, 0, DataManager.Instance.num_player - 1);

		DataManager.Instance.SetTurnPosition(turnCount);
	}

	public void PlayerColorSetBtnOn(bool nextOn)
	{
		if (nextOn)
		{
			playerColorIndex++;

			if (playerColorIndex >= DataManager.Instance.num_player)
			{
				playerColorIndex = -1;
			}
		}
		else
		{
			playerColorIndex--;

			if (playerColorIndex < -1)
			{
				playerColorIndex = DataManager.Instance.num_player - 1;
			}
		}

		SetColorData();
	}

	void SetColorData()
	{
		if (playerColorIndex == -1)
		{
			playerImage.style.backgroundImage = new StyleBackground(playerRandomSprite);
			DataManager.Instance.randomPositionOn = true;
		}
		else
		{
			playerImage.style.backgroundImage = new StyleBackground(playerSpriteList[playerColorIndex]);
			DataManager.Instance.SetCurrentPlayerColor(playerColorIndex);
			DataManager.Instance.randomPositionOn = false;
		}

		SetActive(playerRandomText, DataManager.Instance.randomPositionOn);
	}

	public void PlayBtnClickOn()
	{
		DataManager.Instance.AddCoin(-DataManager.Instance.GetNeedCoin());
		SceneManager.LoadScene("Loading");
	}

	public void AdPlayBtnClickOn()
	{
		int addCoin = 10;

		DataManager.Instance.AddCoin(addCoin);

		SetNeedCoinCheck();
	}
}
