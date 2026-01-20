using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

public class PlacementController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Grid grid;                 // 네 Grid 클래스
    [SerializeField] private Camera worldCamera;        // 보통 Camera.main
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

        // 고스트 생성(있으면)
        SpriteRenderer ghostSr = null;
        if (ghostPrefab != null)
        {
            ghost = Instantiate(ghostPrefab);
            ghostSr = ghost.GetComponentInChildren<SpriteRenderer>();
        }

        ctx.ShowGuide?.Invoke("토양을 선택해주세요 (좌클릭=확정, 우클릭/ESC=취소)");

        Vector3? confirmedPos = null;
        bool shouldCancel = false;

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
                    confirmedPos = screenPos;
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
                shouldCancel = true;
                break;
            }

            yield return null;
        }

        // 3) 정리
        if (ghost != null) Destroy(ghost);
        isPlacing = false;
        placingCo = null;
        ctx.ShowGuide?.Invoke("");

        shovel.IsEnabled = true;
        shovelButton.enabled = true;

        // 코루틴이 완전히 종료된 후에 콜백 호출
        yield return null;
        yield return null; // 한 프레임 더 대기하여 완전히 안전하게

        try
        {
            if (shouldCancel)
            {
                onCancel?.Invoke();
            }
            else if (confirmedPos.HasValue)
            {
                onConfirm?.Invoke(confirmedPos.Value);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlacementController] TilePlacement callback error: {e.Message}\n{e.StackTrace}");
        }
    }

    public void StopPlacementInternal()
    {
        if (placingCo != null) StopCoroutine(placingCo);
        placingCo = null;
        isPlacing = false;

        if (ghost != null) Destroy(ghost);
    }

    public void BeginPlantSelection(
        ShopContext ctx,
        System.Func<Plant, bool> validate,
        System.Action<Plant> onConfirm,
        System.Action onCancel)
    {
        try
        {
            // 진행 중인 배치/선택 종료
            if (isPlacing)
            {
                StopPlacementInternal();
            }
            
            if (ctx == null)
            {
                Debug.LogError("[PlacementController] ShopContext is null!");
                return;
            }
            
            this.ctx = ctx;
            
            if (this == null || !gameObject.activeInHierarchy)
            {
                Debug.LogError("[PlacementController] PlacementController is not active!");
                return;
            }
            
            placingCo = StartCoroutine(PlantSelectionRoutine(validate, onConfirm, onCancel));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlacementController] BeginPlantSelection error: {e.Message}\n{e.StackTrace}");
        }
    }

    private IEnumerator PlantSelectionRoutine(
        System.Func<Plant, bool> validate,
        System.Action<Plant> onConfirm,
        System.Action onCancel)
    {
        try
        {
            isPlacing = true;
            
            if (shovel != null)
            {
                shovel.IsEnabled = false;
            }
            
            if (shovelButton != null)
            {
                shovelButton.enabled = false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlacementController] PlantSelectionRoutine 초기화 오류: {e.Message}\n{e.StackTrace}");
            yield break;
        }

        // 고스트 생성(있으면) - TilePlacementRoutine과 동일한 방식
        SpriteRenderer ghostSr = null;
        if (ghostPrefab != null)
        {
            ghost = Instantiate(ghostPrefab);
            ghostSr = ghost.GetComponentInChildren<SpriteRenderer>();
        }

        hovered = null;
        
        if (ctx != null && ctx.ShowGuide != null)
        {
            ctx.ShowGuide.Invoke("식물을 선택해주세요 (좌클릭=확정, 우클릭/ESC=취소)");
        }

        Plant confirmedPlant = null;
        bool shouldCancel = false;
        float timeout = 30f; // 30초 타임아웃
        float startTime = Time.time;

        while (true)
        {
            // 타임아웃 체크
            if (Time.time - startTime > timeout)
            {
                Debug.LogError("[PlacementController] PlantSelection timeout!");
                shouldCancel = true;
                break;
            }
            
            // UI 위에 있으면 하이라이트 해제 + 취소만 허용
            bool isOverUI = false;
            try
            {
                isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PlacementController] UI 체크 오류: {e.Message}");
                isOverUI = false;
            }
            
            if (isOverUI)
            {
                Hover(null);
                // UI 위에 있을 때는 ghost를 숨기지 않고 그대로 두되 (TilePlacementRoutine과 동일)
                // 투명하게만 만듦
                if (ghostSr != null)
                {
                    var c = ghostSr.color;
                    c.a = 0f; // 완전 투명
                    ghostSr.color = c;
                }
                if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
                {
                    shouldCancel = true;
                    break;
                }
                yield return null;
                continue;
            }

            // 마우스 아래 식물 레이캐스트
            Plant p = null;
            try
            {
                p = RaycastPlantUnderMouse();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PlacementController] RaycastPlantUnderMouse error: {e.Message}\n{e.StackTrace}");
                p = null;
            }
            
            try
            {
                Hover(p);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PlacementController] Hover error: {e.Message}");
            }

            // 고스트 표시 업데이트 - TilePlacementRoutine과 동일한 방식
            if (ghost != null && ghostSr != null)
            {
                if (p != null)
                {
                    // 고스트를 선택된 식물 위치에 표시
                    var soilT = grid != null ? grid.GetSoilTransform(p.gridIndex) : null;
                    if (soilT != null)
                    {
                        ghost.transform.position = soilT.position;
                    }
                    else
                    {
                        // soilT를 못 찾으면 마우스 위치 따라감
                        if (worldCamera != null)
                        {
                            Vector3 screenPos = Input.mousePosition;
                            Vector3 worldPos = worldCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
                            worldPos.z = 0f;
                            ghost.transform.position = worldPos;
                        }
                    }
                    
                    // 유효성 검사
                    bool ok = (validate == null) || validate(p);
                    
                    // 고스트 색상/알파로 피드백
                    var c = ghostSr.color;
                    c.a = ok ? 1f : ghostInvalidAlpha;
                    ghostSr.color = c;
                }
                else
                {
                    // 식물이 없으면 마우스 위치를 따라다니게 함
                    if (worldCamera != null)
                    {
                        Vector3 screenPos = Input.mousePosition;
                        Vector3 worldPos = worldCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
                        worldPos.z = 0f;
                        ghost.transform.position = worldPos;
                        
                        // 마우스 위치에서는 반투명하게
                        var c = ghostSr.color;
                        c.a = ghostInvalidAlpha;
                        ghostSr.color = c;
                    }
                }
            }

            // 좌클릭 확정
            if (Input.GetMouseButtonDown(0) && p != null)
            {
                bool ok = true;
                try { ok = (validate == null) || validate(p); } catch { ok = false; }

                if (ok)
                {
                    confirmedPlant = p;
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
                shouldCancel = true;
                break;
            }

            yield return null;
        }

        // 3) 정리
        EndHover();
        if (ghost != null)
        {
            Destroy(ghost);
            ghost = null;
        }
        isPlacing = false;
        placingCo = null;

        if (ctx != null && ctx.ShowGuide != null)
        {
            ctx.ShowGuide.Invoke("");
        }
        
        if (shovel != null)
        {
            shovel.IsEnabled = true;
        }
        
        if (shovelButton != null)
        {
            shovelButton.enabled = true;
        }

        // 코루틴이 완전히 종료된 후에 콜백 호출
        yield return null;
        yield return null; // 한 프레임 더 대기하여 완전히 안전하게

        try
        {
            if (shouldCancel)
            {
                onCancel?.Invoke();
            }
            else if (confirmedPlant != null)
            {
                onConfirm?.Invoke(confirmedPlant);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlacementController] PlantSelection callback error: {e.Message}\n{e.StackTrace}");
        }
    }

    private Plant RaycastPlantUnderMouse()
    {
        try
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
                    try
                    {
                        if (hit.transform == null) continue;
                        if (hit.transform.GetComponent<NepenthesPheromone>() != null)
                            continue;
                        
                        var t = hit.transform;
                        Plant plant = null;
                        
                        // 안전하게 컴포넌트 가져오기
                        try { plant = t.GetComponent<Plant>(); } catch { }
                        if (plant == null)
                        {
                            try { plant = t.GetComponentInParent<Plant>(); } catch { }
                        }
                        if (plant == null)
                        {
                            try { plant = t.GetComponentInChildren<Plant>(); } catch { }
                        }

                        if (plant == null) continue;

                        if (hit.distance < bestDist)
                        {
                            bestDist = hit.distance;
                            best = plant;
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[PlacementController] Error processing hit: {e.Message}");
                        continue;
                    }
                }
                if (best != null) return best;
            }

            // ② (백업) 그리드 기반: 해당 타일에 식물이 있으면 반환
            if (grid != null)
            {
                try
                {
                    int? idx = grid.GetGridIndexFromPosition(Input.mousePosition);
                    if (idx.HasValue && grid.plantGrid != null)
                    {
                        if (grid.plantGrid.TryGetValue(idx.Value, out var p) && p != null)
                            return p;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[PlacementController] Grid lookup error: {e.Message}");
                }
            }

            // ③ (백업) 2D 경로: 혼합 씬에서 2D 콜라이더도 있는 경우
            try
            {
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
                        if (h == null) continue;
                        try
                        {
                            Plant plant = null;
                            try { plant = h.GetComponent<Plant>(); } catch { }
                            if (plant == null)
                            {
                                try { plant = h.GetComponentInParent<Plant>(); } catch { }
                            }
                            if (plant == null)
                            {
                                try { plant = h.GetComponentInChildren<Plant>(); } catch { }
                            }
                            if (plant != null) return plant;
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning($"[PlacementController] Error processing 2D hit: {e.Message}");
                            continue;
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[PlacementController] 2D raycast error: {e.Message}");
            }

            return null;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlacementController] RaycastPlantUnderMouse fatal error: {e.Message}\n{e.StackTrace}");
            return null;
        }
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