using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.UIElements;

/// <summary>
/// 약관 / 개인정보처리방침 마크다운(Assets/Contents/MDFile)을 읽기 좋은 문서 화면으로 만든다 (2026-09-29).
///
/// 예전에는 전체를 Label 하나의 리치텍스트로 보여줘서 제목·문단·목록이 크기만 다르고 구분이 약했다.
/// 지금은 블록마다 요소를 따로 만들고 모양은 PopupView.uss 의 .md-* 규칙이 정한다.
///
///   맨 앞 **시행일: [..]** 줄들   → 가운데 작은 알약(.md-meta__item) 두 개
///   ## 제목                       → 번호(01, 02 ..) + 제목 + 아래 가는 선 (.md-section__head)
///   **머리말.** 로 시작하는 문단 → 머리말만 시안색, 왼쪽 세로 강조선 (.md-paragraph--lead)
///   - 목록                        → 은은한 카드 안에 시안 점 (.md-list)
///   이메일이 들어간 문단          → 테두리 카드 (.md-contact), 이메일은 눌러서 메일 앱 열기
///   그 밖의 문단                  → 본문 (.md-paragraph)
///
/// 줄 간격은 USS 에 속성이 없어서 리치텍스트 &lt;line-height&gt; 로 준다.
/// 링크는 PointerUpLinkTagEvent 로 받는다 (onLinkClick).
/// </summary>
public static class InfoDocumentBuilder
{
	private const string BodyLineHeight = "<line-height=165%>";

	//본문보다 밝게 : **굵게** 부분 (합성 볼드는 과하게 굵어서 색으로만 구분)
	private const string StrongColor = "#FFFFFF";
	//머리말 / 이메일 강조색 (팝업 판 테두리 빛과 같은 시안 계열)
	private const string AccentColor = "#8BE6EA";

	private static readonly Regex MetaRegex = new Regex(@"^\*\*(.+?)\*\*$");
	private static readonly Regex LeadRegex = new Regex(@"^\*\*(.+?)\*\*\s*(.*)$");
	private static readonly Regex StrongRegex = new Regex(@"\*\*(.*?)\*\*");
	private static readonly Regex EmailRegex = new Regex(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}");

	/// <summary> container 를 비우고 markdown 으로 문서를 채운다 </summary>
	public static void Build(VisualElement container, string markdown, EventCallback<UnityEngine.UIElements.Experimental.PointerUpLinkTagEvent> onLinkClick)
	{
		container.Clear();

		string[] lines = markdown.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

		//제목 아래 장식선
		container.Add(Element("md-title-rule"));

		int index = 0;

		//1. 맨 앞 메타 줄 (시행일 / 최종 수정일)
		VisualElement meta = null;

		while (index < lines.Length)
		{
			string line = lines[index].Trim();

			if (line.Length == 0)
			{
				index++;
				if (meta != null) break;
				continue;
			}

			Match match = MetaRegex.Match(line);
			if (match.Success == false)
				break;

			if (meta == null)
			{
				meta = Element("md-meta");
				container.Add(meta);
			}

			meta.Add(MetaItem(match.Groups[1].Value));
			index++;
		}

		//2. 나머지 블록
		int sectionNumber = 0;
		VisualElement list = null;
		StringBuilder paragraph = new StringBuilder();

		Action flushParagraph = () =>
		{
			if (paragraph.Length == 0)
				return;

			container.Add(Paragraph(paragraph.ToString(), onLinkClick));
			paragraph.Length = 0;
		};

		for (; index <= lines.Length; index++)
		{
			string raw = index == lines.Length ? string.Empty : lines[index].TrimEnd();
			string line = raw.Trim();

			//빈 줄 = 블록의 끝
			if (line.Length == 0)
			{
				flushParagraph();
				list = null;
				continue;
			}

			if (line.StartsWith("#"))
			{
				flushParagraph();
				list = null;

				string title = line.TrimStart('#').Trim();
				sectionNumber++;

				container.Add(SectionHead(sectionNumber, title, sectionNumber == 1));
				continue;
			}

			if (line.StartsWith("- ") || line.StartsWith("* "))
			{
				flushParagraph();

				if (list == null)
				{
					list = Element("md-list");
					container.Add(list);
				}

				list.Add(ListItem(line.Substring(2).Trim(), onLinkClick));
				continue;
			}

			//한 문단 안의 줄바꿈은 그대로
			if (paragraph.Length > 0)
			{
				paragraph.Append('\n');
			}

			paragraph.Append(line);
		}

		flushParagraph();

		//문서 끝 장식
		container.Add(Element("md-end-rule"));
	}

	private static VisualElement Element(string className)
	{
		VisualElement element = new VisualElement();
		element.AddToClassList(className);
		element.pickingMode = PickingMode.Ignore;
		return element;
	}

	private static Label Text(string className, string text)
	{
		Label label = new Label(text);
		label.AddToClassList(className);
		label.enableRichText = true;
		label.pickingMode = PickingMode.Ignore;
		return label;
	}

	/// <summary> "시행일: [2026-08-21]" → 이름은 흐리게, 날짜는 밝게 </summary>
	private static VisualElement MetaItem(string text)
	{
		text = text.Replace("[", string.Empty).Replace("]", string.Empty).Trim();

		int colon = text.IndexOf(':');

		string rich = colon < 0
			? text
			: $"{text.Substring(0, colon).Trim()}<color=#E6EEF5>  {text.Substring(colon + 1).Trim()}</color>";

		return Text("md-meta__item", rich);
	}

	private static VisualElement SectionHead(int number, string title, bool first)
	{
		VisualElement head = Element("md-section__head");

		if (first)
		{
			head.AddToClassList("md-section__head--first");
		}

		head.Add(Text("md-section__num", number.ToString("00")));
		head.Add(Text("md-section__title", Inline(title)));

		return head;
	}

	private static VisualElement Paragraph(string text, EventCallback<UnityEngine.UIElements.Experimental.PointerUpLinkTagEvent> onLinkClick)
	{
		//이메일이 있는 문단 = 문의 카드
		if (EmailRegex.IsMatch(text))
		{
			VisualElement card = Element("md-contact");
			card.Add(Element("md-contact__icon"));
			card.Add(LinkText("md-contact__text", BodyLineHeight + Inline(text), onLinkClick));
			return card;
		}

		//**머리말.** 본문 → 머리말만 강조색
		Match lead = LeadRegex.Match(text);

		if (lead.Success && text.StartsWith("**") && lead.Groups[2].Value.Length > 0)
		{
			string rich = $"<color={AccentColor}>{lead.Groups[1].Value}</color>  {Inline(lead.Groups[2].Value)}";

			Label label = Text("md-paragraph", BodyLineHeight + rich);
			label.AddToClassList("md-paragraph--lead");
			return label;
		}

		return Text("md-paragraph", BodyLineHeight + Inline(text));
	}

	private static VisualElement ListItem(string text, EventCallback<UnityEngine.UIElements.Experimental.PointerUpLinkTagEvent> onLinkClick)
	{
		VisualElement item = Element("md-list__item");
		item.Add(Element("md-list__dot"));
		item.Add(LinkText("md-list__text", BodyLineHeight + Inline(text), onLinkClick));
		return item;
	}

	/// <summary> 링크를 눌러야 하는 글자는 picking 을 켠다 </summary>
	private static Label LinkText(string className, string rich, EventCallback<UnityEngine.UIElements.Experimental.PointerUpLinkTagEvent> onLinkClick)
	{
		Label label = Text(className, rich);

		if (rich.Contains("<link") && onLinkClick != null)
		{
			label.pickingMode = PickingMode.Position;
			label.RegisterCallback(onLinkClick);
		}

		return label;
	}

	/// <summary> 줄 안쪽 서식 : **굵게** → 밝은 흰색, 이메일 → 강조색 + 밑줄 + mailto 링크 </summary>
	private static string Inline(string text)
	{
		string result = StrongRegex.Replace(text, $"<color={StrongColor}>$1</color>");

		result = EmailRegex.Replace(result, m =>
			$"<color={AccentColor}><u><link=\"mailto:{m.Value}\">{m.Value}</link></u></color>");

		return result;
	}
}
