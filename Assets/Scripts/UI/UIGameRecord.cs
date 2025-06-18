using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIGameRecord : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log($"{GameRecordHolder.maxStageReached}, {GameRecordHolder.TotalPeas}, {GameRecordHolder.TotalBugsKilled}");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
