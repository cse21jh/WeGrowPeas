using System.Collections.Generic;
using System.Text;

/// <summary>
/// 밸런스 표용 CSV 직렬화/파싱 (RFC 4180).
/// 설명 문구에 쉼표·따옴표·줄바꿈이 들어가도 안전하게 처리한다.
/// </summary>
public static class BalanceCsv
{
    /// <summary>한 줄을 CSV 형식으로 만든다.</summary>
    public static string ToLine(IEnumerable<string> fields)
    {
        var sb = new StringBuilder();
        bool first = true;
        foreach (var f in fields)
        {
            if (!first) sb.Append(',');
            sb.Append(Escape(f));
            first = false;
        }
        return sb.ToString();
    }

    /// <summary>쉼표·따옴표·줄바꿈이 있으면 따옴표로 감싸고 내부 따옴표는 두 번 쓴다.</summary>
    public static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";

        bool needQuote = value.IndexOf(',') >= 0
                      || value.IndexOf('"') >= 0
                      || value.IndexOf('\n') >= 0
                      || value.IndexOf('\r') >= 0;

        if (!needQuote) return value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    /// <summary>
    /// CSV 전체를 행 목록으로 파싱한다. 따옴표 안의 줄바꿈도 한 셀로 취급한다.
    /// </summary>
    public static List<List<string>> Parse(string text)
    {
        var rows = new List<List<string>>();
        if (string.IsNullOrEmpty(text)) return rows;

        // BOM 제거 (엑셀 호환을 위해 내보낼 때 붙이므로, 읽을 때 반드시 걷어낸다)
        if (text[0] == '﻿') text = text.Substring(1);

        var row = new List<string>();
        var cell = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    // 따옴표 두 개는 따옴표 문자 하나
                    if (i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                    else inQuotes = false;
                }
                else cell.Append(c);
                continue;
            }

            switch (c)
            {
                case '"':
                    inQuotes = true;
                    break;

                case ',':
                    row.Add(cell.ToString());
                    cell.Clear();
                    break;

                case '\r':
                    // \r\n은 \n에서 처리
                    break;

                case '\n':
                    row.Add(cell.ToString());
                    cell.Clear();
                    rows.Add(row);
                    row = new List<string>();
                    break;

                default:
                    cell.Append(c);
                    break;
            }
        }

        // 마지막 줄(개행 없이 끝나는 경우)
        if (cell.Length > 0 || row.Count > 0)
        {
            row.Add(cell.ToString());
            rows.Add(row);
        }

        return rows;
    }
}
