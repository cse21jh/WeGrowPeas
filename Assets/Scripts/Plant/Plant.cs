using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using Unity.VisualScripting;

// �����̳� ���̺� �߰� �� GetResistantValue �� ���� �� Initialize Trait ���� ���׷� ��� �߰� �ʿ�.

public abstract class Plant : MonoBehaviour
{
    public string speciesname;
    protected List<GeneticTrait> traits = new List<GeneticTrait>();
    protected Dictionary<CompleteTraitType, float> additionalResistance = new Dictionary<CompleteTraitType, float>();

    public int gridIndex { get; private set; }
    private Grid grid;

    //�̵��� ���� ����
    private float holdTime = 0f;
    private bool isHolding = false;
    private bool isDragging = false;
    private const float HoldDuration = 1.5f;

    //�ű�� ������
    [SerializeField] private Image holdGaugeImage;
    [SerializeField] private GameObject holdGaugeCanvas;

    //���� ȿ�� ����
    [SerializeField] private float dissolveDuration = 1.0f; // ���� �ִϸ��̼� ���� �ð�
    private SpriteRenderer[] childSpriteRenderers;
    private Material[] childMaterials;
    private int dissolveAmountID = Shader.PropertyToID("_DissolveAmount");
    [SerializeField] private GameObject vanishEffect;

    [SerializeField] private GameObject appearEffect;



    public virtual void Init(int gridIndex, Grid grid)
    {
        this.gridIndex = gridIndex;
        this.grid = grid;

        childSpriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        childMaterials = new Material[childSpriteRenderers.Length];
        for (int i = 0; i < childSpriteRenderers.Length; i++)
        {
            childMaterials[i] = childSpriteRenderers[i].material; 
        }

        StartCoroutine(Appear());
    }

    public virtual void SetTrait(List<GeneticTrait> newTraits)
    {
        traits = newTraits;
    }

    public virtual List<GeneticTrait> GetGeneticTrait()
    {
        return traits;
    }
    protected virtual void OnMouseEnter()
    {
        if (ClickRouter.Instance.IsBlockedByUI) return;

        UIPlantStat.Instance.ShowInfo(speciesname, traits);
    }

    protected virtual void OnMouseExit()
    {
        UIPlantStat.Instance.HideInfo();
    }
    
    //�Ĺ� �̵�
    private void OnMouseDown()
    {
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
        else
        {
            grid.RequestBreedSelect(this.gameObject);
        }

        isDragging = false;
        isHolding = false;
        holdTime = 0f;
        holdGaugeImage.fillAmount = 0f;
        holdGaugeCanvas.SetActive(false);
    }
    protected virtual void Update()
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
            FollowMouse();
        }
    }
    private void StartDragging()
    {
        Debug.Log("�Ĺ� ��� ����");
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

    public void SetGridIndex(int idx)
    {
        gridIndex = idx;
    }

    public bool CanResist(WaveType wave) // if can't resist, Call Die()
    {
        int randomNumber = UnityEngine.Random.Range(0, 100);
        if (randomNumber <= (int)(GetResistanceValue(wave) * 100))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    protected float GetResistanceValue(WaveType wave)
    {
        CompleteTraitType traitType = CompleteTraitType.None;
        float defaultResistance = 0.0f;
        switch(wave)
        {
            case WaveType.Wind: traitType = CompleteTraitType.WindResistance; break;
            case WaveType.Flood: traitType = CompleteTraitType.FloodResistance; break;
            case WaveType.Pest: traitType = CompleteTraitType.PestResistance; break;
            case WaveType.Cold: traitType = CompleteTraitType.ColdResistance; break;
            case WaveType.HeavyRain: traitType = CompleteTraitType.HeavyRainResistance; break;
            case WaveType.Aging: traitType = CompleteTraitType.NaturalDeath; break;
                // Ư�� �߰��Ǹ� �߰�
        }

        foreach(GeneticTrait g in traits)
        {
            if(g.traitType == traitType)
                return g.resistance + g.additionalResistance;
        }
        
        return defaultResistance;
    }

    public virtual void Die()
    {
        StartCoroutine(Vanish());
        UIPlantStat.Instance.HideInfo();
        grid.ClearGridIndex(gridIndex);
        Destroy(this.gameObject, dissolveDuration);
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

            for(int i = 0; i < childMaterials.Length; i++)
            {
                childMaterials[i].SetFloat(dissolveAmountID, lerpedDissolve);
            }


            yield return null;
        }
    }

    private IEnumerator Appear()
    {
        float elapsedTime = 0f;
        while (elapsedTime < dissolveDuration)
        {
            elapsedTime += Time.deltaTime;

            float lerpedDissolve = Mathf.Lerp(1.1f, 0f, elapsedTime / dissolveDuration);

            for (int i = 0; i < childMaterials.Length; i++)
            {
                childMaterials[i].SetFloat(dissolveAmountID, lerpedDissolve);
            }


            yield return null;
        }
    }

    public virtual void MakeSelectedSprite()
    {
        ChangeLayerOfAllChild(gameObject, "Outline"); // "Outline" ���̾�� ����
    }

    public virtual void MakeDefaultSprite()
    {
        ChangeLayerOfAllChild(gameObject, "Default"); // "Default" ���̾�� ����
    }

    private void ChangeLayerOfAllChild(GameObject obj, string layerName)
    {
        if(obj.name == "shadow" || obj.name == "HoldGaugeUI")
        {
            return; // �׸��� �Ǵ� ������ ������Ʈ�� ���̾� �������� ����
        }

        obj.layer = LayerMask.NameToLayer(layerName);

        foreach (Transform child in obj.transform)
        {
            ChangeLayerOfAllChild(child.gameObject, layerName);
        }
    }


    public void AddAdditionalResistance(CompleteTraitType traitType, float value)
    {
        for (int i = 0; i < traits.Count; i++)
        {
            if (traits[i].traitType == traitType)
            {
                traits[i] = new GeneticTrait(traitType, traits[i].resistance, traits[i].genetics, traits[i].additionalResistance + value);

                if (traits[i].additionalResistance >= 0.15f) traits[i] = new GeneticTrait(traitType, traits[i].resistance, traits[i].genetics, 0.15f);

                return;
            }
        }
    }


}
