using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

/// <summary>
/// 우측에 세로로 쌓이는 플레이어 현황 아이콘. 기존 uGUI PlayerIcon(MonoBehaviour) 의 UI Toolkit 버전.
/// (같은 이름의 uGUI PlayerIcon 은 하단바 / YourTurnPopup 등에서 아직 사용하므로 그대로 남겨둔다)
/// </summary>
public class PlayerIconElement
{
	private readonly VisualElement root;
	private readonly VisualElement selectImage;
	private readonly VisualElement bgImage;
	private readonly VisualElement centerBGImage;
	private readonly VisualElement iconImage;
	private readonly Label connectedCountLabel;

	private int iconColorIndex = -1;
	private Color playerColor = Color.white;

	/// <summary> 기본 패널 색 (시안 : 아웃라인 없는 어두운 패널) </summary>
	private static readonly Color PanelColor = new Color32(44, 48, 54, 255);

	public PlayerEnum playerEnum = PlayerEnum.Player_None;
	public int connectedCount;
	public int allAreaCount;
	public UnityAction<PlayerIconElement> playerClickOn = data => { };

	public VisualElement Root => root;

	public PlayerIconElement()
	{
		root = new VisualElement();
		root.AddToClassList("player-icon");

		bgImage = new VisualElement();
		bgImage.AddToClassList("player-icon__bg");
		bgImage.pickingMode = PickingMode.Ignore;

		centerBGImage = new VisualElement();
		centerBGImage.AddToClassList("player-icon__center-bg");
		centerBGImage.pickingMode = PickingMode.Ignore;
		bgImage.Add(centerBGImage);

		iconImage = new VisualElement();
		iconImage.AddToClassList("player-icon__icon");
		iconImage.pickingMode = PickingMode.Ignore;
		bgImage.Add(iconImage);

		selectImage = new VisualElement();
		selectImage.AddToClassList("player-icon__select");
		selectImage.pickingMode = PickingMode.Ignore;
		selectImage.style.display = DisplayStyle.None;

		connectedCountLabel = new Label(string.Empty);
		connectedCountLabel.AddToClassList("player-icon__count");
		connectedCountLabel.pickingMode = PickingMode.Ignore;

		//원본 계층 순서 (SelectImage → BG → Text)
		root.Add(selectImage);
		root.Add(bgImage);
		root.Add(connectedCountLabel);

		root.RegisterCallback<ClickEvent>(_ => BtnClickOn());
	}

	public void SetActive(bool activeOn)
	{
		root.style.display = activeOn ? DisplayStyle.Flex : DisplayStyle.None;
	}

	public bool IsActive()
	{
		return root.style.display.value == DisplayStyle.Flex;
	}

	public void SetPlayer(PlayerEnum playerEnum)
	{
		this.playerEnum = playerEnum;
		SetIconColor();
	}

	public void SetIconColor()
	{
		int colorIndex = DataManager.Instance.GetPlayerColorIndex(playerEnum);
		Color color = DataManager.Instance.GetPlayerColor(colorIndex);

		SetIconSprite(colorIndex);

		playerColor = color;
		selectImage.style.unityBackgroundImageTintColor = color;

		SetCenterBGImage(PlayerEnum.Player_None);
		SetOutline(false);
		SetMineGlow();
	}

	/// <summary> 플레이어 색 인덱스에 맞는 아이콘 이미지로 교체 (USS 클래스로 지정) </summary>
	private void SetIconSprite(int colorIndex)
	{
		if (iconColorIndex == colorIndex)
			return;

		if (iconColorIndex >= 0)
		{
			iconImage.RemoveFromClassList("player-icon__icon--" + iconColorIndex);
		}

		iconColorIndex = colorIndex;

		if (iconColorIndex >= 0)
		{
			iconImage.AddToClassList("player-icon__icon--" + iconColorIndex);
		}
	}

	void SetCenterBGImage(PlayerEnum playerEnum = PlayerEnum.Player_None)
	{
		int colorIndex = DataManager.Instance.GetPlayerColorIndex(playerEnum);

		Color color = playerEnum == PlayerEnum.Player_None
			? new Color32(44, 48, 54, 255)
			: DataManager.Instance.GetPlayerColor(colorIndex);

		centerBGImage.style.unityBackgroundImageTintColor = color;
	}

	public void SetAllianceColor(PlayerEnum ordebPlayerEnum)
	{
		SetCenterBGImage(ordebPlayerEnum);
	}

	public void SetConnectedCount(int connectedCount)
	{
		this.connectedCount = connectedCount;
		connectedCountLabel.SetNumber(connectedCount);

		allAreaCount = DataManager.Instance.GetAreaDtaList(playerEnum).Count;
	}

	/// <summary>
	/// 시안(컴포넌트 수정사항 - Player panel, 2026-09-26) :
	///  - 그 플레이어의 차례에는 플레이어 색 아웃라인
	///  - 본인 영토(나) 는 차례와 상관없이 항상 Drop shadow(글로우)
	///  - 동맹을 맺으면 패널 배경이 제안자 색 (SetAllianceColor)
	/// </summary>
	public void SetMyTurnEffect(bool forceActiveOn = false)
	{
		bool activeOn = forceActiveOn || DataManager.Instance.currentTurnIndex == (int)playerEnum;
		SetOutline(activeOn);
		SetMineGlow();
	}

	public void SetMyTurnEffectOff()
	{
		SetOutline(false);
		SetMineGlow();
	}

	/// <summary> 바깥 bg 가 center-bg 보다 사방 2px 크므로, bg 색이 곧 아웃라인 색이다 </summary>
	void SetOutline(bool activeOn)
	{
		bgImage.style.unityBackgroundImageTintColor = activeOn ? playerColor : PanelColor;
	}

	void SetMineGlow()
	{
		bool mineOn = DataManager.Instance.playerData != null && DataManager.Instance.playerData.pe == playerEnum;
		selectImage.style.display = mineOn ? DisplayStyle.Flex : DisplayStyle.None;
	}

	/// <summary> uGUI 좌표계(캔버스 중앙 기준)로 환산한 이 아이콘의 세로 중심 </summary>
	public float GetCanvasCenterY(float referenceHeight = 1080f)
	{
		return (referenceHeight * 0.5f) - root.worldBound.center.y;
	}

	public void BtnClickOn()
	{
		playerClickOn(this);
	}
}
