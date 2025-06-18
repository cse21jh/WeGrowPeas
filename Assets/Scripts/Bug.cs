using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Bug : MonoBehaviour
{
    private int targetObjIdx;
    private float speed;
    private Grid grid;

    private float rotationOffset = -90f;
    private bool isDie = false;

    private GameObject bugKillerPrefab;
    private GameObject bugKiller;


    // Update is called once per frame
    void Update()
    {
        if (!grid.plantGrid.TryGetValue(targetObjIdx, out Plant plant))
            FindNewTargetObj();
        else if (grid.GetIsBreeding() && !isDie)
        {
            Vector2 currentPos = new Vector2(transform.position.x, transform.position.y);
            Vector2 targetPos = new Vector2(plant.gameObject.transform.position.x, plant.gameObject.transform.position.y);
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
        }
    }
    


    public void InitBug(int gridNum, float speedValue, Grid g, Vector3 initialPos)
    {
        targetObjIdx = gridNum;
        speed = speedValue;
        grid = g;
        transform.position = initialPos;

        bugKillerPrefab = Resources.Load<GameObject>("Prefabs/BugKiller");
    }

    private void FindNewTargetObj()
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
            grid.DestroyPlantByBug(plant);
        }
    }

    public IEnumerator KillBug()
    {
        if(!isDie)
        { 
            SoundManager.Instance.PlayEffect("KillBug");
            grid.killBugCount++;
            isDie = true;
            yield return StartCoroutine(ShowBugKiller());
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
        StartCoroutine(FadeOut());
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
