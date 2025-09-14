using UnityEngine;

public class NearestBugDetector : MonoBehaviour
{
    public string bugTag = "Bug";   // 벌레 오브젝트에 "Bug" 태그를 붙여두면 됨
    private Transform nearestBug;
    private MouseEffectController mouseEffectController;

    void Start()
    {
        mouseEffectController = GetComponent<MouseEffectController>();
        if (mouseEffectController == null)
        {
            Debug.LogError("MouseEffectController 컴포넌트를 찾을 수 없습니다!");
        }
    }

    void Update()
    {
        nearestBug = FindNearestBug();

        if (nearestBug != null)
        {
            // 디버그 라인 그리기 (Scene 뷰에서 확인 가능)
            Debug.DrawLine(Camera.main.ScreenToWorldPoint(Input.mousePosition), nearestBug.position, Color.red);
            mouseEffectController.SetTarget(nearestBug);
        }
        else
        {
            mouseEffectController.SetTarget(null);
        }
    }

    Transform FindNearestBug()
    {
        GameObject[] bugs = GameObject.FindGameObjectsWithTag(bugTag);

        if (bugs.Length == 0) return null;

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f; // 카메라와의 거리 (2D 카메라일 때 적당히 설정)
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mousePos);

        float minDist = Mathf.Infinity;
        Transform closest = null;

        foreach (GameObject bug in bugs)
        {
            float dist = Vector2.Distance(mouseWorldPos, bug.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
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
