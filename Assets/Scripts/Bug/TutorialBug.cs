using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialBug : Bug
{
    protected override BugMove Move => BugMove.Chase;

    private bool letMove = true;
    public bool canCatchBug = false;
    internal bool HasBeenCaught => isHit || isDie;
    private Vector3 spawnPosition = new Vector3(10f, -1f, 0f);

    protected override void Start()
    {
        base.Start();
    }


    protected override void Update()
    {
        if (Input.GetMouseButtonDown(0) && !ClickRouter.Instance.IsBlockedByUI && canCatchBug /*&& grid.Breeder.GetIsBreeding()*/)
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
        Motion.Edge = 1;
        StartCoroutine(Moving());
    }

    protected override IEnumerator Moving()
    {
        Motion.Begin();
        yield return null;

        while (letMove && Motion.Step(Time.deltaTime, !isDie && !isHit, Targets))
        {
            ApplyMotion();
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
        TutorialManager.Instance?.OnCatchBug();
    }

    private void SetFixedPosition()
    {
        transform.position = spawnPosition;
        Motion.Position = spawnPosition;
    }

    public void PrepareStationaryPractice(Vector3 position)
    {
        spawnPosition = position;
        letMove = false;
        canCatchBug = false;
        transform.position = position;
    }

    protected override void OnTriggerEnter(Collider other)
    {
        if (letMove) base.OnTriggerEnter(other);
    }

    public void StopMoving()
    {
        letMove = false;
    }    
}
