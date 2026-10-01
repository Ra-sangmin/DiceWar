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

    //마지막으로 남은 시간을 계산한 실제 시각
    //(Time.deltaTime은 앱이 백그라운드에 있는 동안 흐르지 않고, 복귀 시에도 maximumDeltaTime으로 잘려서 사용 불가)
    private float lastCheckRealtime;

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
        lastCheckRealtime = Time.realtimeSinceStartup;

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

        //실제 경과 시간만큼 차감 (외부에서 timerCurrentDelay를 늘리는 아이템 효과도 그대로 유지됨)
        float now = Time.realtimeSinceStartup;
        timerCurrentDelay -= now - lastCheckRealtime;
        lastCheckRealtime = now;

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
