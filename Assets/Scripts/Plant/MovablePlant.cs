using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(PlantCurseManager))]
public abstract class MovablePlant : Plant
{
    private static readonly HashSet<MovablePlant> activePlants = new HashSet<MovablePlant>();
    public static bool IsAnyPlantHeldOrDragged => activePlants.Count > 0;

    //이동을 위한 변수
    private float holdTime = 0f;
    private bool isHolding = false;
    private bool isDragging = false;
    private const float HoldDuration = 0.7f;

    //옮기기 게이지
    [SerializeField] private Image holdGaugeImage;
    [SerializeField] private GameObject holdGaugeCanvasObj;

    // 스카우터, 골드 스카우터 표시
    [SerializeField] private ScouterShowController scouter;
    [SerializeField] private bool hasGoldScouter;
    [SerializeField] private bool hasResistanceScouter;
    //[SerializeField] private GameObject resistanceScouterImage;
    //[SerializeField] private GameObject goldScouterImage;

    private ParticleSystem waterParticle;

    private bool isReallyMovable = true;

    [SerializeField] private GameObject iceEffect;

    public override bool IsMovable => isReallyMovable;

    public override void Init(int gridIndex, Grid grid)
    {
        base.Init(gridIndex, grid);
        waterParticle = transform.Find("Water").GetComponent<ParticleSystem>();
        if (isOnGoldenSoil())
            stemController.SetGold(true);
        if (holdGaugeCanvasObj) holdGaugeCanvasObj.SetActive(false);
        if(GameManager.Instance)
            CheckResistanceScouterImage(GameManager.Instance.enemyController.CurrentWave.WaveType);
    }

    protected void Update()
    {
        bool stateChanged = false;

        if (isHolding)
        {
            if (ClickRouter.Instance.IsBlockedByUI)
            {
                isDragging = false;
                isHolding = false;
                holdTime = 0f;
                holdGaugeImage.fillAmount = 0f;
                holdGaugeCanvasObj.SetActive(false);
                grid.TryPlacePlant(this, Input.mousePosition);
                stateChanged = true;
            }
            holdTime += Time.deltaTime;
            holdGaugeImage.fillAmount = Mathf.Clamp01(holdTime / HoldDuration);

            if (holdTime >= HoldDuration && !isDragging)
            {
                StartDragging();
                holdGaugeCanvasObj.SetActive(false);
            }
        }

        if (isDragging)
        {
            if (!grid.GetIsBreeding() || isDying)
                grid.TryPlacePlant(this, Input.mousePosition);
            else
            {
                FollowMouse();
                grid.HighlightHoveredTile(grid.GetGridIndexFromPosition(Input.mousePosition));
            }
        }

        if (!grid.GetIsBreeding() || isFrozen)
        {
            if (isDragging || isHolding)
            {
                isDragging = false;
                isHolding = false;
                holdTime = 0f;
                holdGaugeImage.fillAmount = 0f;
                holdGaugeCanvasObj.SetActive(false);
                stateChanged = true;
            }
        }

        if (stateChanged)
        {
            UpdateActiveState();
        }
    }

    private void OnMouseDown()
    {
        if (!CanMove() || ClickRouter.Instance.IsBlockedByUI || grid.isDraggingShovel || isFrozen || grid.IsPointerOverBreedButton())
            return;
        holdTime = 0f;
        isHolding = true;
        holdGaugeImage.fillAmount = 0f;
        holdGaugeCanvasObj.SetActive(true);
    }

    private void OnMouseUp()
    {
        if (isDragging)
        {
            grid.ClearHoverHighlight();
            grid.TryPlacePlant(this, Input.mousePosition);
            FenceUIManager.Instance.SetFenceElements(plantID, this);
            TryRootByDawn(); // 새벽: 이동 시 확률로 뿌리
        }
        else
        {
            if (ClickRouter.Instance.IsBlockedByUI || grid.isDraggingShovel || isFrozen || grid.IsPointerOverBreedButton()) return;
            grid.RequestBreedSelect(this.gameObject);
        }

        isDragging = false;
        isHolding = false;
        holdTime = 0f;
        holdGaugeImage.fillAmount = 0f;
        holdGaugeCanvasObj.SetActive(false);
        UpdateActiveState();
    }

    private void StartDragging()
    {
        Debug.Log("식물 들기 성공");
        isDragging = true;

        Vector3 pos = transform.position;
        transform.position = new Vector3(pos.x, pos.y, pos.z - 0.1f);
        UpdateActiveState();
    }

    public override bool Die(DeathCause cause = DeathCause.Generic, Bug killer = null)
    {
        HideGoldScouterImage();
        HideResistanceScouterImage();
        return base.Die(cause, killer);
    }
    private void FollowMouse()
    {
        Vector3 screenPos = Input.mousePosition;
        screenPos.z = Camera.main.WorldToScreenPoint(transform.position).z;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        transform.position = worldPos;
    }

    public void SetMovable(bool value, PlantValueChangeReason reason = PlantValueChangeReason.None)
    {
        int previousSellingPrice = GetSellingPrice();
        isReallyMovable = value;
        RefreshSellingPriceAndNotify(previousSellingPrice, reason);
    }

    // 새벽: 등장/이동 시 확률로 뿌리를 내려 이동 불가가 됨
    private void TryRootByDawn()
    {
        if (!isReallyMovable) return; // 이미 뿌리내림
        float chance = DawnSystem.Current.rootChancePercent;
        if (chance <= 0f) return;
        if (UnityEngine.Random.Range(0f, 100f) < chance)
        {
            SetMovable(false, PlantValueChangeReason.Rooted); // TODO: 뿌리 시각효과

            // 특수(임시땅콩C): 뿌리를 내리면 모든 저항력 40%p 증가
            if (SpecialItemSystem.Has("peanut_special_12"))
                for (int i = 0; i < Wave.NumberOfWave; i++)
                    ChangeResistance(i, 0.4f);
        }
    }

    /// <summary>새 식물이 실제 흙 위치에 놓인 뒤 등장 시 뿌리 판정을 수행한다.</summary>
    public void TryRootAfterPlacement()
    {
        TryRootByDawn();
    }

    public void CheckResistanceScouterImage(WaveType wave)
    {
        if (!grid.GetHasReistanceScouter() || !grid.GetIsScouterOn())
            return;
        if(GetResistanceValue((int)wave) <= 0.5f)
        {
            ShowResistanceScouterImage();
        }
        else
        {
            HideResistanceScouterImage();
        }
    }

    public void ShowGoldScouterImage()
    {
        //goldScouterImage.SetActive(true);
        hasGoldScouter = true;
        SetScouter();
    }

    public void HideGoldScouterImage()
    {
        //goldScouterImage.SetActive(false);
        hasGoldScouter = false;
        SetScouter();
    }

    public void ShowResistanceScouterImage()
    {
        //resistanceScouterImage.SetActive(true);
        hasResistanceScouter = true;
        SetScouter();
    }

    public void HideResistanceScouterImage()
    {
        //resistanceScouterImage.SetActive(false);
        hasResistanceScouter = false;
        SetScouter();
    }

    private void SetScouter()
    {
        scouter.SetScouter(hasGoldScouter, hasResistanceScouter);
    }

    public void PlayWaterParticle()
    {
        waterParticle.Play();
    }

    public void SetIceEffect(bool val)
    {
        iceEffect.SetActive(val);
    }

    private void UpdateActiveState()
    {
        if (isDragging)
        {
            activePlants.Add(this);
        }
        else
        {
            activePlants.Remove(this);
        }
    }

    private void OnDisable()
    {
        isHolding = false;
        isDragging = false;
        activePlants.Remove(this);
    }
}
