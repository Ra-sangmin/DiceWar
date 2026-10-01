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
		//모든 영토 수
		int allAreaCount = DataManager.Instance.areaDataList.Where(data => data.player == currentPlayerEnum).Count();

		//연결된 영토 수 (본인의 connectedCount = 가장 큰 덩어리의 땅 개수)
		int bigAreaCount = DataManager.Instance.SetBundleKey(currentPlayerEnum).Count;

		AreaData startAreaData = null;

		//본인의 connectedCount가 1인가? (= 가장 큰 덩어리가 땅 1개짜리인가?)
		if (bigAreaCount == 1)
		{
			//두 땅을 연결할 수 있는 땅이 있는가? (자신의 두 땅이 땅 하나를 사이에 두고 있는가?)
			//And 인접한 자신의 땅 중 주사위 2개 이상인 것이 있어서 공격 가능한가?
			AttackData bridgeAttackData = GetBridgeAttackData();

			if (bridgeAttackData != null)
			{
				//그 땅을 공격
				await AttackOn(bridgeAttackData.fromAreaData, bridgeAttackData.toAreaData, source);

				return;
			}

			//땅 1개짜리 덩어리 중 가장 주사위가 많은 땅에서 시작
			startAreaData = DataManager.Instance.areaDataList
				.Where(data => data.player == currentPlayerEnum)
				.OrderByDescending(data => data.dice)
				.FirstOrDefault();
		}

		//공격 횟수 : 두 숫자 중 더 큰 숫자 (AI 알고리즘 수정.pptx, 2026-09-26)
		// 첫째. '모든 땅이 주사위 6개가 되는 공격 횟수' + 1 → 턴이 끝나고 채워져도 6개가 아닌 땅이 생기도록
		// 둘째. 전체 땅 개수 - 가장 큰 덩어리의 땅 개수
		int moreDice = Mathf.Max(GetMoreDice(currentPlayerEnum), 0);
		int fillAttackCount = (int)(moreDice / 6f) + 1;
		int areaAttackCount = allAreaCount - bigAreaCount;
		int attackCount = Mathf.Max(fillAttackCount, areaAttackCount);

		//공격 우선순위대로 한 번씩 골라 공격한다. 해당하는 땅이 없으면 횟수가 남아도 턴 종료 (2026-09-26)
		await HardScenario(attackCount, source, startAreaData);
	}


	/// <summary>
	/// 서로 떨어져 있는 자신의 두 땅을 연결할 수 있는 땅(다리 역할)이 있는지 확인하고,
	/// 그 땅을 공격할 수 있는(주사위 2개 이상) 인접한 자신의 땅을 찾는다.
	/// </summary>
	AttackData GetBridgeAttackData()
	{
		foreach (var target in DataManager.Instance.areaDataList)
		{
			if (target.player == currentPlayerEnum)
				continue;

			if (DataManager.Instance.IsAllAlliance(new List<PlayerEnum>() { currentPlayerEnum, target.player }))
				continue;

			List<AreaData> myAdjList = target.GetAdjList().Where(data => data.player == currentPlayerEnum).ToList();

			//두 땅을 연결할 수 있는 땅인가 (자신의 두 땅이 이 땅을 사이에 두고 있는가)
			if (myAdjList.Count < 2)
				continue;

			//인접한 자신의 땅 중 주사위 2개 이상인 것이 있어서 공격 가능한가
			AreaData attacker = myAdjList.Where(data => data.dice >= 2).OrderByDescending(data => data.dice).FirstOrDefault();

			if (attacker != null)
			{
				return new AttackData(attacker, target);
			}
		}

		return null;
	}

	List<AreaData> GetBigAreaDataList(AreaData anchorArea)
	{
		if (anchorArea != null)
		{
			return new List<AreaData>() { anchorArea };
		}

		return DataManager.Instance.SetBundleKey(currentPlayerEnum);
	}


	/// <summary>
	/// Hard AI 공격 루프 (AI 알고리즘 수정.pptx 슬라이드 2, 2026-09-26)
	/// 공격할 때마다 땅 주인 / 주사위가 바뀌므로 매번 다시 고른다.
	/// </summary>
	async UniTask HardScenario(int attackCount, CancellationTokenSource source, AreaData anchorArea = null)
	{
		while (attackCount > 0)
		{
			AttackData attackData = GetHardAttackData(anchorArea);

			//우선순위에 해당하는 땅이 없으면 공격 횟수가 남더라도 턴을 종료한다
			if (attackData == null)
				break;

			await AttackOn(attackData.fromAreaData, attackData.toAreaData, source);

			attackCount--;

			//'땅 1개짜리 덩어리에서 시작' 은 첫 공격에만 쓰고, 이후에는 가장 큰 덩어리 기준
			anchorArea = null;
		}
	}

	/// <summary>
	/// 공격 우선순위 (가장 큰 덩어리와 인접한 땅을 공격)
	///  첫째. 다른 덩어리와 연결할 수 있으면서, 공격받는 땅의 주사위 ≤ 공격하는 땅의 주사위
	///  둘째. 공격받는 땅의 주사위 &lt; 공격하는 땅의 주사위
	///  셋째. 공격받는 땅의 주사위 ≤ 공격하는 땅의 주사위
	///  → 같은 우선순위 중에서는 세력(연결된 땅 수)이 더 큰 플레이어의 땅을 공격
	/// </summary>
	AttackData GetHardAttackData(AreaData anchorArea)
	{
		List<AreaData> myAreaList = DataManager.Instance.areaDataList.Where(data => data.player == currentPlayerEnum).ToList();

		List<AreaData> bigAreaList = GetBigAreaDataList(anchorArea);
		HashSet<int> bigIdSet = new HashSet<int>(bigAreaList.Select(data => data.id));

		//가장 큰 덩어리가 아닌 내 땅 (첫째 순위의 '다른 덩어리')
		HashSet<int> otherIdSet = new HashSet<int>(myAreaList.Where(data => bigIdSet.Contains(data.id) == false).Select(data => data.id));

		//가장 큰 덩어리와 인접한 공격 가능한 적 땅
		Dictionary<int, AreaData> targetDic = new Dictionary<int, AreaData>();

		foreach (var bigArea in bigAreaList)
		{
			foreach (var adj in bigArea.GetAdjList())
			{
				if (IsEnemyArea(adj) && targetDic.ContainsKey(adj.id) == false)
				{
					targetDic.Add(adj.id, adj);
				}
			}
		}

		List<AttackData> firstList = new List<AttackData>();
		List<AttackData> secondList = new List<AttackData>();
		List<AttackData> thirdList = new List<AttackData>();

		foreach (var target in targetDic.Values)
		{
			List<AreaData> targetAdjList = target.GetAdjList();

			//첫째 : 이 땅을 먹으면 가장 큰 덩어리와 다른 덩어리가 이어진다.
			//       공격은 양쪽 덩어리 어느 쪽에서든 가능하다 (주사위가 가장 많은 땅으로)
			bool bridgeOn = targetAdjList.Any(data => otherIdSet.Contains(data.id));

			if (bridgeOn)
			{
				AreaData bridgeAttacker = targetAdjList
					.Where(data => data.player == currentPlayerEnum && data.dice > 1 && data.dice >= target.dice)
					.Where(data => anchorArea == null || data.id == anchorArea.id)
					.OrderByDescending(data => data.dice)
					.FirstOrDefault();

				if (bridgeAttacker != null)
				{
					firstList.Add(new AttackData(bridgeAttacker, target));
					continue;
				}
			}

			//둘째 / 셋째 : 가장 큰 덩어리의 땅 중 주사위가 가장 많은 땅으로
			AreaData attacker = targetAdjList
				.Where(data => bigIdSet.Contains(data.id) && data.dice > 1 && data.dice >= target.dice)
				.OrderByDescending(data => data.dice)
				.FirstOrDefault();

			if (attacker == null)
				continue;

			if (target.dice < attacker.dice)
			{
				secondList.Add(new AttackData(attacker, target));
			}
			else
			{
				thirdList.Add(new AttackData(attacker, target));
			}
		}

		foreach (var list in new List<List<AttackData>>() { firstList, secondList, thirdList })
		{
			if (list.Count == 0)
				continue;

			//같은 우선순위 : 세력이 큰 플레이어의 땅 → 주사위 차이가 큰 공격 순
			return list
				.OrderByDescending(data => GetPlayerPower(data.toAreaData.player))
				.ThenByDescending(data => data.fromAreaData.dice - data.toAreaData.dice)
				.First();
		}

		return null;
	}

	bool IsEnemyArea(AreaData areaData)
	{
		if (areaData.player == currentPlayerEnum || areaData.player == PlayerEnum.Player_None)
			return false;

		//동맹의 땅은 공격하지 않는다
		return DataManager.Instance.IsAllAlliance(new List<PlayerEnum>() { currentPlayerEnum, areaData.player }) == false;
	}

	int GetPlayerPower(PlayerEnum playerEnum)
	{
		PlayerIconElement playerIcon = playerIconController != null ? playerIconController.GetPlayerIcon(playerEnum) : null;

		return playerIcon != null ? playerIcon.connectedCount : 0;
	}

	public async UniTask<bool> AttackOn(AreaData myData, AreaData enemyData , CancellationTokenSource source)
	{
		attackEventOn = true;
		ServerManager.Instance.receiveOn = false;

		bool win = false;

		try
		{
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

			//공격에 성공했다면
			win = myDiceWarData.diceSum > enemyDiceWarData.diceSum;

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
		}
		finally
		{
			attackEventOn = false;
			ServerManager.Instance.receiveOn = true;
		}

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

		try
		{
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
		}
		finally
		{
			ServerManager.Instance.receiveOn = true;
		}
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
