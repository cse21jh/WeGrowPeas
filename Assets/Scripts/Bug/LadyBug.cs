using System.Collections;
using System.Collections.Generic;
using UnityEditor.SearchService;
using UnityEngine;

public class Ladybug : Bug
{
    protected override void Start()
    {
        base.Start();
        grid.ladybugs.Add(this);
    }

    protected override IEnumerator Moving()
    {
        yield return StartCoroutine(base.Moving());

        float time = 0;

        Vector2 startPos = transform.position;
        Vector2 targetPos = SetRandomPos();
        Vector2 controlPos1 = SetRandomPos();
        Vector2 controlPos2 = SetRandomPos();

        float distSelfToTarget;

        while (true)
        {
            if (!grid.GetIsBreeding() || isDie || isHit)
            {
                yield return null;
                continue;
            }
            time += Time.deltaTime / 10 * speed;

            distSelfToTarget = Vector2.Distance(transform.position, targetPos);

            if (distSelfToTarget < 0.5f)
            {
                time = 0;

                startPos = transform.position;
                targetPos = SetRandomPos();
                controlPos1 = SetRandomPos();
                controlPos2 = SetRandomPos();
            }

            Vector2 position1 = Vector2.Lerp(startPos, controlPos1, time);
            Vector2 position2 = Vector2.Lerp(controlPos1, controlPos2, time);
            Vector2 position3 = Vector2.Lerp(controlPos2, targetPos, time);

            Vector2 position4 = Vector2.Lerp(position1, position2, time);
            Vector2 position5 = Vector2.Lerp(position2, position3, time);

            MoveToward(Vector2.Lerp(position4, position5, time));

            yield return null;
        }
    }

    public IEnumerator KillFuckingBug(Bug bug)
    {
        StopCoroutine(movingCoroutine);
        StartCoroutine(CantHitForOneSecond());
        StartCoroutine(bug.CantHitForOneSecond());
        while (bug != null) // ¹ú·¹ ¾ø¾îÁö¸é Å»Ãâ
        {
            MoveToward(bug.gameObject.transform.position, 50f);
            yield return null;
        }
    }

    private Vector2 SetRandomPos()
    {
        return new Vector2(Random.Range(-9.0f, 9.0f), Random.Range(-5.0f, 5.0f));
    }

    protected override void OnTriggerEnter(Collider obj)
    {
        Bug bug = obj.GetComponent<Bug>();
        if(bug != null)
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
        grid.ladybugs.Remove(this);
        yield return StartCoroutine(base.KillBug());
    }
}
