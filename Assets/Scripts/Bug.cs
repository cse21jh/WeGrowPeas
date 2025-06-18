using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Bug : MonoBehaviour
{
    private int targetObjIdx;
    private float speed;
    private Grid grid;


    private float rotationOffset = -90f;

    // Update is called once per frame
    void Update()
    {
        if (!grid.plantGrid.TryGetValue(targetObjIdx, out Plant plant))
            FindNewTargetObj();
        else if (grid.GetIsBreeding())
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

    public void KillBug()
    {
        SoundManager.Instance.PlayEffect("KillBug");
        Destroy(this.gameObject);
    }
}
