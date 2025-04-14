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

    private List<PlayerToggleIcon> playerToggleIconList = new List<PlayerToggleIcon>();
    private PlayerToggleIcon selectPlayer;

    //private AllianceRequestData allianceRequestData;

    public UnityAction<AllianceRequestData> AllianceClearOn = data => { };

    private void Awake()
    {
        CreatePlayerToggleIcon();
    }
    void CreatePlayerToggleIcon()
    {
        PlayerEnum myPlayerEnum = InGameDataManager.Instance.playerData.playerEnum;

        for (int i = 0; i < InGameDataManager.Instance.num_player; i++)
        {
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

    public void SetData() 
    {
        PlayerEnum myPlayerEnum = InGameDataManager.Instance.playerData.playerEnum;

        foreach (var playerToggleIcon in playerToggleIconList)
        {
            playerToggleIcon.SelectOn(playerToggleIcon.playerEnum == myPlayerEnum);
        }
    }

    public void PlayerSelectOn(int selectIndex)
    {
        PlayerToggleIcon currentSelectPlayer = playerToggleIconList[selectIndex];

        PlayerEnum myPlayerEnum = InGameDataManager.Instance.playerData.playerEnum;

        if (currentSelectPlayer.playerEnum == myPlayerEnum)
        {
            return;
        }

        currentSelectPlayer.ToggleOn();

        SetProposeBtn();
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

        foreach (var selectItem in selectList)
        {
            Debug.LogWarning($"playerEnum = {selectItem.playerEnum} , coinCount = {selectItem.coinBox.coinCount}");
            AllianceData allianceData = new AllianceData(selectItem.playerEnum, selectItem.coinBox.coinCount);
            allianceDataList.Add(allianceData);
        }

        AllianceRequestData allianceRequestData = new AllianceRequestData();
        allianceRequestData.allianceDataList = allianceDataList;

        AllianceData orderData = new AllianceData()
        {
            playerEnum = InGameDataManager.Instance.playerData.playerEnum,
            coinCount = 5
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

        ServerManager.Instance.AllianceRequestOn(request);
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