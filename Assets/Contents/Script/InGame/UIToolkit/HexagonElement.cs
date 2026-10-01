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
		StopTradeBlink();

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
