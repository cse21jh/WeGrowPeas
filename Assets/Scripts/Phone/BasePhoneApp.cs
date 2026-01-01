using UnityEngine;

public abstract class BasePhoneApp : MonoBehaviour
{
    protected PhoneManager Phone { get; private set; }

    public virtual string Title => null;

    public virtual void OnCreate(PhoneManager phone)
    {
        Phone = phone;
    }

    public virtual void OnShow() { }
    public virtual void OnHide() { }
}