using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ApprovePopup : MonoBehaviour
{
	[SerializeField] PlayerToggleIcon PlayerToggleIcon;
	[SerializeField] Text tradeText;
	[SerializeField] Text coinText;

	[SerializeField] Button acceptBtn;

	[SerializeField] RectTransform tradePanel;
	[SerializeField] RectTransform alliancePanel;

	[SerializeField] List<RectTransform> playerIconList = new List<RectTransform>();

	public ApproveData approveData;

	public UnityAction refuseBtnClickEventOn = ()=> { };
	public UnityAction acceptBtnClickEventOn = () => { };

	// Start is called before the first frame update
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{

	}

	public void SetData(ApproveData approveData)
	{
		this.approveData = approveData;

		if (approveData.isAlliance == false) 
		{
			tradePanel.gameObject.SetActive(true);
			alliancePanel.gameObject.SetActive(false);
			LandTradeSetData(approveData.landTradeRequest);
		}
		else 
		{
			tradePanel.gameObject.SetActive(false);
			alliancePanel.gameObject.SetActive(true);
			AllianceSetData(approveData.allianceRequest);
		}
	}

	public void LandTradeSetData(LandTradeRequest landTradeRequest)
	{
		PlayerToggleIcon.SetPlayerData(landTradeRequest.fromPlayerEnum);

		int key = landTradeRequest.buyOn ? 25 : 26;

		string tradeTextValue = LocalizeManager.Instance.GetStrData(LocalizeStatus.Game, key);

		tradeText.text = tradeTextValue;

		coinText.text = landTradeRequest.coinCount.ToString();

		bool interactableValue = true;

		if (landTradeRequest.buyOn == false && DataManager.Instance.userData.myCoin.Value < landTradeRequest.coinCount)
		{
			interactableValue = false;
		}

		acceptBtn.interactable = interactableValue;

		TradeEventOn(true);
	}

	public void TradeEventOn(bool tradeOn)
	{
		AreaData areaData = DataManager.Instance.GetAreaData(approveData.landTradeRequest.areaData.id);
		areaData.TradeEventOn(tradeOn);
	}

	public void AllianceSetData(AllianceRequest allianceRequest)
	{
		AllianceData allianceData = allianceRequest.allianceDataList.FirstOrDefault(data => data.playerEnum == DataManager.Instance.playerData.pe);

		foreach (var playerIcon in playerIconList)
		{
			playerIcon.gameObject.SetActive(false);
		}

		for (int i = 0; i < allianceRequest.allianceDataList.Count; i++)
		{
			AllianceData currentAllianceData = allianceRequest.allianceDataList[i];

			int activeIndex = (int)currentAllianceData.playerEnum;

			playerIconList[activeIndex].gameObject.SetActive(true);
		}

		coinText.text = allianceData.coinCount.ToString();

		bool allReadyAlliance = DataManager.Instance.IsAlliance(DataManager.Instance.playerData.pe);

		acceptBtn.interactable = !allReadyAlliance;
	}

	public void RefuseBtnClickOn()
	{
		refuseBtnClickEventOn();
	}

	public void AcceptBtnClickOn()
	{
		if (approveData.isAlliance)
		{
			bool allReadyAlliance = DataManager.Instance.IsAlliance(DataManager.Instance.playerData.pe);

			if (allReadyAlliance)
			{
				return;
			}
		}

		acceptBtnClickEventOn();
	}
}

public class ApproveData
{
	public LandTradeRequest landTradeRequest;
	public AllianceRequest allianceRequest;
	public bool isAlliance = false;
	//public RequestProtocal requestProtocal;
}