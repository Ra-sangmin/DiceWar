using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

public class SMToggle : MonoBehaviour
{
    [SerializeField] MaskableGraphic target;

    [SerializeField] SMToggleGroup smToggleGroup;

    [SerializeField] Button button;

    public BoolReactiveProperty toggleValue = new BoolReactiveProperty(false);

    public Color trueColor;
    public Color falseColor;
    public Color disableColor;

    public BoolReactiveProperty interactable = new BoolReactiveProperty(true);

    private void Awake()
    {
        EventSet();
    }

    void EventSet()
    {
        if (smToggleGroup != null)
        {
            smToggleGroup.SetToggle(this);
        }

        toggleValue.Subscribe(result =>
        {
            SMToggle[] smToggleList = GetComponentsInChildren<SMToggle>(true);

            for (int i = 0; i < smToggleList.Length; i++)
            {
                if (smToggleList[i] == this)
                    continue;
                smToggleList[i].toggleValue.Value = result;
            }

            SetColor();

            if (result == true && smToggleGroup != null)
            {
                smToggleGroup.ToggleValueChangeOn(this);
            }
        }).AddTo(gameObject);


        interactable.Subscribe(result =>
        {
            SMToggle[] smToggleList = GetComponentsInChildren<SMToggle>(true);

            for (int i = 0; i < smToggleList.Length; i++)
            {
                if (smToggleList[i] == this)
                    continue;
                smToggleList[i].interactable.Value = result;
            }

            if (button != null)
            {
                button.interactable = interactable.Value;
            }

            SetColor();
        }).AddTo(gameObject);
    }

    public void ToggleOn()
    {
        toggleValue.Value = !toggleValue.Value;
    }

    public void SetToggleValue(bool isOn)
    {
        toggleValue.Value = isOn;
    }

    void SetColor()
    {
        if (target != null)
        {
            if (interactable.Value)
            {
                target.color = toggleValue.Value ? trueColor : falseColor;
            }
            else
            {
                target.color = disableColor;
            }
            
        }
    }


}
