using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XPPellet : Pellet
{
    [SerializeField] private int ammount;
    protected override void Grab(Wyzard wyzard)
    {
        wyzard.AddXP(ammount);
    }
}
