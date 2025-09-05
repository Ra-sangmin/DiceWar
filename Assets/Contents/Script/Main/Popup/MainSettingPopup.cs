using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UniRx;
using Cysharp.Threading.Tasks;
using static Unity.VisualScripting.Member;

public class MainSettingPopup : MonoBehaviour
{
	[SerializeField] Toggle soundToggle;
	[SerializeField] Toggle mapCompensationToggle;
	[SerializeField] List<SMToggle> langToggleList = new List<SMToggle>();

	private void Awake()
	{
        Init();
	}

    void Init()
    {
		soundToggle.isOn = DataManager.Instance.GetSoundOn();
		mapCompensationToggle.isOn = DataManager.Instance.GetMapCompensation();

		SetLangToggle();
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
	}

	// Start is called before the first frame update
	void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SoundToggleChangeOn(bool isValue)
    {
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
		SceneManager.LoadScene("Intro");
	}
}
