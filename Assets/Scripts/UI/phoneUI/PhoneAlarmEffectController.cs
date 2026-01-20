using DG.Tweening;
using System.Collections;
using System.Net.NetworkInformation;
using UnityEngine;

public class PhoneAlarmEffectController : MonoBehaviour
{
    [SerializeField] private GameObject maxRoot;
    [SerializeField] private GameObject minRoot;

    [SerializeField] private float maxRotateZ = 5f;
    [SerializeField] private float minRotateZ = 15f;

    [SerializeField] private float alarmDuration = 5f;
    [SerializeField] private float strength = 10f;
    [SerializeField] private int vibrato = 10;
    [SerializeField] private float alarmInterval = 1f;



    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            AlarmPermanent();
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            AlarmImpermanent();
        }
    }


    public void AlarmPermanent()
    {
        StartCoroutine(AlarmEffectCoroutine(0));
    }

    public void AlarmImpermanent()
    {
        StartCoroutine(AlarmEffectCoroutine(1));
    }

    private IEnumerator AlarmEffectCoroutine(int count)
    {
        minRoot.transform.DOShakeRotation(alarmDuration, new Vector3(0, 0, strength), vibrato, 90, false, ShakeRandomnessMode.Harmonic);
        //maxRoot.transform.DOShakeRotation(alarmDuration, new Vector3(0, 0, strength), vibrato, 90, false, ShakeRandomnessMode.Harmonic);

        count--;

        yield return new WaitForSecondsRealtime(alarmInterval);

        if (count != 0)
            StartCoroutine(AlarmEffectCoroutine(count));
    }

}
