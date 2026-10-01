using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using System.Linq;

public class ApproveController : MonoBehaviour
{
    [SerializeField] ApprovePopup approvePopup;

	private UnityEngine.UIElements.Button beforeBtn;
	private UnityEngine.UIElements.Button afterBtn;
	private Label countBadge;

	//List<LandTradeRequest> landTradeRequestList = new List<LandTradeRequest>();

	List<ApproveData> approveDataList = new List<ApproveData>();

	int currentIndex = -1;

	private void Awake()
	{
        approvePopup.refuseBtnClickEventOn = RefuseBtnClickEventOn;
        approvePopup.acceptBtnClickEventOn = AcceptBtnClickEventOn;
	}

	/// <summary> UI Toolkit 요소 연결 </summary>
	public void InitView(VisualElement root)
	{
		approvePopup.InitView(root);

		beforeBtn = root.Q<UnityEngine.UIElements.Button>("approve-before-btn");
		afterBtn = root.Q<UnityEngine.UIElements.Button>("approve-after-btn");
		countBadge = root.Q<Label>("approve-count-badge");

		if (beforeBtn != null)
		{
			beforeBtn.clicked += () => NextBtnClickOn(false);
		}

		if (afterBtn != null)
		{
			afterBtn.clicked += () => NextBtnClickOn(true);
		}

		approvePopup.SetPanelActive(false);
		SetBtnActive(beforeBtn, false);
		SetBtnActive(afterBtn, false);
	}

	static void SetBtnActive(UnityEngine.UIElements.Button btn, bool activeOn)
	{
		if (btn == null)
			return;

		btn.style.display = activeOn ? DisplayStyle.Flex : DisplayStyle.None;
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

	/// <summary>
	/// 다른 플레이어가 동맹 제안에 답했다. 대기 중인 그 요청에 기록해 두고,
	/// 지금 보고 있는 요청이면 바의 체크 / X 를 바로 갱신한다. (2026-09-19)
	/// </summary>
	public void AllianceApprovedOn(PlayerEnum orderPlayerEnum, PlayerEnum playerEnum, bool approveOn)
	{
		ApproveData approveData = approveDataList.FirstOrDefault(data => data.isAlliance &&
			data.allianceRequest != null &&
			data.allianceRequest.orderData.playerEnum == orderPlayerEnum);

		if (approveData == null)
			return;

		approveData.answerDic[playerEnum] = approveOn;

		if (currentIndex < 0 || currentIndex >= approveDataList.Count)
			return;

		if (approveDataList[currentIndex] != approveData)
			return;

		approvePopup.SetAllianceAnswer(playerEnum, approveOn);
		approvePopup.SetAllianceAcceptEnabled(approveData);
	}

	/// <summary> 제안자가 철회했을 때, 그 사람이 낸 동맹 요청을 대기 목록에서 지운다 (2026-09-19) </summary>
	public void RemoveAllianceOn(PlayerEnum orderPlayerEnum)
	{
		int removeCount = approveDataList.RemoveAll(data => data.isAlliance &&
			data.allianceRequest != null &&
			data.allianceRequest.orderData.playerEnum == orderPlayerEnum);

		if (removeCount == 0)
			return;

		if (approveDataList.Count == 0)
		{
			currentIndex = -1;
		}
		else if (currentIndex >= approveDataList.Count)
		{
			currentIndex = approveDataList.Count - 1;
		}

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
					checkData.landTradeRequest.areaData.id == approveData.landTradeRequest.areaData.id &&
					checkData.landTradeRequest.coinCount == approveData.landTradeRequest.coinCount)   //교환 : 주고받는 두 땅이 모두 같아야 같은 제안
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
            approvePopup.SetPanelActive(false);
		}
        else
        {
			approvePopup.SetPanelActive(true);
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
		SetBtnActive(afterBtn, currentIndex < approveDataList.Count - 1);

		SetBtnActive(beforeBtn, currentIndex >= 1);

		//시안 : 첫 제안을 보고 있을 때 → 버튼 위에 대기 중인 제안 수 (2026-09-26)
		if (countBadge != null)
		{
			bool badgeOn = currentIndex == 0 && approveDataList.Count > 1;
			countBadge.SetNumber(approveDataList.Count);
			countBadge.style.display = badgeOn ? DisplayStyle.Flex : DisplayStyle.None;
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
				//영토 교환을 수락했다면, 같은 땅이 걸린 다른 교환 제안은 더 이상 성립하지 않으므로 지운다 (2026-09-26)
				//  areaData.id = 내가 줄 땅, coinCount = 내가 받을 땅의 id
				int giveAreaId = approveData.landTradeRequest.areaData.id;
				int receiveAreaId = approveData.landTradeRequest.coinCount;

				approveDataList = approveDataList.Where(data => data.isAlliance ||
					(data.landTradeRequest.areaData.id != giveAreaId &&
					 data.landTradeRequest.areaData.id != receiveAreaId &&
					 data.landTradeRequest.coinCount != giveAreaId &&
					 data.landTradeRequest.coinCount != receiveAreaId)).ToList();
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
