using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public interface ColorSetterInterface
{
    void Refresh();

    void SetColor(float time);
}

public enum LightColorType
{
    None,
    Natural,
    Rain,
    Drought,
    Night,
    Day
}


[ExecuteInEditMode]
public class LightColorController : MonoBehaviour
{
    [Range(0, 1)] public float time;

    private List<LightColorSetter> setters = new List<LightColorSetter>();

    [SerializeField] private List<LightColorSetter> naturalSetters = new List<LightColorSetter>();
    [SerializeField] private List<LightColorSetter> rainSetters = new List<LightColorSetter>();
    [SerializeField] private List<LightColorSetter> droughtSetters = new List<LightColorSetter>();

    [SerializeField] private List<LightColorSetter> nightSetters = new List<LightColorSetter>();
    [SerializeField] private List<LightColorSetter> daySetters = new List<LightColorSetter>();

    [SerializeField] private LightColorType currentType = LightColorType.None;

    private float currentTime = 0;

    public float timeValue => currentTime;

    public void GetSetters()
    {
        setters.Clear();
        naturalSetters.Clear();
        rainSetters.Clear();
        droughtSetters.Clear();
        nightSetters.Clear();
        daySetters.Clear();

        setters = GetComponentsInChildren<LightColorSetter>().ToList();
        Debug.Log($"Found {setters.Count} LightColorSetters");


        foreach (var setter in setters)
        {
            setter.Refresh();

            switch (setter.type)
            {
                case LightColorType.Natural:
                    naturalSetters.Add(setter);
                    break;
                case LightColorType.Rain:
                    rainSetters.Add(setter);
                    break;
                case LightColorType.Drought:
                    droughtSetters.Add(setter);
                    break;
                case LightColorType.Night:
                    nightSetters.Add(setter);
                    break;
                case LightColorType.Day:
                    daySetters.Add(setter);
                    break;
            }
        }
    }

    private void OnEnable()
    {
        time = 0;
        GetSetters();
        UpdateSetters();

        foreach (var setter in naturalSetters)
        {
            setter.SetColor(0);
        }
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
            case LightColorType.None:
                setters = null;
                break;
            case LightColorType.Natural:
                setters = naturalSetters;
                break;
            case LightColorType.Rain:
                setters = rainSetters;
                break;
            case LightColorType.Drought:
                setters = droughtSetters;
                break;
            case LightColorType.Night:
                setters = nightSetters;
                break;
            case LightColorType.Day:
                setters = daySetters;
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



