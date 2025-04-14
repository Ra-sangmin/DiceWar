using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;


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

    public void DiceClear()
    {
        myDiceWarUI.DiceClear();
        enemyDiceWarUI.DiceClear();
    }

    public IEnumerator AttackOn(DiceWarData myDiceWarData, DiceWarData enemyDiceWarData)
    {
        DiceClear();

        myDiceWarUI.SetPlayer(myDiceWarData.playerEnum);
        enemyDiceWarUI.SetPlayer(enemyDiceWarData.playerEnum);

        for (int i = 0; i < 8; i++)
        {
            if (myDiceWarData.diceResult.Count > i)
            {
                myDiceWarUI.DiceOn(i, myDiceWarData.diceResult[i]);
            }

            if (enemyDiceWarData.diceResult.Count > i)
            {
                enemyDiceWarUI.DiceOn(i, enemyDiceWarData.diceResult[i]);
            }

            yield return new WaitForSeconds(0.02f);
            //yield return new WaitForSeconds(0.2f);
        }

        myDiceWarUI.SetDiceResultText(myDiceWarData.diceSum);
        enemyDiceWarUI.SetDiceResultText(enemyDiceWarData.diceSum);

        DiceWarUI winDiceWarUI = myDiceWarData.diceSum > enemyDiceWarData.diceSum ? myDiceWarUI : enemyDiceWarUI;

        winDiceWarUI.WinTextEffectOn();
        //yield return 
    }
}

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
