using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public static class Utils
{
    /// <summary>
    /// 리스트 랜덤으로 섞기
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    public static void Shuffle<T>(this List<T> list)
    {
        var temp = list.OrderBy(item => Guid.NewGuid()).ToList();
        list.Clear();
        list.AddRange(temp);
    }

    public static SendAreaData GetSendAreaData(this AreaData areaData)
    {
        SendAreaData sendAreaData = new SendAreaData()
        {
            id = areaData.id,
            player = areaData.player,
            dice = areaData.dice
        };

        return sendAreaData;
    }

	public static void TweenKill(this Tween tween)
	{
		if (tween != null && tween.IsActive() && tween.IsPlaying())
		{
			tween.Kill();
		}
	}

	/// <summary>
	/// 마크다운(Assets/Contents/MDFile)을 UI Toolkit 리치텍스트로 바꾼다.
	/// 약관 / 개인정보처리방침 팝업(InfoPopup)이 쓴다.
	///
	/// 크기·줄간격·문단 간격은 같은 문서를 HTML 로 뽑았을 때(final_terms_ko.html)의 CSS 와 맞췄다.
	///   body { line-height: 1.7 }   p { margin: 1em 0 }
	///   h1 { font-size: 2.25rem }   h2 { font-size: 1.5em }   h1,h2 { line-height: 1.25; margin: 1.4em 0 0.6em }
	/// 전부 % 로 쓰기 때문에 Label 의 font-size 를 바꿔도 비율이 그대로 유지된다.
	///
	/// 여백은 "빈 줄 하나의 높이"로 만든다. 빈 줄은 본문 글자 크기를 쓰므로
	/// HTML 의 여백(px)을 본문 기준 em 으로 환산한 값이 곧 그 줄의 line-height 다.
	/// (마진 상쇄까지 반영: 문단↔문단 = max(1em,1em) = 1em, 본문→제목 = max(1em, 1.4 x 1.5em) = 2.1em,
	///  제목→본문 = max(0.6 x 1.5em, 1em) = 1em)
	/// </summary>
	public static string ConvertMarkdownToRichText(this string rawText)
	{
		//닫는 태그로 되돌리지 않고 매번 값을 다시 지정한다.
		//UI Toolkit 에서 </line-height> 뒤 값 복구가 미덥지 않아서다.
		const string bodyLineHeight = "<line-height=170%>";
		const string headLineHeight = "<line-height=125%>";
		const string gapNormal = "<line-height=100%>";
		const string gapBeforeHeading = "<line-height=210%>";

		string[] lines = rawText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

		List<string> blockText = new List<string>();
		List<bool> blockHeading = new List<bool>();

		System.Text.StringBuilder current = new System.Text.StringBuilder();

		for (int i = 0; i <= lines.Length; i++)
		{
			bool end = i == lines.Length;
			string raw = end ? string.Empty : lines[i];

			//빈 줄 = 블록(문단)의 끝
			if (end || raw.Trim().Length == 0)
			{
				if (current.Length > 0)
				{
					blockText.Add(current.ToString());
					blockHeading.Add(false);
					current.Length = 0;
				}
				continue;
			}

			//제목은 항상 자기 혼자 한 블록이다
			if (raw.StartsWith("# ") || raw.StartsWith("## "))
			{
				if (current.Length > 0)
				{
					blockText.Add(current.ToString());
					blockHeading.Add(false);
					current.Length = 0;
				}

				bool h1 = raw.StartsWith("# ");
				string size = h1 ? "<size=225%>" : "<size=150%>";
				string title = InlineMarkdown(raw.Substring(h1 ? 2 : 3));

				blockText.Add(size + "<b>" + title + "</b></size>");
				blockHeading.Add(true);
				continue;
			}

			string line = raw.StartsWith("- ")
				? "• " + InlineMarkdown(raw.Substring(2))
				: InlineMarkdown(raw);

			//한 문단 안의 줄바꿈은 그대로 살린다 (HTML 의 <br> 에 해당)
			if (current.Length > 0)
			{
				current.Append('\n');
			}

			current.Append(line);
		}

		System.Text.StringBuilder result = new System.Text.StringBuilder();

		for (int i = 0; i < blockText.Count; i++)
		{
			if (i > 0)
			{
				//블록 사이의 빈 줄 하나가 곧 여백이 된다
				result.Append('\n');
				result.Append(blockHeading[i] ? gapBeforeHeading : gapNormal);
				result.Append('\n');
			}

			result.Append(blockHeading[i] ? headLineHeight : bodyLineHeight);
			result.Append(blockText[i]);
		}

		return result.ToString();
	}

	/// <summary> 줄 안쪽 서식 (굵게 / 이메일 링크) </summary>
	static string InlineMarkdown(string line)
	{
		// **텍스트** -> <b>텍스트</b>
		string result = Regex.Replace(line, @"\*\*(.*?)\*\*", "<b>$1</b>");

		// 이메일은 색 + 밑줄 + 클릭 가능한 link 태그로 (InfoPopup 이 PointerUpLinkTagEvent 로 받는다)
		result = Regex.Replace(result, @"(wyeth123@naver\.com)", "<color=#60a5fa><u><link=\"mailto:$1\">$1</link></u></color>");

		return result;
	}
}