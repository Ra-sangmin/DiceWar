using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialPopup : MonoBehaviour
{
    [SerializeField] Image bgImage;
    [SerializeField] List<Sprite> spriteList = new List<Sprite>();

	[SerializeField] Text contenstText;

	[SerializeField] Button beforeBtn;
	[SerializeField] Button afterBtn;

	private int key = 0;
    private int maxPage = 14;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TutorialOn()
    {
        key = 0;
		SetData();
    }

    void SetData()
    {
        //이미지 설정
		bgImage.sprite = spriteList[key];

        //글 내용 설정
        contenstText.text = LocalizeManager.Instance.GetStrData(LocalizeStatus.TutorialPopup, key);

		//버튼 설정
		afterBtn.gameObject.SetActive(key < maxPage);
		beforeBtn.gameObject.SetActive(key > 0);
    }

	public void NextBtnClickOn(bool nextOn)
    {
        if (nextOn)
        {
            key++;
		}
        else 
        {
            key--;
		}

        key = Mathf.Clamp(key, 0, maxPage);
		SetData();
    }

    public void NextBtnOn()
    {
        if (key == 0)
        {
            key = 1;
			SetData();
        }
        else 
        {
            gameObject.SetActive(false);
        }
    }

	public void CloseBtnClickOn()
	{
		Destroy(gameObject);
	}
}
