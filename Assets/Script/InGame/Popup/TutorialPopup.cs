using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialPopup : MonoBehaviour
{
    [SerializeField] Image bgImage;
    [SerializeField] List<Sprite> spriteList = new List<Sprite>();

    private int status = 0;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TutorialOn()
    {
        //status = 0;
        //SetBGImage();
    }

    void SetBGImage()
    {
        //bgImage.sprite = spriteList[status];
    }

    public void NextBtnOn()
    {
        if (status == 0)
        {
            status = 1;
            SetBGImage();
        }
        else 
        {
            gameObject.SetActive(false);
        }
    }
}
