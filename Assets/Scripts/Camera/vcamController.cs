using Cinemachine;
using UnityEngine;

public class VcamController : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private CinemachineVirtualCamera vcam;

    public CinemachineVirtualCamera Vcam => vcam;

    private void Reset()
    {
        vcam = GetComponent<CinemachineVirtualCamera>();
    }

    private void Awake()
    {
        if (vcam == null)
        {
            vcam = GetComponent<CinemachineVirtualCamera>();
        }
    }

    public void SetFollowTarget(Transform target)
    {
        if (vcam == null || target == null)
        {
            return;
        }

        vcam.Follow = target;
    }

    public float GetOrthographicSize()
    {
        if (vcam == null)
        {
            return 0f;
        }

        return vcam.m_Lens.OrthographicSize;
    }

    public void SetOrthographicSize(float size)
    {
        if (vcam == null)
        {
            return;
        }

        vcam.m_Lens.OrthographicSize = size;
    }
}
