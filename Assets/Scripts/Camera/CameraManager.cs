using UnityEngine;
using Cinemachine;
using System.Collections.Generic;

public class CameraManager : MonoBehaviour
{

    public enum CameraType
    {
        Normal,
        Upgrade,
        Shop,
        Ending
    }


    [SerializeField] private List<CinemachineVirtualCamera> cameras = new List<CinemachineVirtualCamera>();

    public CinemachineVirtualCamera ActiveCamera = null;

    public bool IsActiveCamera(CinemachineVirtualCamera camera)
    {
        return ActiveCamera == camera;
    }

    public void SwitchCamera(CameraType type, float time = 0.5f)
    {
        int camIndex = (int)type;
        if(camIndex > 1) camIndex -= 1;
        cameras[camIndex].Priority = 10;
        this.gameObject.GetComponent<CinemachineBrain>().m_DefaultBlend.m_Time = time; // Set the blend time for camera transitions
        ActiveCamera = cameras[camIndex];

        foreach (CinemachineVirtualCamera cam in cameras)
        {
            if (cam != cameras[camIndex])
            {
                cam.Priority = 0;
            }
        }
    }

    public void SwitchFollowTarget(Transform target)
    {
        foreach (CinemachineVirtualCamera cam in cameras)
        {
            cam.Follow = target;
        }
    }
}
