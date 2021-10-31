using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildBehaviour : MonoBehaviour, IDataBar
{
    public DataBarLinks InfoBarLinks { private set; get; }

    [SerializeField] private float timerUnitCreating = 5f;//sec
    [SerializeField] private GameObject menu;
    private float startTime;
    private int cntCreatingUnits = 0;
    private int createdCntUnits = 0;
    private float progressCurUnit = 0f;
    private float progressForCntUnit = 0f;
    private Vector3 buildSize;
    private Vector3 buildPos;

    private void Awake() {
        buildSize = this.GetComponent<MeshFilter>().sharedMesh.bounds.size;
        buildPos = this.transform.position;
        InfoBarLinks = this.transform.GetChild(0).GetComponent<DataBarLinks>();
        SetVisibleProgressBar(false);
    }
    // Start is called before the first frame update

    // Update is called once per frame
    void Update()
    {
        SetViewBarToCamera(CamMover.cam);
    }

    public void StartCreatingPlayerUnits(int cnt) {
        SetVisibleProgressBar(true);
        cntCreatingUnits = cnt;
        createdCntUnits = 0;
        startTime = Time.time;
        StartCoroutine(CoroutineCreatingAllUnits());
    }

    private IEnumerator CoroutineCreatingAllUnits() {
        do {
            startTime = Time.time;
            do {
                progressCurUnit = Mathf.Clamp((Time.time - startTime) / timerUnitCreating, 0, 1);
                SetProgressBarValue((progressCurUnit + createdCntUnits) / cntCreatingUnits);
                yield return null;
            } while (progressCurUnit < 1f);
            CreateUnit();
        } while (cntCreatingUnits > createdCntUnits);
        Debug.Log("Finish building of units");
        SetVisibleProgressBar(false);
    }

    private void SetVisibleProgressBar(bool flg) {
        InfoBarLinks.gameObject.SetActive(flg);
    }

    private GameObject CreateUnit() {
        Unit curUnit = UnitCreator.Inst.SpawnPlayer(buildPos, Vector3.one, Quaternion.identity);
        GameObject curUnitGO = curUnit.gameObject;
        Vector3 curUnitSize = UnitCreator.Inst.GetSizeEnemy();
        curUnit.transform.position = new Vector3(buildPos.x, curUnitSize.y/2, buildPos.z);
        createdCntUnits++;
        return curUnitGO;
    }

    public void SetProgressBarValue(float f) {
        InfoBarLinks.SetValue(f);
    }

    private void SetViewBarToCamera(Camera cam) {
//      we can call SetViewBarToCamera when camera change rotation......
        InfoBarLinks.transform.rotation = Quaternion.Euler(cam.transform.rotation.eulerAngles);
    }
}
