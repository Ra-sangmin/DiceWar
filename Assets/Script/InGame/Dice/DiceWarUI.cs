using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class DiceWarUI : MonoBehaviour
{
    [SerializeField] Text resultText;
    [SerializeField] DiceUI diceUIPrefab;
    private List<DiceUI> diceUIList = new List<DiceUI>();

    private void Awake()
    {
        for (int i = 0; i < 8; i++)
        {
            DiceUI diceUI = Instantiate(diceUIPrefab, diceUIPrefab.transform.parent);
            diceUIList.Add(diceUI);
        }   
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetPlayer(PlayerEnum playerEnum)
    {
        foreach (var diceUI in diceUIList)
        {
            diceUI.SetPlayer(playerEnum);
        }
    }

    public void DiceClear()
    {
        resultText.text = string.Empty;

        foreach (var diceUI in diceUIList)
        {
            diceUI.gameObject.SetActive(false);
        }
    }

    public void DiceOn(int index , int diceStatus)
    {
        diceUIList[index].gameObject.SetActive(true);
        diceUIList[index].SetDice(diceStatus);
    }

    public void SetDiceResultText(int diceSum)
    {
        resultText.text = diceSum.ToString();
    }

    public void WinTextEffectOn()
    {
        resultText.transform.DOScale(1.1f, 0.2f).SetLoops(4, LoopType.Yoyo);
        //yield return new WaitForSeconds(1);
    }
}
