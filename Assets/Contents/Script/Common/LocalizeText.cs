using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

public class LocalizeText : MonoBehaviour
{
	/// <summary> 현지화 언어 key값 </summary>
	[SerializeField] LocalizeStatus statusEnum = LocalizeStatus.None;

	/// <summary> 현지화 언어 key값 </summary>
	[SerializeField] int key = -1;

	private Text text;
	private TextMeshProUGUI textMesh;


	private void Awake()
	{
		text = GetComponent<Text>();
		textMesh = GetComponent<TextMeshProUGUI>();

		LocalizeManager.Instance.language
			.Subscribe(_ => LocalizeTextSet())
			.AddTo(gameObject);
	}

	public void LocalizeTextSet()
	{
		if (statusEnum == LocalizeStatus.None || key == -1)
			return;

		//현재 오브젝트 키값에 맞는 현지화 언어 취득
		string resultStr = LocalizeManager.Instance.GetStrData(statusEnum, key);

		if (text != null)
		{
			text.text = resultStr;
		}
		else if (textMesh != null)
		{
			textMesh.text = resultStr;
		}
	}
}
