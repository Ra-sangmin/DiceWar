using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BetrayPopup : MonoBehaviour
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
        playerToggleIcon.SetPlayerData(DataManager.Instance.playerData.playerEnum);
    }


    public void BetrayBtnClickOn()
    {
        AllianceBetrayRequest request = new AllianceBetrayRequest()
        {
            playerEnum = DataManager.Instance.playerData.playerEnum,
        };

        ServerManager.Instance.SendMessageOn(request);

        //BetrayClearOn();
        gameObject.SetActive(false);
    }
}
