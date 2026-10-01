using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

/// <summary>
/// 확인 / 취소 공용 팝업. (uGUI Canvas → UI Toolkit 이식)
/// </summary>
[RequireComponent(typeof(PanelRenderer))]
public class OkPopup : BasePopup
{
	private PanelUI panelUI;

	private Label contensText;
	private Label okBtnText;
	private Label cancelBtnText;

	private UnityEngine.UIElements.Button okBtn;
	private UnityEngine.UIElements.Button cancelBtn;

	UnityAction okBtnClickEvent;
	UnityAction cancelBtnClickEvent;

	bool clickOn = false;
	float clickDelay = 1;

	bool maskClickDestoryOn = false;

	enum BtnKind
	{
		okOnlyBtn,
		okBtn,
		cancelBtn
	}

	void Awake()
	{
		panelUI = new PanelUI(this, OnUIReady);
	}

	/// <summary> PanelRenderer 의 UI 가 준비되면 한 번 호출된다 (예전 Awake 의 요소 찾기 / 이벤트 등록, 2026-09-29) </summary>
	private void OnUIReady(VisualElement root)
	{
		contensText = root.Q<Label>("ok-popup-contens-text");
		okBtnText = root.Q<Label>("ok-popup-ok-label");
		cancelBtnText = root.Q<Label>("ok-popup-cancel-label");

		okBtn = root.Q<UnityEngine.UIElements.Button>("ok-popup-ok-btn");
		cancelBtn = root.Q<UnityEngine.UIElements.Button>("ok-popup-cancel-btn");

		UnityEngine.UIElements.Button mask = root.Q<UnityEngine.UIElements.Button>("ok-popup-mask");

		if (okBtn != null) okBtn.clicked += OkBtnClick;
		if (cancelBtn != null) cancelBtn.clicked += CancelBtnClick;
		if (mask != null) mask.clicked += MaskClick;
	}

	private void OnDestroy()
	{
		panelUI?.Dispose();
	}

	void Update()
	{
		if (clickOn == false)
		{
			clickDelay -= Time.deltaTime;
			if (clickDelay < 0)
			{
				clickOn = true;
				clickDelay = 0.5f;
			}
		}
	}

	public void DataSet(string contensStr, UnityAction okBtnClickEvent, UnityAction cancelBtnClickEvent, bool okBtnOnly, bool maskClickDestoryOn, string okBtnStr, string cancelBtnStr)
	{
		//만들자마자 불리므로 UI 가 준비된 뒤에 채운다 (PanelRenderer, 2026-09-29)
		panelUI.Run(() => DataSetReady(contensStr, okBtnClickEvent, cancelBtnClickEvent, okBtnOnly, maskClickDestoryOn, okBtnStr, cancelBtnStr));
	}

	void DataSetReady(string contensStr, UnityAction okBtnClickEvent, UnityAction cancelBtnClickEvent, bool okBtnOnly, bool maskClickDestoryOn, string okBtnStr, string cancelBtnStr)
	{
		this.okBtnClickEvent = okBtnClickEvent;
		this.cancelBtnClickEvent = cancelBtnClickEvent;

		this.maskClickDestoryOn = maskClickDestoryOn;

		contensText.text = contensStr;

		if (okBtnOnly)
		{
			//원본은 okBtnRect.offsetMin 을 조정해 확인 버튼만 가운데로 옮겼다
			cancelBtn.style.display = DisplayStyle.None;
			okBtn.AddToClassList("ok-popup__ok-btn--only");

			okBtnText.text = GetBtnStrKey(BtnKind.okOnlyBtn);
		}
		else
		{
			cancelBtn.style.display = DisplayStyle.Flex;
			okBtn.RemoveFromClassList("ok-popup__ok-btn--only");

			if (string.IsNullOrEmpty(okBtnStr))
			{
				okBtnStr = GetBtnStrKey(BtnKind.okBtn);
			}

			if (string.IsNullOrEmpty(cancelBtnStr))
			{
				cancelBtnStr = GetBtnStrKey(BtnKind.cancelBtn);
			}

			okBtnText.text = okBtnStr;
			cancelBtnText.text = cancelBtnStr;
		}

		clickOn = false;
		clickDelay = 0.5f;
	}

	private string GetBtnStrKey(BtnKind btnKind)
	{
		string resultKey = string.Empty;

#if UNITY_WEBGL
		switch (btnKind)
		{
			case BtnKind.okOnlyBtn: resultKey = "key_42"; break;
			case BtnKind.okBtn: resultKey = "key_33"; break;
			case BtnKind.cancelBtn: resultKey = "key_32"; break;
		}
#else
		switch (btnKind)
		{
			case BtnKind.okOnlyBtn: resultKey = "key_5"; break;
			case BtnKind.okBtn: resultKey = "key_4"; break;
			case BtnKind.cancelBtn: resultKey = "key_3"; break;
		}
#endif

		return resultKey;
	}

	public void OkBtnClick()
	{
		if (clickOn == false)
			return;

		if (okBtnClickEvent != null)
		{
			okBtnClickEvent();
		}
		DestroyPopup();
	}

	public void CancelBtnClick()
	{
		if (clickOn == false)
			return;

		if (cancelBtnClickEvent != null)
		{
			cancelBtnClickEvent();
		}
		DestroyPopup();
	}

	public void MaskClick()
	{
		if (clickOn == false)
			return;

		if (maskClickDestoryOn == false)
		{
			if (cancelBtnClickEvent != null)
			{
				cancelBtnClickEvent();
			}
		}

		DestroyPopup();
	}

	void DestroyPopup()
	{
		Destroy(gameObject);
	}
}
