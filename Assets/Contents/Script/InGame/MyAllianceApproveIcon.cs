using UnityEngine;

public class MyAllianceApproveIcon : MonoBehaviour
{
    public PlayerEnum playerEnum;

    [SerializeField] RectTransform aprroveOnObj;
	[SerializeField] RectTransform aprroveOffObj;

    public bool approvedOn = false;

	private void Awake()
	{
		//sInit();
	}

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
        
    }

    public void Init() 
    {
		aprroveOnObj.gameObject.SetActive(false);
		aprroveOffObj.gameObject.SetActive(false);

		approvedOn = false;
	}


	public void SetData()
    {

    }

    public void ApproveOn(bool approveOn)
    {
        this.approvedOn = approveOn;

		aprroveOnObj.gameObject.SetActive(approveOn);
		aprroveOffObj.gameObject.SetActive(!approveOn);
	}

    // Update is called once per frame
    void Update()
    {
        
    }
}
