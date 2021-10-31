using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDataBar {
    DataBarLinks InfoBarLinks { get;}
    void SetProgressBarValue(float f);
}
