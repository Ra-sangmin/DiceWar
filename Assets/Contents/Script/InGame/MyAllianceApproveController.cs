using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MyAllianceApproveController : MonoBehaviour
{
    [SerializeField] CanvasGroup canvasGroup;
	[SerializeField] List<MyAllianceApproveIcon> playerIconList = new List<MyAllianceApproveIcon>();
    [SerializeField] Text approvedCountText;
	
	private void Awake()
	{
        Init();
	}

    private void Init()
    {
        foreach (var icon in playerIconList) 
        {
			icon.Init();
			icon.gameObject.SetActive(false);
		}

        SetApprovedCountText();

        canvasGroup.alpha = 0;
	}

    public void SetData(AllianceRequest allianceRequest)
    {
        Init();

		var playerEnumList = allianceRequest.allianceDataList.Select(data => data.playerEnum).ToList();
        SetActiveList(playerEnumList);

        ApprovedOn(allianceRequest.orderData.playerEnum, true);

        canvasGroup.DOFade(1, 0.25f);
	}

    public void SetActiveList(List<PlayerEnum> playerEnumList)
    {
        foreach (var playerEnum in playerEnumList)
        {
            var icon = playerIconList.FirstOrDefault(data => data.playerEnum == playerEnum);

            if (icon != null)
            {
                icon.gameObject.SetActive(true);
			}
		}
    }

    public void ApprovedOn(PlayerEnum playerEnum , bool approvedOn )
    {
        var icon = playerIconList.FirstOrDefault(data => data.playerEnum == playerEnum);

        if (icon != null)
        {
			icon.ApproveOn(approvedOn);
		}

        SetApprovedCountText();
	}

	private void SetApprovedCountText()
	{
		var activeList = playerIconList.Where(data => data.gameObject.activeSelf).ToList();

		int currentCount = activeList.Count(data => data.approvedOn);

		approvedCountText.text = $"{currentCount}/{activeList.Count}";
	}

    public void FadeOn()
    {
		canvasGroup.DOFade(0, 1f).SetDelay(1f);
	}

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


}
