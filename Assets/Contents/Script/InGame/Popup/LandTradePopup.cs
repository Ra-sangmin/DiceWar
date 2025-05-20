using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UniRx;

public class LandTradePopup : MonoBehaviour
{
    [SerializeField] PlayerToggleIcon playerToggleIconPrefab;
    [SerializeField] RectTransform playerToggleIconParant;

    private List<PlayerToggleIcon> playerToggleIconList = new List<PlayerToggleIcon>();

    private AreaData selectAreaData;

    public bool buyOn = false;

    private PlayerToggleIcon selectPlayer;

    private UnityAction proposeBtnClickEventOn;

    private void Awake()
    {
        CreatePlayerToggleIcon();
    }

    void CreatePlayerToggleIcon()
    {
        PlayerEnum myPlayerEnum = DataManager.Instance.playerData.playerEnum;

        for (int i = 0; i < DataManager.Instance.num_player; i++)
        {
            PlayerEnum currentPlayerEnum = (PlayerEnum)i;

            if (currentPlayerEnum == myPlayerEnum)
            {
                continue;
            }

            PlayerToggleIcon playerToggleIcon = Instantiate(playerToggleIconPrefab, playerToggleIconParant);
            playerToggleIcon.gameObject.SetActive(true);
            playerToggleIcon.SetPlayerData((PlayerEnum)i);

            int index = playerToggleIconList.Count;

            playerToggleIcon.GetComponent<Button>()
                .OnClickAsObservable()
                .Subscribe(_ => PlayerSelectOn(index))
                .AddTo(gameObject);

            playerToggleIconList.Add(playerToggleIcon);
        }   
    }

    public void PlayerSelectOn(int selectIndex)
    {
        PlayerToggleIcon currentSelectPlayer = playerToggleIconList[selectIndex];

        if (selectPlayer != null && selectPlayer == currentSelectPlayer)
        {
            return;
        }

        if (buyOn && selectAreaData.player != currentSelectPlayer.playerEnum) 
        {
            return;
        }

        selectPlayer = currentSelectPlayer;

        foreach (var playerToggleIcon in playerToggleIconList)
        {
            bool selectOn = playerToggleIcon.playerEnum == selectPlayer.playerEnum;

            playerToggleIcon.SelectOn(selectOn);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetDataOn(AreaData selectAreaData , bool buyOn , UnityAction proposeBtnClickEventOn)
    {
        this.selectAreaData = selectAreaData;

        this.buyOn = buyOn;

        this.proposeBtnClickEventOn = proposeBtnClickEventOn;

        if (buyOn)
        {
            int index = playerToggleIconList.FindIndex(0, data => data.playerEnum == selectAreaData.player);
            PlayerSelectOn(index);
        }
    }

    public void ProposeBtnClickOn()
    {
        if (selectPlayer == null)
        {
            return;
        }

        PlayerEnum fromPlayerEnum = DataManager.Instance.playerData.playerEnum;
        PlayerEnum toPlayerEnum = selectPlayer.playerEnum;

        LandTradeRequest request = new LandTradeRequest()
        {
            fromPlayerEnum = fromPlayerEnum,
            toPlayerEnum = toPlayerEnum,
            areaData = selectAreaData,
            coinCount = selectPlayer.coinBox.coinCount.Value,
            buyOn = buyOn,
        };

        ServerManager.Instance.SendMessageOn(request);

        proposeBtnClickEventOn();

        gameObject.SetActive(false);
    }
}
