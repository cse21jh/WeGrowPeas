using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI; // CanvasGroup

public class PlacementController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Grid grid;                 // 네 Grid 클래스
    [SerializeField] private Camera worldCamera;        // 보통 Camera.main
    [SerializeField] private CanvasGroup shopCanvas;    // ShopUI 루트 CanvasGroup (선택)
    [SerializeField] private GameObject ghostPrefab;    // 배치 미리보기 프리팹(선택)

    [Header("Ghost")]
    [SerializeField] private float ghostInvalidAlpha = 0.4f;

    [Header("Selection")]
    [SerializeField] private LayerMask plantLayer = ~0; // Plant가 있는 레이어로 지정
    [SerializeField] private float rayMaxDistance = 100f;

    [Header("Shovel")]
    [SerializeField] private Shovel shovel;
    [SerializeField] private ButtonController shovelButton;

    private Plant hovered;

    private Coroutine placingCo;
    private GameObject ghost;
    private bool isPlacing;

    private ShopContext ctx;

    public void BeginTilePlacement(
        ShopContext ctx,
        System.Func<Vector3, bool> validate,          // 인자: screenPos
        System.Action<Vector3> onConfirm,             // 인자: screenPos
        System.Action onCancel)
    {
        if (isPlacing) StopPlacementInternal();

        this.ctx = ctx;
        placingCo = StartCoroutine(TilePlacementRoutine(validate, onConfirm, onCancel));
    }

    private IEnumerator TilePlacementRoutine(
        System.Func<Vector3, bool> validate,
        System.Action<Vector3> onConfirm,
        System.Action onCancel)
    {
        isPlacing = true;
        shovel.IsEnabled = false;
        shovelButton.enabled = false;

        // 1) Shop UI 클릭 비활성 (화면은 보이되, 입력은 통과)
        bool hadCanvas = shopCanvas != null;
        bool prevInteractable = false, prevBlocks = false;
        if (hadCanvas)
        {
            prevInteractable = shopCanvas.interactable;
            prevBlocks = shopCanvas.blocksRaycasts;
            shopCanvas.interactable = false;
            shopCanvas.blocksRaycasts = false;
        }

        // 2) 고스트 생성(있으면)
        SpriteRenderer ghostSr = null;
        if (ghostPrefab != null)
        {
            ghost = Instantiate(ghostPrefab);
            ghostSr = ghost.GetComponentInChildren<SpriteRenderer>();
        }

        ctx.ShowGuide?.Invoke("토양을 선택해주세요 (좌클릭=확정, 우클릭/ESC=취소)");

        while (true)
        {
            // 현재 마우스 스크린 좌표
            Vector3 screenPos = Input.mousePosition;

            // 그리드 인덱스 찾기(고스트 위치 스냅용)
            int? idx = grid.GetGridIndexFromPosition(screenPos);
            if (idx.HasValue)
            {
                var soilT = grid.GetSoilTransform(idx.Value);
                if (ghost != null) ghost.transform.position = soilT.position;
            }
            else
            {
                // 토양을 못 찾으면 고스트는 마우스 월드 위치 따라감
                if (ghost != null)
                {
                    var wp = worldCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
                    wp.z = 0f;
                    ghost.transform.position = wp;
                }
            }

            // 유효성 검사 (ItemData.ValidatePosition는 screenPos를 기대)
            bool ok = validate?.Invoke(screenPos) ?? true;

            // 고스트 색상/알파로 피드백
            if (ghostSr != null)
            {
                var c = ghostSr.color;
                c.a = ok ? 1f : ghostInvalidAlpha;
                ghostSr.color = c;
            }

            // 좌클릭 확정
            if (Input.GetMouseButtonDown(0))
            {
                if (ok)
                {
                    onConfirm?.Invoke(screenPos); // screen 좌표를 그대로 넘김
                    break;
                }
                else
                {
                    // 유효하지 않으면 무시 (원하면 에러 사운드/푸터 표기 호출)
                }
            }

            // 우클릭 or Esc 취소
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                onCancel?.Invoke();
                break;
            }

            yield return null;
        }

        // 3) 정리
        if (ghost != null) Destroy(ghost);
        if (hadCanvas)
        {
            shopCanvas.interactable = prevInteractable;
            shopCanvas.blocksRaycasts = prevBlocks;
        }
        isPlacing = false;
        placingCo = null;
        ctx.ShowGuide?.Invoke("");

        shovel.IsEnabled = true;
        shovelButton.enabled = true;

        yield return null;
    }

    public void StopPlacementInternal()
    {
        if (placingCo != null) StopCoroutine(placingCo);
        placingCo = null;
        isPlacing = false;

        if (ghost != null) Destroy(ghost);
        if (shopCanvas != null)
        {
            // 안전하게 다시 켜주기
            shopCanvas.interactable = true;
            shopCanvas.blocksRaycasts = true;
        }
    }

    public void BeginPlantSelection(
        ShopContext ctx,
        System.Func<Plant, bool> validate,
        System.Action<Plant> onConfirm,
        System.Action onCancel)
    {
        // 진행 중인 배치/선택 종료
        if (isPlacing) StopPlacementInternal();
        this.ctx = ctx;
        placingCo = StartCoroutine(PlantSelectionRoutine(validate, onConfirm, onCancel));
    }

    private IEnumerator PlantSelectionRoutine(
        System.Func<Plant, bool> validate,
        System.Action<Plant> onConfirm,
        System.Action onCancel)
    {
        isPlacing = true;
        shovel.IsEnabled = false;
        shovelButton.enabled = false;

        // 1) Shop UI 입력 비활성
        bool hadCanvas = shopCanvas != null;
        bool prevInteractable = false, prevBlocks = false;
        if (hadCanvas)
        {
            prevInteractable = shopCanvas.interactable;
            prevBlocks = shopCanvas.blocksRaycasts;
            shopCanvas.interactable = false;
            shopCanvas.blocksRaycasts = false;
        }

        hovered = null;
        ctx.ShowGuide?.Invoke("식물을 선택해주세요 (좌클릭=확정, 우클릭/ESC=취소)");

        while (true)
        {
            // UI 위에 있으면 하이라이트 해제 + 취소만 허용
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()
                && (shopCanvas == null || shopCanvas.blocksRaycasts))
            {
                Hover(null);
                if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
                {
                    onCancel?.Invoke();
                    break;
                }

                continue;
            }



            // 마우스 아래 식물 레이캐스트
            Plant p = RaycastPlantUnderMouse();
            Hover(p);

            // 좌클릭 확정
            if (Input.GetMouseButtonDown(0) && p != null)
            {
                bool ok = true;
                try { ok = (validate == null) || validate(p); } catch { ok = false; }

                if (ok)
                {
                    var cb = onConfirm;
                    cb?.Invoke(p);
                    break;
                }
                else
                {
                    Debug.Log("선택 불가한 식물입니다.");
                }
            }

            // 우클릭/ESC 취소
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                onCancel?.Invoke();
                break;
            }

            yield return null;
        }

        // 3) 정리
        EndHover();
        if (hadCanvas)
        {
            shopCanvas.interactable = prevInteractable;
            shopCanvas.blocksRaycasts = prevBlocks;
        }
        isPlacing = false;
        placingCo = null;

        ctx.ShowGuide?.Invoke("");
        shovel.IsEnabled = true;
        shovelButton.enabled = true;

        yield return null;
    }

    private Plant RaycastPlantUnderMouse()
    {
        var cam = worldCamera != null ? worldCamera : Camera.main;
        if (!cam) return null;

        // ① 3D 경로: BoxCollider(3D)용
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        const float MaxPickDist = 1000f;

        // 트리거까지 포함해서 모두 맞춰봄(레이어 안 씀)
        var hits3D = Physics.RaycastAll(ray, MaxPickDist, ~0, QueryTriggerInteraction.Collide);
        if (hits3D != null && hits3D.Length > 0)
        {
            // 카메라에 가장 가까운(거리 가장 작은) Plant를 우선
            float bestDist = float.PositiveInfinity;
            Plant best = null;

            foreach (var hit in hits3D)
            {
                if (hit.transform.GetComponent<NepenthesPheromone>() != null)
                    continue;
                var t = hit.transform;
                    var plant =
                    t.GetComponent<Plant>() ??
                    t.GetComponentInParent<Plant>() ??
                    t.GetComponentInChildren<Plant>();

                if (plant == null) continue;

                if (hit.distance < bestDist)
                {
                    bestDist = hit.distance;
                    best = plant;
                }
            }
            if (best != null) return best;
        }

        // ② (백업) 그리드 기반: 해당 타일에 식물이 있으면 반환
        int? idx = grid.GetGridIndexFromPosition(Input.mousePosition);
        if (idx.HasValue)
        {
            grid.plantGrid.TryGetValue(idx.Value, out var p);
            if (p) return p;
        }

        // ③ (백업) 2D 경로: 혼합 씬에서 2D 콜라이더도 있는 경우
        Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
        world.z = 0f;
        Vector2 p2 = new Vector2(world.x, world.y);
        var hits2D = Physics2D.OverlapPointAll(p2);
        if (hits2D != null && hits2D.Length == 0)
            hits2D = Physics2D.OverlapCircleAll(p2, 0.1f);

        if (hits2D != null)
        {
            foreach (var h in hits2D)
            {
                var plant =
                    h.GetComponent<Plant>() ??
                    h.GetComponentInParent<Plant>() ??
                    h.GetComponentInChildren<Plant>();
                if (plant) return plant;
            }
        }

        return null;
    }

    private void Hover(Plant p)
    {
        if (hovered == p) return;
        // 이전 하이라이트 해제
        if (hovered != null)
        {
            try { hovered.MakeDefaultSprite(); } catch { }
        }
        hovered = p;
        // 새 하이라이트
        if (hovered != null)
        {
            try { hovered.MakeSelectedSprite(); } catch { }
        }
    }

    private void EndHover()
    {
        if (hovered != null)
        {
            try { hovered.MakeDefaultSprite(); } catch { }
            hovered = null;
        }
    }
}