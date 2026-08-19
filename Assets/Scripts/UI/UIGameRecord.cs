using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 결과 화면(편지)의 본문을 채운다.
///
/// 어떤 런의 요약이든 <see cref="Show"/> 하나로 그린다. 이번 런은 <see cref="Start"/>가,
/// 과거 런은 회상 화면이 넘겨준다. 유전자 지급 같은 상태 변화는 여기서 하지 않는다
/// (회상으로 같은 화면을 다시 볼 때 또 지급되면 안 되므로 런 종료 지점으로 옮겼다).
/// </summary>
public class UIGameRecord : MonoBehaviour
{
    /*[SerializeField] private TextMeshProUGUI textStage;
    [SerializeField] private TextMeshProUGUI textPea;
    [SerializeField] private TextMeshProUGUI textBug;*/

    [SerializeField] private Image peaEmotionUI;
    private TextMeshProUGUI endingText, pg1, pg2, pg3, pg4;

    private Sprite[] peaEmotionSprite;

    private bool initialized;

    /// <summary>바깥에서 이미 채웠는가. 회상이 과거 기록을 넣어 둔 화면을 Start가 덮어쓰지 않게 한다.</summary>
    private bool filledExternally;

    // Start is called before the first frame update
    void Start()
    {
        // 회상은 Instantiate 직후(= Start 전에) Show를 부른다. 그 경우 여기서 손대지 않는다.
        if (filledExternally) return;

        Show(GameRecordHolder.Current);
        ShowNewlyUnlockedItems();
    }

    /// <summary>요약 하나로 결과 화면을 채운다. 회상도 이 경로로 과거 기록을 그린다.</summary>
    public void Show(RunSummary summary)
    {
        if (summary == null) return;

        filledExternally = true;
        EnsureInitialized();

        SetPeaEmotion(summary.playerRank);
        SetEndingMailContent(summary);
    }

    /// <summary>Start보다 Show가 먼저 불릴 수 있으므로(회상에서 즉시 호출) 참조를 지연 초기화한다.</summary>
    private void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;

        peaEmotionSprite = Resources.LoadAll<Sprite>("peaFace_1-sheet");

        var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        endingText = texts.FirstOrDefault(t => t.name == "EndingText");
        pg1 = texts.FirstOrDefault(t => t.name == "PG1");
        pg2 = texts.FirstOrDefault(t => t.name == "PG2");
        pg3 = texts.FirstOrDefault(t => t.name == "PG3");
        pg4 = texts.FirstOrDefault(t => t.name == "PG4");
    }

    /// <summary>이번 판에 새로 해금된 아이템이 있으면 해금 팝업으로 보여준다.</summary>
    private void ShowNewlyUnlockedItems()
    {
        List<ItemData> newlyUnlocked = UnlockRunTracker.GetNewlyUnlocked();
        if (newlyUnlocked.Count == 0) return;

        if (UIManager.Instance == null || UIManager.Instance.Popup == null)
        {
            Debug.LogWarning("[UIGameRecord] UIManager가 없어 해금 팝업을 띄우지 못했습니다");
            return;
        }

        UIManager.Instance.Popup.ShowUnlockPopup(newlyUnlocked);
    }

    private void SetPeaEmotion(int rank)
    {
        if (peaEmotionUI == null || peaEmotionSprite == null) return;

        int index = RunRecordFormatter.GetPeaEmotionSpriteIndex(rank);
        if (index >= 0 && index < peaEmotionSprite.Length)
            peaEmotionUI.sprite = peaEmotionSprite[index];
    }

    private void SetEndingMailContent(RunSummary summary)
    {
        if (endingText != null) endingText.text = RunRecordFormatter.BuildEndingText(summary);
        if (pg1 != null) pg1.text = RunRecordFormatter.BuildDaysLine(summary);
        if (pg2 != null) pg2.text = RunRecordFormatter.BuildStatsLine(summary);
        if (pg3 != null) pg3.text = RunRecordFormatter.BuildFarmNoteLine(summary);
        if (pg4 != null) pg4.text = "";
    }
}
