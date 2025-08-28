using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    [SerializeField] Slider timer;
    [SerializeField] RectTransform activeOnPanel;
    private bool timerOn = false;

    public float timerCurrentDelay;
    private float timerMaxDelay = 10;

    public UnityAction timerOverOn = () => { };

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void SetTimerOn(bool timerOn)
    {
        this.timerOn = timerOn;

        activeOnPanel.gameObject.SetActive(timerOn);

        timerCurrentDelay = timerMaxDelay;

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
    }
}
