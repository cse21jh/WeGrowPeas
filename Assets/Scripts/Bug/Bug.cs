using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Bug : MonoBehaviour
{
    protected Grid grid;
    protected EconomyManager economyManager;

    protected int gold = BugRules.BaseGold;

    protected virtual BugMove Move => BugMove.Chase;
    /// <summary>식물에 닿으면 멈춰서 먹는가.</summary>
    protected virtual bool StopsToEat => true;
    protected BugMotion Motion { get; private set; }
    protected IBugTargets Targets => grid;

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

    protected Coroutine movingCoroutine;

    //각종 효과 관련
    [SerializeField] private float dissolveDuration = 1.0f; // 분해 애니메이션 지속 시간
    private SpriteRenderer[] childSpriteRenderers;
    private Material[] childMaterials;
    [SerializeField] private GameObject vanishEffect;



    protected virtual void Start()
    {
        if (s_bugKillerPrefab == null) s_bugKillerPrefab = Resources.Load<GameObject>(ResourcePaths.BugKillerPrefab);
        if (s_warningPrefab == null) s_warningPrefab = Resources.Load<GameObject>(ResourcePaths.WarningPrefab);
        if (s_economyManager == null) s_economyManager = FindAnyObjectByType<EconomyManager>();
        if (s_grid == null) s_grid = FindAnyObjectByType<Grid>();
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

        float mul = ModManager.Instance
        ? ModManager.Instance.GetMul(StatId.BugSpeedMul, -1)
        : 1f;
        speed = BugRules.GetSpeed(speed, grid.Modifiers, mul);
        Motion = new BugMotion(Move, speed, StopsToEat) { Position = transform.position };

        InitBug();
    }

    protected virtual void InitBug()
    {
        Motion.PlaceAtRandomEdge();
        transform.position = Motion.Position;
        StartCoroutine(Warning());
    }

    protected virtual void Update()
    {
        if (Input.GetMouseButtonDown(0) && !ClickRouter.Instance.IsBlockedByUI && grid.Breeder.GetIsBreeding())
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
        yield return new WaitForSeconds(BugRules.WarningSeconds);
        DestroyWarningSign(2.0f);
        movingCoroutine = StartCoroutine(Moving());
    }

    protected virtual IEnumerator Moving()
    {
        Motion.Begin();
        yield return null;

        while (Motion.Step(Time.deltaTime, grid.Breeder.GetIsBreeding() && !isDie && !isHit, Targets))
        {
            ApplyMotion();
            yield return null;
        }
        Destroy(gameObject);
    }

    /// <summary>움직인 자리로 옮기고 진행 방향을 본다.</summary>
    protected void ApplyMotion()
    {
        Vector2 delta = Motion.Position - (Vector2)transform.position;
        if (delta == Vector2.zero) return;
        Face(delta);
        transform.position = new Vector3(Motion.Position.x, Motion.Position.y, transform.position.z);
    }

    private void Face(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffset);
    }

    /// <summary>네펜데스의 페로몬에 이끌린다.</summary>
    public void LureTo(Nepenthes nepenthes) => Motion?.LureTo(nepenthes.gridIndex);

    private void ShowWarningSign()
    {
        WarningSign = Instantiate(s_warningPrefab);
        Vector3 pos = this.transform.position;

        switch (Motion.Edge)
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


    protected virtual void OnTriggerEnter(Collider obj)
    {
        Plant plant = obj.gameObject.GetComponent<Plant>();
        if (plant != null && !isDie)
        {
            if (plant.GetType() == typeof(Nepenthes))
            {
                economyManager.AddGold(BugRules.GetNepenthesGold(grid.Modifiers));
                StartCoroutine(KillBug());
                return;
            }
            
            if (plant.Die(DeathCause.Bug, this))
            {
                GameEvents.RaisePeaDiedByBug();
            }
            Motion.Eat();
        }
    }

    protected void MoveToward(Vector2 targetPos, float s)
    {
        Vector2 current = transform.position;
        if (targetPos != current) Face(targetPos - current);
        Vector2 next = Vector2.MoveTowards(current, targetPos, s * Time.deltaTime);
        transform.position = new Vector3(next.x, next.y, transform.position.z);
    }

    protected virtual IEnumerator HitBug()
    {
        if (!isDie && !isHit)
        {
            SoundManager.Instance.PlayEffect(SoundIds.HitBug);
            isHit = true;
            yield return StartCoroutine(ShowBugKiller());
        }
    }

    public virtual IEnumerator KillBug()
    {
        if(!isDie)
        { 
            grid.killBugCount++;
            grid.Upgrades.AddAdditionalPestResistance(BugRules.PestResistancePerKill);
            isDie = true;
            GameEvents.RaiseBugKilled();
            if (GameManager.Instance?.Tutorial == null)
                CodexProgress.AddBugKill(GetType().Name);
            economyManager.AddGold(BugRules.GetKillGold(gold, grid.Modifiers));
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
                childMaterials[i].SetFloat(ShaderProps.DissolveAmount, lerpedDissolve);
            }


            yield return null;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawCube(transform.position, new Vector3(hitRange * 2,hitRange * 2));
    }

    public IEnumerator CantHitForOneSecond() // 이걸 키면 1초동안 벌레가 터치되지 않습니다..
    {
        isHit = true;
        yield return new WaitForSeconds(1f);
        isHit = false;
    }
}
