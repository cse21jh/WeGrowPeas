using UnityEngine;


public interface ColorSetterInterface
{
    void Refresh();

    void SetColor(float time);
}

public enum LightColorType
{
    All,
    Natural,
    Rain
}


[ExecuteInEditMode]
public class LightColorController : MonoBehaviour
{
    [Range(0, 1)] public float time;

    private ColorSetterInterface[] setters;

    [SerializeField] private LightColorSetter[] naturalSetters;
    [SerializeField] private LightColorSetter[] rainSetters;

    [SerializeField] private LightColorType currentType = LightColorType.All;

    private float currentTime = 0;

    public float timeValue => currentTime;

    public void GetSetters()
    {
        setters = GetComponentsInChildren<ColorSetterInterface>();
        foreach (var setter in setters)
        {
            setter.Refresh();
        }
    }

    private void OnEnable()
    {
        time = 0;
        GetSetters();
        UpdateSetters();
    }

    private void OnDisable()
    {
        time = 0;
        UpdateSetters();
    }

    private void Update()
    {
        if (currentTime != time)
        {
            UpdateSetters();
        }
    }

    public void UpdateType(LightColorType type)
    {
        currentType = type;

        switch (currentType)
        {
            case LightColorType.All:
                setters = GetComponentsInChildren<ColorSetterInterface>();
                break;
            case LightColorType.Natural:
                setters = naturalSetters;
                break;
            case LightColorType.Rain:
                setters = rainSetters;
                break;
        }

        UpdateSetters();
    }

    public void UpdateSetters()
    {
        currentTime = time;

        foreach (var setter in setters)
        {
            setter.SetColor(time);
        }
    }
}



