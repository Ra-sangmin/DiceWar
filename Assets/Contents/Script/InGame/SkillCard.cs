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
        SetCountIcon(DataManager.Instance.playerData.playerEnum);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetCountIcon(PlayerEnum playerEnum)
    {
        PlayerData playerData = DataManager.Instance.GetPlayerData(playerEnum);

        int countIndex = playerData.skillCardCount;

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
