using UnityEngine.UIElements;

/// <summary>
/// 동맹 승인 현황 바의 플레이어 아이콘. 기존 uGUI MyAllianceApproveIcon 의 UI Toolkit 버전.
/// </summary>
public class MyAllianceApproveIconElement
{
	private readonly VisualElement root;
	private readonly VisualElement approveOnImage;
	private readonly VisualElement approveOffImage;

	public PlayerEnum playerEnum = PlayerEnum.Player_None;
	public bool approvedOn = false;

	public VisualElement Root => root;

	private int iconColorIndex = -1;

	public MyAllianceApproveIconElement(PlayerEnum playerEnum)
	{
		this.playerEnum = playerEnum;

		root = new VisualElement();
		root.AddToClassList("alliance-approve__icon");
		root.pickingMode = PickingMode.Ignore;

		approveOnImage = new VisualElement();
		approveOnImage.AddToClassList("alliance-approve__on");
		approveOnImage.pickingMode = PickingMode.Ignore;
		root.Add(approveOnImage);

		approveOffImage = new VisualElement();
		approveOffImage.AddToClassList("alliance-approve__off");
		approveOffImage.pickingMode = PickingMode.Ignore;
		root.Add(approveOffImage);

		Init();
	}

	/// <summary> 플레이어 색 인덱스에 맞는 아이콘으로 교체 </summary>
	public void SetIconColor()
	{
		int colorIndex = DataManager.Instance.GetPlayerColorIndex(playerEnum);

		if (iconColorIndex == colorIndex)
			return;

		if (iconColorIndex >= 0)
		{
			root.RemoveFromClassList("alliance-approve__icon--" + iconColorIndex);
		}

		iconColorIndex = colorIndex;

		if (iconColorIndex >= 0)
		{
			root.AddToClassList("alliance-approve__icon--" + iconColorIndex);
		}
	}

	public void Init()
	{
		SetIconColor();

		approveOnImage.style.display = DisplayStyle.None;
		approveOffImage.style.display = DisplayStyle.None;

		approvedOn = false;
	}

	public void SetActive(bool activeOn)
	{
		root.style.display = activeOn ? DisplayStyle.Flex : DisplayStyle.None;
	}

	public bool IsActive()
	{
		return root.style.display.value == DisplayStyle.Flex;
	}

	public void ApproveOn(bool approveOn)
	{
		approvedOn = approveOn;

		approveOnImage.style.display = approveOn ? DisplayStyle.Flex : DisplayStyle.None;
		approveOffImage.style.display = approveOn ? DisplayStyle.None : DisplayStyle.Flex;
	}
}
