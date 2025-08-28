using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 사운드를 관리하는 클래스
/// </summary>
public class SoundManager : MonoSingleton<SoundManager>
{
	private AudioSource bgmAudio;
	private List<AudioSource> seAudioList = new List<AudioSource>();

    public float masterValue = 1;
    public float bgmValue = 0.4f;
	public float seValue = 1;
	public bool soundOff = false;

	private BGMEnum currentBGMEnum = BGMEnum.None;

	/// <summary>
	/// 초기화
	/// </summary>
	public override void Init()
	{
		BgmVolumeReset();
		SeVolumeReset();
	}

	/// <summary>
	/// 믐앱 - 갤러리 종료 시 배경음악 종료
	/// </summary>
	public void BgmStop()
    {
		bgmAudio.Stop();
    }

	public void BgmPause()
    {
		bgmAudio.Pause();
    }

	public void BgmPlay()
    {
		bgmAudio.Play();
    }

    /// <summary>
    /// 마스터 볼륨 출력값 수정
    /// </summary>
    public void MasterValueReset(float value)
    {
        masterValue = value;
        //PlayerPrefs.SetFloat("masterValue", bgmValue);
        AudioListener.volume = masterValue;
    }

	/// <summary>
	/// BGM의 출력값 수정
	/// </summary>
	/// <param name="value"></param>
	public void BgmValueReset(float value)
	{
		bgmValue = value;
		PlayerPrefs.SetFloat("bgmValue", bgmValue);
		BgmVolumeReset();
	}

	public void SoundOffOn(bool soundOff)
	{
		this.soundOff = soundOff;

		masterValue = this.soundOff ? 0 : 1;

		AudioListener.volume = masterValue;
	}

	/// <summary>
	/// bgmAudio의 볼륨값 수정
	/// </summary>
	void BgmVolumeReset()
	{
		if (bgmAudio == null)
			bgmAudio = gameObject.AddComponent<AudioSource>();

		bgmAudio.volume = bgmValue;
	}

	/// <summary>
	/// SE의 출력값 수정
	/// </summary>
	/// <param name="value"></param>
	public void SeValueReset(float value)
	{
		seValue = value;
		PlayerPrefs.SetFloat("seValue", seValue);
		SeVolumeReset();
	}

	/// <summary>
	/// seAudio의 볼륨값 수정 
	/// </summary>
	void SeVolumeReset()
	{
		foreach (var seAudio in seAudioList)
		{
			seAudio.volume = seValue;
		}
	}

	/// <summary>
	/// BGM 실행
	/// </summary>
	/// <param name="path"></param>
	public void PlayBGM(BGMEnum bgmEnum)
	{
		if (currentBGMEnum == bgmEnum)
			return;

		currentBGMEnum = bgmEnum;

		string path = string.Empty;

		switch (bgmEnum)
		{
			case BGMEnum.Intro: path = "BGM_Intro"; break;
			case BGMEnum.Game: path = "BGM_Game"; break;
			case BGMEnum.Loading: path = "BGM_Loading"; break;
		}

		bgmAudio.clip = Resources.Load<AudioClip>("BGM/" + path);
		bgmAudio.loop = true;
		bgmAudio.Play();
	}


	public AudioSource PlaySe(SeEnum seEnum)
	{
		string path = string.Empty;

		switch (seEnum)
		{
			case SeEnum.Yes: path = "YES"; break;
			case SeEnum.No: path = "NO"; break;
			case SeEnum.AttackWin: path = "저장"; break;
			case SeEnum.AttackLose: path = "No"; break;
		}

		return PlaySe(path);
	}

	/// <summary>
	/// SE 실행
	/// </summary>
	/// <param name="path"></param>
	public AudioSource PlaySe(string path)
	{
		AudioSource audioSource = null;

		foreach (var seAudio in seAudioList)
		{
			if (!seAudio.isPlaying)
			{
				audioSource = seAudio;
				break;
			}
		}

		if (audioSource == null)
		{
			audioSource = gameObject.AddComponent<AudioSource>();
			seAudioList.Add(audioSource);
			SeVolumeReset();
		}

		audioSource.clip = Resources.Load<AudioClip>("SE/" + path);
		audioSource.Play();

		return audioSource;
	}

}

public enum BGMEnum
{
	None,
	Intro,
	Game,
	Loading
}

public enum SeEnum
{
	Yes,
	No,
	AttackWin,
	AttackLose
}


