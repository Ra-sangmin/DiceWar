using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 화면 상단에 잠깐 떴다 사라지는 토스트. (uGUI Canvas → UI Toolkit 이식)
/// 원본의 CanvasGroup + DOTween 페이드는 USS transition 으로 대체했다.
/// </summary>
[RequireComponent(typeof(PanelRenderer))]
public class ToastPopup : MonoBehaviour
{
	[SerializeField] protected LocalizeStatus localizeStatus = LocalizeStatus.None;
	[SerializeField] protected int localizeKey = -1;

	/// <summary> 원본 InGameWarningPopup 처럼 폭이 넓은 토스트 </summary>
	[SerializeField] protected bool wideOn = false;

	public float maxAlphaValue = 0.85f;

	/// <summary> 완전히 보이는 상태로 머무는 시간 (초) </summary>
	[SerializeField] protected float fadeDelay = 0.5f;

	/// <summary> 사라지는 데 걸리는 시간 (초) </summary>
	[SerializeField] protected float fadeDuration = 1f;

	protected PanelUI panelUI;

	protected VisualElement toastRoot;
	protected VisualElement iconElement;
	protected Label textLabel;

	protected virtual void Awake()
	{
		panelUI = new PanelUI(this, OnUIReady);
	}

	/// <summary> PanelRenderer 의 UI 가 준비되면 한 번 호출된다 (예전 Awake 의 요소 찾기 / 이벤트 등록, 2026-09-29) </summary>
	protected virtual void OnUIReady(VisualElement root)
	{
		toastRoot = root.Q<VisualElement>("toast-popup");
		iconElement = root.Q<VisualElement>("toast-icon");
		textLabel = root.Q<Label>("toast-text");

		if (wideOn && toastRoot != null)
		{
			toastRoot.AddToClassList("toast-popup--wide");
		}

		LocalizeTextSet();
	}

	protected virtual void OnDestroy()
	{
		panelUI?.Dispose();
	}

	/// <summary> 원본 LocalizeText 컴포넌트 역할 (프리팹마다 키가 다르다) </summary>
	protected void LocalizeTextSet()
	{
		if (localizeStatus == LocalizeStatus.None || localizeKey < 0 || textLabel == null)
			return;

		textLabel.text = LocalizeManager.Instance.GetStrData(localizeStatus, localizeKey);
	}

	public void SetImageColor()
	{
	}

	public void SetText(string text)
	{
		panelUI.Run(() =>
		{
			if (textLabel != null)
			{
				textLabel.text = text;
			}
		});
	}

	public void ActiveOn()
	{
		//UI 가 준비된 뒤에 페이드를 시작한다 (PanelRenderer, 2026-09-29)
		panelUI.Run(ActiveOnReady);
	}

	void ActiveOnReady()
	{
		//페이드가 끝나고 한 프레임 뒤에 정리한다
		float lifeTime = fadeDelay + fadeDuration + 0.1f;

		if (toastRoot == null)
		{
			Destroy(gameObject, lifeTime);
			return;
		}

		//최대 알파로 켠다. 등장 키프레임(ToastIn)이 첫 프레임엔 아직 재생 전이라
		//바로 켜면 제자리에 한 번 보였다가 위로 튀므로 두 프레임쯤 뒤에 켠다 (2026-09-29)
		const long showDelayMs = 30;

		toastRoot.schedule.Execute(() => SetAlpha(maxAlphaValue, 0f)).ExecuteLater(showDelayMs);

		//fadeDelay 만큼 머물렀다가 fadeDuration 에 걸쳐 사라진다.
		//
		// 주의 : "머무는 시간" 을 transition-delay 로 주면 안 된다. (2026-09-20)
		// ExecuteLater(0) 가 같은 프레임의 스케줄러에서 바로 돌아버려서
		// 한 번의 스타일 계산 안에서 opacity 가 0(USS 기본) -> max -> 0 으로 덮어써진다.
		// 스타일 시스템이 보기엔 값이 안 바뀐 셈이라 트랜지션이 시작되지 않고,
		// 토스트가 처음부터 끝까지 opacity 0 인 채로 보이지 않았다.
		// 머무는 시간은 스케줄러로 처리해서 알파 변화가 반드시 다른 프레임에 일어나게 한다.
		toastRoot.schedule.Execute(() => SetAlpha(0f, fadeDuration)).ExecuteLater(showDelayMs + (long)(fadeDelay * 1000f));

		Destroy(gameObject, lifeTime);
	}

	void SetAlpha(float alpha, float duration)
	{
		toastRoot.style.transitionDuration = new List<TimeValue> { new TimeValue(duration, TimeUnit.Second) };
		toastRoot.style.transitionDelay = new List<TimeValue> { new TimeValue(0f, TimeUnit.Second) };
		toastRoot.style.opacity = alpha;
	}
}
