using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 카운트다운 승리 (카운트다운 기능.pptx / 26.10.04 수정 제안, 2026-10-08)
///
///  발동 : 싱글 Hard 또는 멀티. 세력(오른쪽 숫자 = 가장 큰 연결 덩어리)이 전체 영토의 과반이 되면 시작한다.
///         멀티는 동맹원의 세력을 합친다. 과반 = 전체 땅 수 / 2 의 몫 + 1 (45 → 23)
///  턴    : '처음으로 과반을 달성한 플레이어'(holder) 의 차례로 센다.
///         시작 3 → holder 의 차례가 돌아올 때마다 1 씩 줄고(팝업) → 세 번째로 돌아왔을 때 판정.
///  판정  : 그 순간 holder(와 그 동맹)의 세력이 과반이면 승리, 아니면 '카운트다운 중단'.
///         중간에 과반을 잃어도 카운트다운은 판정 때까지 유지된다. 동맹에서 누가 배신해도 holder 쪽이 과반이면 유지.
///  여럿  : 진행 중인 카운트다운은 끝까지 유지되고, 새로 과반이 된 쪽은 자기 카운트다운을 따로 시작한다.
///         (동시에 두 쪽이 과반일 수는 없어서 판정 때 승자는 많아야 하나)
///
/// 멀티는 모든 클라이언트가 같은 순서로 땅/턴 정보를 받으므로 각자 계산해도 결과가 같다 (서버 프로토콜 추가 없음).
/// </summary>
public class CountdownManager
{
	public const int CountdownTurns = 3;

	/// <summary> 지금 씬의 카운트다운 (AttackController 등 다른 곳에서 조회) </summary>
	public static CountdownManager Current { get; private set; }

	public class CountdownData
	{
		public PlayerEnum holder;
		public int remainTurns;
	}

	private readonly List<CountdownData> countdownList = new List<CountdownData>();

	private readonly Func<PlayerIconController> getPlayerIconController;

	/// <summary> (holder, 문구, 내 차례 시작과 겹치는지) — 팝업 </summary>
	public Action<PlayerEnum, string, bool> popupOn = (holder, text, turnStartOn) => { };

	/// <summary> holder 쪽 승리 판정 </summary>
	public Action<PlayerEnum> countdownWinOn = holder => { };

	/// <summary> 남은 턴 표시 갱신 </summary>
	public Action viewChangedOn = () => { };

	public IReadOnlyList<CountdownData> CountdownList => countdownList;

	public CountdownManager(Func<PlayerIconController> getPlayerIconController)
	{
		this.getPlayerIconController = getPlayerIconController;
		Current = this;
	}

	public void Dispose()
	{
		if (Current == this)
		{
			Current = null;
		}
	}

	/// <summary> 싱글은 Hard 에서만, 멀티는 항상 </summary>
	public static bool IsEnabled()
	{
		DataManager dataManager = DataManager.Instance;

		return dataManager.isMultiOn || dataManager.gameAILevel == AILevel.Hard;
	}

	/// <summary> 진행 중인 카운트다운이 있는가 (AI 알고리즘의 '카운트다운 중인가?') </summary>
	public bool IsCountdownOn => countdownList.Count > 0;

	public static int GetMajorityCount()
	{
		return DataManager.Instance.areaDataList.Count / 2 + 1;
	}

	public void Clear()
	{
		countdownList.Clear();
		viewChangedOn();
	}

	#region 세력 계산

	/// <summary> playerEnum 과 같은 편 (멀티 : 동맹원 전체, 싱글 : 본인) </summary>
	public static List<PlayerEnum> GetGroupMembers(PlayerEnum playerEnum)
	{
		DataManager dataManager = DataManager.Instance;

		if (dataManager.isMultiOn && dataManager.IsAlliance(playerEnum))
		{
			AllianceAllData allianceAllData = dataManager.GetAllAlliance(playerEnum);

			if (allianceAllData != null && allianceAllData.allianceDataList.Count > 0)
			{
				List<PlayerEnum> memberList = allianceAllData.allianceDataList.Select(data => data.playerEnum).ToList();

				if (memberList.Contains(playerEnum) == false)
				{
					memberList.Add(playerEnum);
				}

				return memberList;
			}
		}

		return new List<PlayerEnum>() { playerEnum };
	}

	/// <summary> 같은 편의 세력 합 (땅이 남아 있는 사람만) </summary>
	public static int GetGroupPower(PlayerIconController playerIconController, PlayerEnum playerEnum)
	{
		if (playerIconController == null)
			return 0;

		return GetGroupMembers(playerEnum).Sum(member => playerIconController.GetPlayerIcon(member)?.connectedCount ?? 0);
	}

	/// <summary> 세력이 가장 큰 편(동점이면 모두)의 플레이어들 </summary>
	public static HashSet<PlayerEnum> GetLeaderPlayers(PlayerIconController playerIconController)
	{
		HashSet<PlayerEnum> resultSet = new HashSet<PlayerEnum>();

		if (playerIconController == null)
			return resultSet;

		int maxPower = -1;
		List<List<PlayerEnum>> leaderGroupList = new List<List<PlayerEnum>>();
		HashSet<PlayerEnum> countedSet = new HashSet<PlayerEnum>();

		foreach (var playerIcon in playerIconController.GetActiveList())
		{
			if (countedSet.Contains(playerIcon.playerEnum))
				continue;

			List<PlayerEnum> memberList = GetGroupMembers(playerIcon.playerEnum);

			foreach (var member in memberList)
			{
				countedSet.Add(member);
			}

			int power = memberList.Sum(member => playerIconController.GetPlayerIcon(member)?.connectedCount ?? 0);

			if (power > maxPower)
			{
				maxPower = power;
				leaderGroupList.Clear();
			}

			if (power == maxPower)
			{
				leaderGroupList.Add(memberList);
			}
		}

		foreach (var group in leaderGroupList)
		{
			foreach (var member in group)
			{
				resultSet.Add(member);
			}
		}

		return resultSet;
	}

	#endregion

	/// <summary>
	/// 과반이 된 편이 있으면 카운트다운을 시작한다. 땅 / 동맹이 바뀔 때마다 부른다.
	/// 이미 그 편의 누군가가 holder 인 카운트다운이 진행 중이면 새로 시작하지 않는다.
	/// </summary>
	public void CheckStart(bool turnStartOn = false)
	{
		if (IsEnabled() == false)
			return;

		PlayerIconController playerIconController = getPlayerIconController();

		if (playerIconController == null)
			return;

		List<PlayerIconElement> activeList = playerIconController.GetActiveList();

		//한 편만 남았으면 곧 일반 승리 판정이 난다
		if (activeList.Count <= 1)
			return;

		int majority = GetMajorityCount();
		PlayerEnum currentPlayer = DataManager.Instance.currentPlayer;
		HashSet<PlayerEnum> countedSet = new HashSet<PlayerEnum>();

		foreach (var playerIcon in activeList)
		{
			if (countedSet.Contains(playerIcon.playerEnum))
				continue;

			List<PlayerEnum> memberList = GetGroupMembers(playerIcon.playerEnum);

			foreach (var member in memberList)
			{
				countedSet.Add(member);
			}

			int power = memberList.Sum(member => playerIconController.GetPlayerIcon(member)?.connectedCount ?? 0);

			if (power < majority)
				continue;

			//모두가 같은 편이면(다른 편이 남아 있지 않으면) 일반 승리 판정에 맡긴다
			if (activeList.All(data => memberList.Contains(data.playerEnum)))
				continue;

			if (countdownList.Any(data => memberList.Contains(data.holder)))
				continue;

			//처음으로 과반을 달성한 플레이어 : 지금 차례인 사람이 그 편이면 그 사람, 아니면 그 편에서 세력이 가장 큰 사람
			PlayerEnum holder = memberList.Contains(currentPlayer) && playerIconController.GetPlayerIcon(currentPlayer) != null
				? currentPlayer
				: memberList.OrderByDescending(member => playerIconController.GetPlayerIcon(member)?.connectedCount ?? 0)
							.ThenBy(member => (int)member)
							.First();

			CountdownData data = new CountdownData()
			{
				holder = holder,
				remainTurns = CountdownTurns,
			};

			countdownList.Add(data);

			Debug.Log($"[Countdown] 시작 holder={holder} power={power}/{DataManager.Instance.areaDataList.Count} (과반 {majority})");

			popupOn(holder, GetCountdownText(data.remainTurns, majority), turnStartOn);
		}

		viewChangedOn();
	}

	/// <summary>
	/// 차례가 시작될 때 (모든 클라이언트에서 같은 순서로) 부른다.
	/// holder 의 차례면 남은 턴을 줄이고, 0 이 되면 판정한다.
	/// </summary>
	/// <returns> 카운트다운 승리로 게임이 끝났으면 true </returns>
	public bool TurnStartOn(PlayerEnum currentPlayer)
	{
		if (IsEnabled() == false)
		{
			if (countdownList.Count > 0)
			{
				Clear();
			}
			return false;
		}

		PlayerIconController playerIconController = getPlayerIconController();

		if (playerIconController == null)
			return false;

		int majority = GetMajorityCount();
		bool myTurn = DataManager.Instance.IsMyTurn();

		foreach (var data in countdownList.ToList())
		{
			//holder 가 땅을 모두 잃어 퇴장했으면 차례가 다시 오지 않는다 → 중단
			if (playerIconController.GetPlayerIcon(data.holder) == null)
			{
				countdownList.Remove(data);
				popupOn(data.holder, GetStoppedText(), myTurn);
				continue;
			}

			if (data.holder != currentPlayer)
				continue;

			data.remainTurns--;

			if (data.remainTurns > 0)
			{
				popupOn(data.holder, GetCountdownText(data.remainTurns, majority), myTurn);
				continue;
			}

			//세 번째로 돌아왔다 → 판정
			int power = GetGroupPower(playerIconController, data.holder);

			countdownList.Remove(data);

			if (power >= majority)
			{
				Debug.Log($"[Countdown] 승리 holder={data.holder} power={power} (과반 {majority})");

				viewChangedOn();
				countdownWinOn(data.holder);
				return true;
			}

			Debug.Log($"[Countdown] 중단 holder={data.holder} power={power} (과반 {majority})");

			popupOn(data.holder, GetStoppedText(), myTurn);
		}

		//판정 뒤 새로 과반이 된 편 (이번 차례에 시작한 카운트다운은 줄이지 않는다)
		CheckStart(true);

		viewChangedOn();

		return false;
	}

	/// <summary> 타이머 자리에 보여줄 남은 턴 (진행 중인 것 중 가장 적은 값, 없으면 0) </summary>
	public int GetDisplayRemainTurns()
	{
		return countdownList.Count == 0 ? 0 : countdownList.Min(data => data.remainTurns);
	}

	public CountdownData GetDisplayCountdown()
	{
		return countdownList.OrderBy(data => data.remainTurns).FirstOrDefault();
	}

	#region 문구 (현지화 시트에 키가 없어서 언어만 보고 고른다)

	static bool IsKorean()
	{
		return LocalizeManager.Instance != null && LocalizeManager.Instance.language.Value == SystemLanguage.Korean;
	}

	public static string GetCountdownText(int remainTurns, int majority)
	{
		const string titleSize = "<size=60>";
		const string bodySize = "<size=36>";

		if (IsKorean())
		{
			return $"{titleSize}카운트다운</size>\n{bodySize}{remainTurns}턴간 {majority} 이상 유지 시 승리</size>";
		}

		string turnWord = remainTurns == 1 ? "turn" : "turns";

		return $"{titleSize}Countdown</size>\n{bodySize}Hold {majority}+ for {remainTurns} {turnWord} to win</size>";
	}

	public static string GetStoppedText()
	{
		return IsKorean() ? "카운트다운 중단" : "Countdown stopped";
	}

	#endregion
}
