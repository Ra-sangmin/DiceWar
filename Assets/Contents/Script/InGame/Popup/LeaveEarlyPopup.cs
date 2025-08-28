using UnityEngine;
using UnityEngine.UI;

public class LeaveEarlyPopup : MonoBehaviour
{
	[SerializeField] Text getCoinText;
	private InGameControllerBase inGameController;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	public void DataInit(InGameControllerBase inGameController , int getCoin)
	{
		this.inGameController = inGameController;

		DataManager.Instance.leaveEarlyPopupReadyOn = false;
		DataManager.Instance.leaveEarlyPopupOpenOn = true;

		getCoinText.text = $"+{getCoin}";
	}

	public void FinishBtnClickOn()
    {
		inGameController.LeaveEarlyFinishOn(true);
	}

	public void ContinueBtnClickOn()
	{
		inGameController.LeaveEarlyFinishOn(false);
		
	}

	public void FinishBtnClickOn(bool finish)
	{
		inGameController.LeaveEarlyFinishOn(finish);

		Destroy(gameObject);
	}
}
