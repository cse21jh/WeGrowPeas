using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnstoppableBug : Bug
{
    private SpriteRenderer bodySprite;
    private SpriteRenderer bodySprite_Main;

    private Color[] targetColors = new Color[]
        {
        new Color(255/255f, 0/255f, 0/255f),
        new Color(255/255f, 0/255f, 255/255f),
        new Color(0/255f, 0/255f, 255/255f),
        new Color(0/255f, 255/255f, 255/255f),
        new Color(0/255f, 255/255f, 0/255f),
        new Color(255/255f, 255/255f, 0/255f)
        };

    private int currentColorIndex = 0;
    private float colorValue = 1f;

    protected override void Start()
    {
        base.Start();
        bodySprite = transform.Find("RoachSprite").transform.Find("roach_body").GetComponent<SpriteRenderer>();
        bodySprite_Main = transform.Find("RoachSprite_Main").transform.Find("roach_body").GetComponent<SpriteRenderer>();
        StartCoroutine(ChangeColor());
    }

    protected override IEnumerator Moving()
    {
        yield return StartCoroutine(base.Moving());

        while(true)
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

    private IEnumerator ChangeColor()
    {
        bodySprite.color = Color.red;

        while (true)
        {
            int nextColorIndex = (currentColorIndex + 1) % targetColors.Length;
            Color targetColor = targetColors[nextColorIndex];
            Color currentColor = bodySprite.color;

            while (Mathf.Abs(currentColor.r - targetColor.r) + Mathf.Abs(currentColor.g - targetColor.g) + Mathf.Abs(currentColor.b - targetColor.b) > 0.1f)
            {
                currentColor.r = MoveTowards(currentColor.r, targetColor.r);
                currentColor.g = MoveTowards(currentColor.g, targetColor.g);
                currentColor.b = MoveTowards(currentColor.b, targetColor.b);
                bodySprite.color = currentColor;
                bodySprite_Main.color = currentColor;
                yield return null;
            }
            currentColorIndex = nextColorIndex;
        }

    }

    private float MoveTowards(float current, float target)
    {
        float step = 1 / 255f;

        if (current < target)
        {
            return Mathf.Min(current + step, target);
        }
        else if (current > target)
        {
            return Mathf.Max(current - step, target);
        }

        return target;
    }

}
