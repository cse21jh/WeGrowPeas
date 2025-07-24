using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DefaultBug : Bug
{
    void Update()
    {
        if (!grid.plantGrid.TryGetValue(targetObjIdx, out Plant plant))
            FindNewTargetObj();
        else if (grid.GetIsBreeding() && !isDie && !isHit)
        {
            Vector2 targetPos = new Vector2(plant.gameObject.transform.position.x, plant.gameObject.transform.position.y);
            MoveToward(targetPos);
        }
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
