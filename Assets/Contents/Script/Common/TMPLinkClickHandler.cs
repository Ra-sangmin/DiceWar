using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.Events;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TMPLinkClickHandler : MonoBehaviour, IPointerClickHandler
{
	private TextMeshProUGUI textMeshPro;
	private Camera uiCamera;

	public UnityAction linkClickEventOn;

	void Awake()
	{
		textMeshPro = GetComponent<TextMeshProUGUI>();

		// 캔버스의 렌더 모드에 따라 카메라 설정
		Canvas canvas = GetComponentInParent<Canvas>();
		if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
		{
			uiCamera = canvas.worldCamera;
		}
		else
		{
			uiCamera = null;
		}
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		// 클릭한 위치에 링크가 있는지 확인 (-1이면 링크가 없는 빈 공간 클릭)
		int linkIndex = TMP_TextUtilities.FindIntersectingLink(textMeshPro, eventData.position, uiCamera);

		if (linkIndex != -1)
		{
			TMP_LinkInfo linkInfo = textMeshPro.textInfo.linkInfo[linkIndex];
			string linkId = linkInfo.GetLinkID();
			Application.OpenURL(linkId);
		}
	}
}