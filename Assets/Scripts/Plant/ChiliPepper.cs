using UnityEngine;
using UnityEngine.UI;

public class ChiliPepper : Plant
{
    protected override float GetResistanceValue(WaveType wave)
    {
        return 1f;
    }
    public override float GetResistanceBasedOnGenetics(int genetics)
    {
        return 1f;
    }

    public override void ContactBug(Bug bug)
    {
        StartCoroutine(bug.KillBug());
    }



    // 테스트용 더미 옮기기 가능하도록

    private float holdTime = 0f;
    private bool isHolding = false;
    private bool isDragging = false;
    private const float HoldDuration = 0.7f;

    [SerializeField] private Image holdGaugeImage;
    [SerializeField] private GameObject holdGaugeCanvas;

    protected void Update()
    {
        if (isHolding)
        {
            holdTime += Time.deltaTime;
            holdGaugeImage.fillAmount = Mathf.Clamp01(holdTime / HoldDuration);

            if (holdTime >= HoldDuration && !isDragging)
            {
                StartDragging();
                holdGaugeCanvas.SetActive(false);
            }
        }

        if (isDragging)
        {
            if (!grid.GetIsBreeding())
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
            holdGaugeCanvas.SetActive(false);
        }
    }

    private void OnMouseDown()
    {
        if (!grid.GetIsBreeding())
            return;
        holdTime = 0f;
        isHolding = true;
        holdGaugeImage.fillAmount = 0f;
        holdGaugeCanvas.SetActive(true);
    }

    private void OnMouseUp()
    {
        if (isDragging)
        {
            grid.TryPlacePlant(this, Input.mousePosition);
        }

        isDragging = false;
        isHolding = false;
        holdTime = 0f;
        holdGaugeImage.fillAmount = 0f;
        holdGaugeCanvas.SetActive(false);
    }

    private void StartDragging()
    {
        Debug.Log("식물 들기 성공");
        isDragging = true;

        Vector3 pos = transform.position;
        transform.position = new Vector3(pos.x, pos.y, pos.z - 0.1f);
    }

    private void FollowMouse()
    {
        Vector3 screenPos = Input.mousePosition;
        screenPos.z = Camera.main.WorldToScreenPoint(transform.position).z;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        transform.position = worldPos;
    }

}
