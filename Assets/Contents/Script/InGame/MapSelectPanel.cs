using UnityEngine;
using UniRx;

public class MapSelectPanel : MonoBehaviour
{
	[SerializeField] PlayCoinBtn playCoinBtn;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
		DataManager.Instance.userData.myCoin
			.Subscribe(_ => SetData())
			.AddTo(gameObject);
	}

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetData()
    {
        if (playCoinBtn != null)
        {
			playCoinBtn.SetNeedCoinCheck();
		}
	}
}
