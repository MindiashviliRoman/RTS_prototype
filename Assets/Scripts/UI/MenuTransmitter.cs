using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuTransmitter: MonoBehaviour
{
    [SerializeField] public Slider CntUnitsSlider;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] public Button ConfirmButt;
    public void SetValueForSlider() {
        int value;
        if(int.TryParse(inputField.text, out value)) {
            float fValue = (float)value;
            fValue = Mathf.Clamp(fValue, CntUnitsSlider.minValue, CntUnitsSlider.maxValue);
            CntUnitsSlider.SetValueWithoutNotify(fValue);
            inputField.SetTextWithoutNotify(fValue.ToString());
        }
    }
    public void SetValueForInputField() {
        inputField.SetTextWithoutNotify(CntUnitsSlider.value.ToString());
    }
}
