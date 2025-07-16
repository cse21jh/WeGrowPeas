using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickRouter : MonoBehaviour
{
    public static ClickRouter Instance { get; private set; }

    public bool IsBlockedByUI { get; set; } = false;

    private void Awake()
    {
        Instance = this;
    }
}
