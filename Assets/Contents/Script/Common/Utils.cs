using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
}