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

    }

    public void SetPlayerData(PlayerEnum playerEnum) 
    {
        this.playerEnum = playerEnum;

        int colorIndex = DataManager.Instance.GetPlayerColorIndex(playerEnum);

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
    }

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
