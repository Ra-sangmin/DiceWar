using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static GameResultPopup;

public class InGameControllerSingle : InGameControllerBase
{
	[SerializeField] ApproveController approveController;

	protected override void Awake()
	{
		base.Awake();
	}

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	protected override void StartReady()
	{
		base.StartReady();
	}

	protected override void Update()
	{
		base.Update();
	}

	protected override void MyTurnPlayOn()
	{
		endTurnBtn.SetEnabled(true);
	}

	protected override async UniTask OtherTurnPlayOn()
	{
		await base.OtherTurnPlayOn();

		//TurnOffOn();
	}

	protected override void OtherPlayerNotOn()
	{
		TurnOffOn();
	}

	protected override int GetCoin(GameResultEnum gameResultEnum)
	{
		int coinCount = 0;

		if (gameResultEnum == GameResultEnum.Win || gameResultEnum == GameResultEnum.LeaveEarly)
		{
			coinCount = DataManager.Instance.GetSingleRewardCoin(gameResultEnum);
		}

		return coinCount;
	}

	protected override async UniTask AIPlayOn(PlayerEnum currentPlayer , CancellationTokenSource source)
	{
		await mapController.AIAttackOn(currentPlayer, source);

		await UniTask.Delay(100, cancellationToken: source.Token);

		EndTurnEventOn();

		//mapController.EndTurnBtnClickOn();
	}

	protected override void EndTurnEventOn()
	{
		base.EndTurnEventOn();

		SingleEndTurnEventOn().Forget();
		
	}

	private async UniTask SingleEndTurnEventOn()
	{
		mapController.DiceAddOn(DataManager.Instance.currentPlayer);

		await UniTask.Delay(200);

		//조기 종료는 카운트다운으로 대체했다 (26.10.04 수정 제안, 2026-10-08)
		//if (DataManager.Instance.CheckLeaveEarly(mapController.playerIconController))
		//{
		//	LeaveEarlyPopupOn();
		//}

		TurnOffOn();
	}
}
