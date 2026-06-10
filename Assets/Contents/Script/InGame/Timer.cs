using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    [SerializeField] Slider timer;
    [SerializeField] RectTransform activeOnPanel;
	[SerializeField] Text timeText;
	private bool timerOn = false;

    public float timerCurrentDelay;
    private float timerMaxDelay = 20;

    public UnityAction timerOverOn = () => { };

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void SetTimerOn(bool timerOn , bool haveNoAreaOn = false)
    {
        this.timerOn = timerOn;

        activeOnPanel.gameObject.SetActive(timerOn);

        timerCurrentDelay = haveNoAreaOn ? 0 : timerMaxDelay;

        ResetTimerValue();
    }

    // Update is called once per frame
    void Update()
    {
        TimerCheck();
    }

    void TimerCheck()
    {
        if (this.timerOn == false)
            return;

        timerCurrentDelay -= Time.deltaTime;

        if (timerCurrentDelay < 0)
        {
            SetTimerOn(false);

            timerOverOn();
        }

        ResetTimerValue();
    }

    public void ResetTimerValue()
    {
        float value = timerCurrentDelay / timerMaxDelay;
        timer.value = value;

        if (timeText != null )
        {
            string text = timerCurrentDelay >= 0 ? ((int)timerCurrentDelay).ToString() : string.Empty;
			timeText.text = text;
		}
	}
}
