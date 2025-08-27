using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialBug : Bug
{
    private bool letMove = true;

    protected override void Start()
    {
        base.Start();
    }

    protected override void InitBug()
    {
        SetFixedPosition();
        spawnEdge = 3;
        StartCoroutine(Moving());
    }

    protected override IEnumerator Moving()
    {
        yield return StartCoroutine(base.Moving());

        while(letMove)
        {
            if (!grid.plantGrid.TryGetValue(targetObjIdx, out Plant plant))
                FindNewTargetObj();
            else if (grid.GetIsBreeding() && !isDie && !isHit)
            {
                Vector2 targetPos = new Vector2(plant.gameObject.transform.position.x, plant.gameObject.transform.position.y);
                MoveToward(targetPos);
            }
            yield return null;
        }
    }

    protected override IEnumerator HitBug()
    {
        yield return StartCoroutine(base.HitBug());
        yield return StartCoroutine(KillBug());
    }

    public override IEnumerator KillBug()
    {
        yield return StartCoroutine(base.KillBug());
    }

    private void SetFixedPosition()
    {
        transform.position = new Vector3(-10.0f, -1.0f, 0);
    }

    public void StopMoving()
    {
        letMove = false;
    }    
}
