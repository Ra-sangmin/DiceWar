using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonSound : MonoBehaviour
{
    private Button btn;

    // Start is called before the first frame update
    void Start()
    {
        btn = GetComponent<Button>();
        if (btn != null) 
        {
            btn.onClick.AddListener(ButtonClickOn);
        }
        
    }

    public void ButtonClickOn()
    {
		//SoundManager.Instance.PlaySe("유아이클릭열기");
		//SoundManager.Instance.PlaySe("YES");
		SoundManager.Instance.PlaySe(SeEnum.Yes);
	}

}
