using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OriginMapCreater : MonoBehaviour
{
    // 셀 데이터 (cell data)
    private int XMAX;
    private int YMAX;
    private int cel_max;
    private int[] cel;
    private Join[] join;//인접 셀을 포함한 배열(arrangement with adjacent cells)

    private int AREA_MAX;//최대 영역 수 (maximum number of areas)
    public List<AreaData> adat = new List<AreaData>(); //인접 셀을 포함한 배열(arrangement with adjacent cells)

    private int[] num;// 지역번호 (area serial number)
    private int[] rcel;// 인접 셀(adjacent cell)
    private int[] next_f;// 침투시 사용하는 주변 셀(peripheral cell to use for penetration)
    public int[] chk;
    private int pmax;

    private void Awake()
    {
        this.XMAX = 20;
        this.YMAX = 10;
        this.cel_max = this.XMAX * this.YMAX;
        cel = new int[this.cel_max];

        //인접 셀을 포함한 배열(arrangement with adjacent cells)
        join = new Join[this.cel_max];

        for (int i = 0; i < this.cel_max; i++)
        {
            this.join[i] = new Join();
            for (int j = 0; j < 6; j++)
            {
                this.join[i].dir[j] = this.NextCel(i, j);
            }
        }

        // 지역 데이터 (area data)
        AREA_MAX = 20;//최대 영역 수 (maximum number of areas)
        adat = new List<AreaData>();

        for (int i = 0; i < AREA_MAX; i++)
        {
            this.adat.Add(new AreaData(i));
        }

        // 맵을 만들 때 사용 (used for map creation)
        num = new int[this.cel_max];// 지역번호 (area serial number)

        for (int i = 0; i < this.cel_max; i++)
        {
            this.num[i] = i;
        }

        rcel = new int[this.cel_max];// 인접 셀(adjacent cell)
        next_f = new int[this.cel_max];// 침투시 사용하는 주변 셀(peripheral cell to use for penetration)
        //this.alist = new Array(this.AREA_MAX);  // 지역 목록 (area list)
        this.chk = new int[this.AREA_MAX];        // 영역 그리기 선용 (for area drawing lines)
        //this.tc = new Array(this.AREA_MAX);     // 인접 지역 수로 사용 (used in adjacent area number)
        // 게임 데이터 (game data)
        this.pmax = 2;     // 플레이어 수 (number of players)
        //this.user = 0;      // 사용자 플레이어 (user player)
        //this.put_dice = 3;  // 배치 다이스 평균 수 (average number of placement dice)
        //this.jun = [0, 1, 2, 3, 4, 5, 6, 7];            // 순서 (order)
        //this.ban = 0;           // 수차 현재 플레이어는 player = jun[ban]; (the current player is player = jun[ban];)
        //this.area_from = 0; // 공격원 (attack source)
        //this.area_to = 0;       // 공격 대상 (attack destination)
        //this.defeat = 0;        // 0. 공격 실패 1. 공격 성공 (0. attack failure, 1. attack success)
        // 플레이어 데이터 (player data)
        //this.player = new Array(8);
        //this.STOCK_MAX = 64;    // 최대 스톡 수 (maximum number of stocks)
        // COM 사고 (com thinking)
        //this.list_from = new Array(this.AREA_MAX * this.AREA_MAX);
        //this.list_to = new Array(this.AREA_MAX * this.AREA_MAX);
        //역사 (history)
        //this.his = new Array();
        //this.his_c = 0;
        // 초기 배치 (initial placement)
        //this.his_arm = new Array(this.AREA_MAX);
        //this.his_dice = new Array(this.AREA_MAX);

        
    }

    public int NextCel(int opos, int dir)
    {
        int ox = opos % this.XMAX;
        int oy = opos / this.XMAX;
        var f = oy % 2;
        var ax = 0;
        var ay = 0;
        switch (dir)
        {
            case 0: ax = f; ay = -1; break; // 右上 (upper right)
            case 1: ax = 1; break;  // 右 (right)
            case 2: ax = f; ay = 1; break;  // 右下 (bottom right)
            case 3: ax = f - 1; ay = 1; break;  // 左下 (bottom left)
            case 4: ax = -1; break; // 左 (left)
            case 5: ax = f - 1; ay = -1; break; // 左上 (upper left)
        }
        var x = ox + ax;
        var y = oy + ay;
        if (x < 0 || y < 0 || x >= this.XMAX || y >= this.YMAX) return -1;
        return y * this.XMAX + x;
    }

    //public void MakeMap()
    //{
    //    int c, an;

    //    // 일련 번호 셔플
    //    for (int i = 0; i < cel_max; i++)
    //    {
    //        int r = Random.Range(0, cel_max);
    //        int tmp = num[i];
    //        num[i] = num[r];
    //        num[r] = tmp;
    //    }

    //    // 셀 초기화
    //    for (int i = 0; i < cel_max; i++)
    //    {
    //        cel[i] = 0;
    //        rcel[i] = 0; // 인접 셀
    //    }
    //    an = 1; // 지역 번호
    //    rcel[Random.Range(0, cel_max)] = 1; // 첫 번째 셀

    //    while (true)
    //    {
    //        // 침투 개시 셀 결정
    //        int pos = -1;
    //        int min = 9999;
    //        for (int i = 0; i < cel_max; i++)
    //        {
    //            if (cel[i] > 0) continue;
    //            if (num[i] > min) continue;
    //            if (rcel[i] == 0) continue;
    //            min = num[i];
    //            pos = i;
    //        }
    //        if (min == 9999) break;

    //        // 침투 개시
    //        int ret = Percolate(pos, 8, an);
    //        if (ret == 0) break;
    //        an++;
    //        if (an >= AREA_MAX) break;
    //    }

    //    // 바다에서 면적 1의 셀을 없애기
    //    for (int i = 0; i < cel_max; i++)
    //    {
    //        if (cel[i] > 0) continue;
    //        int pos;
    //        int f = 0;
    //        int a = 0;
    //        for (int k = 0; k < 6; k++)
    //        {
    //            pos = join[i].dir[k];
    //            if (pos < 0) continue;
    //            if (cel[pos] == 0) f = 1; else a = cel[pos];
    //        }
    //        if (f == 0) cel[i] = a;
    //    }

    //    // 영역 데이터 초기화
    //    for (int i = 0; i < AREA_MAX; i++) adat[i] = new AreaData();

    //    // 면적
    //    for (int i = 0; i < cel_max; i++)
    //    {
    //        an = cel[i];
    //        if (an > 0) adat[an].size++;
    //    }

    //    // 면적 10 이하의 영역을 지우기
    //    for (int i = 1; i < AREA_MAX; i++)
    //    {
    //        if (adat[i].size <= 5) adat[i].size = 0;
    //    }
    //    for (int i = 0; i < cel_max; i++)
    //    {
    //        an = cel[i];
    //        if (adat[an].size == 0) cel[i] = 0;
    //    }

    //    //지역의 중심지 결정
    //    for (int i = 1; i < AREA_MAX; i++)
    //    {
    //        adat[i].left = XMAX;
    //        adat[i].right = -1;
    //        adat[i].top = YMAX;
    //        adat[i].bottom = -1;
    //        adat[i].len_min = 9999;
    //    }
    //    c = 0;
    //    for (int i = 0; i < YMAX; i++)
    //    {
    //        for (int j = 0; j < XMAX; j++)
    //        {
    //            an = cel[c];
    //            if (an > 0)
    //            {
    //                if (j < adat[an].left) adat[an].left = j;
    //                if (j > adat[an].right) adat[an].right = j;
    //                if (i < adat[an].top) adat[an].top = i;
    //                if (i > adat[an].bottom) adat[an].bottom = i;
    //            }
    //            c++;
    //        }
    //    }
    //    for (int i = 1; i < AREA_MAX; i++)
    //    {
    //        adat[i].cx = (adat[i].left + adat[i].right) / 2;
    //        adat[i].cy = (adat[i].top + adat[i].bottom) / 2;
    //    }
    //    c = 0;
    //    int x, y, len;
    //    for (int i = 0; i < YMAX; i++)
    //    {
    //        for (int j = 0; j < XMAX; j++)
    //        {
    //            an = cel[c];
    //            if (an > 0)
    //            {
    //                // 중심지로부터의 거리(경계선 근처는 가능한 한 피한다)
    //                x = Mathf.Abs(adat[an].cx - j);
    //                y = Mathf.Abs(adat[an].cy - i);
    //                len = x + y;
    //                int f = 0;
    //                for (int k = 0; k < 6; k++)
    //                {
    //                    int pos = join[c].dir[k];
    //                    if (pos > 0)
    //                    {
    //                        int an2 = cel[pos];
    //                        if (an2 != an)
    //                        {
    //                            f = 1;
    //                            // 이어서 인접 데이터도 작성
    //                            adat[an].join[an2] = 1;
    //                        }
    //                    }
    //                }
    //                if (f > 0) len += 4;
    //                // 거리가 가까운 것을 중심지로 한다
    //                if (len < adat[an].len_min)
    //                {
    //                    adat[an].len_min = len;
    //                    adat[an].cpos = i * XMAX + j;
    //                }
    //            }
    //            c++;
    //        }
    //    }

    //    // 지역 속군을 결정
    //    for (int i = 0; i < AREA_MAX; i++) adat[i].arm = -1;
    //    int arm = 0; // 속군
    //    int[] alist = new int[AREA_MAX]; // 지역 목록
    //    while (true)
    //    {
    //        c = 0;
    //        for (int i = 1; i < AREA_MAX; i++)
    //        {
    //            if (adat[i].size == 0) continue;
    //            if (adat[i].arm >= 0) continue;
    //            alist[c] = i;
    //            c++;
    //        }
    //        if (c == 0) break;
    //        an = alist[Random.Range(0,c)];
    //        adat[an].arm = arm;
    //        arm++; if (arm >= pmax) arm = 0;
    //    }

    //    // 영역 그리기 선 데이터 작성
    //    for (int i = 0; i < this.AREA_MAX; i++) this.chk[i] = 0;
    //    for (int i = 0; i < this.cel_max; i++)
    //    {
    //        var area = this.cel[i];
    //        if (area == 0) continue;
    //        if (this.chk[area] > 0) continue;
    //        for (int k = 0; k < 6; k++)
    //        {
    //            if (this.chk[area] > 0) break;
    //            var n = this.join[i].dir[k];
    //            if (n >= 0)
    //            {
    //                if (this.cel[n] != area)
    //                {
    //                    this.set_area_line(i, k);
    //                    this.chk[area] = 1;
    //                }
    //            }
    //        }
    //    }
    //}

    //public int Percolate(int pt, int cmax, int an)
    //{
    //    if (cmax < 3) cmax = 3;

    //    int i, j, k;
    //    int opos = pt; // 시작 셀

    //    // 인접 플래그
    //    for (i = 0; i < cel_max; i++) next_f[i] = 0;

    //    int c = 0; // 셀 수
    //    while (true)
    //    {
    //        cel[opos] = an;
    //        c++;
    //        // 주변 셀
    //        for (i = 0; i < 6; i++)
    //        {
    //            int pos = join[opos].dir[i];
    //            if (pos < 0) continue;
    //            next_f[pos] = 1;
    //        }
    //        // 주변 셀에서 최소 번호를 다음 셀로 설정
    //        int min = 9999;
    //        for (i = 0; i < cel_max; i++)
    //        {
    //            if (next_f[i] == 0) continue; // 인접하지 않음
    //            if (cel[i] > 0) continue; // 이미 지역화
    //            if (num[i] > min) continue; // 최소 주문 번호가 아님
    //            min = num[i];
    //            opos = i;
    //        }
    //        if (min == 9999) break;
    //        if (c >= cmax) break; // 주어진 면적을 초과
    //    }
    //    // 인접 셀 추가
    //    for (i = 0; i < cel_max; i++)
    //    {
    //        if (next_f[i] == 0) continue;
    //        if (cel[i] > 0) continue; // 이미 지역화
    //        cel[i] = an;
    //        c++;
    //        // 또한, 인접 셀을 다음 영역의 후보로한다.
    //        for (k = 0; k < 6; k++)
    //        {
    //            int pos = join[i].dir[k];
    //            if (pos < 0) continue;
    //            rcel[pos] = 1;
    //        }
    //    }
    //    return c;
    //}

}


//[System.Serializable]
//public class AreaData
//{
//    //public int size;// 0. 부재 1~
//    //public int left;// 중심 셀
//    //public int right;
//    //public int top;
//    //public int bottom;
//    //public int cx;
//    //public int cy;
//    //public int len_min;
//    //public int cpos;
//    //public int arm;
//    //public int[] join = new int[6]; // Assuming 6 directions

//    public int pieceIndex;
//    public int size=0;      // 0. 부재 1~
//    public int cpos=0;      // 중심 셀
//    //public int arm=0;       // 속군


//    public int dice=0;      // 주사위 수

//    //중심지를 결정하는 변수
//    public int left=0;
//    public int right=0;
//    public int top=0;
//    public int bottom=0;
//    public int cx=0;        // left,right 중간지
//    public int cy=0;        // top,bottom 중간지
//    public int len_min=0;

//    //주변 라인용
//    public int[] line_cel = new int[101];
//    public int[] line_dir = new int[101];
//    public int[] join = new int[32]; // Assuming 6 directions


//    public PlayerEnum playerEnum = PlayerEnum.Player_None;
//    public bool choisOn = false;
//    public List<int> connectedPieceList = new List<int>();
//    public List<Hexagon> hexagonList = new List<Hexagon>();
//    public string bundleKey = string.Empty;

//    public AreaData(int pieceIndex)
//    {
//        this.pieceIndex = pieceIndex;
//    }

//    public void SetHexagonList(List<Hexagon> hexagonList)
//    {
//        this.hexagonList = hexagonList;
//    }

//    public void AddHexagon(Hexagon hexagon)
//    {
//        this.hexagonList.Add(hexagon);
//    }

//    public bool PlayerChangeOn(PlayerEnum playerEnum)
//    {
//        this.playerEnum = playerEnum;
//        this.choisOn = false;
//        SetColor();

//        return true;
//    }

//    public void ChoisEventOn(bool choisOn)
//    {
//        this.choisOn = choisOn;
//        SetColor();
//    }

//    void SetColor()
//    {
//        for (int i = 0; i < hexagonList.Count; i++)
//        {
//            hexagonList[i].SetPlayer(playerEnum, choisOn);
//        }
//    }

//    public void SetConnectedPieceList()
//    {
//        for (int i = 0; i < join.Length; i++)
//        {
//            if (join[i] == 1)
//            {
//                connectedPieceList.Add(i);
//            }
//        }
//    }

//    public void SetBundleKey(List<AreaData> adat, PlayerEnum playerEnum, string bundleKey)
//    {
//        this.bundleKey = bundleKey;

//        foreach (int connectedPiece in connectedPieceList)
//        {
//            AreaData areaData = adat[connectedPiece];


//            if (areaData.playerEnum == playerEnum && areaData.bundleKey == string.Empty)
//            {
//                areaData.SetBundleKey(adat, playerEnum, bundleKey);
//            }
//        }
//    }
//}