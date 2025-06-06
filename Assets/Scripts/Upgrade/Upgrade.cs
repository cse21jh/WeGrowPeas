using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Upgrade : MonoBehaviour
{
    public virtual string Name => null;
    public virtual string Explanation => null;
    public virtual int MaxAmount => 0; 
    public virtual void OnSelectAction() { }

    public int Amount = 0;
}
