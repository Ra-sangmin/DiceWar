using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BetrayPopup : MonoBehaviour
{
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

    }

    public void BetrayBtnClickOn()
    {
        PlayerEnum playerEnum = InGameDataManager.Instance.playerData.playerEnum;

        AllianceBetrayRequest request = new AllianceBetrayRequest()
        {
            playerEnum = playerEnum,
        };

        ServerManager.Instance.AllianceBetrayRequestOn(request);

        //BetrayClearOn();
        gameObject.SetActive(false);
    }
}
