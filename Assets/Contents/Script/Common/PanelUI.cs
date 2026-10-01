using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// PanelRenderer(Unity 6.5+, UIDocument 후속) 의 UI 가 준비되면 실행해 주는 도우미 (2026-09-29).
///
/// PanelRenderer 는 UIDocument 와 달리 UI 트리를 비동기로 만든다.
/// 컴포넌트가 활성화된 그 프레임엔 트리가 비어 있고, RegisterUIReloadCallback 의 콜백이
/// 나중(PanelRenderer 업데이트)에 root 를 넘겨준다. 공개 rootVisualElement 도 없다.
///
/// 사용법 :
///   panelUI = new PanelUI(this, OnUIReady);          // Awake 에서
///   void OnUIReady(VisualElement root) { ...Q... }    // 예전 Awake 의 요소 찾기/이벤트 등록
///   panelUI.Run(() => { ... });                       // 준비 전에 불릴 수 있는 공개 함수 본문
///   if (!panelUI.IsReady) return;                     // Update 등 매 프레임 코드
///
/// 에디터에서 UXML 을 고쳐 라이브 리로드되면 새 root 로 콜백이 다시 오지만,
/// 이벤트 이중 등록을 막기 위해 첫 번째 준비만 처리한다 (Play 모드를 다시 시작하면 반영).
/// </summary>
public sealed class PanelUI
{
	public PanelRenderer Renderer { get; }

	/// <summary> 준비 전에는 null </summary>
	public VisualElement Root { get; private set; }

	public bool IsReady => Root != null;

	private readonly Action<VisualElement> onReady;
	private readonly List<Action> pendingActionList = new List<Action>();

	public PanelUI(Component owner, Action<VisualElement> onReady)
	{
		this.onReady = onReady;

		Renderer = owner.GetComponent<PanelRenderer>();

		if (Renderer == null)
		{
			Debug.LogError($"[PanelUI] {owner.name} 에 PanelRenderer 가 없습니다.", owner);
			return;
		}

		Renderer.RegisterUIReloadCallback(OnUIReload);
	}

	private void OnUIReload(PanelRenderer panelRenderer, VisualElement root)
	{
		if (root == null)
			return;

		if (Root != null)
		{
			//오브젝트를 껐다 켜면 같은 root 로 다시 온다 (6.7b2 에서 확인, 2026-09-29) → 무시.
			//다른 root 가 오면 예전 요소를 잡고 있는 상태라 화면이 먹통이 될 수 있다. 에디터 라이브 리로드가 아니면 알려 준다
			if (root != Root && Application.isEditor == false)
			{
				Debug.LogWarning($"[PanelUI] {Renderer.name} 의 UI 가 새로 만들어졌습니다. 요소를 다시 찾지 않으므로 화면이 반응하지 않을 수 있습니다.", Renderer);
			}

			return;
		}

		if (readyPosted)
			return;

		//이 콜백은 엔진이 PanelRenderer 목록을 도는 중(UIElementsRuntimeUtility.PreUpdatePanelRenderers)에 불린다.
		//여기서 팝업(=새 PanelRenderer)을 만들면 "Collection was modified" 예외가 나므로
		//실제 준비 처리는 다음 Update 로 미룬다 (2026-09-29)
		readyPosted = true;

		Cysharp.Threading.Tasks.UniTask.Post(() => Ready(root));
	}

	private bool readyPosted = false;

	private void Ready(VisualElement root)
	{
		//그 사이 오브젝트가 파괴됐으면 아무것도 하지 않는다
		if (disposed || Renderer == null)
			return;

		Root = root;

		StartOpenAnimation(root);

		onReady?.Invoke(root);

		//준비 전에 들어온 호출을 들어온 순서대로 실행
		for (int i = 0; i < pendingActionList.Count; i++)
		{
			pendingActionList[i]?.Invoke();
		}

		pendingActionList.Clear();
	}

	private const string OpenAnimClass = "popup-open-anim";
	private const string OpenAnimShownClass = "popup-open-anim--shown";
	private const string OpenAnimReadyClass = "popup-open-anim--ready";

	/// <summary>
	/// 팝업 등장 키프레임(USS .popup-open-anim > * → PopupPanelIn.asset / 뒤 막 → PopupDimIn.asset) 보조 (2026-09-29).
	/// --ready : 준비 즉시 판·내용 표시 (그 전 프레임엔 글자가 비어 있다).
	/// 애니메이션은 요소가 처음 그려진 '다음' 프레임부터 재생돼서, 그냥 두면 첫 프레임에 팝업이 다 보였다가
	/// 투명해진 뒤 떠오르는 깜빡임이 생긴다. 그래서 USS 기본 opacity 를 0 으로 두고,
	/// 애니메이션이 재생되는 도중(0.1초 뒤)에 opacity 1 클래스를 붙인다. 재생 중에는 애니메이션 값이 우선이라 티가 안 나고,
	/// 끝난 뒤에는 이 클래스 덕분에 1 로 남는다. 애니메이션이 없거나 실패해도 0.1초 뒤에는 보인다.
	/// </summary>
	private static void StartOpenAnimation(VisualElement root)
	{
		root.Query(className: OpenAnimClass).ForEach(element =>
		{
			//판·내용은 글자가 채워지는 이 프레임에 불투명하게 한 번에 나타난다 (반투명 잔상 방지, 2026-09-29)
			element.AddToClassList(OpenAnimReadyClass);

			element.schedule.Execute(() => element.AddToClassList(OpenAnimShownClass)).ExecuteLater(100);
		});
	}

	/// <summary> 준비됐으면 바로, 아니면 준비된 직후에 실행 </summary>
	public void Run(Action action)
	{
		if (action == null)
			return;

		if (IsReady)
		{
			action();
		}
		else
		{
			pendingActionList.Add(action);
		}
	}

	/// <summary> OnDestroy 에서 호출 </summary>
	private bool disposed = false;

	public void Dispose()
	{
		disposed = true;

		if (Renderer != null)
		{
			Renderer.UnregisterUIReloadCallback(OnUIReload);
		}

		pendingActionList.Clear();
	}
}
