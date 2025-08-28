using UnityEngine;
using UniRx;
using UnityEngine.UI;

public class UnirxDataManager : MonoBehaviour
{
    private Button button;

    bool activeOn = false;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //button.onClick.AddListener(() => Debug.Log("Å¬¸¯µÊ"));


        button
            .OnClickAsObservable()
            .Where(_ => activeOn == true)
            .Subscribe(_ => Debug.Log("Å¬¸¯µÊ"));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
