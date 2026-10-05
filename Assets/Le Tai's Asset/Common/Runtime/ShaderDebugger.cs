// Copyright (c) Le Loc Tai <leloctai.com> . All rights reserved. Do not redistribute.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace LeTai
{
public static class ShaderDebugger
{
    public static class Session<T>
    {
        public static T current;

        public readonly struct Scope : IDisposable
        {
            readonly T previous;

            public Scope(T value)
            {
                previous = current;
                current  = value;
            }

            public void Dispose() => current = previous;
        }

        public static Scope Enter(T value) => new(value);
    }

    public const int CAPACITY = 1 << 18;

    public const int TAG_BOX       = 1;
    public const int TAG_LINE      = 2;
    public const int TAG_ARROW     = 3;
    public const int TAG_CIRCLE    = 4;
    public const int TAG_QUADRATIC = 5;
    public const int TAG_VALUE     = 6;

    public struct Record
    {
        public int        tag;
        public Color      color;
        public Vector4    data0;
        public Vector4    data1;
        public Vector2Int pixel;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct RawRec
    {
        public uint    tag;
        public uint    color;
        public Vector4 data0;
        public Vector4 data1;
        public uint    pixelX;
        public uint    pixelY;
    }

    static class ShaderId
    {
        public static readonly int DBG_CAPACITY     = Shader.PropertyToID("_LeTai_DbgCapacity");
        public static readonly int DBG_PIXEL_RADIUS = Shader.PropertyToID("_LeTai_DbgPixelRadius");
        public static readonly int DBG_ENABLED      = Shader.PropertyToID("_LeTai_DbgEnabled");
        public static readonly int DBG_TRACE_MODE   = Shader.PropertyToID("_LeTai_DbgTraceMode");
    }

    static ComputeBuffer _records;
    static ComputeBuffer _counter;
    static int           _capacity;

    static          RawRec[] _readback  = new RawRec[CAPACITY];
    static readonly uint[]   COUNT_DATA = new uint[1];

    static void EnsureBuffers(int capacity)
    {
        if (_records != null && _capacity == capacity)
            return;

        _records?.Release();
        _counter?.Release();

        _capacity = capacity;
        _records  = new ComputeBuffer(capacity, Marshal.SizeOf<RawRec>(), ComputeBufferType.Structured);
        _counter  = new ComputeBuffer(1,        sizeof(uint),             ComputeBufferType.Structured);

        if (_readback.Length < capacity)
            _readback = new RawRec[capacity];

#if UNITY_EDITOR
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= Release;
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += Release;
        UnityEditor.EditorApplication.quitting                -= Release;
        UnityEditor.EditorApplication.quitting                += Release;
#endif
    }

    static void Release()
    {
#if UNITY_EDITOR
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= Release;
        UnityEditor.EditorApplication.quitting                -= Release;
#endif
        _records?.Release();
        _counter?.Release();
        _records  = null;
        _counter  = null;
        _capacity = 0;
    }

    public static void Begin(bool traceMode, int pixelRadius, int capacity = CAPACITY)
    {
        EnsureBuffers(capacity);

        Shader.SetGlobalInt(ShaderId.DBG_ENABLED,      1);
        Shader.SetGlobalInt(ShaderId.DBG_TRACE_MODE,   traceMode ? 1 : 0);
        Shader.SetGlobalInt(ShaderId.DBG_CAPACITY,     capacity);
        Shader.SetGlobalInt(ShaderId.DBG_PIXEL_RADIUS, pixelRadius);

        COUNT_DATA[0] = 0;
        _counter.SetData(COUNT_DATA);

        Bind();
    }

    public static void Disable()
    {
        Shader.SetGlobalInt(ShaderId.DBG_ENABLED, 0);
    }

    public static void SetPixelRadius(int pixelRadius)
    {
        Shader.SetGlobalInt(ShaderId.DBG_PIXEL_RADIUS, pixelRadius);
    }

    public static void Bind()
    {
        if (_records == null)
            return;

        Graphics.SetRandomWriteTarget(1, _records, false);
        Graphics.SetRandomWriteTarget(2, _counter, false);
    }

    public static int SnapshotCount()
    {
        if (_counter == null)
            return 0;

        _counter.GetData(COUNT_DATA);
        return Mathf.Min((int)COUNT_DATA[0], _capacity);
    }

    public static int End(List<Record> records)
    {
        records.Clear();

        if (_records == null)
            return 0;

        Graphics.ClearRandomWriteTargets();

        _counter.GetData(COUNT_DATA);
        int count = (int)COUNT_DATA[0];
        int n     = Mathf.Min(count, _capacity);

        if (n <= 0)
            return count;

        _records.GetData(_readback, 0, 0, n);

        for (int i = 0; i < n; i++)
        {
            RawRec     raw   = _readback[i];
            Vector2Int pixel = new Vector2Int((int)raw.pixelX, (int)raw.pixelY);

            records.Add(new Record {
                tag   = (int)raw.tag,
                color = DecodeColor(raw.color),
                data0 = raw.data0,
                data1 = raw.data1,
                pixel = pixel,
            });
        }

        return count;
    }

    static void Line(Vector3 a, Vector3 b, Color color)
    {
#if UNITY_EDITOR
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.DrawLine(a, b, UnityEditor.Handles.lineThickness);
#else
        Gizmos.color = color;
        Gizmos.DrawLine(a, b);
#endif
    }

    public static void Draw(
        List<Record>           records,
        int                    start,
        int                    count,
        Func<Vector2, Vector3> toScene
    )
    {
        var end = Mathf.Min(start + count, records.Count);
        for (var index = start; index < end; index++)
        {
            var rec = records[index];
            if (rec.tag == TAG_VALUE)
                continue;

            Color color = rec.color;

            switch (rec.tag)
            {
            case TAG_BOX:
                DrawBox(new Vector2(rec.data0.x, rec.data0.y), new Vector2(rec.data0.z, rec.data0.w), toScene, color);
                break;
            case TAG_LINE:
                Line(toScene(new Vector2(rec.data0.x, rec.data0.y)), toScene(new Vector2(rec.data0.z, rec.data0.w)), color);
                break;
            case TAG_ARROW:
                DrawArrow(new Vector2(rec.data0.x, rec.data0.y), new Vector2(rec.data0.z, rec.data0.w), toScene, color);
                break;
            case TAG_CIRCLE:
                DrawCircle(new Vector2(rec.data0.x, rec.data0.y), rec.data0.z, toScene, color);
                break;
            case TAG_QUADRATIC:
                DrawQuadratic(
                    new Vector2(rec.data0.x, rec.data0.y),
                    new Vector2(rec.data0.z, rec.data0.w),
                    new Vector2(rec.data1.x, rec.data1.y),
                    rec.data1.z, rec.data1.w,
                    toScene,     color);
                break;
            }
        }
    }

    static Color DecodeColor(uint packed)
    {
        float r = (packed & 0xFFu) / 255f;
        float g = ((packed >> 8) & 0xFFu) / 255f;
        float b = ((packed >> 16) & 0xFFu) / 255f;
        float a = ((packed >> 24) & 0xFFu) / 255f;
        return new Color(r, g, b, a);
    }

    static void DrawBox(Vector2 min, Vector2 max, Func<Vector2, Vector3> toScene, Color color)
    {
        Vector3 p00 = toScene(min);
        Vector3 p10 = toScene(new Vector2(max.x, min.y));
        Vector3 p11 = toScene(max);
        Vector3 p01 = toScene(new Vector2(min.x, max.y));
        Line(p00, p10, color);
        Line(p10, p11, color);
        Line(p11, p01, color);
        Line(p01, p00, color);
    }

    static void DrawArrow(Vector2 a, Vector2 b, Func<Vector2, Vector3> toScene, Color color)
    {
        Vector3 pa = toScene(a);
        Vector3 pb = toScene(b);
        Line(pa, pb, color);

        Vector3 shaft = pb - pa;
        float   len   = shaft.magnitude;
        if (len < 1e-6f)
            return;

        Vector3 dir       = shaft / len;
        float   headLen   = len * .15f;
        Vector3 backLeft  = Quaternion.Euler(0, 0, 25f) * -dir * headLen;
        Vector3 backRight = Quaternion.Euler(0, 0, -25f) * -dir * headLen;

        Line(pb, pb + backLeft,  color);
        Line(pb, pb + backRight, color);
    }

    static void DrawCircle(Vector2 center, float radius, Func<Vector2, Vector3> toScene, Color color)
    {
        const int segments = 32;

        Vector3 prev = default;
        for (int k = 0; k <= segments; k++)
        {
            float   a  = k / (float)segments * Mathf.PI * 2f;
            Vector3 sp = toScene(center + radius * new Vector2(Mathf.Cos(a), Mathf.Sin(a)));
            if (k > 0)
                Line(prev, sp, color);
            prev = sp;
        }
    }

    static void DrawQuadratic(Vector2 a, Vector2 b, Vector2 c, float tMin, float tMax, Func<Vector2, Vector3> toScene, Color color)
    {
        const int steps = 16;

        Vector3 prev = default;
        for (int k = 0; k <= steps; k++)
        {
            float   t  = Mathf.Lerp(tMin, tMax, k / (float)steps);
            Vector3 sp = toScene(a + b * t + c * t * t);
            if (k > 0)
                Line(prev, sp, color);
            prev = sp;
        }
    }

    public static void WriteRaw(string path, string header, List<Record> records)
    {
        var sb = new StringBuilder();
        sb.Append(header);
        sb.Append("pixel.x\tpixel.y\ttag\tdata0.x\tdata0.y\tdata0.z\tdata0.w\tdata1.x\tdata1.y\tdata1.z\tdata1.w\n");

        foreach (var r in records)
            sb.Append(r.pixel.x).Append('\t').Append(r.pixel.y).Append('\t')
              .Append(r.tag).Append('\t')
              .Append(F(r.data0.x)).Append('\t').Append(F(r.data0.y)).Append('\t').Append(F(r.data0.z)).Append('\t').Append(F(r.data0.w)).Append('\t')
              .Append(F(r.data1.x)).Append('\t').Append(F(r.data1.y)).Append('\t').Append(F(r.data1.z)).Append('\t').Append(F(r.data1.w)).Append('\n');

        WriteFile(path, sb.ToString());
    }

    public static void WriteError(string path, string header, string error)
    {
        WriteFile(path, $"{header}# status=error {error}\n");
    }

    static string F(float v) => v.ToString("0.######", CultureInfo.InvariantCulture);

    static void WriteFile(string path, string content)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(path, content);
    }
}
}
