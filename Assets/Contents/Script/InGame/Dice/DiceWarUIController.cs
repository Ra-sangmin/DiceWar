using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class DiceWarUIController : MonoBehaviour
{
    private DiceWarRowView myDiceWarUI;
    private DiceWarRowView enemyDiceWarUI;

    /// <summary> UI Toolkit 요소 연결 (기존 MyPanel/EnemyPanel 의 DiceWarUI 대체) </summary>
    public void InitView(VisualElement root, string myRowName, string myResultName, string enemyRowName, string enemyResultName)
    {
        myDiceWarUI = new DiceWarRowView(root.Q<VisualElement>(myRowName), root.Q<Label>(myResultName));
        enemyDiceWarUI = new DiceWarRowView(root.Q<VisualElement>(enemyRowName), root.Q<Label>(enemyResultName));

        float spacingValue = DataManager.Instance.isMultiOn ? 12 : 20;

        myDiceWarUI.SetSpacing(spacingValue);
        enemyDiceWarUI.SetSpacing(spacingValue);
    }

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

    public async UniTask AttackOn(DiceWarData myDiceWarData, DiceWarData enemyDiceWarData, CancellationTokenSource source)
    {
        DiceClear();

        myDiceWarUI.SetPlayer(myDiceWarData.playerEnum);
        enemyDiceWarUI.SetPlayer(enemyDiceWarData.playerEnum);

        int diceMax = Mathf.Max(myDiceWarData.diceResult.Count, enemyDiceWarData.diceResult.Count);

        bool isHardOn = DataManager.Instance.gameAILevel == AILevel.Hard;   //이번 판 난이도 (2026-10-08)
		int delay = isHardOn ? 0 : 50;

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

			await UniTask.Delay(delay , cancellationToken: source.Token);
		}


		myDiceWarUI.SetDiceResultText(myDiceWarData.diceSum);
        enemyDiceWarUI.SetDiceResultText(enemyDiceWarData.diceSum);

        DiceWarRowView winDiceWarUI = myDiceWarData.diceSum > enemyDiceWarData.diceSum ? myDiceWarUI : enemyDiceWarUI;

        winDiceWarUI.WinTextEffectOn();

        if (isHardOn == false)
        {
			await UniTask.Delay(300, cancellationToken: source.Token);
		}

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
