using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class DiceWarUIController : MonoBehaviour
{
    [SerializeField] DiceWarUI myDiceWarUI;
    [SerializeField] DiceWarUI enemyDiceWarUI;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetTransform(Transform parent)
    {
        transform.SetParent(parent);
        RectTransform rt = transform as RectTransform;
        //Left 값 설정
        rt.offsetMin = new Vector2(0, rt.offsetMin.y);
        //Right 값 설정
        rt.offsetMax = new Vector2(0, rt.offsetMax.y);

        float spacingValue = DataManager.Instance.isMultiOn ? 12 : 20;

        myDiceWarUI.GetComponentInChildren<HorizontalLayoutGroup>().spacing = spacingValue;
        enemyDiceWarUI.GetComponentInChildren<HorizontalLayoutGroup>().spacing = spacingValue;

    }

    public void DiceClear()
    {
        myDiceWarUI.DiceClear();
        enemyDiceWarUI.DiceClear();
    }

    public async UniTask AttackOn(DiceWarData myDiceWarData, DiceWarData enemyDiceWarData)
    {
        DiceClear();

        myDiceWarUI.SetPlayer(myDiceWarData.playerEnum);
        enemyDiceWarUI.SetPlayer(enemyDiceWarData.playerEnum);

        int diceMax = Mathf.Max(myDiceWarData.diceResult.Count, enemyDiceWarData.diceResult.Count);
        
        for (int i = 0; i < diceMax; i++)
        {
            if (myDiceWarData.diceResult.Count > i)
            {
                myDiceWarUI.DiceOn(i, myDiceWarData.diceResult[i]);
            }

            if (enemyDiceWarData.diceResult.Count > i)
            {
                enemyDiceWarUI.DiceOn(i, enemyDiceWarData.diceResult[i]);
            }
        }

        myDiceWarUI.SetDiceResultText(myDiceWarData.diceSum);
        enemyDiceWarUI.SetDiceResultText(enemyDiceWarData.diceSum);

        DiceWarUI winDiceWarUI = myDiceWarData.diceSum > enemyDiceWarData.diceSum ? myDiceWarUI : enemyDiceWarUI;

        winDiceWarUI.WinTextEffectOn();
        //yield return 
    }
}

[System.Serializable]
public class DiceWarData
{
    public PlayerEnum playerEnum = PlayerEnum.Player_None;
    public List<int> diceResult = new List<int>();
    public int diceSum;

    public DiceWarData(PlayerEnum playerEnum, List<int> diceResult)
    {
        this.playerEnum = playerEnum;
        this.diceResult = diceResult;
        diceSum = diceResult.Sum(x => x);
    }
}
