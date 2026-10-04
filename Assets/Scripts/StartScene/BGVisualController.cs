using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class BGVisualController : MonoBehaviour
{
    [Header("Circles (center to outside)")]
    [Tooltip("Assign the smallest/front circle first and the largest/back circle last.")]
    [SerializeField] private Transform[] circles;

    [Header("Grass reveal")]
    [SerializeField] private Transform[] grasses;
    [Tooltip("첫 원이 커지기 시작한 뒤 풀이 올라오기까지의 시간. 0이면 동시에 시작합니다.")]
    [SerializeField, Min(0f)] private float grassRevealDelay = 0.2f;
    [Tooltip("0번 풀이 아래에서 올라오는 거리 (로컬 좌표 기준)")]
    [SerializeField, Min(0f)] private float grassRiseDistance = 0.5f;
    [Tooltip("인덱스가 하나 커질 때마다 추가되는 이동 거리")]
    [SerializeField, Min(0f)] private float grassRiseStep = 0.25f;

    [Header("Pop animation")]
    [SerializeField, Min(0f)] private float startDelay = 0.1f;
    [SerializeField, Min(0.01f)] private float popDuration = 0.55f;
    [SerializeField, Min(0f)] private float staggerDelay = 0.18f;
    [Tooltip("Higher values give the circles a stronger bounce past their final size.")]
    [SerializeField, Min(0f)] private float overshoot = 1.2f;

    [Header("Start peas")]
    [SerializeField] private Rigidbody2D startPeaPrefab;
    [SerializeField, Min(0)] private int peaCount = 40;
    [Tooltip("완두콩을 하나씩 생성하는 간격 (초)")]
    [SerializeField, Min(0.01f)] private float peaSpawnInterval = 0.1f;
    [Tooltip("BG 위치 기준 생성 영역의 중심 오프셋 (월드 단위)")]
    [SerializeField] private Vector2 peaSpawnOffset = new Vector2(0f, 0.1f);
    [Tooltip("생성 영역의 가로/세로 크기 (월드 단위). 바닥과 벽 안쪽으로 설정하세요.")]
    [SerializeField] private Vector2 peaSpawnArea = new Vector2(3.4f, 1.4f);
    [Tooltip("완두콩 중심 사이 최소 간격. 원형 충돌체 지름보다 작으면 자동으로 늘립니다.")]
    [SerializeField, Min(0.01f)] private float peaSpacing = 0.28f;
    [Tooltip("각 생성 위치에 더하는 가로/세로 무작위 오프셋의 최대 크기 (월드 단위)")]
    [SerializeField] private Vector2 peaSpawnJitter = new Vector2(0.1f, 0.1f);
    [Tooltip("0이면 고르게, 1이면 양쪽 가장자리로 강하게 모입니다. 3까지 높일 수 있고 일부는 중앙에도 생성됩니다.")]
    [SerializeField, Range(0f, 3f)] private float peaSideBias = 0.65f;
    [Tooltip("프리팹 원래 크기에 곱할 무작위 배율의 최솟값/최댓값. 가로/세로 비율은 유지합니다.")]
    [SerializeField] private Vector2 peaScaleRange = new Vector2(0.75f, 1.3f);
    [Tooltip("켜면 생성 시 표시 순서를 무작위로 정하고 유지합니다. 끄면 낮은 Y 위치일수록 앞에 표시합니다.")]
    [SerializeField] private bool randomizePeaSortingOrder = true;
    [Tooltip("무작위 Order in Layer의 최솟값/최댓값. 표시 순서 기준값이 추가됩니다.")]
    [SerializeField] private Vector2Int peaSortingOrderRange = new Vector2Int(0, 100);
    [Tooltip("완두콩 전체의 표시 순서에 더하는 기준값")]
    [SerializeField] private int peaSortingOrderOffset = 0;

    [Header("Pea popup sequence")]
    [Tooltip("기본~바람 프리팹 아래의 Animator들. 한 번에 하나씩 재생하고, 모두 재생한 뒤 순서를 다시 섞습니다.")]
    [SerializeField] private Animator[] peaAnimators;
    [Tooltip("첫 완두콩의 isStarted를 켜기 전 대기 시간 (초)")]
    [SerializeField, Min(0f)] private float peaAnimationStartDelay = 0f;
    [Tooltip("완두콩의 재생 시작 사이 최소 간격 (초). 이 시간이 지나도 이전 완두콩이 들어갈 때까지 기다립니다.")]
    [SerializeField, Min(0.01f)] private float peaAnimationInterval = 0.2f;

    private static readonly int IsStartedId = Animator.StringToHash("isStarted");
    private static readonly int PeaPopupStateId = Animator.StringToHash("Base Layer.Popup");
    private static readonly int PeaIdleStateId = Animator.StringToHash("Base Layer.New State");
    private readonly List<Animator> pendingPeaAnimators = new();
    private Animator activePeaAnimator;
    private Animator lastStartedPeaAnimator;
    private bool activePeaEnteredPopup;
    private int nextPeaAnimatorIndex;
    private float peaAnimationTimer;
    private Vector3[] originalScales;
    private Vector3[] originalGrassPositions;
    private Sequence revealSequence;
    private Transform spawnedPeasRoot;
    private readonly List<SortingGroup> spawnedPeaGroups = new();
    private readonly Collider2D[] peaSpawnOverlaps = new Collider2D[1];
    private ContactFilter2D peaSpawnFilter;
    private Vector3[] peaSpawnPositions;
    private Vector3 peaSpawnCenter;
    private int targetPeaCount;
    private int spawnedPeaCount;
    private float peaSpawnTimer;
    private float peaPrefabRadius;

    private void Awake()
    {
        int count = circles == null ? 0 : circles.Length;
        originalScales = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            if (circles[i] != null)
            {
                originalScales[i] = circles[i].localScale;
            }
        }

        int grassCount = grasses == null ? 0 : grasses.Length;
        originalGrassPositions = new Vector3[grassCount];
        for (int i = 0; i < grassCount; i++)
        {
            if (grasses[i] != null)
            {
                originalGrassPositions[i] = grasses[i].localPosition;
            }
        }
    }

    private void OnEnable()
    {
        PlayReveal();
    }

    private void Start()
    {
        SpawnStartPeas();
        PreparePeaAnimationStarts();
    }

    private void Update()
    {
        UpdatePeaAnimationStarts();

        if (spawnedPeasRoot == null || startPeaPrefab == null ||
            spawnedPeaCount >= targetPeaCount || Time.deltaTime <= 0f)
        {
            return;
        }

        // Use physics time so a scene-wide DOTween.KillAll(true) cannot spawn the batch at once.
        peaSpawnTimer -= Time.deltaTime;
        if (peaSpawnTimer > 0f) return;

        float minScale = Mathf.Max(0.01f, Mathf.Min(peaScaleRange.x, peaScaleRange.y));
        float maxScale = Mathf.Max(minScale, Mathf.Max(peaScaleRange.x, peaScaleRange.y));
        float scaleMultiplier = Random.Range(minScale, maxScale);
        float spawnRadius = peaPrefabRadius * scaleMultiplier + 0.01f;
        // Keep a small share spread across the area; retries retain this pea's chosen distribution.
        float sideBias = Random.value < 0.1f ? 0f : Mathf.Max(0f, peaSideBias);
        int firstSlot = Random.Range(0, peaSpawnPositions.Length);
        for (int i = 0; i < peaSpawnPositions.Length; i++)
        {
            Vector3 position = GetPeaSpawnPosition((firstSlot + i) % peaSpawnPositions.Length, spawnRadius, sideBias);
            if (Physics2D.OverlapCircle(position, spawnRadius, peaSpawnFilter, peaSpawnOverlaps) > 0)
            {
                continue;
            }

            SpawnPea(position, scaleMultiplier);
            break;
        }

        // If every slot is occupied, wait for the falling peas to make room.
        peaSpawnTimer = Mathf.Max(0.01f, peaSpawnInterval);
    }

    private void PreparePeaAnimationStarts()
    {
        pendingPeaAnimators.Clear();
        nextPeaAnimatorIndex = 0;
        activePeaAnimator = null;
        lastStartedPeaAnimator = null;
        peaAnimationTimer = Mathf.Max(0f, peaAnimationStartDelay);
        if (peaAnimators == null) return;

        foreach (Animator animator in peaAnimators)
        {
            if (animator == null || animator.runtimeAnimatorController == null ||
                pendingPeaAnimators.Contains(animator) ||
                !animator.HasState(0, PeaPopupStateId) || !animator.HasState(0, PeaIdleStateId)) continue;

            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.nameHash != IsStartedId || parameter.type != AnimatorControllerParameterType.Bool) continue;
                animator.SetBool(IsStartedId, false);
                pendingPeaAnimators.Add(animator);
                break;
            }
        }

        ShufflePeaAnimationOrder();
    }

    private void ShufflePeaAnimationOrder()
    {
        for (int i = pendingPeaAnimators.Count - 1; i > 0; i--)
        {
            int other = Random.Range(0, i + 1);
            (pendingPeaAnimators[i], pendingPeaAnimators[other]) =
                (pendingPeaAnimators[other], pendingPeaAnimators[i]);
        }

        // Avoid repeating the same pea at the boundary between two shuffled rounds.
        if (pendingPeaAnimators.Count > 1 && pendingPeaAnimators[0] == lastStartedPeaAnimator)
        {
            int other = Random.Range(1, pendingPeaAnimators.Count);
            (pendingPeaAnimators[0], pendingPeaAnimators[other]) =
                (pendingPeaAnimators[other], pendingPeaAnimators[0]);
        }
    }

    private void UpdatePeaAnimationStarts()
    {
        if (pendingPeaAnimators.Count == 0 || Time.deltaTime <= 0f) return;

        peaAnimationTimer -= Time.deltaTime;
        if (activePeaAnimator != null)
        {
            AnimatorStateInfo state = activePeaAnimator.GetCurrentAnimatorStateInfo(0);
            if (state.fullPathHash == PeaPopupStateId)
            {
                activePeaEnteredPopup = true;
                // Consume the start signal so this pea returns to idle after one popup.
                activePeaAnimator.SetBool(IsStartedId, false);
            }

            if (!activePeaEnteredPopup || activePeaAnimator.IsInTransition(0) ||
                state.fullPathHash != PeaIdleStateId) return;

            activePeaAnimator = null;
        }

        if (peaAnimationTimer > 0f) return;

        for (int attempts = 0; attempts < pendingPeaAnimators.Count; attempts++)
        {
            if (nextPeaAnimatorIndex >= pendingPeaAnimators.Count)
            {
                ShufflePeaAnimationOrder();
                nextPeaAnimatorIndex = 0;
            }

            Animator animator = pendingPeaAnimators[nextPeaAnimatorIndex++];
            if (animator == null || !animator.isActiveAndEnabled) continue;

            animator.SetBool(IsStartedId, true);
            activePeaAnimator = animator;
            lastStartedPeaAnimator = animator;
            activePeaEnteredPopup = false;
            peaAnimationTimer = Mathf.Max(0.01f, peaAnimationInterval);
            break;
        }
    }

    private void LateUpdate()
    {
        if (randomizePeaSortingOrder) return;

        foreach (SortingGroup group in spawnedPeaGroups)
        {
            if (group != null) UpdatePeaSorting(group);
        }
    }

    private void OnDisable()
    {
        KillSequence();
        RestoreScales();
        RestoreGrassPositions();
    }

    [ContextMenu("Replay Reveal (Play Mode)")]
    private void ReplayReveal()
    {
        PlayReveal();
    }

    /// <summary>
    /// Restarts the reveal. Append or join more tweens/callbacks on the returned sequence.
    /// Returns null outside Play Mode or when there are no configured circles or grasses.
    /// </summary>
    public Sequence PlayReveal()
    {
        if (!Application.isPlaying || !isActiveAndEnabled ||
            originalScales == null || originalGrassPositions == null ||
            (originalScales.Length == 0 && originalGrassPositions.Length == 0))
        {
            return null;
        }

        KillSequence();
        revealSequence = DOTween.Sequence()
            .SetUpdate(true)
            .OnKill(() => revealSequence = null);

        AppendCircleReveal(revealSequence);
        AppendGrassReveal(revealSequence);
        return revealSequence;
    }

    private void AppendCircleReveal(Sequence sequence)
    {
        float duration = Mathf.Max(0.01f, popDuration);
        float interval = Mathf.Max(0f, staggerDelay);
        float startTime = sequence.Duration() + Mathf.Max(0f, startDelay);

        for (int i = 0; i < originalScales.Length; i++)
        {
            if (circles[i] == null) continue;

            circles[i].localScale = Vector3.zero;
            sequence.Insert(startTime + i * interval,
                circles[i].DOScale(originalScales[i], duration)
                    .SetEase(Ease.OutBack, Mathf.Max(0f, overshoot)));
        }
    }

    private void AppendGrassReveal(Sequence sequence)
    {
        if (grasses == null || grasses.Length == 0) return;

        float duration = Mathf.Max(0.01f, popDuration);
        float interval = Mathf.Max(0f, staggerDelay);
        float startTime = Mathf.Max(0f, startDelay) + Mathf.Max(0f, grassRevealDelay);

        for (int i = 0; i < grasses.Length; i++)
        {
            if (grasses[i] == null) continue;
            Vector3 targetPosition = originalGrassPositions[i];
            float riseDistance = Mathf.Max(0f, grassRiseDistance) + i * Mathf.Max(0f, grassRiseStep);
            grasses[i].localPosition = targetPosition + Vector3.down * riseDistance;
            sequence.Insert(startTime + i * interval,
                grasses[i].DOLocalMoveY(targetPosition.y, duration)
                    .SetEase(Ease.OutBack, Mathf.Max(0f, overshoot)));
        }
    }

    private void KillSequence()
    {
        if (revealSequence != null)
        {
            revealSequence.Kill();
            revealSequence = null;
        }
    }

    private void RestoreScales()
    {
        if (originalScales == null) return;

        for (int i = 0; i < originalScales.Length; i++)
        {
            if (circles[i] != null)
            {
                circles[i].localScale = originalScales[i];
            }
        }
    }

    private void RestoreGrassPositions()
    {
        if (originalGrassPositions == null) return;

        for (int i = 0; i < originalGrassPositions.Length; i++)
        {
            if (grasses[i] != null)
            {
                grasses[i].localPosition = originalGrassPositions[i];
            }
        }
    }

    [ContextMenu("Spawn Start Peas (Play Mode)")]
    public void SpawnStartPeas()
    {
        if (!Application.isPlaying || !isActiveAndEnabled ||
            startPeaPrefab == null || peaCount <= 0 || spawnedPeasRoot != null)
        {
            return;
        }

        float spacing = Mathf.Max(0.01f, peaSpacing);
        peaPrefabRadius = spacing * 0.5f;
        CircleCollider2D circle = startPeaPrefab.GetComponent<CircleCollider2D>();
        if (circle != null)
        {
            Vector3 scale = startPeaPrefab.transform.localScale;
            float diameter = circle.radius * 2f * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
            spacing = Mathf.Max(spacing, diameter + 0.02f);
            peaPrefabRadius = diameter * 0.5f;
        }

        int columns = Mathf.FloorToInt(Mathf.Max(0f, peaSpawnArea.x) / spacing);
        int rows = Mathf.FloorToInt(Mathf.Max(0f, peaSpawnArea.y) / spacing);
        int capacity = columns * rows;
        if (capacity == 0) return;

        // A scene-root container keeps BG's non-uniform scale away from the physics bodies.
        spawnedPeasRoot = new GameObject("Start Peas").transform;
        SceneManager.MoveGameObjectToScene(spawnedPeasRoot.gameObject, gameObject.scene);

        peaSpawnPositions = new Vector3[capacity];
        peaSpawnCenter = transform.position + (Vector3)peaSpawnOffset;
        for (int i = 0; i < capacity; i++)
        {
            int column = i % columns;
            int row = i / columns;
            peaSpawnPositions[i] = peaSpawnCenter + new Vector3(
                (column - (columns - 1) * 0.5f) * spacing,
                (row - (rows - 1) * 0.5f) * spacing, 0f);
        }

        peaSpawnFilter = new ContactFilter2D { useTriggers = false };
        peaSpawnFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(startPeaPrefab.gameObject.layer));
        targetPeaCount = peaCount;
        spawnedPeaCount = 0;
        spawnedPeaGroups.Clear();
        peaSpawnTimer = 0f;
    }

    private Vector3 GetPeaSpawnPosition(int slot, float spawnRadius, float sideBias)
    {
        Vector3 position = peaSpawnPositions[slot];
        float halfWidth = Mathf.Max(0f, peaSpawnArea.x * 0.5f - spawnRadius);
        float halfHeight = Mathf.Max(0f, peaSpawnArea.y * 0.5f - spawnRadius);

        // A stronger curve concentrates samples near both edges, even at a bias of one.
        float normalizedX = Mathf.Clamp((position.x - peaSpawnCenter.x) / Mathf.Max(0.0001f, halfWidth), -1f, 1f);
        float exponent = 1f / (1f + 6f * Mathf.Max(0f, sideBias));
        position.x = peaSpawnCenter.x + Mathf.Sign(normalizedX) *
            Mathf.Pow(Mathf.Abs(normalizedX), exponent) * halfWidth;

        float jitterX = Mathf.Max(0f, peaSpawnJitter.x);
        float jitterY = Mathf.Max(0f, peaSpawnJitter.y);
        position.x += Random.Range(-jitterX, jitterX);
        position.y += Random.Range(-jitterY, jitterY);

        // Keep the collider inside the spawn area, then let the overlap check reject occupied spots.
        position.x = Mathf.Clamp(position.x, peaSpawnCenter.x - halfWidth, peaSpawnCenter.x + halfWidth);
        position.y = Mathf.Clamp(position.y, peaSpawnCenter.y - halfHeight, peaSpawnCenter.y + halfHeight);
        return position;
    }

    private void SpawnPea(Vector3 position, float scaleMultiplier)
    {
        Rigidbody2D pea = Instantiate(startPeaPrefab, position,
            startPeaPrefab.transform.rotation, spawnedPeasRoot);
        pea.transform.localScale = startPeaPrefab.transform.localScale * scaleMultiplier;
        spawnedPeaCount++;
        pea.name = $"StartPea_{spawnedPeaCount:00}";

        // Keep each pea's outline, fill and SpriteMask together when peas overlap.
        SortingGroup group = pea.GetComponent<SortingGroup>();
        if (group == null) group = pea.gameObject.AddComponent<SortingGroup>();
        SpriteRenderer sprite = pea.GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            group.sortingLayerID = sprite.sortingLayerID;
        }

        spawnedPeaGroups.Add(group);
        if (randomizePeaSortingOrder)
        {
            int minimum = Mathf.Clamp(Mathf.Min(peaSortingOrderRange.x, peaSortingOrderRange.y), short.MinValue, short.MaxValue);
            int maximum = Mathf.Clamp(Mathf.Max(peaSortingOrderRange.x, peaSortingOrderRange.y), short.MinValue, short.MaxValue);
            group.sortingOrder = (int)System.Math.Clamp(
                (long)peaSortingOrderOffset + Random.Range(minimum, maximum + 1), short.MinValue, short.MaxValue);
        }
        else
        {
            UpdatePeaSorting(group);
        }
    }

    private void UpdatePeaSorting(SortingGroup group)
    {
        // Larger sorting orders render in front; lower peas therefore get larger values.
        group.sortingOrder = Mathf.Clamp(
            peaSortingOrderOffset - Mathf.RoundToInt(group.transform.position.y * 100f),
            short.MinValue, short.MaxValue);
    }

    private void OnDestroy()
    {
        if (spawnedPeasRoot != null)
        {
            Destroy(spawnedPeasRoot.gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.5f, 1f, 0.3f, 0.7f);
        Gizmos.DrawWireCube(transform.position + (Vector3)peaSpawnOffset,
            new Vector3(peaSpawnArea.x, peaSpawnArea.y, 0f));
    }
}
