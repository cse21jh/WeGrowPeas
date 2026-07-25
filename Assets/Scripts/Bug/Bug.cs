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

    private static GameObject s_bugKillerPrefab;
    private static GameObject s_warningPrefab;
    private static EconomyManager s_economyManager;
    private static Grid s_grid;
    private static Camera s_mainCamera;

    private GameObject bugKiller;
    private GameObject WarningSign;

    protected bool eatingPlant = false;
    protected float eatingTime = 1.0f;

    protected Coroutine movingCoroutine;

    //���� ȿ�� ����
    [SerializeField] private float dissolveDuration = 1.0f; // ���� �ִϸ��̼� ���� �ð�
    private SpriteRenderer[] childSpriteRenderers;
    private Material[] childMaterials;
    private int dissolveAmountID = Shader.PropertyToID("_DissolveAmount");
    [SerializeField] private GameObject vanishEffect;



    protected virtual void Start()
    {
        if (s_bugKillerPrefab == null) s_bugKillerPrefab = Resources.Load<GameObject>("BugKiller");
        if (s_warningPrefab == null) s_warningPrefab = Resources.Load<GameObject>("Warning");
        if (s_economyManager == null)
        {
            GameObject em = GameObject.Find("EconomyManager");
            if (em != null) s_economyManager = em.GetComponent<EconomyManager>();
        }
        if (s_grid == null)
        {
            GameObject g = GameObject.Find("Grid");
            if (g != null) s_grid = g.GetComponent<Grid>();
        }
        if (s_mainCamera == null) s_mainCamera = Camera.main;

        economyManager = s_economyManager;
        grid = s_grid;

        childSpriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        childMaterials = new Material[childSpriteRenderers.Length];
        for (int i = 0; i < childSpriteRenderers.Length; i++)
        {
            childMaterials[i] = childSpriteRenderers[i].material;
        }
        //Debug.Log(childMaterials.Length);

        speed = speed * (1f - grid.GetBugSpeedDecreasement());
        float mul = ModManager.Instance
        ? ModManager.Instance.GetMul(StatId.BugSpeedMul, -1)
        : 1f;
        speed = speed * mul;

        InitBug();
    }

    protected virtual void InitBug()
    {
        InitRandomPos();
        StartCoroutine(Warning());
    }

    protected virtual void Update()
    {
        if (Input.GetMouseButtonDown(0) && !ClickRouter.Instance.IsBlockedByUI && grid.GetIsBreeding())
        {
            if (s_mainCamera == null) s_mainCamera = Camera.main;
            Vector3 mousePos = s_mainCamera.ScreenToWorldPoint(new Vector3 (Input.mousePosition.x, Input.mousePosition.y, 0));
            //Debug.Log(mousePos);
            if (Mathf.Abs(transform.position.x - mousePos.x) < hitRange && Mathf.Abs(transform.position.y - mousePos.y) < hitRange)
            { 
                StartCoroutine(HitBug());
            }
        }
    }

    protected virtual IEnumerator Warning()
    {
        ShowWarningSign();
        yield return new WaitForSeconds(1.0f);
        DestroyWarningSign(2.0f);
        movingCoroutine = StartCoroutine(Moving());
    }

    protected virtual IEnumerator Moving()
    {
        yield return null;
    }

    public virtual IEnumerator MoveToNepenthes(GameObject nepenthes)
    {
        StopCoroutine(movingCoroutine);
        while(true)
        {
            if (!grid.GetIsBreeding() || isDie || isHit)
            {
                yield return null;
                continue;
            }
            if (nepenthes == null)
                break;
            MoveToward(nepenthes.transform.position, speed * 0.5f);
            yield return null;
        }
        //    浥   ٽ  ƾ 
        movingCoroutine = StartCoroutine(Moving());
        yield return null;
    }

    private void ShowWarningSign()
    {
        WarningSign = Instantiate(s_warningPrefab);
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
        WarningSign.transform.position = pos;
        return;
        
    }

    protected void DestroyWarningSign(float delay)
    {
        Destroy(WarningSign, delay);
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

        if(plant == null) // ��ǥ�� ã�� ���� (�̹� �Ĺ��� ���� ���. ���� ����)
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
                int baseGold = 100;
                int additionalGold = grid != null ? grid.AdditionalNepenthesGold : 0;
                economyManager.AddGold(baseGold + additionalGold);
                StartCoroutine(KillBug());
                return;
            }
            
            if (plant.Die(DeathCause.Bug, this))
            {
                GameEvents.RaisePeaDiedByBug();
            }
            eatingPlant = true;
        }
    }

    protected void MoveToward(Vector2 targetPos, float s = 0f)
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

        Vector2 newPosition2D = Vector2.MoveTowards(currentPos, targetPos, ((s == 0) ? speed : s) * Time.deltaTime);
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
            grid.AddAdditionalPestResistance(0.002f);
            isDie = true;
            GameEvents.RaiseBugKilled();
            CodexProgress.AddBugKill(GetType().Name); // 도감: 벌레 발견 + 종류별/전체 잡은 수
            economyManager.AddGold(Mathf.Max(0, gold + grid.GetAdditionalBugGold() - DawnSystem.Current.bugPriceReduction)); // 새벽: 벌레 기본 가격 감소
            yield return StartCoroutine(Vanish());
            Destroy(this.gameObject);
        }
    }

    protected IEnumerator ShowBugKiller()
    {
        bugKiller = Instantiate(s_bugKillerPrefab);
        if (s_mainCamera == null) s_mainCamera = Camera.main;
        Vector3 mousePos = s_mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0));
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

    public IEnumerator CantHitForOneSecond() // �̰� Ű�� 1�ʵ��� ������ ��ġ���� �ʽ��ϴ�..
    {
        isHit = true;
        yield return new WaitForSeconds(1f);
        isHit = false;
    }
}
