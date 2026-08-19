using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// 회상 상세 화면. 목록에서 사진을 고르면 그 런의 결과 화면(편지 + 그래프)을 그대로 다시 보여준다.
///
/// 결과 화면 UI를 새로 만들지 않고 GameOverScene에서 뽑아낸 프리팹을 그대로 쓴다
/// (Tools/Recall/Extract Ending UI Prefab). 값만 과거 기록으로 갈아끼우므로
/// 엔딩 화면을 손보면 회상도 같이 따라간다.
/// </summary>
public class RecallDetailUI : MonoBehaviour
{
    /// <summary>결과 화면 프리팹의 기본 위치. 인스펙터에 직접 물려두면 이 경로는 쓰지 않는다.</summary>
    public const string ContentPrefabPath = "Prefabs/Recall/RecallEndingContent";

    [Header("Root")]
    [SerializeField] private GameObject detailPanel;

    [Header("Content")]
    [Tooltip("GameOverScene에서 뽑아낸 결과 화면(Envelope) 프리팹. 비워두면 Resources에서 찾는다.")]
    [SerializeField] private GameObject endingContentPrefab;
    [SerializeField] private Transform contentRoot;

    [Header("Header")]
    [SerializeField] private TMP_Text headerText;

    [Header("Timeline")]
    [SerializeField] private RecallTimelineUI timelineUI;

    private GameObject _content;
    private UIGameRecord _record;
    private GraphBuilder _graph;

    /// <summary>지금 펼쳐 둔 기록. 타임라인이 같은 기록을 이어받는다.</summary>
    private RecallRunFile _run;

    /// <summary>상세가 열려 있는가.</summary>
    public bool IsOpen => detailPanel != null && detailPanel.activeSelf;

    /// <summary>기록 하나를 펼친다.</summary>
    public void Show(string id)
    {
        RecallRunFile run = RecallStore.LoadRun(id);
        if (run == null)
        {
            Debug.LogWarning($"[Recall] 기록을 읽지 못했습니다: {id}");
            return;
        }

        _run = run;

        // 차트가 제대로 초기화되려면 활성 상태여야 한다. 내용을 붙이기 전에 먼저 켠다.
        if (detailPanel != null) detailPanel.SetActive(true);

        EnsureContent();

        if (headerText != null) headerText.text = BuildHeader(run);

        // UIGameRecord.Show가 Start보다 먼저 불려야 현재 런 값으로 덮이지 않는다.
        if (_record != null) _record.Show(run.summary);
        if (_graph != null) _graph.SetData(run.graph);
    }

    /// <summary>
    /// 열려 있는 것 중 가장 위 한 겹만 닫는다 (타임라인 → 상세). ESC 처리용.
    /// </summary>
    /// <returns>닫은 게 있으면 true.</returns>
    public bool CloseTopmost()
    {
        if (timelineUI != null && timelineUI.IsOpen)
        {
            timelineUI.Close();
            return true;
        }

        if (IsOpen)
        {
            Close();
            return true;
        }

        return false;
    }

    /// <summary>지금 보고 있는 기록의 일자별 타임라인을 연다. (버튼에 연결)</summary>
    public void OpenTimeline()
    {
        if (_run == null || timelineUI == null) return;
        timelineUI.Show(_run);
    }

    public void Close()
    {
        if (timelineUI != null) timelineUI.Close();
        if (detailPanel != null) detailPanel.SetActive(false);
    }

    private void EnsureContent()
    {
        if (_content != null) return;

        GameObject prefab = endingContentPrefab != null
            ? endingContentPrefab
            : Resources.Load<GameObject>(ContentPrefabPath);

        if (prefab == null)
        {
            Debug.LogWarning("[Recall] 결과 화면 프리팹이 없습니다. " +
                             "GameOverScene을 열고 Tools/Recall/Extract Ending UI Prefab을 실행하세요.");
            return;
        }

        _content = Instantiate(prefab, contentRoot != null ? contentRoot : transform);
        _content.SetActive(true);

        _record = _content.GetComponentInChildren<UIGameRecord>(true);
        _graph = _content.GetComponentInChildren<GraphBuilder>(true);

        if (_record == null) Debug.LogWarning("[Recall] 프리팹에서 UIGameRecord를 찾지 못했습니다.");
        if (_graph == null) Debug.LogWarning("[Recall] 프리팹에서 GraphBuilder를 찾지 못했습니다.");
    }

    /// <summary>이 농장이 어떤 농장이었는지 — 식물 / 특성 / 승천 단계.</summary>
    private string BuildHeader(RecallRunFile run)
    {
        var sb = new StringBuilder();

        var header = run.header;
        sb.Append($"{header.day}일");
        if (!string.IsNullOrEmpty(header.plantName)) sb.Append($" · {header.plantName}");
        sb.Append(header.dawnStage > 0 ? $" · 승천 {header.dawnStage}단계" : " · 일반 모드");

        if (run.plantAbilityNames.Count > 0)
        {
            sb.Append("\n특성: ");
            for (int i = 0; i < run.plantAbilityNames.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(run.plantAbilityNames[i]);

                int level = i < run.plantAbilityLevels.Count ? run.plantAbilityLevels[i] : 0;
                if (level > 0) sb.Append($" Lv{level}");
            }
        }

        if (run.generalAbilityNames.Count > 0)
            sb.Append($"\n일반 특성: {string.Join(", ", run.generalAbilityNames)}");

        return sb.ToString();
    }
}
