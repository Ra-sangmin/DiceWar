using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

/// <summary>
/// 맵의 육각형 타일 한 칸. 기존 uGUI Hexagon(MonoBehaviour) 의 UI Toolkit 버전.
/// </summary>
public class HexagonElement : MapElementBase
{
	private readonly VisualElement centerImage;
	private readonly Label areaLabel;

	public int area;
	public PlayerEnum playerEnum = PlayerEnum.Player_None;
	/// <summary>
	/// 인접 셀 정보. 원본은 MonoBehaviour 직렬화 필드라 유니티가 기본 인스턴스를 넣어줬기 때문에
	/// (SetJoin 을 호출하는 곳이 없어도) null 이 아니었다. 같은 동작을 위해 기본값을 채워둔다.
	/// </summary>
	public Join join = new Join();
	public UnityAction<int> clickOn = data => { };

	private bool choisOn = false;
	private bool tradeOn = false;

	private IVisualElementScheduledItem tradeSchedule;
	private bool tradeToggle = false;

	public HexagonElement(Vector2 size) : base(size)
	{
		root = new VisualElement();
		root.AddToClassList("hexagon");
		root.style.width = size.x;
		root.style.height = size.y;
		root.pickingMode = PickingMode.Position;

		centerImage = new VisualElement();
		centerImage.AddToClassList("hexagon__center");
		centerImage.pickingMode = PickingMode.Ignore;
		root.Add(centerImage);

		areaLabel = new Label(string.Empty);
		areaLabel.AddToClassList("hexagon__area-text");
		areaLabel.pickingMode = PickingMode.Ignore;
		root.Add(areaLabel);

		root.RegisterCallback<ClickEvent>(_ => HexagonClickOn());
	}

	public void SetJoin(Join join)
	{
		this.join = join;
	}

	/// <summary> 주변이 모두 같은 땅인지 체크 (기존 Hexagon.AllCenterCheck 과 동일) </summary>
	public bool AllCenterCheck()
	{
		bool allCenterOn = true;

		for (int i = 0; i < join.dir.Length; i++)
		{
			int index = join.dir[i];

			if (index != -1)
			{
				int dirData = DataManager.Instance.GetCelData(index);

				if (dirData != area)
				{
					allCenterOn = false;
				}
			}
			else
			{
				allCenterOn = false;
			}
		}

		return allCenterOn;
	}

	public void SetArea(int area)
	{
		this.area = area;
	}

	public void SetPlayer(PlayerEnum playerEnum, bool choisOn = false)
	{
		this.playerEnum = playerEnum;
		this.choisOn = choisOn;

		SetColor();
	}

	public void TradeOn(bool tradeOn)
	{
		this.tradeOn = tradeOn;
		SetColor();
	}

	public void SetAreaText(string value)
	{
		areaLabel.text = value;
	}

	/// <summary>
	/// 타일 색 갱신. 거래중(tradeOn)이면 기존 DOTween Yoyo 루프 대신
	/// USS 트랜지션 + 스케줄러로 같은 깜빡임 연출을 만든다.
	/// </summary>
	public void SetColor()
	{
		if (emptyOn)
			return;

		StopTradeBlink();

		UpdateBorderMask();

		if (tradeOn)
		{
			Color playerColor = DataManager.Instance.GetPlayerColor(playerEnum);
			Color darkColor = new Color(0.2f, 0.2f, 0.2f);

			centerImage.style.unityBackgroundImageTintColor = playerColor;

			tradeToggle = false;
			tradeSchedule = centerImage.schedule.Execute(() =>
			{
				tradeToggle = !tradeToggle;
				centerImage.style.unityBackgroundImageTintColor = tradeToggle ? darkColor : playerColor;
			}).Every(750);

			return;
		}

		Color color = choisOn ? Color.gray : DataManager.Instance.GetPlayerColor(playerEnum);

		centerImage.style.unityBackgroundImageTintColor = color;
	}

	private bool emptyOn = false;

	/// <summary>
	/// 영토가 없는 칸 (2026-10-13) : 보드판 격자처럼 연한 육각 윤곽만 보이고 클릭되지 않는다.
	/// 맵을 바꾸면 같은 칸이 영토가 되기도 하므로 매번 켜고 끈다.
	/// </summary>
	public void SetEmpty(bool emptyOn)
	{
		this.emptyOn = emptyOn;

		root.EnableInClassList("hexagon--empty", emptyOn);
		root.pickingMode = emptyOn ? PickingMode.Ignore : PickingMode.Position;

		if (emptyOn)
		{
			StopTradeBlink();
			areaLabel.text = string.Empty;
			//인라인 틴트(플레이어 색)를 지워 USS 의 빈 칸 색이 적용되게
			centerImage.style.unityBackgroundImageTintColor = StyleKeyword.Null;
		}
	}

	private int borderMask = -1;

	/// <summary>
	/// 영토 경계 테두리 (2026-10-13).
	/// 6 방향 이웃 중 다른 영토(또는 맵 밖)인 변을 비트로 모아 hexagon__center--mNN 클래스로 붙인다.
	/// 비트 0:오른쪽 위, 1:오른쪽, 2:오른쪽 아래, 3:왼쪽 아래, 4:왼쪽, 5:왼쪽 위 (화면 기준, 타일 이미지와 같은 순서).
	/// join.dir 의 순서와 상관없이 이웃의 격자 위치로 방향을 구한다 (홀수 줄은 반 칸 왼쪽).
	/// </summary>
	public void UpdateBorderMask()
	{
		DataManager dataManager = DataManager.Instance;

		int mask = 0;

		Vector2 myPos = dataManager.GetPos(index);
		float myX = myPos.x - ((int)myPos.y % 2 != 0 ? 0.5f : 0f);

		for (int i = 0; i < join.dir.Length; i++)
		{
			int neighborIndex = join.dir[i];

			if (neighborIndex == -1)
				continue;

			if (dataManager.GetCelData(neighborIndex) != area)
				continue;

			Vector2 neighborPos = dataManager.GetPos(neighborIndex);
			float dx = (neighborPos.x - ((int)neighborPos.y % 2 != 0 ? 0.5f : 0f)) - myX;
			int dy = (int)neighborPos.y - (int)myPos.y;

			int edge;

			if (dy == 0) edge = dx > 0 ? 1 : 4;
			else if (dy > 0) edge = dx > 0 ? 0 : 5;      //격자 y 가 크면 화면 위
			else edge = dx > 0 ? 2 : 3;

			mask |= 1 << edge;
		}

		//위에서 모은 것은 '같은 영토' 변 → 뒤집으면 경계 변
		mask = ~mask & 0x3F;

		if (mask == borderMask)
			return;

		if (borderMask >= 0)
		{
			centerImage.RemoveFromClassList("hexagon__center--m" + borderMask.ToString("00"));
		}

		borderMask = mask;
		centerImage.AddToClassList("hexagon__center--m" + borderMask.ToString("00"));
	}

	private void StopTradeBlink()
	{
		if (tradeSchedule != null)
		{
			tradeSchedule.Pause();
			tradeSchedule = null;
		}
	}

	public void HexagonClickOn()
	{
		clickOn(index);
	}
}
