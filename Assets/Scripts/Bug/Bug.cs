using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Bug : MonoBehaviour
{
    protected Grid grid;

    protected int targetObjIdx = 999;
    
    protected float speed;
    protected bool isDie = false;
    protected bool isHit = false;


    private float rotationOffset = -90f;

    private GameObject bugKillerPrefab;
    private GameObject bugKiller;

    
    private void OnMouseDown()
    {
        if(!ClickRouter.Instance.IsBlockedByUI && grid.GetIsBreeding())
            StartCoroutine(HitBug());
    }

    public void InitBug(float speedValue, Grid g, Vector3 initialPos)
    {
        speed = speedValue;
        grid = g;
        transform.position = initialPos;

        bugKillerPrefab = Resources.Load<GameObject>("Prefabs/BugKiller");
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
        if (!isDie)
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
