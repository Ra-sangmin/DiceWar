using NUnit.Framework;
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;

public class ApproveController : MonoBehaviour
{
    [SerializeField] ApprovePopup approvePopup;

	[SerializeField] Button beforeBtn;
	[SerializeField] Button afterBtn;

	//List<LandTradeRequest> landTradeRequestList = new List<LandTradeRequest>();

	List<ApproveData> approveDataList = new List<ApproveData>();

	int currentIndex = -1;

	private void Awake()
	{
        approvePopup.refuseBtnClickEventOn = RefuseBtnClickEventOn;
        approvePopup.acceptBtnClickEventOn = AcceptBtnClickEventOn;
	}

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
		approvePopup.gameObject.SetActive(false);
		beforeBtn.gameObject.SetActive(false);
		afterBtn.gameObject.SetActive(false);
	}

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AddPopupOn(ApproveData approveData)
    {
		if (IsHaveCheck(approveData)) 
		{
			return;
		}

		approveDataList.Add(approveData);

        if (currentIndex == -1)
        {
            currentIndex = 0;
		}

		SetData();
	}

	public void ResetApproveData()
	{
		SetData();
	}

	private bool IsHaveCheck(ApproveData approveData)
	{
		bool allReadyHaveOn = false;

		if (approveData.isAlliance)
		{
			foreach (var checkData in approveDataList)
			{
				if (checkData.isAlliance == true &&
					checkData.allianceRequest.orderData.playerEnum == approveData.allianceRequest.orderData.playerEnum &&
					checkData.allianceRequest.allianceDataList.Count == approveData.allianceRequest.allianceDataList.Count)
				{
					allReadyHaveOn = true;
					continue;
				}
			}
		}
		else
		{
			foreach (var checkData in approveDataList)
			{
				if (checkData.isAlliance == false &&
					checkData.landTradeRequest.buyOn == approveData.landTradeRequest.buyOn &&
					checkData.landTradeRequest.areaData.id == approveData.landTradeRequest.areaData.id)
				{
					allReadyHaveOn = true;
					continue;
				}
			}
		}
		
		return allReadyHaveOn;
	}


	void SetData()
    {
        if (approveDataList.Count == 0)
        {
            approvePopup.gameObject.SetActive(false);
		}
        else
        {
			approvePopup.gameObject.SetActive(true);
			approvePopup.SetData(approveDataList[currentIndex]);
		}

		SetNextBtnActiveOn();
	}

    public void NextBtnClickOn(bool nextOn)
    {
		TradeOffCheck();

		if (nextOn)
        {
            currentIndex++;
		}
        else 
        {
			currentIndex--;
		}

		SetData();
	}

    void SetNextBtnActiveOn()
    {
        if (currentIndex < approveDataList.Count -1)
        {
			afterBtn.gameObject.SetActive(true);
		}
        else
        {
			afterBtn.gameObject.SetActive(false);
		}

		if (currentIndex >= 1)
		{
			beforeBtn.gameObject.SetActive(true);
		}
		else
		{
			beforeBtn.gameObject.SetActive(false);
		}
	}

	void RefuseBtnClickEventOn()
    {
		bool approveOn = false;

		RequestDataOn(approveOn);

		RequestClearOn(approveOn);
	}

	void AcceptBtnClickEventOn()
	{
		bool approveOn = true;

		RequestDataOn(approveOn);

		RequestClearOn(approveOn);
	}

	void RequestDataOn(bool approveOn) 
	{
		ApproveData approveData = approveDataList[currentIndex];

		if (approveData.isAlliance)
		{
			AllianceApproveRequest request = new AllianceApproveRequest()
			{
				orderData = approveData.allianceRequest.orderData,
				playerEnum = DataManager.Instance.playerData.pe,
				approveOn = approveOn,
			};

			ServerManager.Instance.SendMessageOn(request);
		}
		else
		{
			LandTradeApproveRequest request = new LandTradeApproveRequest()
			{
				landTradeRequest = approveData.landTradeRequest,
				approveOn = approveOn,
			};

			ServerManager.Instance.SendMessageOn(request);
		}
	}

	void RequestClearOn(bool approveOn)
	{
		TradeOffCheck();

		ApproveData approveData = approveDataList[currentIndex];

		approveDataList.RemoveAt(currentIndex);

		if (approveOn)
		{
			//���� ���� ������ �ߴٸ�, �ٸ� ���� ���� ����
			if (approveData.isAlliance)
			{
				approveDataList = approveDataList.Where(data => data.isAlliance == false).ToList();
			}
			//���� �ŷ� ������ �ߴٸ�
			else 
			{
				bool buyOn = approveData.landTradeRequest.buyOn;

				//���� ���� ���� �̾��ٸ�
				if (buyOn)
				{
					int areaIndex = approveData.landTradeRequest.areaData.id;

					List<ApproveData> tempApproveDataList = new List<ApproveData>();

					foreach (var tempApproveData in approveDataList)
					{
						if (tempApproveData.isAlliance == false && 
							tempApproveData.landTradeRequest.buyOn == true &&
							tempApproveData.landTradeRequest.areaData.id == areaIndex)
						{
							continue;
						}

						tempApproveDataList.Add(tempApproveData);
					}

					approveDataList = tempApproveDataList;
				}
			}
		}

		if (approveDataList.Count == 0)
		{
			currentIndex = -1;
		}
		else
		{
			if (currentIndex >= approveDataList.Count)
			{
				currentIndex = approveDataList.Count - 1;
			}
		}

		SetData();
	}

	void TradeOffCheck()
	{
		ApproveData approveData = approveDataList[currentIndex];

		if (approveData.isAlliance == false)
		{
			approvePopup.TradeEventOn(false);
		}
	}
}
