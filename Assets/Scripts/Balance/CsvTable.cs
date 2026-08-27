using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Balance
{
    /// <summary>
    /// 밸런스 CSV 한 장을 헤더 기반으로 읽고 쓰는 최소 파서.
    ///
    /// 규칙
    /// - 첫 줄이 헤더. 열 순서는 상관없고 <b>이름으로</b> 찾는다.
    /// - <c>#</c> 로 시작하는 줄은 주석.
    /// - 값에 쉼표가 필요하면 큰따옴표로 감싼다 (<c>""</c> = 따옴표 1개).
    /// - 배열 값은 <c>|</c> 로 구분한다. 예: <c>10|15|22|30|40</c>
    /// - 빈 칸은 기본값 유지.
    /// </summary>
    public class CsvTable
    {
        public const char ArraySeparator = '|';

        private readonly List<string>                 _headers = new();
        private readonly List<Dictionary<string, string>> _rows = new();

        public IReadOnlyList<Dictionary<string, string>> Rows => _rows;
        public int RowCount => _rows.Count;

        // ── 파싱 ────────────────────────────────────────────────────

        public static CsvTable Parse(string text)
        {
            var table = new CsvTable();
            if (string.IsNullOrWhiteSpace(text)) return table;

            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            bool headerRead = false;

            foreach (var raw in lines)
            {
                if (raw.Length == 0)          continue;
                if (raw.TrimStart().StartsWith("#")) continue;

                var fields = SplitLine(raw);
                if (fields.Count == 0) continue;

                if (!headerRead)
                {
                    foreach (var h in fields) table._headers.Add(h.Trim());
                    headerRead = true;
                    continue;
                }

                var row = new Dictionary<string, string>();
                for (int i = 0; i < table._headers.Count; i++)
                    row[table._headers[i]] = i < fields.Count ? fields[i].Trim() : "";
                table._rows.Add(row);
            }
            return table;
        }

        private static List<string> SplitLine(string line)
        {
            var result = new List<string>();
            var sb     = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                        else inQuotes = false;
                    }
                    else sb.Append(c);
                }
                else if (c == '"')      inQuotes = true;
                else if (c == ',')      { result.Add(sb.ToString()); sb.Clear(); }
                else                    sb.Append(c);
            }
            result.Add(sb.ToString());
            return result;
        }

        // ── 쓰기 ────────────────────────────────────────────────────

        public static string Escape(string value)
        {
            value ??= "";
            bool needsQuote = value.Contains(",") || value.Contains("\"") || value.Contains("\n");
            return needsQuote ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
        }

        public static string JoinArray(float[] values)
        {
            if (values == null || values.Length == 0) return "";
            var parts = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
                parts[i] = values[i].ToString("0.####", CultureInfo.InvariantCulture);
            return string.Join(ArraySeparator.ToString(), parts);
        }

        public static string JoinArray(int[] values)
        {
            if (values == null || values.Length == 0) return "";
            var parts = new string[values.Length];
            for (int i = 0; i < values.Length; i++) parts[i] = values[i].ToString(CultureInfo.InvariantCulture);
            return string.Join(ArraySeparator.ToString(), parts);
        }

        public static string ToHex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);
    }

    /// <summary>CSV 한 행에서 타입별로 값을 꺼내는 헬퍼. 빈 칸이면 <c>fallback</c> 을 그대로 돌려준다.</summary>
    public static class CsvRow
    {
        public static string Str(Dictionary<string, string> row, string key, string fallback = "")
            => row.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v) ? v : fallback;

        public static float Float(Dictionary<string, string> row, string key, float fallback = 0f)
            => row.TryGetValue(key, out var v)
               && float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var f)
               ? f : fallback;

        public static int Int(Dictionary<string, string> row, string key, int fallback = 0)
            => row.TryGetValue(key, out var v)
               && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)
               ? i : fallback;

        public static bool Bool(Dictionary<string, string> row, string key, bool fallback = false)
        {
            var v = Str(row, key);
            if (string.IsNullOrEmpty(v)) return fallback;
            v = v.ToLowerInvariant();
            return v == "1" || v == "true" || v == "y" || v == "yes";
        }

        public static float[] Floats(Dictionary<string, string> row, string key, float[] fallback = null)
        {
            var v = Str(row, key);
            if (string.IsNullOrEmpty(v)) return fallback;

            var parts  = v.Split(CsvTable.ArraySeparator);
            var result = new float[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                float.TryParse(parts[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out result[i]);
            return result;
        }

        public static int[] Ints(Dictionary<string, string> row, string key, int[] fallback = null)
        {
            var v = Str(row, key);
            if (string.IsNullOrEmpty(v)) return fallback;

            var parts  = v.Split(CsvTable.ArraySeparator);
            var result = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                int.TryParse(parts[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result[i]);
            return result;
        }

        public static Color Color(Dictionary<string, string> row, string key, Color fallback)
        {
            var v = Str(row, key);
            if (string.IsNullOrEmpty(v)) return fallback;
            if (!v.StartsWith("#")) v = "#" + v;
            return ColorUtility.TryParseHtmlString(v, out var c) ? c : fallback;
        }
    }
}
