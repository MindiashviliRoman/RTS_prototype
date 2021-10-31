using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationFuncs : MonoBehaviour
{
    [SerializeField] public System.Action unitDaying;

    private void CallUnitDaying() {
        unitDaying();
    }
}
