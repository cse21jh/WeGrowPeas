using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

public class NepenthesPheromone : MonoBehaviour
{
    [SerializeField] private Nepenthes nepenthes;

    private void Start()
    {
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
    }
    protected void OnTriggerEnter(Collider obj)
    {
        Bug bug = obj.GetComponent<Bug>();
        if (bug != null && !nepenthes.isDying)
        {
            if (bug.GetType() != typeof(Ladybug))
            {
                StartCoroutine(bug.MoveToNepenthes(nepenthes.gameObject));
            }
        }
        return;
    }

    
    public void OnMouseEnter()
    {
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
    }
    
}
