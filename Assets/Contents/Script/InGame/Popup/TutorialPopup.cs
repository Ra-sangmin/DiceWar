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

	private int status = 0;
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
        status = 0;
		SetData();
    }

    void SetData()
    {
        //이미지 설정
		bgImage.sprite = spriteList[status];

        //글 내용 설정
        contenstText.text = GetContentsStr();

		//버튼 설정
		afterBtn.gameObject.SetActive(status < maxPage);
		beforeBtn.gameObject.SetActive(status > 0);
    }

    public string GetContentsStr()
    {
        string strValue = string.Empty;

        switch (status)
        {
            case 0: strValue = "Before starting game, Victory Reward and Entrance fee\nare displayed."; break;
			case 1: strValue = "The map can be changed if not wanted."; break;
			case 2: strValue = "Tap your zone and select adjecent opponent zone to\nattack."; break;
			case 3: strValue = "Dice are rolled to decide the winner."; break;
			case 4: strValue = "Right number shows how many zones linked “at once.”"; break;
			case 5: strValue = "When “End Turn” is pressed, that many dice are added\nto your territory."; break;
			case 6: strValue = "Remaining dice are displayed next to “Stash.”\nTo win “Single Player”, conquer all map."; break;
			case 7: strValue = "In “Multi Player”, settings and map cannot be changed."; break;
			case 8: strValue = "In multiplayer, “Card” and “Timer” are added at bottom.\nFinish your turn until Timer ends."; break;
			case 9: strValue = "To make a deal, press Card."; break;
			case 10: strValue = "Alliance offer : select player, set reward, then propose.\nThe right-bottom number shows extra or missing coin."; break;
			case 11: strValue = "Land trade : select land, select player, set price,\nthen propose."; break;
			case 12: strValue = "When a deal is offered, it can be accepted or refused."; break;
			case 13: strValue = "To leave your alliance, tap “Betray” using your card.\nPenalty will be same as your share in the alliance."; break;
			case 14: strValue = "To win “Multi Player”, you or your alliance should\nconquer all map. "; break;
		}

        return strValue;
	}

	public void NextBtnClickOn(bool nextOn)
    {
        if (nextOn)
        {
            status++;
		}
        else 
        {
            status--;
		}

        status = Mathf.Clamp(status, 0, maxPage);
		SetData();
    }

    public void NextBtnOn()
    {
        if (status == 0)
        {
            status = 1;
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
