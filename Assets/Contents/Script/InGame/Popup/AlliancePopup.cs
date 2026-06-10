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

    private PlayerIconController playerIconController;

	private void Awake()
    {
        CreatePlayerToggleIcon();

		playerIconController = FindFirstObjectByType<PlayerIconController>();
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

    /// <summary>
    /// 토글이 On 된 플레이어들에게 코인 배분
    /// </summary>
    private void SetCointCount()
    {
        PlayerEnum myPlayerEnum = DataManager.Instance.playerData.pe;

        int maxCoinCount = this.maxCoinCount;

        List<PlayerToggleIcon> toggleOnPlayerList = playerToggleIconList.Where(data => data.toggle.isOn).ToList();

		List<PlayerEnum> playerEnumList = toggleOnPlayerList.Select(data => data.playerEnum).ToList();

		List<AllianceData> allianceDataList = DataManager.Instance.GetAllianceDefaultData(myPlayerEnum, playerEnumList, playerIconController);

        foreach (var toggleOnPlayer in toggleOnPlayerList)
        {
            var data = allianceDataList.FirstOrDefault(data => data.playerEnum == toggleOnPlayer.playerEnum);

            if (data != null)
            {
                toggleOnPlayer.playerEnum = data.playerEnum;
				toggleOnPlayer.coinBox.SetCoinCount(data.coinCount);
			}
		}

		SetLeftCoint();
    }

    /// <summary>
    /// 전체 코인에서 현재 분배된 코인을 뺀 나머지 코인수
    /// </summary>
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

        ServerManager.Instance.SendMessageOn(request);
        gameObject.SetActive(false);
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