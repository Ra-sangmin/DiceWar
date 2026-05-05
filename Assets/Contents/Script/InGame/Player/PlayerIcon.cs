using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PlayerIcon : MonoBehaviour
{
    [SerializeField] Text connectedCountText;
    [SerializeField] Image iconImage;
    [SerializeField] Image aroundBGImage;
    [SerializeField] Image centerBGImage;
    [SerializeField] Image selectIconImage;
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
        int colorIndex = DataManager.Instance.GetPlayerColorIndex(playerEnum);
        Color color = DataManager.Instance.GetPlayerColor(colorIndex);

        iconImage.sprite = spriteiconList[colorIndex];
        aroundBGImage.color = color;
        selectIconImage.color = color;

        SetCenterBGImage(PlayerEnum.Player_None);
    }

    void SetCenterBGImage(PlayerEnum playerEnum = PlayerEnum.Player_None)
    {
        int colorIndex = DataManager.Instance.GetPlayerColorIndex(playerEnum);

        Color color = playerEnum == PlayerEnum.Player_None ? new Color32(44, 48, 54, 255) : DataManager.Instance.GetPlayerColor(colorIndex);

        centerBGImage.color = color;
    }

    public void SetAllianceColor(PlayerEnum ordebPlayerEnum)
    {
        SetCenterBGImage(ordebPlayerEnum);
    }

    public void SetConnectedCount(int connectedCount)
    {
        this.connectedCount = connectedCount;
        connectedCountText.text = connectedCount.ToString();
    }

    public void SetMyTurnEffect(bool forceActiveOn = false)
    {
        bool activeOn = forceActiveOn || DataManager.Instance.currentTurnIndex == (int)playerEnum;
        selectIconImage.gameObject.SetActive(activeOn);
    }

	public void SetMyTurnEffectOff()
	{
		selectIconImage.gameObject.SetActive(false);
	}

	public void BtnClickOn()
    {
        playerClickOn(this);
    }
}
