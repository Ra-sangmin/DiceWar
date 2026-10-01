using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class Timer : MonoBehaviour
{
    private VisualElement inactivePanel;
    private VisualElement activeOnPanel;
    private VisualElement fillElement;
    private Label timeText;
    private bool timerOn = false;

    public float timerCurrentDelay;
    private float timerMaxDelay = 20;

    private const float DefaultTurnDelay = 5f;
    private const float FirstTurnDelay = 10f;
    private const float AttackExtraDelay = 1f;
    private const float MaxTurnDelay = 10f;     //땅 공격으로 늘어나도 타이머는 최대 10초까지만 채워진다

    //방사형(시계방향) 게이지용 - UI Toolkit 에는 Image Filled(Radial360) 가 없어서 직접 부채꼴 메시를 그린다
    private Texture fillTexture;
    private Rect fillUvRect = new Rect(0f, 0f, 1f, 1f);
    private float fillRatio = 1f;
    private const int FillSegmentMax = 72;      //5도 단위

    public UnityAction timerOverOn = () => { };

    // Start is called before the first frame update
    void Start()
    {

    }

    /// <summary> UI Toolkit 요소 연결 (원본 Slider + ActivePanel 대체) </summary>
    public void InitView(VisualElement root)
    {
        inactivePanel = root.Q<VisualElement>("timer-inactive");
        activeOnPanel = root.Q<VisualElement>("timer-active");
        fillElement = root.Q<VisualElement>("timer-fill");
        timeText = root.Q<Label>("timer-text");

        if (fillElement != null)
        {
            fillElement.generateVisualContent += OnGenerateFillMesh;
            fillElement.RegisterCallback<GeometryChangedEvent>(OnFillGeometryChanged);
        }

        SetTimerOn(false);
    }

    /// <summary> USS 의 background-image 에서 스프라이트를 한번만 읽어온 뒤,
    /// 배경을 지워서 부채꼴 메시만 보이도록 한다 (2026-09-18) </summary>
    private void OnFillGeometryChanged(GeometryChangedEvent evt)
    {
        if (fillElement == null)
            return;

        if (fillTexture == null)
        {
            Background bg = fillElement.resolvedStyle.backgroundImage;

            if (bg.sprite != null)
            {
                fillTexture = bg.sprite.texture;

                //아틀라스/스프라이트 시트 대응 : 텍스처 안에서 스프라이트가 차지하는 영역
                Rect texRect = bg.sprite.textureRect;

                fillUvRect = new Rect(texRect.x / fillTexture.width,
                                      texRect.y / fillTexture.height,
                                      texRect.width / fillTexture.width,
                                      texRect.height / fillTexture.height);
            }
            else if (bg.texture != null)
            {
                fillTexture = bg.texture;
                fillUvRect = new Rect(0f, 0f, 1f, 1f);
            }

            if (fillTexture != null)
            {
                //원본 배경(꽉 찬 원)은 지운다. 이제 메시가 대신 그린다
                fillElement.style.backgroundImage = StyleKeyword.None;
                fillElement.UnregisterCallback<GeometryChangedEvent>(OnFillGeometryChanged);
            }
        }

        fillElement.MarkDirtyRepaint();
    }

    /// <summary> 12시 방향에서 시계방향으로 채워진 부채꼴을 그린다 (원본 Image Filled / Radial360 / Top / Clockwise) </summary>
    /// <summary> 12시 방향에서 시계방향으로 채워진 부채꼴을 그린다 (원본 Image Filled / Radial360 / Top / Clockwise) </summary>
    /// <summary> 남은 시간만큼의 부채꼴을 그린다.
    /// 줄어드는(사라지는) 쪽이 12시에서 시계방향으로 돌게 한다 (2026-09-18) </summary>
    private void OnGenerateFillMesh(MeshGenerationContext mgc)
    {
        if (fillElement == null || fillTexture == null)
            return;

        float ratio = Mathf.Clamp01(fillRatio);

        if (ratio <= 0.0001f)
            return;

        Rect rect = fillElement.contentRect;

        if (rect.width <= 0f || rect.height <= 0f)
            return;

        int segment = Mathf.Max(1, Mathf.CeilToInt(FillSegmentMax * ratio));

        MeshWriteData mesh = mgc.Allocate(segment + 2, segment * 3, fillTexture);

        Rect uvRegion = mesh.uvRegion;

        Vector2 center = rect.center;
        float radiusX = rect.width * 0.5f;
        float radiusY = rect.height * 0.5f;

        //중심점
        mesh.SetNextVertex(MakeVertex(center, rect, uvRegion));

        //빈 부분(이미 사라진 시간)이 12시에서 시계방향으로 자라난다.
        //따라서 남은 부채꼴은 그 끝에서 시작해 12시로 돌아온다.
        //UI Toolkit 은 y 가 아래로 증가하므로 각도를 더하면 그대로 시계방향이다
        float startRad = -Mathf.PI * 0.5f + Mathf.PI * 2f * (1f - ratio);
        float sweepRad = Mathf.PI * 2f * ratio;

        for (int i = 0; i <= segment; i++)
        {
            float rad = startRad + sweepRad * ((float)i / segment);

            Vector2 point = new Vector2(center.x + Mathf.Cos(rad) * radiusX,
                                        center.y + Mathf.Sin(rad) * radiusY);

            mesh.SetNextVertex(MakeVertex(point, rect, uvRegion));
        }

        for (int i = 0; i < segment; i++)
        {
            mesh.SetNextIndex(0);
            mesh.SetNextIndex((ushort)(i + 1));
            mesh.SetNextIndex((ushort)(i + 2));
        }
    }

    private Vertex MakeVertex(Vector2 point, Rect rect, Rect uvRegion)
    {
        Vertex vertex = new Vertex();

        vertex.position = new Vector3(point.x, point.y, Vertex.nearZ);
        vertex.tint = Color.white;

        //요소 안에서의 0~1 위치
        float x = rect.width > 0f ? (point.x - rect.x) / rect.width : 0f;
        float y = rect.height > 0f ? (point.y - rect.y) / rect.height : 0f;

        //텍스처는 좌하단이 원점이라 세로를 뒤집는다
        float u = fillUvRect.x + x * fillUvRect.width;
        float v = fillUvRect.y + (1f - y) * fillUvRect.height;

        //동적 아틀라스 대응
        vertex.uv = new Vector2(uvRegion.x + u * uvRegion.width,
                                uvRegion.y + v * uvRegion.height);

        return vertex;
    }

    public void SetTimerOn(bool timerOn , bool haveNoAreaOn = false, bool isFirstTurn = false)
    {
        this.timerOn = timerOn;

        if (activeOnPanel != null)
        {
            activeOnPanel.style.display = timerOn ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (inactivePanel != null)
        {
            inactivePanel.style.display = timerOn ? DisplayStyle.None : DisplayStyle.Flex;
        }

        if (timerOn)
        {
            timerMaxDelay = isFirstTurn ? FirstTurnDelay : DefaultTurnDelay;
        }

        timerCurrentDelay = haveNoAreaOn ? 0 : timerMaxDelay;

        ResetTimerValue();
    }

    public void AddExtraTimeOn()
    {
        if (timerOn == false)
            return;

        timerMaxDelay = Mathf.Min(timerMaxDelay + AttackExtraDelay, MaxTurnDelay);
        timerCurrentDelay = Mathf.Min(timerCurrentDelay + AttackExtraDelay, MaxTurnDelay);

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
        float value = Mathf.Clamp01(timerCurrentDelay / timerMaxDelay);

        if (fillElement != null)
        {
            //시계방향으로 줄어드는 방사형 게이지
            if (Mathf.Abs(fillRatio - value) > 0.0005f)
            {
                fillRatio = value;
                fillElement.MarkDirtyRepaint();
            }
        }

        if (timeText != null)
        {
            string text = timerCurrentDelay >= 0 ? ((int)timerCurrentDelay).ToString() : string.Empty;
            timeText.text = text;
        }
    }
}
