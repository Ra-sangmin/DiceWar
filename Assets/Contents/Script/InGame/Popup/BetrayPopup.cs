using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BetrayPopup : BasePopup
{
    [SerializeField] PlayerToggleIcon playerToggleIcon;

    public UnityAction BetrayClearOn = () => { };
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void SetData()
    {
		int addCoin = DataManager.Instance.GetNeedBetrayCoin();
		playerToggleIcon.SetPlayerData(DataManager.Instance.playerData.pe);
		playerToggleIcon.coinBox.SetCoinCount(addCoin);
	}


    public void BetrayBtnClickOn()
    {
	    AllianceBetrayRequest request = new AllianceBetrayRequest()
        {
			betrayPlayerEnum = DataManager.Instance.playerData.pe,
			allianceDataList = GetNewAllianceData(),
		};

        ServerManager.Instance.SendMessageOn(request);

        //BetrayClearOn();
        Destroy(gameObject);
    }
	List<AllianceData> GetNewAllianceData()
    {
		List<AllianceData> resultAllianceDataList = new List<AllianceData>();

		PlayerEnum myPlayer = DataManager.Instance.playerData.pe;
		List<AllianceData> allDataList = DataManager.Instance.GetAllAlliance(myPlayer).Where(data => data.playerEnum != myPlayer).ToList();

		int maxCoinCount = DataManager.Instance.GetNeedCoin() * DataManager.Instance.num_player;

		float oneManCoinCount = maxCoinCount / (float)allDataList.Count;

		foreach (AllianceData data in allDataList)
		{
			int tempCount = 0;

			tempCount = Mathf.CeilToInt(oneManCoinCount);

			tempCount = Mathf.Min(tempCount, maxCoinCount);

			data.coinCount = tempCount;

			maxCoinCount -= tempCount;

			if (maxCoinCount < 0)
			{
				maxCoinCount = 0;
			}

            resultAllianceDataList.Add(data);
		}

        return resultAllianceDataList;

	}
}
