using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ToastPopup : MonoBehaviour
{
    [SerializeField] CanvasGroup canvasGroup;
    //[SerializeField] List<Image> haxagonImageList = new List<Image>();

    private Tween fadeTween;

    public void SetImageColor()
    {
        //Color color = InGameDataManager.Instance.GetPlayerColor();

        //foreach (var image in haxagonImageList)
        //{
        //    image.color = color;
        //}
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

        canvasGroup.alpha = 0.85f;
        fadeTween = canvasGroup.DOFade(0,1f).SetDelay(0.5f);
    }
}
