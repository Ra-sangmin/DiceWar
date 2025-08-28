using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VictoryPopup : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GoMainBtnClickOn()
    {
        //FindFirstObjectByType<InGameController>().GoMainOn();
        gameObject.SetActive(false);
    }

    public void NewGameBtnClickOn()
    {
        //FindFirstObjectByType<InGameController>().NewGameOn();
        gameObject.SetActive(false);
    }
}
