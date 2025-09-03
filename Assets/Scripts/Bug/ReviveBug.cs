using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReviveBug : Bug
{
    private int reviveCount = 1;
    [SerializeField]
    private Sprite eggSprite;
    [SerializeField] private GameObject egg;

    [SerializeField] private GameObject[] metalBodies;
    [SerializeField] private GameObject[] normalBodies;
    [SerializeField] private GameObject fragEffect;

    protected override void Start()
    {
        base.Start();
    }

    protected override IEnumerator Moving()
    {
        yield return StartCoroutine(base.Moving());

        while (true)
        {
            if (eatingPlant)
            {
                yield return new WaitForSeconds(eatingTime);
                eatingPlant = false;
            }

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
        if (!isDie && !isHit)
        {
            SoundManager.Instance.PlayEffect("HitBug");
            isHit = true;
            yield return StartCoroutine(ShowBugKiller());

            if (reviveCount > 0)
            {
                /*
                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                transform.GetChild(0).gameObject.SetActive(false);
                sr.sprite = eggSprite;
                //egg.SetActive(true);
                //egg.transform.localRotation = transform.GetChild(0).localRotation;
                yield return new WaitForSeconds(1.0f);
                transform.GetChild(0).gameObject.SetActive(true);
                //egg.SetActive(false);
                sr.sprite = null;
                */

                foreach (GameObject body in metalBodies)
                {
                    body.SetActive(false);
                }
                foreach (GameObject body in normalBodies)
                {
                    body.SetActive(true);
                }
                ParticleSystem[] effects = fragEffect.GetComponentsInChildren<ParticleSystem>();
                foreach (ParticleSystem effect in effects)
                {
                    effect.Play();
                }
                yield return new WaitForSeconds(1.0f);

                reviveCount--;
                isHit = false;
            }
            else
            {
                yield return StartCoroutine(KillBug());
            }
        }
    }

    public override IEnumerator KillBug()
    {
        yield return StartCoroutine(base.KillBug());
    }
}
