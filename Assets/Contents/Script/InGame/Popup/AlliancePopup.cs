using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System.Linq;
using UnityEngine.Events;

public class AlliancePopup : MonoBehaviour
{
    [SerializeField] PlayerToggleIcon playerToggleIconPrefab;
    [SerializeField] RectTransform playerToggleIconParant;
    [SerializeField] Text leftCoinCount;
    [SerializeField] Button propossBtn;

    private List<PlayerToggleIcon> playerToggleIconList = new List<PlayerToggleIcon>();
    private PlayerToggleIcon selectPlayer;

    //private AllianceRequestData allianceRequestData;

    public UnityAction<AllianceRequestData> AllianceClearOn = data => { };

    private int maxCoinCount;

    private void Awake()
    {
        CreatePlayerToggleIcon();
    }
    void CreatePlayerToggleIcon()
    {
        PlayerEnum myPlayerEnum = DataManager.Instance.playerData.pe;

        for (int i = 0; i < DataManager.Instance.num_player; i++)
        {
            PlayerToggleIcon playerToggleIcon = Instantiate(playerToggleIconPrefab, playerToggleIconParant);
            playerToggleIcon.gameObject.SetActive(true);
            playerToggleIcon.SetPlayerData((PlayerEnum)i);

            int index = playerToggleIconList.Count;

            playerToggleIcon.GetComponent<Button>()
                .OnClickAsObservable()
                .Subscribe(_ => PlayerSelectOn(index))
                .AddTo(gameObject);

            playerToggleIcon.coinBox.coinCount
                .Subscribe(_ => SetLeftCoint())
                .AddTo(gameObject);

            playerToggleIconList.Add(playerToggleIcon);
        }
    }

    public void SetData() 
    {
        PlayerEnum myPlayerEnum = DataManager.Instance.playerData.pe;

        foreach (var playerToggleIcon in playerToggleIconList)
        {
            playerToggleIcon.SelectOn(playerToggleIcon.playerEnum == myPlayerEnum);
        }

        maxCoinCount = DataManager.Instance.GetNeedCoin() * DataManager.Instance.num_player;

        SetCointCount();
    }

    public void PlayerSelectOn(int selectIndex)
    {
        PlayerToggleIcon currentSelectPlayer = playerToggleIconList[selectIndex];

        PlayerEnum myPlayerEnum = DataManager.Instance.playerData.pe;

        if (currentSelectPlayer.playerEnum == myPlayerEnum)
        {
            return;
        }

        currentSelectPlayer.ToggleOn();

        SetCointCount();
    }

    private void SetCointCount()
    {
        PlayerEnum myPlayerEnum = DataManager.Instance.playerData.pe;

        int maxCoinCount = this.maxCoinCount;

        int allianceCount = playerToggleIconList.Count(data => data.toggle.isOn);

        float oneManCoinCount = maxCoinCount / (float)allianceCount;

        foreach (var playerToggleIcon in playerToggleIconList)
        {
            int tempCount = 0;

            if (playerToggleIcon.toggle.isOn == false)
            {
                continue;
            }

            tempCount = Mathf.CeilToInt(oneManCoinCount);

            tempCount = Mathf.Min(tempCount, maxCoinCount);

            playerToggleIcon.coinBox.SetCoinCount(tempCount);

            maxCoinCount -= tempCount;

            if (maxCoinCount < 0)
            {
                maxCoinCount = 0;
            }
        }

        SetLeftCoint();
    }

    void SetLeftCoint()
    {
        int leftCoin = maxCoinCount;

        foreach (var playerToggleIcon in playerToggleIconList)
        {
            if (playerToggleIcon.toggle.isOn == false)
            {
                continue;
            }

            int coinCount = playerToggleIcon.coinBox.coinCount.Value;

            leftCoin -= coinCount;
        }

        leftCoinCount.text = leftCoin.ToString();


        List<PlayerToggleIcon> selectList = playerToggleIconList.Where(data => data.toggle.isOn).ToList();

        bool activeOn = selectList.Count > 1 && leftCoin == 0;
        propossBtn.interactable = activeOn;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void SetProposeBtn()
    {
        List<PlayerToggleIcon> selectList = playerToggleIconList.Where(data => data.toggle.isOn).ToList();
    }


    public void ProposeBtnClickOn()
    {
        List<PlayerToggleIcon> selectList = playerToggleIconList.Where(data => data.toggle.isOn).ToList();

        if (selectList.Count <= 1) 
        {
            return;
        }

        List<AllianceData> allianceDataList = new List<AllianceData> ();

        PlayerEnum myPlayerEnum = DataManager.Instance.playerData.pe;
        int myCoinCount = 0;

        foreach (var selectItem in selectList)
        {
            AllianceData allianceData = new AllianceData(selectItem.playerEnum, selectItem.coinBox.coinCount.Value);
            allianceDataList.Add(allianceData);

            if (selectItem.playerEnum == myPlayerEnum) 
            {
                myCoinCount = selectItem.coinBox.coinCount.Value;
            }
        }

        AllianceRequestData allianceRequestData = new AllianceRequestData();
        allianceRequestData.allianceDataList = allianceDataList;

        AllianceData orderData = new AllianceData()
        {
            playerEnum = myPlayerEnum,
            coinCount = myCoinCount
        };

        AllianceRequest request = new AllianceRequest()
        {
            orderData = orderData,
            allianceDataList = allianceDataList,
        };

        //List<AllianceData> allianceDataList = new List<AllianceData>()
        //{
        //    new AllianceData()
        //    {
        //        playerEnum = PlayerEnum.Player_1,
        //        coinCount = 5
        //    }
        //};

        ServerManager.Instance.SendMessageOn(request);
        gameObject.SetActive(false);

        //AllianceRequestOn(allianceRequestData);

    }
    
    public void AllianceRequestOn(AllianceRequestData allianceRequestData)
    {
        AllianceClear(allianceRequestData);
    }

    public void AllianceClear(AllianceRequestData allianceRequestData)
    {
        //AllianceClearOn(allianceRequestData);
        gameObject.SetActive(false);
    }
}
public class AllianceRequestData
{
    public List<AllianceData> allianceDataList = new List<AllianceData>() { };
}