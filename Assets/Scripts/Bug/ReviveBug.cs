using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReviveBug : Bug
{
    private int reviveCount = 1;
    [SerializeField]
    private Sprite eggSprite;
    [SerializeField]
    private Sprite bugSprite;

    protected override void Start()
    {
        base.Start();
    }

    protected override IEnumerator Moving()
    {
        yield return StartCoroutine(base.Moving());

        while (true)
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
        if(reviveCount > 0)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            //sr.sprite = eggSprite;
            yield return new WaitForSeconds(1.0f);
            //sr.sprite = bugSprite;
            reviveCount--;
            isHit = false;
        }
        else
        {
            yield return StartCoroutine(KillBug());
        }
        
    }

    protected override IEnumerator KillBug()
    {
        yield return StartCoroutine(base.KillBug());
    }
}
