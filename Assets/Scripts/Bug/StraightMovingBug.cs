using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StraightMovingBug : Bug
{
    protected override void Start()
    {
        base.Start();
    }

    protected override IEnumerator Moving()
    {
        yield return StartCoroutine(base.Moving());
        Vector2 targetPos = -transform.position;

        float distSelfToTarget = Vector2.Distance(transform.position, targetPos);

        while (distSelfToTarget > 0.5f)
        {
            if (!grid.GetIsBreeding() || isDie || isHit)
            {
                yield return null;
                continue;
            }
            MoveToward(targetPos);
            yield return null;
            distSelfToTarget = Vector2.Distance(transform.position, targetPos);
        }
        Destroy(this.gameObject);
    }

    protected override IEnumerator HitBug()
    {
        yield return StartCoroutine(base.HitBug());
        yield return StartCoroutine(KillBug());
    }

    protected override IEnumerator KillBug()
    {
        yield return StartCoroutine(base.KillBug());
    }
}
