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

	private PlayerIconController playerIconController;

	// Start is called before the first frame update
	void Start()
    {
		playerIconController = FindFirstObjectByType<PlayerIconController>();
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
		DataManager.Instance.MyBetrayOn(playerIconController);

        //BetrayClearOn();
        Destroy(gameObject);
    }
}
