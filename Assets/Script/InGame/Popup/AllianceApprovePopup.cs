using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System.Linq;
using UnityEngine.Events;

public class AllianceApprovePopup : MonoBehaviour
{
    [SerializeField] PlayerToggleIcon playerToggleIconPrefab;
    [SerializeField] RectTransform playerToggleIconParant;

    private List<PlayerToggleIcon> playerToggleIconList = new List<PlayerToggleIcon>();

    private AllianceRequest allianceRequest;

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

            playerToggleIcon.GetComponent<Image>().raycastTarget = false;
            playerToggleIcon.coinBox.SetCoinBtnActiveOn(false);

            int index = playerToggleIconList.Count;

            playerToggleIcon.GetComponent<Button>()
                .OnClickAsObservable()
                .Subscribe(_ => PlayerSelectOn(index))
                .AddTo(gameObject);

            playerToggleIconList.Add(playerToggleIcon);
        }
    }

    public void SetData(AllianceRequest allianceRequest)
    {
        this.allianceRequest = allianceRequest;

        PlayerEnum myPlayerEnum = InGameDataManager.Instance.playerData.playerEnum;

        foreach (var playerToggleIcon in playerToggleIconList)
        {
            AllianceData allianceData = allianceRequest.allianceDataList.FirstOrDefault(data => data.playerEnum == playerToggleIcon.playerEnum);

            if (allianceData == null)
            {
                playerToggleIcon.SelectOn(false);
            }
            else 
            {
                playerToggleIcon.SelectOn(true);
                playerToggleIcon.coinBox.SetCoinCount(allianceData.coinCount);
            }
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
    }

    

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AcceptBtnClickOn()
    {
        DataRequestOn(true);
    }

    public void RefuseBtnClickOn()
    {
        DataRequestOn(false);
    }

    void DataRequestOn(bool approveOn)
    {
        PlayerEnum playerEnum = InGameDataManager.Instance.playerData.playerEnum;

        AllianceApproveRequest request = new AllianceApproveRequest()
        {
            orderData = allianceRequest.orderData,
            playerEnum = playerEnum,
            approveOn = approveOn,
        };
        
        ServerManager.Instance.AllianceApproveRequestOn(request);
        gameObject.SetActive(false);
    }
}
