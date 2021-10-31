using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class MenuHolder : MonoBehaviour {
    public static MenuHolder Inst;
    [SerializeField] private GameObject buildingMenuPrefab;
    [SerializeField] private GameObject dataBarPrefab;
    private MenuTransmitter menuTransmitter;
    private Button buildingButtOk;
    private GameObject buildingMenu;
    public bool IsActive{private set; get;}
    private void Awake() {
        if (Inst == null) {
            Inst = this;
        }
    }

    delegate int returnFunc();
    public GameObject BuildingMenuChangeActive(BuildBehaviour buildB) {
        if (!IsActive) {
            if (menuTransmitter == null) {
                buildingMenu = GameObject.Instantiate(buildingMenuPrefab, this.gameObject.transform);
                menuTransmitter = buildingMenu.GetComponent<MenuTransmitter>();
                buildingButtOk = menuTransmitter.ConfirmButt;
            }
            buildingButtOk.onClick.RemoveAllListeners();
            buildingButtOk.onClick.AddListener(delegate { buildB.StartCreatingPlayerUnits((int)menuTransmitter.CntUnitsSlider.value); });
            buildingButtOk.onClick.AddListener(delegate { ActivateMenu(false); });
            ActivateMenu(true);
        } else {
            ActivateMenu(false);
        }
        return buildingMenu;
    }

    private void ActivateMenu(bool flg) {
        IsActive = flg;
        buildingMenu.SetActive(flg);
    }

    public GameObject CreateDataBar(string name, Transform parentTransform) {
        GameObject dataBar = GameObject.Instantiate(dataBarPrefab, parentTransform, true);
        return dataBar;
    }
}
