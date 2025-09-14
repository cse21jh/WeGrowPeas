using UnityEngine;

public class DontDestroyOnLand : MonoBehaviour
{

    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }


}
