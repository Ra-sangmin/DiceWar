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

    private float timerCurrentDelay;
    private float timerMaxDelay = 10;

    //앱이 백그라운드로 가도 흐르는 실제 시간 기준 종료 시각
    //(Time.deltaTime은 일시정지 중엔 흐르지 않고, 복귀 시에도 maximumDeltaTime으로 잘려서 사용 불가)
    private float timerEndTime;

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
        timerEndTime = Time.realtimeSinceStartup + timerMaxDelay;

        ResetTimerValue();
    }

    //백그라운드에서 복귀하면 즉시 남은 시간 갱신 (그 사이 시간이 다 지났으면 바로 턴 종료)
    void OnApplicationPause(bool pause)
    {
        if (pause == false)
        {
            TimerCheck();
        }
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

        timerCurrentDelay = timerEndTime - Time.realtimeSinceStartup;

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
