using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISelectable
{
    CamMover CamDirector { get; }
    bool IsSelected { get; }
    bool SelectSwitch(System.Func<Unit, bool> releaseSelecterFunc, SelectionForceType forceSelection);

    System.Func<Unit, bool> ReleaseFromSelecterCam { get; }
}

public enum SelectionForceType {
    Default,
    Deselect,
    Select
}