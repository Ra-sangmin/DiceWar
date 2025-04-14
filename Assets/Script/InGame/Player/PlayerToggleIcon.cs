using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerToggleIcon : MonoBehaviour
{
    public Toggle toggle;
    [SerializeField] Image bgImage;
    [SerializeField] Image fgImage;
    [SerializeField] List<Sprite> bgSpriteList = new List<Sprite>();
    [SerializeField] List<Sprite> fgSpriteList = new List<Sprite>();

    [SerializeField] Sprite disableBgSprite;
    public CoinBox coinBox;
    
    //[SerializeField] RectTransform tradeCoinPanel;
    //[SerializeField] Image activeImage;
    //[SerializeField] RectTransform addCoinPanel;
    //[SerializeField] Text coinText;

    [SerializeField] Text playerDiscriptText;

    public PlayerEnum playerEnum;
    //public int coinCount = 5;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Alpha1))
        //{
        //    SetCoinBtnActiveOn(false);
        //    //activeImage.gameObject.SetActive(false);
        //    //Debug.LogWarning("d");
        //}
        //if (Input.GetKeyDown(KeyCode.Alpha2))
        //{
        //    SetCoinBtnActiveOn(true);
        //    //activeImage.gameObject.SetActive(true);
        //    //Debug.LogWarning("3");
        //}
    }

    //public void SetCoinBtnActiveOn(bool activeOn)
    //{
    //    activeImage.gameObject.SetActive(activeOn);
    //}

    public void SetPlayerData(PlayerEnum playerEnum) 
    {
        this.playerEnum = playerEnum;

        int colorIndex = InGameDataManager.Instance.GetPlayerColorIndex(playerEnum);

        bgImage.sprite = bgSpriteList[colorIndex];
        fgImage.sprite = fgSpriteList[colorIndex];
    }

    public void ToggleOn()
    {
        bool activeOn = !toggle.isOn;

        SelectOn(activeOn);
    }

    public void SelectOn(bool selectOn)
    {
        toggle.isOn = selectOn;

        coinBox.SelectOn(selectOn);

        //tradeCoinPanel.transform.SetParent(playerToggleIcon.transform);
        //tradeCoinPanel.transform.localPosition = new Vector2(0, -215);
        //tradeCoinPanel.gameObject.SetActive(selectOn);

        //coinCount = 5;
        //coinText.text = coinCount.ToString();

        //SetActiveAddCoinPanel(false);
    }

    //public void SetActiveAddCoinPanel(bool activeOn)
    //{
    //    addCoinPanel.gameObject.SetActive(activeOn);
    //}

    public void CoinCountChange(bool addOn)
    {
        //if (addOn)
        //{
        //    coinCount++;
        //}
        //else
        //{
        //    coinCount--;
        //}

        //coinText.text = coinCount.ToString();
    }
}
