using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;

/// <summary>
/// [Balance] 필드를 찾아 읽고 쓰는 공용 로직.
/// 에디터의 내보내기/불러오기와 (나중에) 시트 연동이 같은 규칙을 쓰도록 한 곳에 모았다.
/// </summary>
public static class BalanceSchema
{
    private const BindingFlags Flags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    /// <summary>대상 오브젝트의 [Balance] 필드 목록. 상속받은 필드도 포함.</summary>
    public static List<FieldInfo> GetFields(Type type)
    {
        var result = new List<FieldInfo>();
        for (Type t = type; t != null && t != typeof(UnityEngine.Object); t = t.BaseType)
        {
            foreach (var f in t.GetFields(Flags | BindingFlags.DeclaredOnly))
            {
                if (f.IsDefined(typeof(BalanceAttribute), true))
                    result.Add(f);
            }
        }
        result.Reverse(); // 부모 필드가 앞에 오도록
        return result;
    }

    /// <summary>표에 쓸 컬럼 이름. [Balance("라벨")]이 있으면 그것, 없으면 필드명.</summary>
    public static string GetLabel(FieldInfo field)
    {
        var attr = field.GetCustomAttribute<BalanceAttribute>(true);
        return string.IsNullOrEmpty(attr?.Label) ? field.Name : attr.Label;
    }

    /// <summary>
    /// CSV 헤더 문자열. "필드명(라벨)" 형식.
    /// 앞의 영문 필드명으로 매칭하므로, 스프레드시트가 한글을 깨뜨려도 컬럼을 찾을 수 있다.
    /// </summary>
    public static string GetHeader(FieldInfo field)
    {
        string label = GetLabel(field);
        return label == field.Name ? field.Name : $"{field.Name}({label})";
    }

    /// <summary>헤더 문자열에서 필드명 부분만 뽑는다. "Price(가격)" → "Price"</summary>
    public static string HeaderToFieldName(string header)
    {
        if (string.IsNullOrEmpty(header)) return "";
        header = header.Trim();
        int p = header.IndexOf('(');
        return (p > 0 ? header.Substring(0, p) : header).Trim();
    }

    /// <summary>헤더에 해당하는 필드를 찾는다. 필드명 우선, 없으면 라벨로도 시도.</summary>
    public static FieldInfo MatchField(List<FieldInfo> fields, string header)
    {
        string name = HeaderToFieldName(header);
        foreach (var f in fields) if (f.Name == name) return f;
        foreach (var f in fields) if (GetLabel(f) == header.Trim()) return f; // 구버전 CSV 호환
        return null;
    }

    /// <summary>필드 값을 CSV 셀 문자열로. 배열/리스트는 세미콜론으로 잇는다.</summary>
    public static string ToCell(object target, FieldInfo field)
    {
        object value = field.GetValue(target);
        if (value == null) return "";

        if (value is string s) return s;

        // 배열/리스트 (예: TaxConfig.schedule)
        if (value is IEnumerable list && !(value is string))
        {
            var parts = new List<string>();
            foreach (var item in list) parts.Add(Convert.ToString(item, CultureInfo.InvariantCulture));
            return string.Join(";", parts);
        }

        return Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    /// <summary>CSV 셀 문자열을 필드에 반영. 실패하면 false(값은 건드리지 않음).</summary>
    public static bool FromCell(object target, FieldInfo field, string cell)
    {
        try
        {
            Type t = field.FieldType;

            if (t == typeof(string)) { field.SetValue(target, cell); return true; }
            if (t == typeof(int)) { field.SetValue(target, int.Parse(cell, CultureInfo.InvariantCulture)); return true; }
            if (t == typeof(float)) { field.SetValue(target, float.Parse(cell, CultureInfo.InvariantCulture)); return true; }
            if (t == typeof(bool)) { field.SetValue(target, ParseBool(cell)); return true; }
            if (t.IsEnum) { field.SetValue(target, Enum.Parse(t, cell, true)); return true; }

            // 배열 (예: int[])
            if (t.IsArray)
            {
                Type elem = t.GetElementType();
                string[] parts = SplitList(cell);
                var arr = Array.CreateInstance(elem, parts.Length);
                for (int i = 0; i < parts.Length; i++)
                    arr.SetValue(ParseScalar(elem, parts[i]), i);
                field.SetValue(target, arr);
                return true;
            }

            // List<T>
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type elem = t.GetGenericArguments()[0];
                string[] parts = SplitList(cell);
                var listObj = (IList)Activator.CreateInstance(t);
                foreach (var p in parts) listObj.Add(ParseScalar(elem, p));
                field.SetValue(target, listObj);
                return true;
            }

            Debug.LogWarning($"[Balance] 지원하지 않는 타입: {t.Name} ({field.Name})");
            return false;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Balance] '{field.Name}' 값 '{cell}' 변환 실패: {e.Message}");
            return false;
        }
    }

    /// <summary>이 타입이 밸런스 표 대상인가([Balance] 또는 [BalanceRows] 필드가 있는가).</summary>
    public static bool HasBalanceFields(Type type)
        => GetFields(type).Count > 0 || GetRowsField(type) != null;

    // ── [BalanceGroup] : 카테고리 폴더 / 파일 통합 ────────────────────────────

    /// <summary>
    /// 이 타입의 CSV 경로. [BalanceGroup]이 있으면 "카테고리/파일명",
    /// 없으면 타입 이름만. 상속받은 그룹도 인정하므로 서브클래스가 한 파일로 묶인다.
    /// </summary>
    public static string GetCsvPath(Type type)
    {
        var attr = type.GetCustomAttribute<BalanceGroupAttribute>(true);
        if (attr == null) return type.Name;

        string file = string.IsNullOrEmpty(attr.FileName) ? type.Name : attr.FileName;
        return string.IsNullOrEmpty(attr.Category) ? file : $"{attr.Category}/{file}";
    }

    /// <summary>
    /// 표를 묶는 기준이 되는 타입. [BalanceGroup]이 선언된 상위 타입을 찾아 올라간다.
    /// (예: PeaItemData → ItemData). 없으면 자기 자신.
    /// </summary>
    public static Type GetGroupType(Type type)
    {
        Type found = type;
        for (Type t = type; t != null && t != typeof(UnityEngine.Object); t = t.BaseType)
        {
            if (t.GetCustomAttribute<BalanceGroupAttribute>(false) != null) found = t;
        }
        return found;
    }

    // ── [BalanceRows] : 리스트를 행으로 펼치는 경우 ───────────────────────────

    public class RowsInfo
    {
        public FieldInfo Field;
        public string KeyField;
    }

    /// <summary>이 타입의 [BalanceRows] 필드 정보. 없으면 null.</summary>
    public static RowsInfo GetRowsField(Type type)
    {
        for (Type t = type; t != null && t != typeof(UnityEngine.Object); t = t.BaseType)
        {
            foreach (var f in t.GetFields(Flags | BindingFlags.DeclaredOnly))
            {
                var attr = f.GetCustomAttribute<BalanceRowsAttribute>(true);
                if (attr != null) return new RowsInfo { Field = f, KeyField = attr.KeyField };
            }
        }
        return null;
    }

    /// <summary>리스트/배열 필드의 요소 타입.</summary>
    public static Type GetElementType(FieldInfo field)
    {
        Type t = field.FieldType;
        if (t.IsArray) return t.GetElementType();
        if (t.IsGenericType) return t.GetGenericArguments()[0];
        return t;
    }

    /// <summary>리스트 필드의 항목들.</summary>
    public static IEnumerable<object> GetRowItems(object target, FieldInfo field)
    {
        if (field.GetValue(target) is IEnumerable list)
            foreach (var item in list)
                if (item != null) yield return item;
    }

    /// <summary>
    /// 항목의 키 값(문자열). 행을 식별하는 데 쓴다.
    /// 키 필드가 없으면 목록에서의 순번(1부터)을 키로 쓴다.
    /// </summary>
    public static string GetKeyValue(object item, string keyFieldName, int index = -1)
    {
        var f = item.GetType().GetField(keyFieldName, Flags);
        if (f == null) return index >= 0 ? (index + 1).ToString() : "";
        return Convert.ToString(f.GetValue(item), CultureInfo.InvariantCulture);
    }

    /// <summary>키 값이 일치하는 항목을 찾는다. (키 필드가 없으면 순번으로 찾음)</summary>
    public static object FindRowItem(object target, FieldInfo field, string keyFieldName, string key)
    {
        int i = 0;
        foreach (var item in GetRowItems(target, field))
        {
            if (GetKeyValue(item, keyFieldName, i) == key) return item;
            i++;
        }
        return null;
    }

    private static string[] SplitList(string cell)
        => string.IsNullOrWhiteSpace(cell)
            ? Array.Empty<string>()
            : cell.Split(';', StringSplitOptions.RemoveEmptyEntries);

    private static object ParseScalar(Type t, string raw)
    {
        raw = raw.Trim();
        if (t == typeof(string)) return raw;
        if (t == typeof(int)) return int.Parse(raw, CultureInfo.InvariantCulture);
        if (t == typeof(float)) return float.Parse(raw, CultureInfo.InvariantCulture);
        if (t == typeof(bool)) return ParseBool(raw);
        if (t.IsEnum) return Enum.Parse(t, raw, true);
        throw new NotSupportedException($"지원하지 않는 요소 타입: {t.Name}");
    }

    private static bool ParseBool(string raw)
    {
        raw = raw.Trim().ToLowerInvariant();
        return raw == "true" || raw == "1" || raw == "y" || raw == "yes" || raw == "o";
    }
}
