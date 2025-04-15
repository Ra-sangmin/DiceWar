using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SkillCard : MonoBehaviour
{
    [SerializeField] List<Image> skillCardList = new List<Image>();

    // Start is called before the first frame update
    void Start()
    {
        DataManager.Instance.playerData.skillCardCount = 3;
        SetCountIcon();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetCountIcon()
    {
        int countIndex = DataManager.Instance.playerData.skillCardCount;

        for (int i = 0; i < skillCardList.Count; i++)
        {
            bool activeOn = i == countIndex;
            skillCardList[i].gameObject.SetActive(activeOn);
        }

        if (skillCardList.All(data => data.gameObject.activeSelf == false))
        {
            skillCardList[0].gameObject.SetActive(true);
        }
    }
}
