using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Bug : MonoBehaviour
{
    protected Grid grid;
    protected EconomyManager economyManager;

    protected int targetObjIdx = 999;
    protected int spawnEdge;
    protected int gold = 100;

    [SerializeField]
    protected float speed;
    [SerializeField]
    protected float hitRange;
    protected bool isDie = false;
    protected bool isHit = false;
    

    private float rotationOffset = -90f;

    private GameObject bugKillerPrefab;
    private GameObject bugKiller;

    private GameObject WarningPrefab;
    private GameObject Warning;



    //각종 효과 관련
    [SerializeField] private float dissolveDuration = 1.0f; // 분해 애니메이션 지속 시간
    private SpriteRenderer[] childSpriteRenderers;
    private Material[] childMaterials;
    private int dissolveAmountID = Shader.PropertyToID("_DissolveAmount");
    [SerializeField] private GameObject vanishEffect;



    protected virtual void Start()
    {
        bugKillerPrefab = Resources.Load<GameObject>("Prefabs/BugKiller");
        WarningPrefab = Resources.Load<GameObject>("Prefabs/Warning");
        economyManager = GameObject.Find("EconomyManager").GetComponent<EconomyManager>();
        grid = GameObject.Find("Grid").GetComponent<Grid>();

        childSpriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        childMaterials = new Material[childSpriteRenderers.Length];
        for (int i = 0; i < childSpriteRenderers.Length; i++)
        {
            childMaterials[i] = childSpriteRenderers[i].material;
        }
        Debug.Log(childMaterials.Length);

        speed = speed * (1f - grid.GetBugSpeedDecreasement());

        InitRandomPos();
        StartCoroutine(Moving());
    }

    protected void Update()
    {
        if (Input.GetMouseButtonDown(0) && !ClickRouter.Instance.IsBlockedByUI && grid.GetIsBreeding())
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(new Vector3 (Input.mousePosition.x, Input.mousePosition.y, 0));
            Debug.Log(mousePos);
            if (Mathf.Abs(transform.position.x - mousePos.x) < hitRange && Mathf.Abs(transform.position.y - mousePos.y) < hitRange)
            { 
                StartCoroutine(HitBug());
            }
        }
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
                if (plant.GetType() != typeof(Nepenthes))
                {
                    targetObjIdx = newTarget;
                    break;
                }
            }
            newTarget++;
        }

        if(plant == null) // 목표를 찾지 못함 (이미 식물이 없는 경우. 게임 오버)
        {
            Destroy(this.gameObject);
        }
    }
    

    protected virtual void OnTriggerEnter(Collider obj)
    {
        Plant plant = obj.gameObject.GetComponent<Plant>();
        if (plant != null && !isDie)
        {
            if (plant.GetType() == typeof(Nepenthes))
            {
                economyManager.AddGold(100);
                StartCoroutine(KillBug());
            }
            else
                plant.Die(DeathCause.Bug, this);
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

    public virtual IEnumerator KillBug()
    {
        if(!isDie)
        { 
            grid.killBugCount++;
            grid.AddAdditionalPestResistance(0.0005f);
            isDie = true;
            economyManager.AddGold(gold + grid.GetAdditionalBugGold());
            yield return StartCoroutine(Vanish());
            Destroy(this.gameObject);
        }
    }

    protected IEnumerator ShowBugKiller()
    {
        bugKiller = Instantiate(bugKillerPrefab);
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0));
        bugKiller.transform.position = new Vector3(mousePos.x + 0.3f, mousePos.y - 0.3f, transform.position.z);
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

    private IEnumerator Vanish()
    {
        ParticlePrefab effect = Instantiate(vanishEffect, transform.position, Quaternion.identity).GetComponent<ParticlePrefab>();
        effect.PlayEffect();

        float elapsedTime = 0f;
        while (elapsedTime < dissolveDuration)
        {
            elapsedTime += Time.deltaTime;

            float lerpedDissolve = Mathf.Lerp(0f, 1.1f, elapsedTime / dissolveDuration);

            for (int i = 0; i < childMaterials.Length; i++)
            {
                childMaterials[i].SetFloat(dissolveAmountID, lerpedDissolve);
            }


            yield return null;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawCube(transform.position, new Vector3(hitRange * 2,hitRange * 2));
    }
}
