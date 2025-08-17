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
    }


    [SerializeField] private List<CinemachineVirtualCamera> cameras = new List<CinemachineVirtualCamera>();

    public CinemachineVirtualCamera ActiveCamera = null;

    public bool IsActiveCamera(CinemachineVirtualCamera camera)
    {
        return ActiveCamera == camera;
    }

    public void SwitchCamera(CameraType type, float time = 0.5f)
    {
        cameras[(int) type].Priority = 10;
        this.gameObject.GetComponent<CinemachineBrain>().m_DefaultBlend.m_Time = time; // Set the blend time for camera transitions
        ActiveCamera = cameras[(int)type];

        foreach (CinemachineVirtualCamera cam in cameras)
        {
            if (cam != cameras[(int)type])
            {
                cam.Priority = 0;
            }
        }
    }
}
