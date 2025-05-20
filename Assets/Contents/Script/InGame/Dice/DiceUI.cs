using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DiceUI : MonoBehaviour
{
    [SerializeField] Image bgImage;
    [SerializeField] List<Sprite> diceSpriteList = new List<Sprite>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetPlayer(PlayerEnum playerEnum)
    {
        bgImage.color = DataManager.Instance.GetPlayerColor(playerEnum);
    }

    public void SetDice(int diceStatus)
    {
        bgImage.sprite = diceSpriteList[diceStatus];
    }
}
