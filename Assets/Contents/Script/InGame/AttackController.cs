using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public class AttackController
{
	private bool attackEventOn = false;
	public int choisIndex = -1;

	private PlayerIconController playerIconController;
	private InGameBottomController inGameBottomController;

	private PlayerEnum currentPlayerEnum;

	public void SetClass(PlayerIconController playerIconController , InGameBottomController inGameBottomController)
	{
		this.playerIconController = playerIconController;
		this.inGameBottomController = inGameBottomController;
	}

	List<AreaData> GetMyAttackArea(PlayerEnum playerEnum)
	{
		return DataManager.Instance.areaDataList.Where(data => data.player == playerEnum && data.dice > 1).ToList();
	}

	public async UniTask AIAttackOn(PlayerEnum playerEnum , CancellationTokenSource source)
	{
		this.currentPlayerEnum = playerEnum;

		await UniTask.Delay(500 , cancellationToken: source.Token);

		switch (DataManager.Instance.aiLevel)
		{
			case AILevel.Easy:		await EasyAttackOn(source);
				break;
			case AILevel.Normal:	await NormalAttackOn(source);
				break;
			case AILevel.Hard:		await HardAttackOn(source);
				break;
		}
	}

	public bool AttackAreaOn(AreaData attackArea, AreaData checkArea)
	{
		bool notAttackOn = false;

		if (checkArea.player != attackArea.player &&  //���� Area�� ��� Area �� ���� �÷��̶��
			DataManager.Instance.IsAllAlliance(new List<PlayerEnum>() { attackArea.player, checkArea.player }) == false)
		{
			notAttackOn = true;
		}

		return notAttackOn;
	}

	/// <summary>
	/// ������ ����� ��, �ֻ��� ������ �� ���� ���� ������, ����
	/// </summary>
	/// <param name="playerEnum"></param>
	/// <param name="attackAreaList"></param>
	/// <returns></returns>
	public async UniTask EasyAttackOn(CancellationTokenSource source)
	{
		int searchCount = 0;

		while (searchCount < 2)
		{
			List<AttackData> attackDataList = GetEasyAttackDataList();

			foreach (var attackData in attackDataList)
			{
				if (CheckData(attackData))
					continue;
				
				await AttackOn(attackData.fromAreaData, attackData.toAreaData, source);
			}

			searchCount++;
		}
	}

	bool CheckData(AttackData attackData , bool hardOn = false)
	{
		bool notAttackDataOn = false;

		AreaData fromAreaData = DataManager.Instance.GetAreaData(attackData.fromAreaData.id);
		AreaData toAreaData = DataManager.Instance.GetAreaData(attackData.toAreaData.id);

		List<PlayerEnum> checkPlayerEnumList = new List<PlayerEnum>() { fromAreaData.player, toAreaData.player };

		bool isMyAlliance = DataManager.Instance.IsAllAlliance(checkPlayerEnumList);

		if (fromAreaData.player == toAreaData.player || // 공격 과 수비 영토가 같은 플레이어 영토라면
			fromAreaData.dice == 1 || // 공격 영토의 주사위가 1개라면
			isMyAlliance) // 동맹 플레이어를 공격하는것이라면
		{
			notAttackDataOn = true;
		}
		else
		{
			if (hardOn)
			{
				notAttackDataOn = fromAreaData.dice < toAreaData.dice;
			}
		}

		return notAttackDataOn;
	}

	List<AttackData> GetEasyAttackDataList()
	{
		List<AreaData> attackAreaList = DataManager.Instance.areaDataList.Where(data => data.player == currentPlayerEnum && data.dice > 1).ToList();

		List<AttackData> attackDataList = GetAttackData(attackAreaList,0);

		if (attackDataList.Count == 0)
		{
			attackDataList = GetAttackData(attackAreaList, 1);
		}

		return attackDataList;
	}

	List<AttackData> GetAttackData(List<AreaData> attackAreaList , int status)
	{
		List<AttackData> attackDataList = new List<AttackData>();

		foreach (var attackArea in attackAreaList)
		{
			AreaData checkAreaData = attackArea.GetAttackAreaList(status); 

			if (checkAreaData != null)
			{
				attackDataList.Add(new AttackData(attackArea, checkAreaData));
			}
		}

		return attackDataList;
	}

	List<AttackData> GetAttackDataToPlayer(List<AreaData> attackAreaList, int status , PlayerEnum playerEnum)
	{
		List<AttackData> attackDataList = new List<AttackData>();

		foreach (var attackArea in attackAreaList)
		{
			AreaData checkAreaData = attackArea.GetAttackAreaListToPlayer(status, playerEnum);

			if (checkAreaData != null)
			{
				attackDataList.Add(new AttackData(attackArea, checkAreaData));
			}
		}

		return attackDataList;
	}

	/// <summary>
	/// �븻 ����
	/// </summary>
	/// <param name="playerEnum"></param>
	/// <param name="attackAreaList"></param>
	/// <returns></returns>
	public async UniTask NormalAttackOn(CancellationTokenSource source)
	{
		int searchCount = 0;

		while (searchCount < 2)
		{
			List<AttackData> attackDataList = GetNormalAttackDataList();

			foreach (var attackData in attackDataList)
			{
				if (CheckData(attackData))
					continue;

				await AttackOn(attackData.fromAreaData, attackData.toAreaData, source);
			}

			searchCount++;
		}
	}
	List<AttackData> GetNormalAttackDataList()
	{
		List<AreaData> attackAreaList = DataManager.Instance.areaDataList.Where(data => data.player == currentPlayerEnum && data.dice > 1).ToList();

		List<AttackData> attackDataList = GetAttackData(attackAreaList, 0);

		if (attackDataList.Count == 0)
		{
			attackDataList = GetAttackData(attackAreaList, 1);
		}

		return attackDataList;
	}

	/// <summary>
	/// ���� ū ����� �̾����� ���� ã��
	/// </summary>
	/// <param name="playerEnum"></param>
	/// <returns></returns>
	private async UniTask<List<AttackData>> GetAdjTarget()
	{
		//���� ū ���
		List<AreaData> bigAreaDataList = DataManager.Instance.SetBundleKey(currentPlayerEnum);

		//�ٸ� �����
		List<AreaData> otherAreaList = DataManager.Instance.areaDataList.Where(data => data.player == currentPlayerEnum && bigAreaDataList.Any(bigData => bigData.id == data.id) == false).ToList();

		List<AreaData> enemyList = new List<AreaData>();

		List<AttackData> attackDataList = new List<AttackData>();

		foreach (var areaData in bigAreaDataList)
		{
			enemyList.AddRange(areaData.CheckAdjAttakList2(otherAreaList));
		}

		foreach (var enemy in enemyList)
		{
			List<AreaData> adjList = enemy.GetAdjList();

			foreach (var adjData in adjList) 
			{
				if (adjData.player == currentPlayerEnum &&
					enemy.dice <= adjData.dice)
				{
					attackDataList.Add(new AttackData(adjData, enemy));
				}
			}
		}

		return attackDataList;
	}

	private int GetMoreDice(PlayerEnum playerEnum)
	{
		//���� ū ���� ����
		int bigAreaCount = DataManager.Instance.SetBundleKey(playerEnum).Count;

		//Stash ��
		int stashCount = DataManager.Instance.GetStashCount(playerEnum);

		//���� ������ �߰� �Ǵ� �ֻ��� ��
		int addDiceCount = stashCount + bigAreaCount;

		List<AreaData> myAllAreaDataList = DataManager.Instance.areaDataList.Where(data => data.player == playerEnum).ToList();

		int nowDiceCount = myAllAreaDataList.Sum(data => data.dice);
		int maxDiceCount = myAllAreaDataList.Count * 6;

		//���� ��� �ֻ��� �� - �ִ� �ֻ��� �� + ä���� �ֻ��� �� 
		int moreDice = nowDiceCount - maxDiceCount + addDiceCount;

		return moreDice;
	}

	public async UniTask HardAttackOn(CancellationTokenSource source)
	{
		//�� ��ü ���� ��
		int allAreaCount = DataManager.Instance.areaDataList.Where(data => data.player == currentPlayerEnum).Count();

		//���� ū ���� ��
		int bigAreaCount = DataManager.Instance.SetBundleKey(currentPlayerEnum).Count;

		//��翵���� �ֻ����� Max ����
		bool allDiceMaxOn = DataManager.Instance.areaDataList.Where(data => data.player == currentPlayerEnum).All(data => data.IsDiceMax());

		//��綥�� �ѵ�� �̰ų� �ֻ��� ���� ��� max ���
		if (allAreaCount == bigAreaCount || allDiceMaxOn)
		{
			int moreDice = Mathf.Max(GetMoreDice(currentPlayerEnum), 0);

			int attackCount = (int)(moreDice / 6f);

			await Scenario_2(attackCount , source);
		}
		else
		{
			int attackCount = allAreaCount - bigAreaCount;

			await Scenario_1(attackCount , source);
		}
	}

	async UniTask Scenario_1(int attackCount , CancellationTokenSource source)
	{
		while (attackCount > 0)
		{
			//���� ū ����� �̾����� ���� ã��
			List<AttackData> attackDataList = await GetAdjTarget();

			if (attackDataList.Count == 0)
			{
				//���� ū ���
				List<AreaData> bigAreaDataList = DataManager.Instance.SetBundleKey(currentPlayerEnum).Where(data => data.dice > 1).ToList();

				attackDataList = GetAttackData(bigAreaDataList, 0);
			}

			if (attackDataList.Count == 0)
			{
				attackCount = 0;
			}
			else
			{
				int beforeAttackCount = attackCount;

				foreach (var attackData in attackDataList)
				{
					if (CheckData(attackData , true))
						continue;

					await AttackOn(attackData.fromAreaData, attackData.toAreaData, source);

					attackCount--;

					if (attackCount <= 0)
					{
						break;
					}
				}

				if (beforeAttackCount == attackCount)
				{
					break;
				}
			}
		}
	}

	async UniTask Scenario_2(int attackCount , CancellationTokenSource source)
	{
		while (attackCount > 0)
		{
			//���� ū ����� �̾����� ���� ã��
			List<AttackData> attackDataList = await GetAdjTarget();

			if (attackDataList.Count == 0)
			{
				//���� ū ���
				List<AreaData> bigAreaDataList = DataManager.Instance.SetBundleKey(currentPlayerEnum).Where(data => data.dice > 1).ToList();

				attackDataList = GetAttackData(bigAreaDataList, 0);
			}

			if (attackDataList.Count == 0)
			{
				//���� ū ���
				List<AreaData> bigAreaDataList = DataManager.Instance.SetBundleKey(currentPlayerEnum).Where(data => data.dice > 1).ToList();

				PlayerEnum playerEnum = playerIconController.BestBigPlayer();

				//'���� ū ���信' ������ ���� �� �ֻ��� ������ �����鼭, ���� ������ ���ڰ� ū �÷��̾ ���� ����
				attackDataList = GetAttackDataToPlayer(bigAreaDataList, 1, playerEnum);

				if (attackDataList.Count == 0)
				{
					//'���� ū ���信' ������ ���� �� �ֻ��� ������ ���� ������ ����
					attackDataList.AddRange(GetAttackData(bigAreaDataList, 1));

					if (attackDataList.Count == 0)
					{
						attackCount = 0;
						break;
					}
				}
			}

			int beforeAttackCount = attackCount;

			foreach (var attackData in attackDataList)
			{
				if (CheckData(attackData, true))
					continue;

				await AttackOn(attackData.fromAreaData, attackData.toAreaData, source);

				attackCount--;

				if (attackCount <= 0)
				{
					break;
				}
			}

			if (beforeAttackCount == attackCount)
			{
				break;
			}
		}
	}

	public async UniTask<bool> AttackOn(AreaData myData, AreaData enemyData , CancellationTokenSource source)
	{
		attackEventOn = true;
		ServerManager.Instance.receiveOn = false;

		int myDice = myData.dice;
		int enemyDice = enemyData.dice;

		List<int> myDiceResult = GetReseultDiceCount(myDice);
		List<int> enemyDiceResult = GetReseultDiceCount(enemyDice);

		DiceWarData myDiceWarData = new DiceWarData(myData.player, myDiceResult);
		DiceWarData enemyDiceWarData = new DiceWarData(enemyData.player, enemyDiceResult);

		myData.ChoisEventOn(true);
		SoundManager.Instance.PlaySe(SeEnum.Yes);

		int delay = 50;
		await UniTask.Delay(delay , cancellationToken: source.Token);

		enemyData.ChoisEventOn(true);

		await UniTask.Delay(delay, cancellationToken: source.Token);

		await inGameBottomController.nonePlayPanel.AttackOn(myDiceWarData, enemyDiceWarData , source);

		//���ɿ� �����Ͽ��ٸ�

		bool win = myDiceWarData.diceSum > enemyDiceWarData.diceSum;

		if (win)
		{
			int resultDice = myData.dice - 1;
			enemyData.SetDice(resultDice);
			enemyData.PlayerChangeOn(myData.player);

			playerIconController.SetBundleKeyuAll();

			SoundManager.Instance.PlaySe(SeEnum.AttackWin);
		}
		else 
		{
			SoundManager.Instance.PlaySe(SeEnum.AttackLose);
		}

		myData.SetDice(1);

		myData.ChoisEventOn(false);
		enemyData.ChoisEventOn(false);

		if (choisIndex == myData.id)
		{
			SetChoisIndex();
		}

		if (DataManager.Instance.isMultiOn)
		{
			AttackRequestOn(myData, enemyData, myDiceWarData, enemyDiceWarData);
		}

		delay = 300;
		await UniTask.Delay(delay, cancellationToken: source.Token);

		attackEventOn = false;
		ServerManager.Instance.receiveOn = true;

		return win;
	}

	public void ForceGetAreaOn(AreaData areaData)
	{
		if (DataManager.Instance.areaGetPlayerEnum != PlayerEnum.Player_None)
		{
			PlayerEnum myPlayerEnum = DataManager.Instance.areaGetPlayerEnum;
			areaData.PlayerChangeOn(myPlayerEnum);
			playerIconController.SetBundleKeyuAll();
		}

		if (DataManager.Instance.diceGetCount != 0)
		{
			areaData.SetDice(DataManager.Instance.diceGetCount);
		}

		if (DataManager.Instance.isMultiOn)
		{
			ServerManager.Instance.ForceGetAreaRequestOn(areaData);
		}
	}

	List<int> GetReseultDiceCount(int dice)
	{
		List<int> result = new List<int>();

		for (int i = 0; i < dice; i++)
		{
			int ranDice = Random.Range(1, 7);
			result.Add(ranDice);
		}

		return result;
	}

	public void SetChoisIndex(int choisIndex = -1)
	{
		this.choisIndex = choisIndex;
	}

	private void AttackRequestOn(AreaData fromAreaData, AreaData toAreaData, DiceWarData fromDiceWarData, DiceWarData toDiceWarData)
	{
		AttackRequest request = new AttackRequest()
		{
			fromAreaData = fromAreaData.GetSendAreaData(),
			toAreaData = toAreaData.GetSendAreaData(),
			fromDiceWarData = fromDiceWarData,
			toDiceWarData = toDiceWarData,
		};

		ServerManager.Instance.SendMessageOn(request);
	}

	public async UniTask AttackReceiveDataOn(AttackRequest attackRequest)
	{
		ServerManager.Instance.receiveOn = false;

		AreaData fromAreaData = DataManager.Instance.GetAreaData(attackRequest.fromAreaData.id);
		AreaData toAreaData = DataManager.Instance.GetAreaData(attackRequest.toAreaData.id);

		fromAreaData.ChoisEventOn(true);
		await UniTask.Delay(100);
		toAreaData.ChoisEventOn(true);
		await UniTask.Delay(100);

		await inGameBottomController.nonePlayPanel.AttackOn(attackRequest.fromDiceWarData, attackRequest.toDiceWarData, new CancellationTokenSource());

		fromAreaData.ChoisEventOn(false);
		toAreaData.ChoisEventOn(false);

		DataManager.Instance.SetAreaData(attackRequest.fromAreaData);
		DataManager.Instance.SetAreaData(attackRequest.toAreaData);

		playerIconController.SetBundleKeyuAll();

		ServerManager.Instance.receiveOn = true;
	}
}

public class AttackData 
{
	public AreaData fromAreaData;
	public AreaData toAreaData;

	public AttackData() { }

	public AttackData(AreaData fromAreaData , AreaData toAreaData) 
	{
		this.fromAreaData = fromAreaData;
		this.toAreaData = toAreaData;
	}
}
