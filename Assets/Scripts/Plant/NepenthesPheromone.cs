using UnityEngine;

public class NepenthesPheromone : MonoBehaviour
{
    [SerializeField] private Nepenthes nepenthes;
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
}
