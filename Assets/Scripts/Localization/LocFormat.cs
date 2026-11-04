using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// 번역문의 자리표시자를 값으로 채운다.
///   {day}        → 값을 그대로 넣는다.
///   {wave:이/가} → 값 뒤에 받침에 맞는 조사를 붙인다. (예: "폭우가", "홍수가", "해충이")
/// 조사 표기는 "받침 있을 때/없을 때" 순서이며 ko 열에서만 쓴다. 다른 언어 열은 {wave}로 쓴다.
/// 인자에 없는 자리표시자는 지우지 않고 그대로 남겨 누락을 눈으로 찾을 수 있게 한다.
/// </summary>
public static class LocFormat
{
    public static string Format(string template, (string name, object value)[] args)
    {
        if (string.IsNullOrEmpty(template) || args == null || args.Length == 0) return template;

        var sb = new StringBuilder(template.Length + 16);
        int i = 0;
        while (i < template.Length)
        {
            char c = template[i];
            int close = c == '{' ? template.IndexOf('}', i + 1) : -1;
            if (close < 0)
            {
                sb.Append(c);
                i++;
                continue;
            }

            string token = template.Substring(i + 1, close - i - 1);
            SplitToken(token, out string name, out string spec);

            if (TryGetArg(args, name, out string value))
            {
                sb.Append(value);
                if (spec != null) sb.Append(ChooseJosa(value, spec));
                i = close + 1;
            }
            else
            {
                sb.Append(c);
                i++;
            }
        }
        return sb.ToString();
    }

    /// <summary>문자열에 쓰인 자리표시자 이름. 언어별 열이 같은 값을 쓰는지 검사할 때 쓴다.</summary>
    public static HashSet<string> GetPlaceholderNames(string template)
    {
        var names = new HashSet<string>();
        if (string.IsNullOrEmpty(template)) return names;

        int i = template.IndexOf('{');
        while (i >= 0)
        {
            int close = template.IndexOf('}', i + 1);
            if (close < 0) break;

            SplitToken(template.Substring(i + 1, close - i - 1), out string name, out _);
            if (IsPlaceholderName(name)) names.Add(name);
            i = template.IndexOf('{', i + 1);
        }
        return names;
    }

    /// <summary>"이/가"처럼 "받침 있을 때/없을 때"로 적은 조사 중 word에 맞는 쪽을 고른다.</summary>
    public static string ChooseJosa(string word, string spec)
    {
        int slash = spec.IndexOf('/');
        if (slash < 0) return "";

        string withFinal = spec.Substring(0, slash);
        string withoutFinal = spec.Substring(slash + 1);

        GetFinalConsonant(word, out bool hasFinal, out bool isRieul);

        // "으로/로"만 예외: ㄹ 받침 뒤에는 "로"가 온다. (물로, 칼로)
        if (withFinal == "으로" && isRieul) return withoutFinal;
        return hasFinal ? withFinal : withoutFinal;
    }

    private static void GetFinalConsonant(string word, out bool hasFinal, out bool isRieul)
    {
        hasFinal = false;
        isRieul = false;
        if (string.IsNullOrEmpty(word)) return;

        char last = word[word.Length - 1];
        if (last >= '가' && last <= '힣')
        {
            int final = (last - '가') % 28;
            hasFinal = final != 0;
            isRieul = final == 8;
        }
        else if (last >= '0' && last <= '9')
        {
            // 한글 독음 기준: 영 일 이 삼 사 오 육 칠 팔 구
            hasFinal = "013678".IndexOf(last) >= 0;
            isRieul = "178".IndexOf(last) >= 0;
        }
    }

    private static void SplitToken(string token, out string name, out string spec)
    {
        int colon = token.IndexOf(':');
        name = colon < 0 ? token : token.Substring(0, colon);
        spec = colon < 0 ? null : token.Substring(colon + 1);
    }

    private static bool TryGetArg((string name, object value)[] args, string name, out string value)
    {
        foreach (var arg in args)
        {
            if (arg.name != name) continue;
            value = Convert.ToString(arg.value, CultureInfo.InvariantCulture);
            return true;
        }
        value = null;
        return false;
    }

    private static bool IsPlaceholderName(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        foreach (char c in name)
        {
            bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_';
            if (!ok) return false;
        }
        return true;
    }
}
