using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Bug : MonoBehaviour
{
    protected Grid grid;

    protected int targetObjIdx = 999;
    protected int spawnEdge;

    [SerializeField]
    protected float speed;
    protected bool isDie = false;
    protected bool isHit = false;


    private float rotationOffset = -90f;

    private GameObject bugKillerPrefab;
    private GameObject bugKiller;

    private GameObject WarningPrefab;
    private GameObject Warning;

    protected virtual void Start()
    {
        bugKillerPrefab = Resources.Load<GameObject>("Prefabs/BugKiller");
        WarningPrefab = Resources.Load<GameObject>("Prefabs/Warning");
        grid = GameObject.Find("Grid").GetComponent<Grid>();
        InitRandomPos();
        StartCoroutine(Moving());
    }

    protected virtual IEnumerator Moving()
    {    
        ShowWarningSign();
        yield return new WaitForSeconds(1.0f);
        DestroyWarningSign();
    }

    private void ShowWarningSign()
    {
        Warning = Instantiate(WarningPrefab);
        Vector3 pos = this.transform.position;

        switch (spawnEdge)
        {
            case 0:
                pos.y += -2f;
                break;
            case 1:
                pos.x += -2f;
                break;
            case 2:
                pos.y += 2f;
                break;
            case 3:
                pos.x += 2f;
                break;
        }
        Warning.transform.position = pos;
        return;
        
    }

    protected void DestroyWarningSign()
    {
        Destroy(Warning);
    }

    private void OnMouseDown()
    {
        if(!ClickRouter.Instance.IsBlockedByUI && grid.GetIsBreeding())
            StartCoroutine(HitBug());
    }

    protected void InitRandomPos()
    {
        spawnEdge = Random.Range(0, 4);

        float x = 0f;
        float y = 0f;

        switch (spawnEdge)
        {
            case 0:
                x = Random.Range(-9f, 9f);
                y = 6.0f;
                break;
            case 1:
                x = 10.0f;
                y = Random.Range(-5f, 5f);
                break;
            case 2:
                x = Random.Range(-9f, 9f);
                y = -6.0f;
                break;
            case 3:
                x = -10.0f;
                y = Random.Range(-5f, 5f);
                break;
        }

        transform.position = new Vector3(x, y, 0f);

        return;
    }

    protected void FindNewTargetObj()
    {
        Plant plant = null;
        int newTarget = targetObjIdx + 1;
        for(int i = 0; i < grid.maxCol * 4; i++)
        {
            if (newTarget >= grid.maxCol * 4)
                newTarget = 0;
            if (grid.plantGrid.TryGetValue(newTarget, out plant))
            {
                targetObjIdx = newTarget;
                break;
            }
            newTarget++;
        }

        if(plant == null) // 목표를 찾지 못함 (이미 식물이 없는 경우. 게임 오버)
        {
            Destroy(this.gameObject);
        }
    }
    

    void OnTriggerEnter(Collider obj)
    {
        Plant plant = obj.gameObject.GetComponent<Plant>();
        if (plant != null)
        {
            plant.Die();
        }
    }

    protected void MoveToward(Vector2 targetPos)
    {
        Vector2 currentPos = new Vector2(transform.position.x, transform.position.y);
        float distanceToTarget2D = Vector2.Distance(currentPos, targetPos);

        Vector2 directionToTarget = (targetPos - currentPos).normalized;

        if (directionToTarget != Vector2.zero)
        {
            float angleInRadians = Mathf.Atan2(directionToTarget.y, directionToTarget.x);
            float angleInDegrees = angleInRadians * Mathf.Rad2Deg;

            transform.rotation = Quaternion.Euler(0f, 0f, angleInDegrees + rotationOffset);
        }

        Vector2 newPosition2D = Vector2.MoveTowards(currentPos, targetPos, speed * Time.deltaTime);
        transform.position = new Vector3(newPosition2D.x, newPosition2D.y, transform.position.z);

        return;
    }

    protected virtual IEnumerator HitBug()
    {
        if (!isDie && !isHit)
        {
            SoundManager.Instance.PlayEffect("HitBug");
            isHit = true;
            yield return StartCoroutine(ShowBugKiller());
        }
    }

    protected virtual IEnumerator KillBug()
    {
        if(!isDie)
        { 
            grid.killBugCount++;
            grid.AddAdditionalPestResistance(0.0005f);
            isDie = true;
            yield return StartCoroutine(FadeOut());
            Destroy(this.gameObject);
        }
    }

    private IEnumerator ShowBugKiller()
    {
        bugKiller = Instantiate(bugKillerPrefab);
        Vector3 currentPos = this.gameObject.transform.position;
        bugKiller.transform.position = new Vector3(currentPos.x + 0.3f, currentPos.y - 0.3f, currentPos.z);
        yield return new WaitForSeconds(0.1f);
        Destroy(bugKiller);
    }

    private IEnumerator FadeOut()
    {
        float f = 1;

        Renderer renderer = gameObject.GetComponent<SpriteRenderer>();
        while (f > 0)
        {
            f -= 0.1f;
            Color ColorAlhpa = renderer.material.color;
            ColorAlhpa.a = f;
            renderer.material.color = ColorAlhpa;
            yield return new WaitForSeconds(0.02f);
        }
    }
}
