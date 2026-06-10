using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.Events;

public class ToastPopup : MonoBehaviour
{
    [SerializeField] CanvasGroup canvasGroup;
	[SerializeField] Text textObj;
	//[SerializeField] List<Image> haxagonImageList = new List<Image>();

	private Tween fadeTween;

    public float maxAlphaValue = 0.85f;

	public void SetImageColor()
    {
        //Color color = InGameDataManager.Instance.GetPlayerColor();

        //foreach (var image in haxagonImageList)
        //{
        //    image.color = color;
        //}
    }

	public void SetText(string text)
	{
		textObj.text = text;
	}

	public void ActiveOn()
    {
        if (fadeTween != null)
        {
            if (fadeTween.IsPlaying() && fadeTween.IsActive()) 
            {
                fadeTween.Kill();
            }
        }

        canvasGroup.alpha = maxAlphaValue;
        fadeTween = canvasGroup.DOFade(0,1f).SetDelay(0.5f).OnComplete(()=> Destroy(gameObject));
    }
}
