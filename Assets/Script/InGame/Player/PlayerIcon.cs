using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PlayerIcon : MonoBehaviour
{
    [SerializeField] Text connectedCountText;
    [SerializeField] Image iconImage;
    [SerializeField] List<Sprite> spriteiconList = new List<Sprite>();

    public PlayerEnum playerEnum = PlayerEnum.Player_None;
    public int connectedCount;
    public UnityAction<PlayerIcon> playerClickOn = data => { };

    // Start is called before the first frame update
    void Start()
    {
        SetIconColor();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetPlayer(PlayerEnum playerEnum)
    {
        this.playerEnum = playerEnum;
        SetIconColor();
    }

    public void SetIconColor() 
    {
        int index = InGameDataManager.Instance.GetPlayerColorIndex(playerEnum);

        iconImage.sprite = spriteiconList[index];
    }

    public void SetConnectedCount(int connectedCount)
    {
        this.connectedCount = connectedCount;
        connectedCountText.text = connectedCount.ToString();
    }

    public void BtnClickOn()
    {
        playerClickOn(this);
    }
}
