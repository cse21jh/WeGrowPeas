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

    public static void SetDataFromLoad(SaveData saveData)
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

    //private PlayerRecordForGraph data;

    private static readonly string[] WaveNames =
    {
        "자연사", "해충", "바람", "홍수", "폭우", "한파"
    };

    private Color[] colors = {
        ColorUtility.TryParseHtmlString("#fccf4e", out var c0) ? c0 : Color.white,
        ColorUtility.TryParseHtmlString("#b6b53a", out var c1) ? c1 : Color.white,
        ColorUtility.TryParseHtmlString("#d6e6eb", out var c2) ? c2 : Color.white,
        ColorUtility.TryParseHtmlString("#469696", out var c3) ? c3 : Color.white,
        ColorUtility.TryParseHtmlString("#629ab7", out var c4) ? c4 : Color.white,
        ColorUtility.TryParseHtmlString("#746d80", out var c5) ? c5 : Color.white
    };

    void Start()
    {
        /*data = new PlayerRecordForGraph
        {
            survivedPlants = new List<int>(),
            earnedGolds = new List<int>(),
            waveEachDay = new List<int>()
        };

        for (int i = 0; i < 40; i++)
        {
            data.survivedPlants.Add(UnityEngine.Random.Range(5, 20));   // 예: 식물 생존 수
            data.earnedGolds.Add(UnityEngine.Random.Range(50, 1500));    // 예: 골드 수익
            data.waveEachDay.Add(UnityEngine.Random.Range(0, 6));      // 예: 웨이브 번호
        }*/

        BuildPlants();
        BuildGold();
        BuildWaves();
    }

    void BuildPlants()
    {
        string lineColorHex = "#618e32";
        int xLabelFontSize = 8;
        int yLabelFontSize = 8;
        
        var xAxis = plantChart.GetChartComponent<XAxis>();
        xAxis.type = Axis.AxisType.Value;
        xAxis.show = true;
        xAxis.axisLabel.show = true;
        xAxis.axisLabel.textStyle.fontSize = xLabelFontSize;
        xAxis.min = 0;
        xAxis.max = PlayerRecordForGraph.survivedPlants.Count;
        xAxis.interval = 5;

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

        for (int d = 0; d < PlayerRecordForGraph.survivedPlants.Count; d++)
            plantChart.AddData(0, d+1, PlayerRecordForGraph.survivedPlants[d]);
    }

    void BuildGold()
    {
        string lineColorHex = "#f1cf30";
        int xLabelFontSize = 8;
        int yLabelFontSize = 8;

        var xAxis = goldChart.GetChartComponent<XAxis>();
        xAxis.type = Axis.AxisType.Value;
        xAxis.show = true;
        xAxis.axisLabel.show = true;
        xAxis.axisLabel.textStyle.fontSize = xLabelFontSize;
        xAxis.min = 0;
        xAxis.max = PlayerRecordForGraph.earnedGolds.Count;
        xAxis.interval = 5;

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

        for (int d = 0; d < PlayerRecordForGraph.earnedGolds.Count; d++)
            goldChart.AddData(0, d+1, PlayerRecordForGraph.earnedGolds[d]);
    }

    void BuildWaves()
    {
        int xLabelFontSize = 8;

        var xAxis = waveChart.GetChartComponent<XAxis>();
        xAxis.type = Axis.AxisType.Value;
        xAxis.show = true;
        xAxis.axisLabel.show = true;
        xAxis.axisLabel.textStyle.fontSize = xLabelFontSize;
        xAxis.min = 0;
        xAxis.max = PlayerRecordForGraph.waveEachDay.Count;
        xAxis.interval = 5;

        var yAxis = waveChart.GetChartComponent<YAxis>();
        yAxis.type = Axis.AxisType.Value;
        //yAxis.boundaryGap = true;
        yAxis.minMaxType = Axis.AxisMinMaxType.Default;
        //yAxis.data.Clear();
        //yAxis.data.AddRange(new List<string> { "자연사", "해충", "바람", "홍수", "폭우", "한파" });

        waveChart.RemoveData();

        var tooltip = waveChart.GetChartComponent<Tooltip>();

        for (int w = 0; w <= 5; w++)
        {
            var s = waveChart.AddSerie<Line>();
            s.lineStyle.show = false;
            s.symbol.show = true;
            s.symbol.size = 3;
            s.symbol.type = SymbolType.Circle;
            s.itemStyle.color = colors[w];

            for (int day = 0; day < PlayerRecordForGraph.waveEachDay.Count; day++)
            {
                if (PlayerRecordForGraph.waveEachDay[day] == w)
                {
                    s.AddXYData(day + 1, w);
                }
            }
        }
    }
}
