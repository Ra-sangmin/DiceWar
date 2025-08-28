using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainSettingPopup : MonoBehaviour
{
	[SerializeField] Toggle soundToggle;
	[SerializeField] Toggle mapCompensationToggle;

	private void Awake()
	{
        Init();
	}

    void Init()
    {
		soundToggle.isOn = DataManager.Instance.GetSoundOn();
		mapCompensationToggle.isOn = DataManager.Instance.GetMapCompensation();
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
