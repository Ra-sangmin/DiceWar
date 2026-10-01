using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 개발용 주사위 개수 조정 팝업. (uGUI Canvas → UI Toolkit 이식)
///
/// Main 씬 좌하단의 투명 핫스팟(dice-setting-btn)으로만 열리는 개발자 패널이다.
/// 원본은 6개의 DataPanel 각각에 DiceSetData(MonoBehaviour) + uGUI InputField 2개가 붙어 있었는데,
/// 줄이 전부 같은 모양이라 여기서 코드로 만든다. (DiceSetData 는 삭제)
///
/// 2026-09-20 : 씬에 미리 놓아두지 않고 다른 팝업과 똑같이
/// PopupManager.DiceSetPopupOn() 이 Resources 프리팹에서 생성한다.
/// </summary>
[RequireComponent(typeof(PanelRenderer))]
public class DiceSetPopup : BasePopup
{
	/// <summary> 원본 DataPanel 6개의 실측 y (ContensPanel 기준). 2~3번째 간격만 112 인 것도 원본 그대로다. </summary>
	static readonly float[] rowTop = { 218f, 336f, 448f, 566f, 684f, 802f };

	static readonly string[] rowTitle = { "Easy", "Normal", "Hard", "Easy_AI", "Normal_AI", "Hard_AI" };
	static readonly bool[] rowIsAI = { false, false, false, true, true, true };
	static readonly AILevel[] rowLevel = { AILevel.Easy, AILevel.Normal, AILevel.Hard, AILevel.Easy, AILevel.Normal, AILevel.Hard };

	private PanelUI panelUI;

	private void Awake()
	{
		panelUI = new PanelUI(this, OnUIReady);
	}

	/// <summary> PanelRenderer 의 UI 가 준비되면 한 번 호출된다 (예전 Awake 의 요소 찾기 / 이벤트 등록, 2026-09-29) </summary>
	private void OnUIReady(VisualElement root)
	{
		if (root == null)
			return;

		UnityEngine.UIElements.Button closeBtn = root.Q<UnityEngine.UIElements.Button>("dice-set-close-btn");

		if (closeBtn != null)
		{
			closeBtn.clicked += CloseBtnClickOn;
		}

		CreateRows(root);
	}

	private void OnDestroy()
	{
		panelUI?.Dispose();
	}

	public override void CloseBtnClickOn()
	{
		SoundManager.Instance.PlaySe(SeEnum.Yes);
		base.CloseBtnClickOn();
	}

	void CreateRows(VisualElement root)
	{
		VisualElement rows = root.Q<VisualElement>("dice-set-rows");

		if (rows == null)
			return;

		//UXML 에 미리 들어있는 자식이 있을 수 있으니 비우고 시작한다
		rows.Clear(VisualElementClearOptions.RecursiveReleaseResources); //6.5+ : 다시 만들 요소라 자원까지 바로 반환 (2026-09-29)

		for (int i = 0; i < rowTop.Length; i++)
		{
			rows.Add(CreateRow(i));
		}
	}

	VisualElement CreateRow(int index)
	{
		VisualElement row = new VisualElement();
		row.AddToClassList("dice-set__row");
		row.style.top = rowTop[index];
		row.pickingMode = PickingMode.Ignore;

		row.Add(CreateLabel(rowTitle[index], "dice-set__row-label--title"));
		row.Add(CreateLabel("Min ", "dice-set__row-label--min"));
		row.Add(CreateLabel("Max", "dice-set__row-label--max"));

		DiceCountData data = DiceCountManager.Instance.GetData(rowIsAI[index], rowLevel[index]);

		row.Add(CreateInput(data, true));
		row.Add(CreateInput(data, false));

		VisualElement line = new VisualElement();
		line.AddToClassList("dice-set__line");
		line.pickingMode = PickingMode.Ignore;
		row.Add(line);

		return row;
	}

	static Label CreateLabel(string text, string modifierClass)
	{
		Label label = new Label(text);
		label.AddToClassList("dice-set__row-label");
		label.AddToClassList(modifierClass);
		label.pickingMode = PickingMode.Ignore;
		return label;
	}

	/// <summary> 원본 DiceSetData 의 역할. 입력할 때마다 DiceCountManager 에 바로 반영한다. </summary>
	static TextField CreateInput(DiceCountData data, bool minOn)
	{
		TextField field = new TextField();
		field.AddToClassList("dice-set__input");
		field.AddToClassList(minOn ? "dice-set__input--min" : "dice-set__input--max");
		field.isDelayed = false;

		if (data == null)
			return field;

		field.SetValueWithoutNotify(minOn ? data.minCount.ToString() : data.maxCount.ToString());

		field.RegisterValueChangedCallback(evt =>
		{
			int value;

			//원본은 int.Parse 라 빈 칸을 지우는 순간 예외가 났다. 여기서는 무시한다.
			if (!int.TryParse(evt.newValue, out value))
				return;

			if (minOn)
				data.minCount = value;
			else
				data.maxCount = value;

			DiceCountManager.Instance.SetData(data);
		});

		return field;
	}
}
