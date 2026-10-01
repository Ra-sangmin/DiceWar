using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// ScrollView 를 마우스(펜)로 끌어서 스크롤하게 해 준다 (2026-09-29).
///
/// UI Toolkit 의 ScrollView 는 터치로 끌 때만 스크롤되고, 마우스는 휠만 된다.
/// 이 도우미는 터치가 아닌 포인터의 드래그를 받아 scrollOffset 을 옮긴다.
/// 터치는 ScrollView 기본 동작(관성·탄성 포함)을 그대로 쓴다 — 둘 다 처리하면 두 배로 움직인다.
///
/// - 조금(DragThreshold) 움직이기 전에는 드래그로 보지 않아서, 본문 안의 링크 클릭은 그대로 된다.
///   드래그로 끝난 경우에는 <see cref="WasDragged"/> 가 true 라서 링크 클릭을 무시할 수 있다.
/// - 놓으면 마지막 속도로 조금 더 미끄러진다 (관성).
///
/// 사용 : var dragScroller = new ScrollViewDragScroller(scrollView);
/// </summary>
public sealed class ScrollViewDragScroller
{
	private const float DragThreshold = 8f;       // 드래그로 인정하는 최소 이동(px)
	private const float Deceleration = 0.135f;    // 관성 감속 (1초 뒤 남는 속도 비율, ScrollView 기본값과 같음)
	private const float MinInertiaSpeed = 30f;    // 이보다 느리면 관성 멈춤 (px/s)

	private readonly ScrollView scrollView;

	private int pointerId = PointerId.invalidPointerId;
	private Vector2 pressPosition;
	private Vector2 lastPosition;
	private float lastMoveTime;
	private Vector2 velocity;
	private bool dragging;

	private IVisualElementScheduledItem inertiaItem;
	private float inertiaLastTime;

	/// <summary> 방금 끝난 포인터 입력이 드래그였는지 (링크 클릭 무시용) </summary>
	public bool WasDragged { get; private set; }

	public ScrollViewDragScroller(ScrollView scrollView)
	{
		this.scrollView = scrollView;

		if (scrollView == null)
			return;

		//자식(본문 Label 등)이 먼저 받기 전에 잡도록 TrickleDown
		scrollView.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
		scrollView.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
		scrollView.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
		scrollView.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
		scrollView.RegisterCallback<DetachFromPanelEvent>(_ => StopInertia());
	}

	private static bool IsTouch(IPointerEvent evt)
	{
		return evt.pointerType == UnityEngine.UIElements.PointerType.touch;
	}

	private void OnPointerDown(PointerDownEvent evt)
	{
		if (IsTouch(evt) || evt.button != 0)
			return;

		//스크롤 바를 직접 잡은 경우는 스크롤 바가 처리한다 (여기서도 움직이면 반대로 끌린다)
		if (evt.target is VisualElement target &&
			(scrollView.verticalScroller.Contains(target) || scrollView.horizontalScroller.Contains(target)))
			return;

		StopInertia();

		pointerId = evt.pointerId;
		pressPosition = evt.position;
		lastPosition = evt.position;
		lastMoveTime = Time.unscaledTime;
		velocity = Vector2.zero;
		dragging = false;
		WasDragged = false;
	}

	private void OnPointerMove(PointerMoveEvent evt)
	{
		if (evt.pointerId != pointerId || IsTouch(evt))
			return;

		Vector2 position = evt.position;

		if (dragging == false)
		{
			if (Mathf.Abs(position.y - pressPosition.y) < DragThreshold)
				return;

			//드래그 시작 : 포인터를 잡아서 ScrollView 밖으로 나가도 계속 받는다
			dragging = true;
			WasDragged = true;
			scrollView.CapturePointer(pointerId);
		}

		Vector2 delta = position - lastPosition;

		ScrollBy(delta);

		float now = Time.unscaledTime;
		float dt = now - lastMoveTime;

		if (dt > 0.0001f)
		{
			//최근 움직임 쪽으로 무게를 둔 속도
			velocity = Vector2.Lerp(velocity, delta / dt, 0.6f);
		}

		lastPosition = position;
		lastMoveTime = now;

		evt.StopPropagation();
	}

	private void OnPointerUp(PointerUpEvent evt)
	{
		if (evt.pointerId != pointerId)
			return;

		pointerId = PointerId.invalidPointerId;

		if (dragging == false)
			return;

		dragging = false;

		if (scrollView.HasPointerCapture(evt.pointerId))
		{
			scrollView.ReleasePointer(evt.pointerId);
		}

		//손을 멈췄다가 놓았으면 관성 없음
		if (Time.unscaledTime - lastMoveTime > 0.1f)
		{
			velocity = Vector2.zero;
		}

		StartInertia();

		evt.StopPropagation();
	}

	private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
	{
		dragging = false;
		pointerId = PointerId.invalidPointerId;
	}

	/// <summary> 포인터가 delta 만큼 움직였을 때 내용이 따라 움직이도록 (위로 끌면 아래 내용이 보인다) </summary>
	private void ScrollBy(Vector2 delta)
	{
		Vector2 offset = scrollView.scrollOffset;
		offset.y -= delta.y;

		//범위를 넘지 않게 (scroller 가 알아서 막지만 관성 종료 판정에 쓰려고 직접 자른다)
		float max = Mathf.Max(0f, scrollView.contentContainer.layout.height - scrollView.contentViewport.layout.height);
		offset.y = Mathf.Clamp(offset.y, 0f, max);

		scrollView.scrollOffset = offset;
	}

	private void StartInertia()
	{
		if (Mathf.Abs(velocity.y) < MinInertiaSpeed)
			return;

		inertiaLastTime = Time.unscaledTime;
		inertiaItem = scrollView.schedule.Execute(InertiaStep).Every(16);
	}

	private void InertiaStep()
	{
		float now = Time.unscaledTime;
		float dt = Mathf.Max(0f, now - inertiaLastTime);

		if (dt <= 0f)
			return;

		inertiaLastTime = now;

		float before = scrollView.scrollOffset.y;

		ScrollBy(velocity * dt);

		velocity *= Mathf.Pow(Deceleration, dt);

		//끝에 닿았거나 충분히 느려지면 멈춤
		if (Mathf.Abs(velocity.y) < MinInertiaSpeed || Mathf.Approximately(before, scrollView.scrollOffset.y))
		{
			StopInertia();
		}
	}

	private void StopInertia()
	{
		inertiaItem?.Pause();
		inertiaItem = null;
	}
}
