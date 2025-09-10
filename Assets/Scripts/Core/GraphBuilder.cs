using System.Collections.Generic;
using System;
using UnityEngine;
using XCharts;
using XCharts.Runtime;

[Serializable] public class PlayerRecordForGraph
{
    public List<int> survivedPlants;
    public List<int> earnedGolds;
    public List<int> waveEachDay;
}

public class GraphBuilder : MonoBehaviour
{
    [SerializeField] private LineChart plantChart;
    [SerializeField] private LineChart goldChart;
    [SerializeField] private ScatterChart waveChart;
    [SerializeField] private Font customFont;

    private PlayerRecordForGraph data;
    
    void Start()
    {
        data = new PlayerRecordForGraph
        {
            survivedPlants = new List<int>(),
            earnedGolds = new List<int>(),
            waveEachDay = new List<int>()
        };

        for (int i = 0; i < 40; i++)
        {
            data.survivedPlants.Add(UnityEngine.Random.Range(5, 20));   // 예: 식물 생존 수
            data.earnedGolds.Add(UnityEngine.Random.Range(50, 1500));    // 예: 골드 수익
            data.waveEachDay.Add(UnityEngine.Random.Range(1, 10));      // 예: 웨이브 번호
        }

        BuildPlants();
        BuildGold();
    }

    void BuildPlants()
    {
        string lineColorHex = "#618e32";
        int xLabelFontSize = 10;
        int yLabelFontSize = 10;

        var chartGrid = plantChart.GetChartComponent<GridCoord>();
        
        var xAxis = plantChart.GetChartComponent<XAxis>();
        xAxis.type = Axis.AxisType.Value;
        xAxis.show = true;
        xAxis.axisLabel.show = true;
        xAxis.axisLabel.textStyle.fontSize = xLabelFontSize;
        xAxis.min = 0;
        xAxis.max = data.survivedPlants.Count;
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

        for (int d = 0; d < data.survivedPlants.Count; d++)
            plantChart.AddData(0, d+1, data.survivedPlants[d]);
    }

    void BuildGold()
    {
        string lineColorHex = "#f1cf30";
        int xLabelFontSize = 10;
        int yLabelFontSize = 10;

        var chartGrid = goldChart.GetChartComponent<GridCoord>();

        var xAxis = goldChart.GetChartComponent<XAxis>();
        xAxis.type = Axis.AxisType.Value;
        xAxis.show = true;
        xAxis.axisLabel.show = true;
        xAxis.axisLabel.textStyle.fontSize = xLabelFontSize;
        xAxis.min = 0;
        xAxis.max = data.earnedGolds.Count;
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

        for (int d = 0; d < data.earnedGolds.Count; d++)
            goldChart.AddData(0, d+1, data.earnedGolds[d]);
    }

    void BuildWaves()
    {
        /*chartWaves.title.show = false;
        chartWaves.grid.SetDefault();

        chartWaves.xAxis0.type = Axis.AxisType.Category;   // 0~47
        chartWaves.yAxis0.type = Axis.AxisType.Value;      // 웨이브 번호
        chartWaves.RemoveData();

        var s = chartWaves.AddSerie<Scatter>("Waves");
        s.symbol.size = 6;
        for (int d = 0; d < data.waveEachDay.Count; d++)
        {
            // 같은 날 여러 웨이브를 점 여러 개로 찍고 싶으면 AddSerie를 종별/난이도별로 나눠서 반복
            chartWaves.AddData(0, d, data.waveEachDay[d]);
        }*/
    }

}
