using System.Collections;
using UnityEngine;
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

    private Coroutine placingCo;
    private GameObject ghost;
    private bool isPlacing;

    public void BeginTilePlacement(
        System.Func<Vector3, bool> validate,          // 인자: screenPos
        System.Action<Vector3> onConfirm,             // 인자: screenPos
        System.Action onCancel)
    {
        if (isPlacing) StopPlacementInternal();

        placingCo = StartCoroutine(TilePlacementRoutine(validate, onConfirm, onCancel));
    }

    private IEnumerator TilePlacementRoutine(
        System.Func<Vector3, bool> validate,
        System.Action<Vector3> onConfirm,
        System.Action onCancel)
    {
        isPlacing = true;

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
        System.Func<Plant, bool> validate,
        System.Action<Plant> onConfirm,
        System.Action onCancel)
    {
        Debug.Log("식물 선택 모드 진입");
        // 실제 구현: 마우스 오버/클릭한 Plant 전달
    }
}