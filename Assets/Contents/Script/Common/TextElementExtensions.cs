using UnityEngine.UIElements;

/// <summary>
/// Unity 6.7 의 할당 없는 TextElement.SetText(int) 를 안전하게 쓰기 위한 확장 (2026-09-29).
/// SetText 는 요소가 아직 패널에 붙지 않았으면 경고를 찍고 문자열 할당으로 돌아가므로,
/// 붙기 전에는 예전처럼 text 에 넣고 붙은 뒤에는 SetText 를 쓴다.
/// </summary>
public static class TextElementExtensions
{
	public static void SetNumber(this TextElement textElement, int value)
	{
		if (textElement == null)
			return;

		if (textElement.panel != null)
			textElement.SetText(value);
		else
			textElement.text = value.ToString();
	}
}
