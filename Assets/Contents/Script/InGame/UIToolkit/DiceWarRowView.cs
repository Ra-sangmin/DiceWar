using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 공격 시 굴린 주사위 한 줄 + 합계 숫자. 기존 uGUI DiceWarUI / DiceUI 의 UI Toolkit 버전.
/// (uGUI 쪽 DiceWarUI·DiceUI 클래스는 동맹 승인 패널이 아직 쓰므로 그대로 둔다)
/// </summary>
public class DiceWarRowView
{
	private const int DiceMaxCount = 8;
	private const string WinClass = "dice-war__result--win";

	private readonly VisualElement row;
	private readonly Label resultLabel;
	private readonly List<VisualElement> diceList = new List<VisualElement>();
	private readonly List<int> diceFaceList = new List<int>();

	public DiceWarRowView(VisualElement row, Label resultLabel)
	{
		this.row = row;
		this.resultLabel = resultLabel;

		row.Clear(VisualElementClearOptions.RecursiveReleaseResources); //6.5+ : 다시 만들 요소라 자원까지 바로 반환 (2026-09-29)

		for (int i = 0; i < DiceMaxCount; i++)
		{
			VisualElement dice = new VisualElement();
			dice.AddToClassList("dice-war__dice");
			dice.pickingMode = PickingMode.Ignore;
			dice.style.display = DisplayStyle.None;

			diceList.Add(dice);
			diceFaceList.Add(-1);
			row.Add(dice);
		}
	}

	/// <summary> 주사위 간격 (원본 HorizontalLayoutGroup spacing) </summary>
	public void SetSpacing(float spacing)
	{
		//6.7 gap : 주사위마다 좌우 margin 을 주던 것을 줄의 column-gap 으로 (2026-09-29).
		//양 끝 여백(예전 margin 절반)은 padding 으로 유지한다
		float half = spacing * 0.5f;

		row.style.columnGap = spacing;
		row.style.paddingLeft = half;
		row.style.paddingRight = half;
	}

	public void SetPlayer(PlayerEnum playerEnum)
	{
		Color color = DataManager.Instance.GetPlayerColor(playerEnum);

		foreach (var dice in diceList)
		{
			dice.style.unityBackgroundImageTintColor = color;
		}
	}

	public void DiceClear()
	{
		resultLabel.text = string.Empty;
		resultLabel.RemoveFromClassList(WinClass);

		foreach (var dice in diceList)
		{
			dice.style.display = DisplayStyle.None;
		}
	}

	public void DiceOn(int index, int diceStatus)
	{
		if (index < 0 || index >= diceList.Count)
			return;

		VisualElement dice = diceList[index];

		dice.style.display = DisplayStyle.Flex;

		//원본 스프라이트 목록은 0,1 이 모두 dice01 이라 1~6 으로 맞춘다
		int face = Mathf.Clamp(diceStatus, 1, 6);

		if (diceFaceList[index] == face)
			return;

		if (diceFaceList[index] > 0)
		{
			dice.RemoveFromClassList("dice-war__dice--" + diceFaceList[index]);
		}

		diceFaceList[index] = face;
		dice.AddToClassList("dice-war__dice--" + face);
	}

	public void SetDiceResultText(int diceSum)
	{
		resultLabel.SetNumber(diceSum);
	}

	/// <summary> 승리 쪽 숫자 강조 (원본 DOScale Yoyo 4회 → USS 트랜지션 왕복) </summary>
	public void WinTextEffectOn()
	{
		int count = 0;

		resultLabel.schedule.Execute(() =>
		{
			bool on = count % 2 == 0;

			if (on)
			{
				resultLabel.AddToClassList(WinClass);
			}
			else
			{
				resultLabel.RemoveFromClassList(WinClass);
			}

			count++;
		}).Every(200).Until(() => count >= 4);
	}
}
