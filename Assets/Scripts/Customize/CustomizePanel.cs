using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 치장 화면 오른쪽 패널. 왼쪽 탭으로 종류를 고르면 그 종류의 아이템을 목록에 채운다.
///
/// 슬롯을 눌렀을 때
///   보유한 아이템   → 장착(<see cref="CustomizeState"/>), "사용중" 표시를 옮긴다.
///   토큰 아이템     → 구매 확인 창 → 토큰을 내고 사면 해금만 된다(장착은 다시 눌러서).
///   장식품(업적)    → 어떤 업적을 달성해야 하는지 알려 준다.
///
/// 패널은 화면 오른쪽 밖으로 밀어 넣었다 꺼낼 수 있다(<see cref="Open"/> / <see cref="Close"/> / <see cref="ToggleOpen"/>).
/// 토글 버튼은 패널 왼쪽 바깥에 붙어 있어서 패널이 들어가도 화면 오른쪽 끝에 남는다.
/// </summary>
public class CustomizePanel : MonoBehaviour
{
    [Tooltip("CustomizeCategory 순서(토양, 목재, 풀밭, 케이스, 장식품)대로.")]
    [SerializeField] private Toggle[] tabs;

    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform content;
    [SerializeField] private CustomizeItemSlot slotPrefab;

    [Header("토큰·구매")]
    [Tooltip("보유 토큰 수를 보여 줄 글자.")]
    [SerializeField] private TMP_Text tokenText;
    [SerializeField] private CustomizePopup popup;

    [Header("열고 닫기")]
    [Tooltip("화살표 스프라이트를 쓴 버튼. 열림/닫힘에 따라 버튼 자체를 돌린다.")]
    [SerializeField] private Button toggleButton;
    [SerializeField] private float slideDuration = 0.3f;
    [SerializeField] private Ease slideEase = Ease.OutCubic;
    [SerializeField] private bool startOpen = true;
    [Tooltip("닫을 때 패널 폭보다 더 밀어 넣을 거리. 패널 바깥에 붙은 왼쪽 테두리(LeftBorder) 두께만큼.")]
    [SerializeField] private float hiddenExtra = 2f;

    public CustomizeCategory CurrentCategory { get; private set; }

    /// <summary>패널이 열려 있는가(닫히는 중이면 false).</summary>
    public bool IsOpen { get; private set; }

    private readonly List<CustomizeItemSlot> _slots = new List<CustomizeItemSlot>();

    private RectTransform _rect;
    private float _openX;
    // 0 = 완전히 열림, 1 = 완전히 닫힘. 픽셀이 아니라 비율로 들고 있다가 매 프레임 지금 폭에 곱한다.
    // 그래야 닫혀 있는 동안 창 크기·해상도가 바뀌어도 패널이 화면 밖에 정확히 숨는다.
    private float _hidden;
    private Tween _slideTween;

    // 원본 화살표(arrow.png)는 아래를 가리킨다. 열렸을 때는 오른쪽(닫기), 닫혔을 때는 왼쪽(열기)을 가리키게 돌린다.
    private const float ArrowRightZ = 90f;
    private const float ArrowLeftZ = -90f;

    private void Awake()
    {
        _rect = (RectTransform)transform;
        _openX = _rect.anchoredPosition.x;
        toggleButton.onClick.AddListener(ToggleOpen);

        for (int i = 0; i < tabs.Length; i++)
        {
            var category = (CustomizeCategory)i;
            // 이미 보고 있는 탭이면 다시 그리지 않는다.
            // (에디터에서는 UI를 다시 계산할 때 Toggle이 "켜짐"을 한 번 더 보내는데,
            //  그때 목록을 지우면 "rebuild loop" 에러가 난다.)
            tabs[i].onValueChanged.AddListener(on => { if (on && category != CurrentCategory) Show(category); });
        }
    }

    private void OnEnable()
    {
        CustomizeState.EquippedChanged += OnItemStateChanged;
        CustomizeInventory.OwnedChanged += OnItemStateChanged;
        CustomizeInventory.TokensChanged += OnTokensChanged;
    }

    private void OnDisable()
    {
        CustomizeState.EquippedChanged -= OnItemStateChanged;
        CustomizeInventory.OwnedChanged -= OnItemStateChanged;
        CustomizeInventory.TokensChanged -= OnTokensChanged;
    }

    private void OnDestroy()
    {
        _slideTween?.Kill();
        toggleButton.transform.DOKill();
    }

    private void LateUpdate() => ApplySlidePosition();

    private void Start()
    {
        SetOpen(startOpen, animate: false);
        OnTokensChanged(CustomizeInventory.Tokens);
        int on = Array.FindIndex(tabs, t => t.isOn);
        Show(on >= 0 ? (CustomizeCategory)on : CustomizeCategory.Soil);
    }

    /// <summary>그 종류의 아이템으로 목록을 다시 채운다.</summary>
    public void Show(CustomizeCategory category)
    {
        CurrentCategory = category;

        foreach (Transform child in content) Destroy(child.gameObject);
        _slots.Clear();

        foreach (var item in CustomizeCatalog.Get(category))
        {
            var slot = Instantiate(slotPrefab, content);
            slot.Setup(item, OnSlotClicked);
            _slots.Add(slot);
        }
        RefreshSlotStates();

        scrollRect.StopMovement();
        scrollRect.verticalNormalizedPosition = 1f; // 맨 위부터
    }

    // ── 열고 닫기 ─────────────────────────────────────────────────────────────

    /// <summary>패널을 꺼낸다.</summary>
    public void Open() => SetOpen(true, animate: true);

    /// <summary>패널을 화면 밖으로 밀어 넣는다. (예: 장식품을 배치하는 동안)</summary>
    public void Close() => SetOpen(false, animate: true);

    public void ToggleOpen() => SetOpen(!IsOpen, animate: true);

    public void SetOpen(bool open, bool animate)
    {
        IsOpen = open;
        float targetHidden = open ? 0f : 1f;
        var arrowRotation = new Vector3(0, 0, open ? ArrowRightZ : ArrowLeftZ);
        var arrow = toggleButton.transform;

        _slideTween?.Kill();
        arrow.DOKill();
        if (animate)
        {
            _slideTween = DOTween.To(() => _hidden, v => _hidden = v, targetHidden, slideDuration).SetEase(slideEase);
            arrow.DOLocalRotate(arrowRotation, slideDuration).SetEase(slideEase);
        }
        else
        {
            _hidden = targetHidden;
            arrow.localEulerAngles = arrowRotation;
            ApplySlidePosition();
        }
    }

    private void ApplySlidePosition()
    {
        float x = _openX + _hidden * (_rect.rect.width + hiddenExtra);
        if (!Mathf.Approximately(_rect.anchoredPosition.x, x))
            _rect.anchoredPosition = new Vector2(x, _rect.anchoredPosition.y);
    }

    // ── 슬롯 클릭 ─────────────────────────────────────────────────────────────

    private void OnSlotClicked(CustomizeItemSlot clicked)
    {
        var item = clicked.Item;
        if (CustomizeInventory.IsOwned(item))
        {
            CustomizeState.Equip(item);
            return;
        }

        if (item.UnlockMethod == CustomizeUnlockMethod.Token) AskPurchase(item);
        else ShowAchievementCondition(item);
    }

    private void AskPurchase(CustomizeItemData item)
    {
        int have = CustomizeInventory.Tokens;
        if (have < item.tokenPrice)
        {
            popup.Notice($"토큰이 부족해요.\n(필요 {item.tokenPrice}개 / 보유 {have}개)");
            return;
        }

        popup.Ask($"'{item.displayName}'을(를)\n토큰 {item.tokenPrice}개로 구매할까요?\n(보유 {have}개)", "구매", () =>
        {
            // 사면 해금(자물쇠 해제)만 된다. 장착은 한 번 더 눌러서 한다.
            var result = CustomizeInventory.TryPurchase(item);
            if (result == CustomizePurchaseResult.NotEnoughTokens) popup.Notice("토큰이 부족해요.");
        });
    }

    private void ShowAchievementCondition(CustomizeItemData item)
    {
        var achievement = item.requiredAchievement;
        popup.Notice(achievement != null
            ? $"'{achievement.LocalizedTitle}' 업적을 달성하면\n해금돼요."
            : "아직 해금 조건이 정해지지 않았어요.");
    }

    // ── 표시 갱신 ─────────────────────────────────────────────────────────────

    private void OnItemStateChanged(CustomizeItemData item)
    {
        if (item.category == CurrentCategory) RefreshSlotStates();
    }

    private void OnTokensChanged(int tokens)
    {
        if (tokenText != null) tokenText.text = $"토큰 {tokens}";
    }

    /// <summary>슬롯마다 보유 여부(자물쇠)와 장착 여부("사용중")를 다시 표시한다.</summary>
    private void RefreshSlotStates()
    {
        foreach (var slot in _slots)
        {
            slot.SetOwned(CustomizeInventory.IsOwned(slot.Item));
            slot.SetEquipped(CustomizeState.IsEquipped(slot.Item));
        }
    }

    // ── 테스트용 ──────────────────────────────────────────────────────────────

    [ContextMenu("디버그: 치장 토큰 10개 받기")]
    private void DebugAddTokens() => CustomizeInventory.AddTokens(10);
}
