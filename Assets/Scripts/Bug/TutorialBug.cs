using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialBug : Bug
{
    private bool letMove = true;
    public bool canCatchBug = false;

    protected override void Start()
    {
        base.Start();
    }


    protected override void Update()
    {
        if (Input.GetMouseButtonDown(0) && !ClickRouter.Instance.IsBlockedByUI && canCatchBug /*&& grid.GetIsBreeding()*/)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0));
            Debug.Log(mousePos);
            if (Mathf.Abs(transform.position.x - mousePos.x) < hitRange && Mathf.Abs(transform.position.y - mousePos.y) < hitRange)
            {
                StartCoroutine(HitBug());
            }
        }
    }

    protected override void InitBug()
    {
        SetFixedPosition();
        spawnEdge = 1;
        StartCoroutine(Moving());
    }

    protected override IEnumerator Moving()
    {
        yield return StartCoroutine(base.Moving());

        while(letMove)
        {
            if (!grid.plantGrid.TryGetValue(targetObjIdx, out Plant plant))
                FindNewTargetObj();
            else if (/*grid.GetIsBreeding() &&*/ !isDie && !isHit)
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
        TutorialManager.Instance.OnCatchBug();
    }

    private void SetFixedPosition()
    {
        transform.position = new Vector3(10.0f, -1.0f, 0);
    }

    public void StopMoving()
    {
        letMove = false;
    }    
}
