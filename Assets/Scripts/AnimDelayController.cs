using System.Collections;
using UnityEngine;

public class AnimDelayController : MonoBehaviour
{
    Animator anim;
    [SerializeField] private float maxDelay = 0.5f; // Maximum delay before the animation starts

    private void Start()
    {
        anim = GetComponent<Animator>();
        if (anim != null)
        {
            StartCoroutine(AnimStart());
        }
    }


    private IEnumerator AnimStart()
    {
        float delay = Random.Range(0f, maxDelay);
        //Debug.Log($"Animation will start after a delay of {delay} seconds.");
        yield return new WaitForSeconds(delay);
        anim.SetTrigger("Start");
    }
}
