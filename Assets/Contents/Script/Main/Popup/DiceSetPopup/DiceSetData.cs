using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class DiceSetData : MonoBehaviour
{
	[SerializeField] bool isAI;
	[SerializeField] AILevel aiLevel;
	[SerializeField] InputField minInput;
	[SerializeField] InputField maxInput;

	private DiceCountData diceCountData;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
		diceCountData = DiceCountManager.Instance.GetData(isAI, aiLevel);

		minInput.text = diceCountData.minCount.ToString();
		maxInput.text = diceCountData.maxCount.ToString();

		minInput.onValueChanged.AddListener(data => { diceCountData.minCount = int.Parse(data); DiceCountManager.Instance.SetData(diceCountData); });
		maxInput.onValueChanged.AddListener(data => { diceCountData.maxCount = int.Parse(data); DiceCountManager.Instance.SetData(diceCountData); });
	}

    // Update is called once per frame
    void Update()
    {
        
    }
}
