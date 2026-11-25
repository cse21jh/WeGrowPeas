using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 업적 달성 알림. 오른쪽 아래에서 올라왔다가 내려간다. 여러 개면 차례로 보여준다.
/// 씬이 바뀌어도 이어지도록 자기 캔버스를 갖고, 레이캐스터가 없어 클릭을 막지 않는다.
/// </summary>
public class AchievementToast : MonoBehaviour
{
    [SerializeField] private RectTransform panel;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private TextMeshProUGUI titleText;

    [Header("Animation")]
    [SerializeField] private float slideDuration = 0.35f;
    [SerializeField] private float holdDuration = 3f;

    private static AchievementToast instance;

    private readonly Queue<AchievementData> waiting = new();
    private Vector2 shownPosition;
    private bool playing;

    public static void Show(AchievementData achievement)
    {
        if (!Application.isPlaying) return;
        if (instance == null)
        {
            var prefab = Resources.Load<AchievementToast>(ResourcePaths.AchievementToastPrefab);
            if (prefab == null) return;
            instance = Instantiate(prefab);
            DontDestroyOnLoad(instance.gameObject);
        }
        instance.waiting.Enqueue(achievement);
        instance.PlayNext();
    }

    /// <summary>아직 뜨지 않은 알림을 버린다.</summary>
    public static void ClearWaiting()
    {
        if (instance != null) instance.waiting.Clear();
    }

    private void Awake()
    {
        shownPosition = panel.anchoredPosition;
        panel.gameObject.SetActive(false);
    }

    private void PlayNext()
    {
        if (playing || waiting.Count == 0) return;
        playing = true;

        AchievementData achievement = waiting.Dequeue();
        headerText.text = Loc.T("game.achievement.unlocked");
        titleText.text = achievement.LocalizedTitle;
        iconImage.sprite = achievement.icon;
        iconImage.gameObject.SetActive(achievement.icon != null);

        Vector2 hidden = shownPosition - new Vector2(0f, panel.rect.height + shownPosition.y);
        panel.anchoredPosition = hidden;
        panel.gameObject.SetActive(true);
        SoundManager.Instance?.PlayEffect(SoundIds.QuestSuccess);

        DOTween.Sequence()
            .Append(panel.DOAnchorPos(shownPosition, slideDuration).SetEase(Ease.OutCubic))
            .AppendInterval(holdDuration)
            .Append(panel.DOAnchorPos(hidden, slideDuration).SetEase(Ease.InCubic))
            .OnComplete(() =>
            {
                panel.gameObject.SetActive(false);
                playing = false;
                PlayNext();
            })
            .SetUpdate(true) // 게임이 멈춰 있어도 움직인다
            .SetLink(gameObject);
    }
}
