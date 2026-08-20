using System.Collections.Generic;
using System;
using UnityEngine;
using XCharts.Runtime;
using UnityEditor;

public static class PlayerRecordForGraph
{
    public static List<int> survivedPlants { get; private set; }
    public static List<int> earnedGolds { get; private set; }
    public static List<int> waveEachDay { get; private set; }

    static PlayerRecordForGraph()
    {
        survivedPlants = new List<int>();
        earnedGolds = new List<int>();
        waveEachDay = new List<int>();
    }

    public static void ClearAll()
    {
        survivedPlants.Clear();
        earnedGolds.Clear();
        waveEachDay.Clear();
    }

    public static void SetSP(int n) => survivedPlants.Add(n);
    public static void SetEG(int n) => earnedGolds.Add(n);
    public static void SetWED(int n) => waveEachDay.Add(n);

    /// <summary>일자별 기록을 저장 데이터에 담는다. <see cref="SetDataFromLoad"/>와 짝.</summary>
    public static void SaveTo(GraphSave save)
    {
        save.survivedPlants = survivedPlants;
        save.earnedGolds = earnedGolds;
        save.waveEachDay = waveEachDay;
    }

    public static void SetDataFromLoad(GraphSave saveData)
    {
        survivedPlants = saveData.survivedPlants;
        earnedGolds = saveData.earnedGolds;
        waveEachDay = saveData.waveEachDay;
    }
}

public class GraphBuilder : MonoBehaviour
{
    [SerializeField] private LineChart plantChart;
    [SerializeField] private LineChart goldChart;
    [SerializeField] private LineChart waveChart;

    private int bottom = 0;
    private int ptop = 40;
    private int gtop = 40;
    private int wtop = 40;
    private int countPerPage = 40;

    /// <summary>그릴 대상 기록. null이면 Start에서 이번 런(<see cref="PlayerRecordForGraph"/>)을 담는다.</summary>
    private GraphSave data;

    // 웨이브 이름·색은 WavePalette 한 곳에서 가져온다.
    // (예전에는 여기 하드코딩돼 있어서 정보 앱·위젯의 색과 서로 달랐다)

    void Start()
    {
        /*for (int i = 0; i < 112; i++)
        {
            data.survivedPlants.Add(UnityEngine.Random.Range(5, 20));
            data.earnedGolds.Add(UnityEngine.Random.Range(50, 1500));
            data.waveEachDay.Add(UnityEngine.Random.Range(0, 8));
        }*/

        if (data == null)
        {
            data = new GraphSave();
            PlayerRecordForGraph.SaveTo(data); // 이번 런 기록
        }

        ResetPage();
        LoadChart();
    }

    /// <summary>
    /// 과거 기록(회상)을 그린다. Start 전후 어느 쪽에서 불러도 된다.
    /// </summary>
    public void SetData(GraphSave save)
    {
        if (save == null) return;

        data = save;
        ResetPage();
        LoadChart();
    }

    /// <summary>첫 페이지로 되돌린다.</summary>
    private void ResetPage()
    {
        bottom = 0;
        ptop = Math.Min(countPerPage, data.survivedPlants.Count);
        gtop = Math.Min(countPerPage, data.earnedGolds.Count);
        wtop = Math.Min(countPerPage, data.waveEachDay.Count);
    }

    private void LoadChart()
    {
        BuildPlants();
        BuildGold();
        BuildWaves();
    }

    private void BuildPlants()
    {
        string lineColorHex = "#618e32";
        int xLabelFontSize = 8;
        int yLabelFontSize = 8;
        
        var xAxis = plantChart.GetChartComponent<XAxis>();
        xAxis.type = Axis.AxisType.Value;
        xAxis.show = true;
        xAxis.axisLabel.show = true;
        xAxis.axisLabel.textStyle.fontSize = xLabelFontSize;
        xAxis.minMaxType = Axis.AxisMinMaxType.Custom;
        xAxis.min = bottom + 1;
        xAxis.max = ptop;
        xAxis.interval = 5;
        xAxis.data.Clear();

        var yAxis = plantChart.GetChartComponent<YAxis>();
        yAxis.type = Axis.AxisType.Value;
        yAxis.axisLabel.show = true;
        yAxis.axisLabel.textStyle.fontSize = yLabelFontSize;
        yAxis.minMaxType = Axis.AxisMinMaxType.Default;

        var tooltip = plantChart.GetChartComponent<Tooltip>();
        if (tooltip != null)
        {
            tooltip.show = true;
            tooltip.offset = new Vector2(6, 6);
        }

        plantChart.RemoveData();

        var s = plantChart.AddSerie<Line>();
        s.symbol.show = false;
        s.lineStyle.width = 2;

        if (ColorUtility.TryParseHtmlString(lineColorHex, out var lineColor))
        {
            s.lineStyle.color = lineColor;
            s.itemStyle.color = lineColor;
        }

        for (int d = bottom; d < ptop; d++)
            plantChart.AddData(0, d+1, data.survivedPlants[d]);
    }

    private void BuildGold()
    {
        string lineColorHex = "#f1cf30";
        int xLabelFontSize = 8;
        int yLabelFontSize = 8;

        var xAxis = goldChart.GetChartComponent<XAxis>();
        xAxis.type = Axis.AxisType.Value;
        xAxis.show = true;
        xAxis.axisLabel.show = true;
        xAxis.axisLabel.textStyle.fontSize = xLabelFontSize;
        xAxis.minMaxType = Axis.AxisMinMaxType.Custom;
        xAxis.min = bottom + 1;
        xAxis.max = gtop;
        xAxis.interval = 5;
        xAxis.data.Clear();

        var yAxis = goldChart.GetChartComponent<YAxis>();
        yAxis.type = Axis.AxisType.Value;
        yAxis.axisLabel.show = true;
        yAxis.axisLabel.textStyle.fontSize = yLabelFontSize;
        yAxis.minMaxType = Axis.AxisMinMaxType.Default;

        var tooltip = goldChart.GetChartComponent<Tooltip>();
        if (tooltip != null)
        {
            tooltip.show = true;
            tooltip.offset = new Vector2(6, 6);
        }

        goldChart.RemoveData();

        var s = goldChart.AddSerie<Line>();
        s.symbol.show = false;
        s.lineStyle.width = 2;

        if (ColorUtility.TryParseHtmlString(lineColorHex, out var lineColor))
        {
            s.lineStyle.color = lineColor;
            s.itemStyle.color = lineColor;
        }

        for (int d = bottom; d < gtop; d++)
            goldChart.AddData(0, d+1, data.earnedGolds[d]);
    }

    private void BuildWaves()
    {
        int xLabelFontSize = 8;

        var xAxis = waveChart.GetChartComponent<XAxis>();
        xAxis.type = Axis.AxisType.Value;
        xAxis.show = true;
        xAxis.axisLabel.show = true;
        xAxis.axisLabel.textStyle.fontSize = xLabelFontSize;
        xAxis.minMaxType = Axis.AxisMinMaxType.Custom;
        xAxis.min = bottom + 1;
        xAxis.max = wtop;
        xAxis.interval = 5;
        xAxis.data.Clear();

        var yAxis = waveChart.GetChartComponent<YAxis>();
        yAxis.type = Axis.AxisType.Value;
        //yAxis.boundaryGap = true;
        yAxis.minMaxType = Axis.AxisMinMaxType.Default;
        //yAxis.data.Clear();
        //yAxis.data.AddRange(new List<string> { "자연사", "해충", "바람", "홍수", "폭우", "한파" });

        waveChart.RemoveData();

        var tooltip = waveChart.GetChartComponent<Tooltip>();

        /*for (int w = 0; w <= 5; w++)
        {
            var s = waveChart.AddSerie<Line>();
            s.lineStyle.show = false;
            s.symbol.show = true;
            s.symbol.size = 3;
            s.symbol.type = SymbolType.Circle;
            s.itemStyle.color = WavePalette.GetColor((WaveType)w);

            for (int day = bottom; day < top; day++)
            {
                if (data.waveEachDay[day] == w)
                {
                    s.AddXYData(day + 1, w);
                }
            }
        }*/

        for (int day = bottom; day < wtop; day++)
        {
            int w = data.waveEachDay[day];

            if (data.waveEachDay[day] == 8) continue;

            var s = waveChart.AddSerie<Line>();
            s.lineStyle.show = false;
            s.symbol.show = true;
            s.symbol.size = 3;
            s.symbol.type = SymbolType.Circle;
            s.itemStyle.color = WavePalette.GetColor((WaveType)w);

            s.AddXYData(day + 1, w);
        }
    }

    public void MovePrev()
    {
        if (bottom <= 0) return;

        bottom = Math.Max(0, bottom - countPerPage);
        ptop = bottom + countPerPage;
        gtop = bottom + countPerPage;
        wtop = bottom + countPerPage;

        LoadChart();
    }

    public void MoveNext()
    {
        if (ptop >= data.survivedPlants.Count) return;

        bottom = bottom + countPerPage;
        ptop = Math.Min(ptop + countPerPage, data.survivedPlants.Count);
        gtop = Math.Min(gtop + countPerPage, data.earnedGolds.Count);
        wtop = Math.Min(wtop + countPerPage, data.waveEachDay.Count);

        LoadChart();
    }
}
