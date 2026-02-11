using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class MovablePlant : Plant
{
    //이동을 위한 변수
    private float holdTime = 0f;
    private bool isHolding = false;
    private bool isDragging = false;
    private const float HoldDuration = 0.7f;

    //옮기기 게이지
    [SerializeField] private Image holdGaugeImage;
    [SerializeField] private GameObject holdGaugeCanvasObj;

    // 스카우터, 골드 스카우터 표시
    [SerializeField] private GameObject resistanceScouterImage;
    [SerializeField] private GameObject goldScouterImage;

    private bool isReallyMovable = true;

    public override bool IsMovable => isReallyMovable;

    public override void Init(int gridIndex, Grid grid)
    {
        base.Init(gridIndex, grid);
        if (holdGaugeCanvasObj) holdGaugeCanvasObj.SetActive(false);
        if(GameManager.Instance)
            CheckResistanceScouterImage(GameManager.Instance.enemyController.CurrentWave.WaveType); 
    }

    protected void Update()
    {
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
                FollowMouse();
        }

        if (!grid.GetIsBreeding())
        {
            isDragging = false;
            isHolding = false;
            holdTime = 0f;
            holdGaugeImage.fillAmount = 0f;
            holdGaugeCanvasObj.SetActive(false);
        }
    }

    private void OnMouseDown()
    {
        if (!CanMove() || ClickRouter.Instance.IsBlockedByUI || grid.isDraggingShovel)
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
            grid.TryPlacePlant(this, Input.mousePosition);
            FenceUIManager.Instance.SetFenceElements(plantID, this);
        }
        else
        {
            if (ClickRouter.Instance.IsBlockedByUI || grid.isDraggingShovel) return;
            grid.RequestBreedSelect(this.gameObject);
        }

        isDragging = false;
        isHolding = false;
        holdTime = 0f;
        holdGaugeImage.fillAmount = 0f;
        holdGaugeCanvasObj.SetActive(false);
    }

    private void StartDragging()
    {
        Debug.Log("식물 들기 성공");
        isDragging = true;

        Vector3 pos = transform.position;
        transform.position = new Vector3(pos.x, pos.y, pos.z - 0.1f);
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

    public void SetMovable(bool value)
    {
        isReallyMovable = value;
    }

    public void CheckResistanceScouterImage(WaveType wave)
    {
        if (!grid.GetHasReistanceScouter())
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
        goldScouterImage.SetActive(true);
    }

    public void HideGoldScouterImage()
    {
        goldScouterImage.SetActive(false);
    }

    public void ShowResistanceScouterImage()
    {
        resistanceScouterImage.SetActive(true);
    }

    public void HideResistanceScouterImage()
    {
        resistanceScouterImage.SetActive(false);
    }
}
