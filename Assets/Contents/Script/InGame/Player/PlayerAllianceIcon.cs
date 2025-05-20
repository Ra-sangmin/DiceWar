using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerAllianceIcon : MonoBehaviour
{
    //public PlayerEnum playerEnum;
    //[SerializeField] Image bgImage;
    [SerializeField] Image fgImage;
    [SerializeField] List<Sprite> fgSpriteList = new List<Sprite>();

    [SerializeField] Text coinText;
    //[SerializeField] List<Sprite> bgSpriteList = new List<Sprite>();

    private AllianceData allianceData;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetPlayerData(AllianceData allianceData)
    {
        this.allianceData = allianceData;

        int colorIndex = DataManager.Instance.GetPlayerColorIndex(allianceData.playerEnum);
        fgImage.sprite = fgSpriteList[colorIndex];

        coinText.text = allianceData.coinCount.ToString();
    }
}
