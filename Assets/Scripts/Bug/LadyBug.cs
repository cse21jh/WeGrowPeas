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
        if (movingCoroutine != null) StopCoroutine(movingCoroutine);
        StartCoroutine(CantHitForOneSecond());
        StartCoroutine(bug.CantHitForOneSecond());
        while (bug != null && !isDie) // 벌레 없어지면 탈출
        {
            MoveToward(bug.gameObject.transform.position, 50f);
            yield return null;
        }
        if (!isDie) movingCoroutine = StartCoroutine(Moving()); // 벌레가 먼저 사라졌으면 다시 돌아다닌다
    }

    // 경고 없이 바로 움직인다
    protected override IEnumerator Warning()
    {
        movingCoroutine = StartCoroutine(Moving());
        yield return null;
    }

    protected override void OnTriggerEnter(Collider obj)
    {
        Bug bug = obj.GetComponent<Bug>();
        if (bug != null)
        {
            if (bug.GetType() != typeof(Ladybug))
            {
                bug.StartCoroutine(bug.KillBug());
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
