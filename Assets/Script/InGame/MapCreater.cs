using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MapCreater
{
    public int num_player = 3;//총 플레이어의 수 (기본 값 : 3)
    public int off_line_num_player = 2;//총 플레이어의 수 (기본 값 : 3)
    public Vector2 mapSizeValue = new Vector2(20, 15); // 가로 세로 육각형 타일의 개수 ( 기본 값 x = 20 , y = 15)
    public int num_area = 10;//총 영토의 수 (기본값 : 10)
    public int diceMaxCount = 6;//영토의 주사위 최대 갯수

    private int cel_max;
    public int[] num;// 지역번호 (area serial number)
    public int[] rcel;// 인접 셀(adjacent cell)
    public int[] next_f;// 침투시 사용하는 주변 셀(peripheral cell to use for penetration)
    public int[] cel;
    public Join[] join;//인접 셀을 포함한 배열(arrangement with adjacent cells)

    public List<HexagonData> hexagonDataList = new List<HexagonData>();

    public List<AreaData> areaDataList = new List<AreaData>(); //인접 셀을 포함한 배열(arrangement with adjacent cells)

    public void InitMapData(Vector2 mapSizeValue, int num_player)
    {
        this.num_player = num_player;

        this.mapSizeValue = mapSizeValue;


        this.cel_max = (int)this.mapSizeValue.x * (int)this.mapSizeValue.y;

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
        num_area = 100;//최대 영역 수 (maximum number of areas)

        // 맵을 만들 때 사용 (used for map creation)
        num = new int[this.cel_max];// 지역번호 (area serial number)

        for (int i = 0; i < this.cel_max; i++)
        {
            this.num[i] = i;
        }

        rcel = new int[this.cel_max];// 인접 셀(adjacent cell)
        next_f = new int[this.cel_max];// 침투시 사용하는 주변 셀(peripheral cell to use for penetration)

        // 일련 번호 셔플
        for (int i = 0; i < cel_max; i++)
        {
            int r = Random.Range(0, cel_max);
            int tmp = num[i];
            num[i] = num[r];
            num[r] = tmp;

            //Debug.LogWarning(i + " , "+num[i]);
        }

        // 셀 초기화
        for (int i = 0; i < cel_max; i++)
        {
            cel[i] = 0;
            rcel[i] = 0; // 인접 셀
        }
    }

    public List<AreaData> GetAreaDataList()
    {
        return areaDataList;
    }

    /// <summary>
    /// 메서드 옆의 셀 번호를 반환합니다 (return the cell number to the next method)
    /// </summary>
    /// <param name="opos"></param>
    /// <param name="dir"></param>
    /// <returns></returns>
    public int NextCel(int opos, int dir)
    {
        //x인덱스
        int ox = opos % (int)this.mapSizeValue.x;
        //int oy = Mathf.FloorToInt((float)opos / this.XMAX);
        //y인덱스
        int oy = opos / (int)this.mapSizeValue.x;
        int f = (oy % 2) * -1; // 짝수면 0 , 홀수면 -1
        int ax = 0;
        int ay = 0;

        switch (dir)
        {
            case 0: ax = f + 1; ay = +1; break;  // 오른쪽 상단 (upper right)
            case 1: ax = f; ay = +1; break;      // 왼쪽 상단 (upper left)
            case 2: ax = f + 1; ay = -1; break;  // 오른쪽 하단 (bottom right)
            case 3: ax = f; ay = -1; break;  // 왼쪽 하단 (bottom left)
            case 4: ax = 1; ay = +0; break;  // 오른쪽 (right)
            case 5: ax = -1; ay = +0; break;  // 왼쪽 (left)
        }

        int x = ox + ax;
        int y = oy + ay;

        if (x < 0 || y < 0 || x >= this.mapSizeValue.x || y >= this.mapSizeValue.y) return -1;

        return y * (int)this.mapSizeValue.x + x;
    }



    public List<AreaData> CreateMap()
    {
        int c, an;
        an = 1; // 지역 번호

        SetCel();

        areaDataList = new List<AreaData>();

        // 영역 데이터 초기화
        for (int i = 0; i < num_area; i++)
        {
            AreaData areaData = new AreaData(i);

            areaData.dice = Random.Range(1, 5);

            areaDataList.Add(areaData);
        }

        // 면적
        for (int i = 0; i < cel_max; i++)
        {
            an = cel[i];
            if (an > 0)
            {
                areaDataList[an].AddCel(i);
            }
        }

        // 면적 10 이하의 영역을 지우기
        for (int i = 1; i < num_area; i++)
        {
            if (areaDataList[i].cel.Count <= 10) 
            {
                areaDataList[i].ClearCel();
            } 
        }

        for (int i = 0; i < cel_max; i++)
        {
            an = cel[i];
            if (areaDataList[an].cel.Count == 0)
            {
                cel[i] = 0;
            }
        }

        // 지역 속군을 결정
        for (int i = 0; i < num_area; i++) areaDataList[i].PlayerChangeOn(PlayerEnum.Player_None);
        int arm = 0; // 속군
        int[] alist = new int[num_area]; // 지역 목록
        while (true)
        {
            c = 0;
            for (int i = 1; i < num_area; i++)
            {
                if (areaDataList[i].cel.Count == 0) continue;
                if ((int)areaDataList[i].player >= 0) continue;
                alist[c] = i;
                c++;
            }
            if (c == 0) break;
            an = alist[Random.Range(0, c)];
            areaDataList[an].player = (PlayerEnum)arm;

            arm++; if (arm >= num_player) arm = 0;
        }

        return areaDataList;
    }

    void SetCel()
    {
        int an = 1;

        int ranValue = Random.Range(0, cel_max);

        rcel[ranValue] = 1; // 첫 번째 셀

        while (true)
        {
            // 침투 개시 셀 결정
            int pos = -1;
            int min = 9999;
            for (int i = 0; i < cel_max; i++)
            {
                if (cel[i] > 0) continue;
                if (num[i] > min) continue;
                if (rcel[i] == 0) continue;
                min = num[i];
                pos = i;
            }

            if (min == 9999) break;

            // 침투 개시
            int ret = Percolate(pos, 8, an);

            if (ret == 0) break;
            an++;
            if (an >= num_area) break;
        }

        // 바다에서 면적 1의 셀을 없애기
        for (int i = 0; i < cel_max; i++)
        {
            if (cel[i] > 0) continue;
            int pos;
            int f = 0;
            int a = 0;
            for (int k = 0; k < 6; k++)
            {
                pos = join[i].dir[k];
                if (pos < 0) continue;
                if (cel[pos] == 0) f = 1; else a = cel[pos];
            }
            if (f == 0) cel[i] = a;
        }
    }

    public int Percolate(int pt, int cmax, int an)
    {
        if (cmax < 3) cmax = 3;

        int i, j, k;
        int opos = pt; // 시작 셀

        // 인접 플래그
        for (i = 0; i < cel_max; i++) next_f[i] = 0;

        int c = 0; // 셀 수
        while (true)
        {
            cel[opos] = an;

            c++;
            // 주변 셀
            for (i = 0; i < 6; i++)
            {
                int pos = join[opos].dir[i];
                if (pos < 0) continue;
                next_f[pos] = 1;
            }
            // 주변 셀에서 최소 번호를 다음 셀로 설정
            int min = 9999;
            for (i = 0; i < cel_max; i++)
            {
                if (next_f[i] == 0) continue; // 인접하지 않음
                if (cel[i] > 0) continue; // 이미 지역화
                if (num[i] > min) continue; // 최소 주문 번호가 아님
                min = num[i];
                opos = i;
            }

            if (min == 9999) break;
            if (c >= cmax) break; // 주어진 면적을 초과
        }

        // 인접 셀 추가
        for (i = 0; i < cel_max; i++)
        {
            if (next_f[i] == 0) continue;
            if (cel[i] > 0) continue; // 이미 지역화
            cel[i] = an;

            c++;
            // 또한, 인접 셀을 다음 영역의 후보로한다.
            for (k = 0; k < 6; k++)
            {
                int pos = join[i].dir[k];
                if (pos < 0) continue;
                rcel[pos] = 1;
            }
        }

        return c;
    }
}