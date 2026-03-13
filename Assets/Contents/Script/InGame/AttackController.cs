using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class AttackController
{
	private bool attackEventOn = false;
	public int choisIndex = -1;

	private PlayerIconController playerIconController;
	private InGameBottomController inGameBottomController;

	private PlayerEnum currentPlayerEnum;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

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
			//case AILevel.Normal:    await EasyAttackOn(playerEnum);
				break;
			case AILevel.Hard:		await HardAttackOn(source);
				break;
		}

		//if (DataManager.Instance.isMultiOn)
		//{
		//	await UniTask.Delay(500);
		//}
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

	bool CheckData(AttackData attackData)
	{
		bool notAttackDataOn = false;

		AreaData fromAreaData = DataManager.Instance.GetAreaData(attackData.fromAreaData.id);
		AreaData toAreaData = DataManager.Instance.GetAreaData(attackData.toAreaData.id);

		if (fromAreaData.player == toAreaData.player ||
			fromAreaData.dice == 1)
		{
			notAttackDataOn = true;
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


	///// <summary>
	///// ���� ū ������� ������ ���信��, �ٸ� ����� �̾��� �� �ִ� ���� ã�� �� ���� �ֻ��� ���� �̻��̸�, ���� ex) �ֻ��� 3���� 3���� ������ �� ����.
	///// </summary>
	///// <param name="playerEnum"></param>
	///// <param name="attackAreaList"></param>
	///// <returns></returns>
	//public async UniTask NormalAttackOn_old(PlayerEnum playerEnum, List<AreaData> attackAreaList)
	//{
	//	List<AttackData> attackDataList = await GetAdjTarget();

	//	Debug.LogWarning(attackDataList.Count);

	//	if (attackDataList.Count != 0)
	//	{
	//		foreach (var attackData in attackDataList)
	//		{
	//			AreaData fromAreaData = DataManager.Instance.GetAreaData(attackData.fromAreaData.id);
	//			AreaData toAreaData = DataManager.Instance.GetAreaData(attackData.toAreaData.id);

	//			if (fromAreaData.player == toAreaData.player ||
	//				fromAreaData.dice == 1 ||
	//				fromAreaData.dice < toAreaData.dice)
	//			{
	//				continue;
	//			}

	//			await AttackOn(attackData.fromAreaData, attackData.toAreaData,null);

	//			await UniTask.Delay(3000);
	//		}
	//	}
	//	else
	//	{
	//		int attackCount = 3;

	//		foreach (var attackArea in attackAreaList)
	//		{
	//			var checkAreaData = attackArea.GetAdjList().FirstOrDefault(checkArea => AttackAreaOn(attackArea, checkArea) && attackArea.dice >= checkArea.dice);

	//			if (checkAreaData != null)
	//			{
	//				await AttackOn(attackArea, checkAreaData, null);

	//				attackCount--;

	//				if (attackCount < 0)
	//				{
	//					break;
	//				}
	//			}
	//		}
	//	}
	//}

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
			//Debug.LogWarning("areaData = " + areaData.id);
		}

		foreach (var areaData in otherAreaList)
		{
			//Debug.LogWarning("other = " + areaData.id);
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

			//Debug.LogWarning("result Target = " + target.id);
		}

		//await UniTask.Delay(3000);

		return attackDataList;



		//List<AreaData> areaData = DataManager.Instance.SetBundleKey(playerIcon.playerEnum);

		//Dictionary<PlayerEnum, List<AreaData>> areaDataDic = new Dictionary<PlayerEnum, List<AreaData>>();

		//foreach (var playerIcon in playerIconList)
		//{
		//	//üũ ����� �����̶��
		//	if (playerIcon.playerEnum == playerEnum)
		//		continue;

		//	//�����̶��
		//	if (DataManager.Instance.IsAllAlliance(new List<PlayerEnum>() { playerEnum, playerIcon.playerEnum }))
		//		continue;

		//	List<AreaData> areaData = DataManager.Instance.SetBundleKey(playerIcon.playerEnum);

		//	//targetAreaDataList.AddRange(areaData);

		//	//areaDataDic.Add(playerIcon.playerEnum, areaData);
		//}

		
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

		//���� ���� ū ���� ����
		//attackCount = await EnemyBigAreaAttack(playerEnum, attackAreaList, attackCount);
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
					AreaData fromAreaData = DataManager.Instance.GetAreaData(attackData.fromAreaData.id);
					AreaData toAreaData = DataManager.Instance.GetAreaData(attackData.toAreaData.id);

					if (fromAreaData.player == toAreaData.player ||
						fromAreaData.dice == 1 ||
						fromAreaData.dice < toAreaData.dice)
					{
						continue;
					}

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
				AreaData fromAreaData = DataManager.Instance.GetAreaData(attackData.fromAreaData.id);
				AreaData toAreaData = DataManager.Instance.GetAreaData(attackData.toAreaData.id);

				if (fromAreaData.player == toAreaData.player ||
					fromAreaData.dice == 1 ||
					fromAreaData.dice < toAreaData.dice)
				{
					continue;
				}

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

	//public async UniTask HardAttackOn_old(List<AreaData> attackAreaList)
	//{
	//	//�� ��ü ���� ��
	//	int allAreaCount = DataManager.Instance.areaDataList.Where(data => data.player == currentPlayerEnum).Count();

	//	//���� ū ���� ��
	//	int bigAreaCount = DataManager.Instance.SetBundleKey(currentPlayerEnum).Count;

	//	int attackCount = allAreaCount - bigAreaCount;

	//	//��綥�� �ѵ�� ���
	//	if (attackCount == 0)
	//	{
	//		int moreDice = Mathf.Max(GetMoreDice(currentPlayerEnum), 0);

	//		attackCount = Mathf.RoundToInt((moreDice) / 7.0f);
	//	}
	//	else 
	//	{
	//		//���� ū ����� �̾����� ���� ã��
	//		attackCount = await MyBigAreaContinueAttack(attackCount);
	//	}

	//	//���� ���� ū ���� ����
	//	attackCount = await EnemyBigAreaAttack(attackAreaList, attackCount);
	//}

	//private async UniTask<int> EnemyBigAreaAttack(List<AreaData> attackAreaList , int attackCount)
	//{
	//	if(attackCount <=  0) 
	//	{
	//		return 0;
	//	}

	//	List<AreaData> targetAreaDataList = GetBigAreaTarget(currentPlayerEnum, attackAreaList);

	//	if (targetAreaDataList.Count != 0)
	//	{
	//		foreach (var targetAreaData in targetAreaDataList)
	//		{
	//			if (attackCount <= 0)
	//			{
	//				break;
	//			}

	//			foreach (var myAreaData in targetAreaData.CheckAdj(currentPlayerEnum))
	//			{
	//				AreaData fromAreaData = DataManager.Instance.GetAreaData(myAreaData.id);
	//				AreaData toAreaData = DataManager.Instance.GetAreaData(targetAreaData.id);

	//				if (fromAreaData.player == toAreaData.player ||
	//					fromAreaData.dice == 1 ||
	//					fromAreaData.dice < toAreaData.dice)
	//				{
	//					continue;
	//				}

	//				await AttackOn(fromAreaData, toAreaData, null);

	//				attackCount--;

	//				if (attackCount <= 0)
	//				{
	//					break;
	//				}
	//			}
	//		}
	//	}

	//	return attackCount;
	//}

	//private async UniTask<int> MyBigAreaContinueAttack(int attackCount)
	//{
	//	if (attackCount <= 0)
	//	{
	//		return 0;
	//	}

	//	//���� ū ����� �̾����� ���� ã��
	//	List<AttackData> attackDataList = await GetAdjTarget();

	//	if (attackDataList.Count != 0)
	//	{
	//		foreach (var attackData in attackDataList)
	//		{
	//			AreaData fromAreaData = DataManager.Instance.GetAreaData(attackData.fromAreaData.id);
	//			AreaData toAreaData = DataManager.Instance.GetAreaData(attackData.toAreaData.id);

	//			if (fromAreaData.player == toAreaData.player ||
	//				fromAreaData.dice == 1 ||
	//				fromAreaData.dice < toAreaData.dice)
	//			{
	//				continue;
	//			}

	//			await AttackOn(attackData.fromAreaData, attackData.toAreaData,null);

	//			attackCount--;

	//			if (attackCount <= 0)
	//			{
	//				break;
	//			}
	//		}
	//	}
	//	else
	//	{
	//		attackCount = 0;
	//	}

	//	return attackCount;
	//}

	//private List<AreaData> GetBigAreaTarget(PlayerEnum playerEnum , List<AreaData> attackAreaList)
	//{
	//	List<PlayerIcon> playerIconList = playerIconController.GetActiveList();

	//	Dictionary<PlayerEnum, List<AreaData>> areaDataDic = new Dictionary<PlayerEnum, List<AreaData>>();

	//	//���� ���� ū ����� �������� ���
	//	List<AreaData> bigAreaDataList = new List<AreaData>();

	//	foreach (var playerIcon in playerIconList)
	//	{
	//		//üũ ����� �����̶��
	//		if (playerIcon.playerEnum == playerEnum)
	//			continue;

	//		//�����̶��
	//		if (DataManager.Instance.IsAllAlliance(new List<PlayerEnum>() { playerEnum, playerIcon.playerEnum }))
	//			continue;

	//		List<AreaData> areaData = DataManager.Instance.SetBundleKey(playerIcon.playerEnum);

	//		bigAreaDataList.AddRange(areaData);
	//	}

	//	List<AreaData> targetAreaData = new List<AreaData>();

	//	foreach (var attackArea in attackAreaList)
	//	{
	//		List<AreaData> temnpCheckAreaDataList = attackArea.CheckAdjAttakList(bigAreaDataList);

	//		foreach (var temnpCheckAreaData in temnpCheckAreaDataList)
	//		{
	//			if (targetAreaData.Any(data => data.id == temnpCheckAreaData.id) == false)
	//			{
	//				targetAreaData.Add(temnpCheckAreaData);
	//			}
	//		}
	//	}

	//	return targetAreaData;
	//}

	//public async UniTask<bool> NoneAttackOn(AreaData attackArea) 
	//{
	//	bool attackOn = false;

	//	var checkAreaData = attackArea.GetAdjList().FirstOrDefault(checkArea => AttackAreaOn(attackArea, checkArea) && attackArea.dice >= checkArea.dice);

	//	if (checkAreaData != null)
	//	{
	//		await AttackOn(attackArea, checkAreaData, null);

	//		attackOn = true;
	//	}

	//	return attackOn; 
	//}


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
