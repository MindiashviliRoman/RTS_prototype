using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DataBarLinks : MonoBehaviour
{
    [SerializeField] private Image ImageLink;
    [SerializeField] private TMP_Text TextLink;

    public void SetValue(float val, float mult = 100, string sunit = "%") {
        ImageLink.fillAmount = val;
        TextLink.text = Mathf.Round((val * mult)).ToString() + sunit;
    }
}
