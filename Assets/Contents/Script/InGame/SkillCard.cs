using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class SkillCard : MonoBehaviour
{
    private VisualElement iconElement;
    private int currentIconIndex = -1;

    /// <summary> UI Toolkit 요소 연결 (원본 Icon_0~3 이미지 교체 대체) </summary>
    public void InitView(VisualElement iconElement)
    {
        this.iconElement = iconElement;
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetCountIcon(PlayerEnum playerEnum)
    {
        if (iconElement == null)
            return;

        PlayerData playerData = DataManager.Instance.GetPlayerData(playerEnum);

        //플레이어 데이터가 아직 없으면(맵 생성 전) 건너뛴다
        if (playerData == null)
            return;

        //보유 스킬 카드 수(0~3)에 맞는 아이콘. 범위를 벗어나면 원본과 동일하게 0번을 쓴다
        int countIndex = playerData.sc;

        if (countIndex < 0 || countIndex > 3)
        {
            countIndex = 0;
        }

        if (currentIconIndex == countIndex)
            return;

        if (currentIconIndex >= 0)
        {
            iconElement.RemoveFromClassList("skill-card__icon--" + currentIconIndex);
        }

        currentIconIndex = countIndex;
        iconElement.AddToClassList("skill-card__icon--" + currentIconIndex);
    }

    public void SetActive(bool activeOn)
    {
        if (iconElement != null)
        {
            iconElement.style.display = activeOn ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
