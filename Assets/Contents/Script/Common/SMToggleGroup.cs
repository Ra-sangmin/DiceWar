using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SMToggleGroup : MonoBehaviour
{
    List<SMToggle> toggleList = new List<SMToggle>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetToggle(SMToggle smToggle)
    {
        toggleList.Add(smToggle);
    }

    public void ToggleValueChangeOn(SMToggle smToggle)
    {
        foreach (SMToggle toggle in toggleList)
        {
            if (toggle == smToggle || toggle.toggleValue.Value == false)
            {
                continue;
            }

            toggle.SetToggleValue(false);
        }
    }

}
