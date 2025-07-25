using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Soil : MonoBehaviour
{
    [SerializeField] private int _gridIndex;

    public int GridIndex => _gridIndex;

    public void Init(int gridIndex)
    {
        _gridIndex = gridIndex;
    }
}