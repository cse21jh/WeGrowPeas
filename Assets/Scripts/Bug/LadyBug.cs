using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ladybug : Bug
{
    protected override BugMove Move => BugMove.Wander;

    protected override void Start()
    {
        base.Start();
        grid.AddLadybug(this);
    }

    public IEnumerator KillFuckingBug(Bug bug)
    {
        StopCoroutine(movingCoroutine);
        StartCoroutine(CantHitForOneSecond());
        StartCoroutine(bug.CantHitForOneSecond());
        while (bug != null) // 벌레 없어지면 탈출
        {
            MoveToward(bug.gameObject.transform.position, 50f);
            yield return null;
        }
    }

    protected override IEnumerator Warning()
    {
        yield return null;
    }

    protected override void OnTriggerEnter(Collider obj)
    {
        Bug bug = obj.GetComponent<Bug>();
        if (bug != null)
        {
            if (bug.GetType() != typeof(Ladybug))
            {
                StartCoroutine(bug.KillBug());
                StartCoroutine(this.KillBug());
            }
        }
        return;
    }
    protected override IEnumerator HitBug()
    {
        yield return StartCoroutine(base.HitBug());
        yield return StartCoroutine(KillBug());
    }

    public override IEnumerator KillBug()
    {
        grid.RemoveLadybug(this);
        yield return StartCoroutine(base.KillBug());
    }
}
