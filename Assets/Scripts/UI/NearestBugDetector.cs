using UnityEngine;

public class NearestBugDetector : MonoBehaviour
{
    public string bugTag = "Bug";
    [SerializeField] private float bugRefreshInterval = 0.1f;

    private Transform nearestBug;
    private MouseEffectController mouseEffectController;
    private Camera mainCamera;
    private GameObject[] cachedBugs;
    private float nextBugRefreshTime;

    void Start()
    {
        mouseEffectController = GetComponent<MouseEffectController>();
        mainCamera = Camera.main;
        if (mouseEffectController == null)
        {
            Debug.LogError("MouseEffectController not found!");
        }
    }

    void Update()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        nearestBug = FindNearestBug();

        if (nearestBug != null)
        {
            mouseEffectController.SetTarget(nearestBug);
        }
        else
        {
            mouseEffectController.SetTarget(null);
        }
    }

    Transform FindNearestBug()
    {
        if (Time.unscaledTime >= nextBugRefreshTime || cachedBugs == null)
        {
            cachedBugs = GameObject.FindGameObjectsWithTag(bugTag);
            nextBugRefreshTime = Time.unscaledTime + bugRefreshInterval;
        }

        if (cachedBugs == null || cachedBugs.Length == 0) return null;
        if (mainCamera == null) return null;

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f;
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mousePos);

        float minSqrDist = float.PositiveInfinity;
        Transform closest = null;

        for (int i = 0; i < cachedBugs.Length; i++)
        {
            GameObject bug = cachedBugs[i];
            if (bug == null) continue;
            Vector3 bp = bug.transform.position;
            float dx = mouseWorldPos.x - bp.x;
            float dy = mouseWorldPos.y - bp.y;
            float sqr = dx * dx + dy * dy;
            if (sqr < minSqrDist)
            {
                minSqrDist = sqr;
                closest = bug.transform;
            }
        }

        return closest;
    }

    public Transform GetNearestBug()
    {
        return nearestBug;
    }
}
