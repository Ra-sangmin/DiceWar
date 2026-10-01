using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UniRx;

/// <summary>
/// 설정 팝업의 선택 토글. 기존 uGUI SMToggle 의 UI Toolkit 버전.
/// (uGUI SMToggle / SMToggleGroup 컴포넌트는 남겨 두고, 새 화면만 이 클래스를 쓴다)
/// </summary>
public class SMToggleElement
{
	private readonly UnityEngine.UIElements.Button root;
	private readonly Label label;

	private SMToggleGroupElement group;

	public BoolReactiveProperty toggleValue = new BoolReactiveProperty(false);
	public BoolReactiveProperty interactable = new BoolReactiveProperty(true);

	//원본 SMToggle 의 색 (Toggle 배경 / 글자 각각)
	static readonly Color bgTrue = new Color(0.529f, 0.914f, 0.957f);
	static readonly Color bgFalse = new Color(0.267f, 0.286f, 0.310f);
	static readonly Color bgDisable = new Color(0.369f, 0.384f, 0.408f);

	static readonly Color textTrue = new Color(0.165f, 0.196f, 0.208f);
	static readonly Color textFalse = new Color(0.529f, 0.914f, 0.957f);
	static readonly Color textDisable = new Color(0.475f, 0.490f, 0.506f);

	public UnityEngine.UIElements.Button Root => root;

	public SMToggleElement(string text, float width, params string[] extraClasses)
	{
		root = new UnityEngine.UIElements.Button();
		root.AddToClassList("sm-toggle");

		//width 0 이하면 USS 의 width:auto (글자 길이에 맞춤) 를 그대로 쓴다
		if (width > 0f)
		{
			root.style.width = width;
		}

		foreach (var c in extraClasses)
		{
			root.AddToClassList(c);
		}

		label = new Label(text);
		label.AddToClassList("sm-toggle__label");
		label.pickingMode = PickingMode.Ignore;
		root.Add(label);

		root.clicked += () =>
		{
			if (interactable.Value == false)
				return;

			SoundManager.Instance.PlaySe(SeEnum.Yes);
			ToggleOn();
		};

		toggleValue.Subscribe(_ =>
		{
			SetColor();

			if (toggleValue.Value && group != null)
			{
				group.ToggleValueChangeOn(this);
			}
		});

		interactable.Subscribe(_ =>
		{
			root.SetEnabled(interactable.Value);
			SetColor();
		});

		SetColor();
	}

	public void SetGroup(SMToggleGroupElement group)
	{
		this.group = group;
		group.SetToggle(this);
	}

	public void SetText(string text)
	{
		label.text = text;
	}

	public void ToggleOn()
	{
		if (toggleValue.Value)
			return;

		toggleValue.Value = !toggleValue.Value;
	}

	public void SetToggleValue(bool isOn)
	{
		toggleValue.Value = isOn;
	}

	void SetColor()
	{
		if (interactable.Value)
		{
			root.style.unityBackgroundImageTintColor = toggleValue.Value ? bgTrue : bgFalse;
			label.style.color = toggleValue.Value ? textTrue : textFalse;
		}
		else
		{
			root.style.unityBackgroundImageTintColor = bgDisable;
			label.style.color = textDisable;
		}
	}
}

/// <summary> 원본 SMToggleGroup 의 UI Toolkit 버전 </summary>
public class SMToggleGroupElement
{
	private readonly List<SMToggleElement> toggleList = new List<SMToggleElement>();

	public void SetToggle(SMToggleElement smToggle)
	{
		toggleList.Add(smToggle);
	}

	public void ToggleValueChangeOn(SMToggleElement smToggle)
	{
		foreach (SMToggleElement toggle in toggleList)
		{
			if (toggle == smToggle || toggle.toggleValue.Value == false)
			{
				continue;
			}

			toggle.SetToggleValue(false);
		}
	}
}
