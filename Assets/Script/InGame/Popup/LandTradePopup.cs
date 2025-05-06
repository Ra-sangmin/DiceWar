using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UniRx;

public class LandTradePopup : MonoBehaviour
{
    //[SerializeField] List<Toggle> playerIconToggleList = new List<Toggle>();
    //[SerializeField] RectTransform tradeCoinPanel;
    //[SerializeField] RectTransform addCoinPanel;
    //[SerializeField] Text coinText;

    [SerializeField] PlayerToggleIcon playerToggleIconPrefab;
    [SerializeField] RectTransform playerToggleIconParant;

    private List<PlayerToggleIcon> playerToggleIconList = new List<PlayerToggleIcon>();

    private AreaData selectAreaData;

    //private int coinCount = 5;

    public bool buyOn = false;

    //public UnityAction<LandTradePopup> tradeClear = data => { };

    //private PlayerEnum selectPlayerEnum;

    private PlayerToggleIcon selectPlayer;

    private void Awake()
    {
        //SetEvent();
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

            //playerToggleIcon.toggle.onValueChanged.AddListener(isOn =>
            //{
            //    if (isOn)
            //    {
            //        SelectOn(currentPlayerEnum);
            //    }
            //});

            playerToggleIconList.Add(playerToggleIcon);
        }   
    }

    public void PlayerSelectOn(int selectIndex)
    {
        //Debug.LogWarning(selectIndex);

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

        //selectPlayerEnum = playerEnum;

        //PlayerToggleIcon playerToggleIcon = playerToggleIconList.FirstOrDefault(data => data.playerEnum == playerEnum);

        //if (playerToggleIcon == null)
        //    return;

        foreach (var playerToggleIcon in playerToggleIconList)
        {
            bool selectOn = playerToggleIcon.playerEnum == selectPlayer.playerEnum;

            playerToggleIcon.SelectOn(selectOn);
        }
    }

    void SetEvent()
    {
        //Instantiate(playerToggleIconPrefab, transform);

        //Create
        //SetToggleEvent(playerIconToggleList, SelectOn);
    }

    //private void SetToggleEvent(List<Toggle> toggleList, UnityAction<int> toggleEventOn, int defaultIndex = 0)
    //{
    //    for (int i = 0; i < toggleList.Count; i++)
    //    {
    //        int index = i;

    //        toggleList[i].onValueChanged.AddListener(isOn =>
    //        {
    //            if (isOn)
    //            {
    //                toggleEventOn(index);
    //            }
    //        });
    //    }

    //    toggleEventOn(defaultIndex);
    //}

    // Start is called before the first frame update
    void Start()
    {
        //if (InGameDataManager.Instance.isMultiOn)
        //{
        //    ServerManager.Instance.receiveDataOn += ReceiveDataOn;
        //}
    }

    private void OnDestroy()
    {
        //if (InGameDataManager.Instance.isMultiOn)
        //{
        //    ServerManager.Instance.receiveDataOn -= ReceiveDataOn;
        //}
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SellOn()
    {

    }

    public void SetDataOn(AreaData selectAreaData , bool buyOn)
    {
        this.selectAreaData = selectAreaData;

        this.buyOn = buyOn;

        if (buyOn)
        {
            int index = playerToggleIconList.FindIndex(0, data => data.playerEnum == selectAreaData.player);
            PlayerSelectOn(index);
            //PlayerSelectOn(selectAreaData.player);
        }
    }

    //public void SelectOn(PlayerEnum playerEnum)
    //{
    //    //selectPlayerEnum = playerEnum;

    //    ////PlayerToggleIcon playerToggleIcon = playerToggleIconList.FirstOrDefault(data => data.playerEnum == playerEnum);

    //    ////if (playerToggleIcon == null)
    //    ////    return;

    //    //foreach (var playerToggleIcon in playerToggleIconList)
    //    //{
    //    //    bool selectOn = playerToggleIcon.playerEnum == playerEnum;

    //    //    playerToggleIcon.SelectOn(selectOn);
    //    //}

    //    //playerToggleIcon.SelectOn();

    //    //tradeCoinPanel.transform.SetParent(playerToggleIcon.transform);
    //    //tradeCoinPanel.transform.localPosition = new Vector2(0,-215);
    //    //tradeCoinPanel.gameObject.SetActive(true);

    //    //SetActiveAddCoinPanel(false);

    //    //coinCount = 5;

    //    //coinText.text = coinCount.ToString();

    //    //Debug.LogWarning(index);
    //}

    //public void SetActiveAddCoinPanel(bool activeOn)
    //{
    //    addCoinPanel.gameObject.SetActive(activeOn);
    //}

    //public void CoinCountChange(bool addOn)
    //{
    //    if (addOn)
    //    {
    //        coinCount++;
    //    }
    //    else
    //    {
    //        coinCount--;
    //    }

    //    coinText.text = coinCount.ToString();
    //}

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
            buyOn = buyOn,
        };

        ServerManager.Instance.SendMessageOn(request);
        gameObject.SetActive(false);
    }

    private void RequestOn(PlayerEnum changePlayerEnum) 
    {
        //PlayerEnum changePlayerEnum = buyOn ? InGameDataManager.Instance.playerData.playerEnum : selectPlayer.playerEnum;

        
        //ProposeClearOn(changePlayerEnum);
    }

    public void ProposeClearOn(PlayerEnum changePlayerEnum)
    {
        selectAreaData.PlayerChangeOn(changePlayerEnum);

        //tradeClear(this);

        //gameObject.SetActive(false);
    }
}
