using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class Upgrade
{
    public virtual string Name => null;
    public virtual string Explanation => null;
    public virtual Sprite Icon { get; }
    public virtual int MaxAmount => 0;
    public virtual int UnlockStage => 0;
    public virtual void OnSelectAction() { }

    public int Amount = 0;
}
