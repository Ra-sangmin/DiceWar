using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UniRx;

/// <summary>
/// 메인 설정 팝업. (uGUI Canvas → UI Toolkit 이식)
/// 원본의 SMToggle 은 그대로 두고 UI Toolkit 대체본(SMToggleElement)을 쓴다.
/// </summary>
[RequireComponent(typeof(PanelRenderer))]
public class MainSettingPopup : BasePopup
{
	private PanelUI panelUI;

	private VisualElement soundCheck;
	private readonly List<SMToggleElement> langToggleList = new List<SMToggleElement>();

	private bool soundOn = true;

	//원본 LanguageToggle_0 / _1 실측 좌표 (LanguagePanel 기준)
	static readonly float[] langLeft = { 648.7f, 916f };
	const float langWidth = 250f;

	private void Awake()
	{
		panelUI = new PanelUI(this, OnUIReady);
	}

	/// <summary> PanelRenderer 의 UI 가 준비되면 한 번 호출된다 (예전 Awake 의 요소 찾기 / 이벤트 등록, 2026-09-29) </summary>
	private void OnUIReady(VisualElement root)
	{
		soundCheck = root.Q<VisualElement>("main-setting-sound-check");

		UnityEngine.UIElements.Button soundBtn = root.Q<UnityEngine.UIElements.Button>("main-setting-sound-btn");
		UnityEngine.UIElements.Button logoutBtn = root.Q<UnityEngine.UIElements.Button>("main-setting-logout-btn");
		UnityEngine.UIElements.Button closeBtn = root.Q<UnityEngine.UIElements.Button>("main-setting-close-btn");

		if (soundBtn != null) soundBtn.clicked += () => SoundToggleChangeOn(!soundOn);
		if (logoutBtn != null) logoutBtn.clicked += () => { PlayClickSe(); LogOutBtnClickOn(); };
		if (closeBtn != null) closeBtn.clicked += () => { PlayClickSe(); CloseBtnClickOn(); };

		LocalizeTextSet(root);

		CreateLangToggle(root);

		Init();
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

	void LocalizeTextSet(VisualElement root)
	{
		root.Q<Label>("main-setting-title").text = Loc(LocalizeStatus.MainSettingPopup, 0);
		root.Q<Label>("main-setting-sound-label").text = Loc(LocalizeStatus.MainSettingPopup, 1);
		root.Q<Label>("main-setting-lang-label").text = Loc(LocalizeStatus.MainSettingPopup, 4);
		root.Q<Label>("main-setting-logout-label").text = Loc(LocalizeStatus.MainSettingPopup, 5);
	}

	void CreateLangToggle(VisualElement root)
	{
		VisualElement langRow = root.Q<VisualElement>("main-setting-lang-row");

		SMToggleGroupElement group = new SMToggleGroupElement();

		string[] texts = { "한국어", "English" };

		for (int i = 0; i < texts.Length; i++)
		{
			SMToggleElement toggle = new SMToggleElement(texts[i], langWidth, "sm-toggle--lang");
			toggle.Root.style.left = langLeft[i];
			toggle.SetGroup(group);

			langRow.Add(toggle.Root);
			langToggleList.Add(toggle);
		}
	}

	void Init()
	{
		soundOn = DataManager.Instance.GetSoundOn();
		SetSoundCheck();

		SetLangToggle();
	}

	void SetSoundCheck()
	{
		if (soundCheck != null)
		{
			soundCheck.style.display = soundOn ? DisplayStyle.Flex : DisplayStyle.None;
		}
	}

	private void SetLangToggle()
	{
		switch (LocalizeManager.Instance.language.Value)
		{
			case SystemLanguage.Korean: langToggleList[0].toggleValue.Value = true; break;
			case SystemLanguage.English: langToggleList[1].toggleValue.Value = true; break;
		}

		for (int i = 0; i < langToggleList.Count; i++)
		{
			int index = i;

			langToggleList[i].toggleValue.Subscribe(isOn =>
			{
				if (isOn)
				{
					LangToggleChangeOn(index);
				}
			}).AddTo(gameObject);
		}
	}

	private void LangToggleChangeOn(int index)
	{
		SystemLanguage lang = SystemLanguage.Korean;

		switch (index)
		{
			case 0: lang = SystemLanguage.Korean; break;
			case 1: lang = SystemLanguage.English; break;
		}

		LocalizeManager.Instance.SaveLang(lang);

		LocalizeTextSet(panelUI.Root);
	}

	public void SoundToggleChangeOn(bool isValue)
	{
		soundOn = isValue;

		SetSoundCheck();

		SoundManager.Instance.PlaySe(SeEnum.Yes);
		DataManager.Instance.SetSoundOn(isValue);
	}

	public void MapCompensationToggleChangeOn(bool isValue)
	{
		SoundManager.Instance.PlaySe(SeEnum.Yes);
		DataManager.Instance.SetMapCompensation(isValue);
	}

	public void LogOutBtnClickOn()
	{
		OkPopup okPopup = PopupManager.Instance.LogOutPopupOn();

		string contensStr = LocalizeManager.Instance.GetStrData(LocalizeStatus.MainSettingPopup, 6);
		string okBtnStr = LocalizeManager.Instance.GetStrData(LocalizeStatus.MainSettingPopup, 5);
		string cancelBtnStr = LocalizeManager.Instance.GetStrData(LocalizeStatus.Common, 1);

		okPopup.DataSet(contensStr, () => SceneManager.LoadScene("Intro"), null, false, true, okBtnStr, cancelBtnStr);
	}
}
