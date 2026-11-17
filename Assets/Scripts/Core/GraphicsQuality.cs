using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public enum GraphicsQualityLevel
{
    Low = 0,
    Medium = 1,
    High = 2,
}

/// <summary>
/// 설정창의 그래픽 품질. 저사양 노트북에서 GPU 부담이 큰 항목만 줄인다.
/// - 높음: 지금 화면 그대로.
/// - 보통: 렌더 해상도 85%, 외곽선 RT 절반 해상도, 잔디 메시 분할 감소.
/// - 낮음: 보통 + 렌더 해상도 75%, HDR 끔, Bloom 끔, 잔디 밀도 60%.
///
/// Unity 품질 레벨(QualitySettings)은 쓰지 않는다. 레벨마다 연결된 렌더 파이프라인 에셋이 비어 있고
/// Low 계열은 수직 동기화가 꺼져 있어, 바꾸면 오히려 프레임을 끝없이 그리게 된다.
/// </summary>
public static class GraphicsQuality
{
    public const int Auto = -1;

    public static event Action Changed;

    public static GraphicsQualityLevel Current { get; private set; } = GraphicsQualityLevel.High;

    /// <summary>저장된 값이 없을 때 기기 사양으로 고른 값.</summary>
    public static GraphicsQualityLevel Recommended => Detect();

    // 실행 중에 바꾼 렌더 파이프라인 에셋 값. 에디터에서는 Play 종료 시 되돌려 에셋이 바뀌지 않게 한다.
    private static UniversalRenderPipelineAsset s_pipeline;
    private static float s_originalRenderScale = 1f;
    private static bool s_originalHdr = true;

    // 낮음에서 끈 Bloom. 다시 올릴 때 이것만 켠다.
    private static readonly List<Bloom> s_disabledBlooms = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        // 도메인 리로드를 끈 에디터에서는 이전 Play의 정적 값이 남아 있으므로 여기서 다시 맞춘다.
        s_pipeline = null;
        s_disabledBlooms.Clear();

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Application.quitting -= RestorePipeline;
        Application.quitting += RestorePipeline;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
#endif

        Current = Resolve(DeviceSettings.Data.graphicsQuality);
        ApplyStatics();
        ApplyPipeline();
    }

    /// <summary>설정창에서 고른 품질을 저장하고 바로 적용한다.</summary>
    public static void Set(GraphicsQualityLevel level)
    {
        DeviceSettings.Data.graphicsQuality = (int)level;
        DeviceSettings.Save();
        if (level == Current) return;

        Current = level;
        ApplyStatics();
        ApplyPipeline();
        ApplyScene();
        GrassChunkController.RebuildAll();
        RTResizeController.ResizeAll();
        Changed?.Invoke();
    }

    private static GraphicsQualityLevel Resolve(int saved)
    {
        if (Enum.IsDefined(typeof(GraphicsQualityLevel), saved)) return (GraphicsQualityLevel)saved;
        return Detect();
    }

    /// <summary>
    /// 기기 사양으로 처음 품질을 고른다. 정확한 벤치마크가 아니라 내장 그래픽을 낮음으로 보내는 정도의 기준이다.
    /// 내장 그래픽은 공유 메모리를 VRAM으로 보고하는 경우가 많아 이름도 함께 본다.
    /// </summary>
    private static GraphicsQualityLevel Detect()
    {
        string gpu = SystemInfo.graphicsDeviceName ?? string.Empty;
        int vramMb = SystemInfo.graphicsMemorySize;
        int ramMb = SystemInfo.systemMemorySize;

        bool integrated =
            gpu.IndexOf("Intel", StringComparison.OrdinalIgnoreCase) >= 0 ||
            gpu.IndexOf("Radeon(TM) Graphics", StringComparison.OrdinalIgnoreCase) >= 0 ||
            gpu.IndexOf("Radeon Graphics", StringComparison.OrdinalIgnoreCase) >= 0 ||
            (gpu.IndexOf("Vega", StringComparison.OrdinalIgnoreCase) >= 0 && gpu.IndexOf("RX", StringComparison.Ordinal) < 0);

        if (integrated || vramMb < 2048) return GraphicsQualityLevel.Low;
        if (vramMb < 4096 || ramMb < 8192) return GraphicsQualityLevel.Medium;
        return GraphicsQualityLevel.High;
    }

    private static void ApplyStatics()
    {
        switch (Current)
        {
            case GraphicsQualityLevel.Low:
                GrassChunkController.QualityDensityScale = 0.6f;
                GrassChunkController.QualityMaxXSegments = 1;
                GrassChunkController.QualityMaxYSegments = 3;
                RTResizeController.OutlineResolutionScale = 0.5f;
                break;
            case GraphicsQualityLevel.Medium:
                GrassChunkController.QualityDensityScale = 1f;
                GrassChunkController.QualityMaxXSegments = 2;
                GrassChunkController.QualityMaxYSegments = 4;
                RTResizeController.OutlineResolutionScale = 0.5f;
                break;
            default:
                GrassChunkController.QualityDensityScale = 1f;
                GrassChunkController.QualityMaxXSegments = int.MaxValue;
                GrassChunkController.QualityMaxYSegments = int.MaxValue;
                RTResizeController.OutlineResolutionScale = 1f;
                break;
        }
    }

    private static void ApplyPipeline()
    {
        if (s_pipeline == null)
        {
            s_pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (s_pipeline == null) return;
            s_originalRenderScale = s_pipeline.renderScale;
            s_originalHdr = s_pipeline.supportsHDR;
        }

        bool low = Current == GraphicsQualityLevel.Low;
        float scale = Current switch
        {
            GraphicsQualityLevel.Low => 0.75f,
            GraphicsQualityLevel.Medium => 0.85f,
            _ => 1f,
        };
        s_pipeline.renderScale = Mathf.Min(scale, s_originalRenderScale);
        s_pipeline.supportsHDR = !low && s_originalHdr;
    }

    private static void RestorePipeline()
    {
        if (s_pipeline == null) return;
        s_pipeline.renderScale = s_originalRenderScale;
        s_pipeline.supportsHDR = s_originalHdr;
    }

#if UNITY_EDITOR
    private static void OnPlayModeChanged(UnityEditor.PlayModeStateChange state)
    {
        if (state != UnityEditor.PlayModeStateChange.ExitingPlayMode) return;
        RestorePipeline();
        s_pipeline = null;
    }
#endif

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyScene();
    }

    /// <summary>씬마다 따로 있는 볼륨에 적용한다. 공유 프로필 에셋 대신 실행용 복사본을 고친다.</summary>
    private static void ApplyScene()
    {
        // 씬이 바뀌면 이전 씬의 Bloom은 사라지므로 정리한다.
        s_disabledBlooms.RemoveAll(b => b == null);

        if (Current != GraphicsQualityLevel.Low)
        {
            foreach (var bloom in s_disabledBlooms) bloom.active = true;
            s_disabledBlooms.Clear();
            return;
        }

        foreach (var volume in UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (volume.sharedProfile == null) continue;
            if (!volume.sharedProfile.TryGet(out Bloom shared) || !shared.active) continue;

            // volume.profile은 처음 접근할 때 복사본을 만든다. 에셋은 그대로 남는다.
            if (volume.profile.TryGet(out Bloom bloom) && bloom.active)
            {
                bloom.active = false;
                s_disabledBlooms.Add(bloom);
            }
        }
    }
}
