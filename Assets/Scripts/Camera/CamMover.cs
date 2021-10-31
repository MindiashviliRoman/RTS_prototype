using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CamMover : MonoBehaviour
{
    //camMoving parameters
    public static Camera cam { private set; get; }

    //For set destination point to units
    private HashSet<ISelectable> selectedObjs;
    private int unselectTapCnt = 2;
    private int curTapCnt = 0;


    private bool flgLockMove = false;
    private bool flgLockMem = false;

    //touch movable param
    private float minScreenQuadDistTouchMovable = 20f;
    private Vector2 startDpiPos = Vector2.zero;
    private Vector2 lastDpiPos = Vector2.zero;

    //Movement camera
    [SerializeField] private float moveTimeSetpoint = 3f;//sec
    [SerializeField] private float speed = 100f;// units per second
    private float moveTime = 0f;
    private float destTime = 0f;

    private bool isAutoMoving = false;
    private Vector3 strtPosCam = Vector3.zero;
    private Vector3 destPosCam = Vector3.zero;
    private bool flgConstSpeed = true; //true = const speed moving, false = const time moving

    //ScaleCamera
    private float startValueScale = 0f;
    private float lastValueScale = 0f;
    private float scaleDeltaWheel = 1f;
    private float scaleFactor = 0.1f;
    private bool isManyTouches = false;

    //movement cam block pick point
    private Vector3 strtPosInpt = Vector3.zero;
    private Vector3 lastPosInpt = Vector3.zero;

    //Many Clicks
    private float timerTripleClick = 0.5f; //sec
    private bool isTripleClickStart = false;
    private float destTripleTime = 0f;
    private int curCntClickForTriple = 0;

    private float timerDoubleClick = 0.3f;
    private bool isDoubleClickStart = false;
    private float destDoubleTimer = 0f;
    private int curCntClickForDouble = 0;

    //PickPoint timer
    private float timerPickPointToMove;
    private float curTimerPickPointToMove;




    private float distFromCentralPntToPlane = 0f;

    private Bounds camBounds = new Bounds(Vector3.zero, new Vector3(600, 0, 600));

 


    private void Awake() {
        cam = this.GetComponent<Camera>();
        CalculateDistToPlane();
        selectedObjs = new HashSet<ISelectable>();
        timerPickPointToMove = timerDoubleClick;
    }

    private void Update() {
        if (!isAutoMoving) {
            //pick
            PickOnViaPhysics(flgLockMove);
        }
    }
    private void LateUpdate() {
        if (!isAutoMoving) {

#if PLATFORM_ANDROID
            CamTouchScale();
#else
            CamWheelScale();
#endif

            //isManyClick
            //TripleClick
            bool isTripleClick = IsManyClick(3, timerTripleClick, ref isTripleClickStart, ref destTripleTime, ref curCntClickForTriple);
            if (isTripleClick) {
                if (cam.transform.position != strtPosCam) {
                    MoveTo(strtPosCam);
                }
            }
            //dualclick on ground will be selected all players on frustum of camera
            bool isDoubleClick = IsManyClick(2, timerDoubleClick, ref isDoubleClickStart, ref destDoubleTimer, ref curCntClickForDouble);
            if (isDoubleClick) {
                SelectAllVisibleUnits();
            }

            //move camera
            bool isTouchMove = false;
            if (!MenuHolder.Inst.IsActive && !isManyTouches) {
                isTouchMove = CamMoveViaPhysics();
            }

            flgLockMove = isTouchMove || isDoubleClick || isTripleClick;

            /*
            if (Application.platform == RuntimePlatform.Android) {
                CamTouchScale();
            } else {
                CamWheelScale();
            }
            */
        } else {
            MoveCam();
        }

    }


    private void CalculateDistToPlane() {
        float camAnglX = cam.transform.rotation.eulerAngles.x * Mathf.Deg2Rad;
        distFromCentralPntToPlane = cam.transform.position.y / Mathf.Sin(camAnglX);
    }

    private bool IsManyClick(int cntClick, float delaySetpoint, ref bool isStartedTimer, ref float curDestTime, ref int curClickedCnt) {
        if (Input.GetKeyDown(KeyCode.Mouse0)) {
            if (!EventSystem.current.IsPointerOverGameObject()) {
                if (!isStartedTimer) {
                    isStartedTimer = true;
                    curDestTime = Time.time + delaySetpoint;
                } 
                
                if (Time.time < curDestTime) {
                    curClickedCnt++;
                    Debug.Log(curClickedCnt);
                    if (curClickedCnt > cntClick - 1) {
                        curClickedCnt = 0;
                        isStartedTimer = false;
                        return true;
                    }
                }
            }
        }
        if (Time.time > curDestTime) {
            curClickedCnt = 0;
            isStartedTimer = false;
            return false;
        }
        return false;
    }


    private bool CamMoveViaPhysics() {
        if (Input.GetKeyDown(KeyCode.Mouse0)) {
            if (!EventSystem.current.IsPointerOverGameObject()) {
                RaycastHit rHit;
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out rHit, 10000, GameManager.Inst.OnlyGroundMask)) {
                    strtPosInpt = rHit.point;
                    startDpiPos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                }
            }
        }

        if (Input.GetKey(KeyCode.Mouse0)) {
            if (!EventSystem.current.IsPointerOverGameObject()) {
                RaycastHit rHit;
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out rHit, 10000, GameManager.Inst.OnlyGroundMask)) {
                    lastPosInpt = rHit.point;
                    Vector3 nPos = cam.transform.position - (lastPosInpt - strtPosInpt);

                    if (!IsReachToBound(ref nPos)) {
                        cam.transform.position = nPos;
                    }
                    lastDpiPos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                }
            }
        }

        bool isTouchMove = (startDpiPos - lastDpiPos).sqrMagnitude / Screen.dpi > minScreenQuadDistTouchMovable;
        if (isTouchMove) {
            Debug.Log("isTouchMove");
        }
        if (Input.GetKeyUp(KeyCode.Mouse0)) {
            lastPosInpt = strtPosInpt;
            lastDpiPos = startDpiPos;
            isTouchMove = false;
        }
        return isTouchMove;
    }


    private void PickOnViaPhysics(bool flgLock = false) {
        if (flgLock) {
            flgLockMem = true;
        }
        if (!flgLockMem && Input.GetKeyUp(KeyCode.Mouse0)) {
            if (!EventSystem.current.IsPointerOverGameObject()) {
                RaycastHit rHit;
                Ray ray = CamMover.cam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out rHit, 10000, GameManager.Inst.OnlyUnitPlayerMask)) {
                    Unit catchedUnit = rHit.transform.gameObject.GetComponent<Unit>();
                    SelectUnit(catchedUnit);
                } else if (Physics.Raycast(ray, out rHit, 10000, GameManager.Inst.OnlyBuildMask)) {
                    BuildBehaviour buildingBehaviour = rHit.transform.gameObject.GetComponent<BuildBehaviour>();
                    MenuHolder.Inst.BuildingMenuChangeActive(buildingBehaviour);
                } else if (Physics.Raycast(ray, out rHit, 10000, GameManager.Inst.OnlyGroundMask)) {
                    foreach (ISelectable curSel in selectedObjs) {
                        PlayerUnit curPlayerUnit = curSel as PlayerUnit;
                        curPlayerUnit.MoveTo(rHit.point);
                    }
                }
            }
        }
        if (Input.GetKeyUp(KeyCode.Mouse0)) {
            flgLockMem = false;
        }
    }

    private void CamTouchScale() {
        isManyTouches = Input.touchCount > 1;
        if (isManyTouches) {
            if (Input.touches[0].phase == TouchPhase.Began || Input.touches[1].phase == TouchPhase.Began) {
                startValueScale = (Input.touches[0].position - Input.touches[1].position).sqrMagnitude;
                lastValueScale = startValueScale;
                strtPosCam = cam.transform.position;
                destPosCam = strtPosCam;
            }

            if (Input.touches[0].phase == TouchPhase.Moved || Input.touches[1].phase == TouchPhase.Moved &&
                !(Input.touches[0].phase == TouchPhase.Began || Input.touches[1].phase == TouchPhase.Began)) {
                lastValueScale = (Input.touches[0].position - Input.touches[1].position).sqrMagnitude;
            }

            Vector3 camPos = strtPosCam;
            Vector3 camRot = cam.transform.rotation.eulerAngles;
            float startDistToPlane = camPos.y / Mathf.Sin(camRot.x * Mathf.Deg2Rad);
            float camRotYDegr = camRot.y * Mathf.Deg2Rad;

            float xPos = camPos.x + startDistToPlane * Mathf.Sin(camRotYDegr);
            float zPos = camPos.z + startDistToPlane * Mathf.Cos(camRotYDegr);

            float scale = startValueScale / lastValueScale;

            camPos.x += (camPos.x - xPos) * (scale - 1);
            camPos.y = camPos.y * scale;
            camPos.z += (camPos.z - zPos) * (scale - 1);
            cam.transform.position = camPos;

            if (Input.touches[0].phase == TouchPhase.Ended || Input.touches[0].phase == TouchPhase.Ended) {
                startValueScale = lastValueScale;
                strtPosCam = cam.transform.position;
                destPosCam = strtPosCam;
            }
        }
    }

    private void CamWheelScale() {
        scaleDeltaWheel = scaleFactor * Input.mouseScrollDelta.y;

        Vector3 camPos = cam.transform.position;
        Vector3 camRot = cam.transform.rotation.eulerAngles;
        float startDistToPlane = camPos.y / Mathf.Sin(camRot.x * Mathf.Deg2Rad);
        float camRotYDegr = camRot.y * Mathf.Deg2Rad;

        float xPos = camPos.x + startDistToPlane * Mathf.Sin(camRotYDegr);
        float zPos = camPos.z + startDistToPlane * Mathf.Cos(camRotYDegr);

        camPos.x += (camPos.x - xPos) * scaleDeltaWheel;
        camPos.y += camPos.y * scaleDeltaWheel;
        camPos.z += (camPos.z - zPos) * scaleDeltaWheel;
        cam.transform.position = camPos;
    }

    public void MoveTo(Vector3 destPos) {
        strtPosCam = cam.transform.position;
        destPosCam = destPos;
        if (flgConstSpeed) {
            moveTime = (strtPosCam - destPosCam).magnitude / speed;
        } else {
            moveTime = moveTimeSetpoint;
        }
        destTime = Time.time + moveTime;
        isAutoMoving = true;
    }
    private void MoveCam() {
        float t = (Time.time + moveTime - destTime) / moveTime;
        Vector3 pos = cam.transform.position;
        pos = Vector3.Lerp(strtPosCam, destPosCam, t);
        
        if (cam.transform.position == destPosCam || IsReachToBound(ref pos)) {
            isAutoMoving = false;
            strtPosCam = destPosCam;
        }
        cam.transform.position = pos;
    }

    private Vector3 GetFocusPnt() {
        Vector3 posFocus = Vector3.zero;
        Vector3 camPos = cam.transform.position;
        Vector3 camRot = cam.transform.rotation.eulerAngles;
        float startDistToPlane = camPos.y / Mathf.Sin(camRot.x * Mathf.Deg2Rad);
        float camRotYDegr = camRot.y * Mathf.Deg2Rad;
        Vector3 destCamPos = new Vector3(camPos.x + startDistToPlane * Mathf.Sin(camRotYDegr)
            , 0
            , camPos.z + startDistToPlane * Mathf.Cos(camRotYDegr));
        return posFocus;
    }
    public Vector3 GetCamPosFromShowedObject(Vector3 posShowedObject) {
        Vector3 camPos = cam.transform.position;
        Vector3 camRot = cam.transform.rotation.eulerAngles;
        float startDistToPlane = camPos.y / Mathf.Sin(camRot.x * Mathf.Deg2Rad);
        float camRotYDegr = camRot.y * Mathf.Deg2Rad;
        Vector3 destCamPos = new Vector3(posShowedObject.x - startDistToPlane * Mathf.Sin(camRotYDegr)
            , camPos.y
            , posShowedObject.z - startDistToPlane * Mathf.Cos(camRotYDegr));
        return destCamPos;
    }

    private bool IsReachToBound(ref Vector3 pos) {
        bool result = false;
        if(pos.x <= camBounds.min.x) {
            pos.x = camBounds.min.x;
            result = true;
        }
        if (pos.x >= camBounds.max.x) {
            pos.x = camBounds.max.x;
            result = true;
        }
        if (pos.z <= camBounds.min.z) {
            pos.z = camBounds.min.z;
            result = true;
        }
        if (pos.z >= camBounds.max.z) {
            pos.z = camBounds.max.z;
            result = true;
        }
        return result;
    }

    private void SelectUnit(Unit catchedUnit, SelectionForceType selForceType = SelectionForceType.Default) {
        bool isSelectedUnit = catchedUnit.SelectSwitch(ReleaseSelecter, selForceType);
        if (isSelectedUnit) {
            selectedObjs.Add(catchedUnit);
        } else {
            ReleaseSelecter(catchedUnit);
        }
    }
    private bool ReleaseSelecter(Unit catchedUnit) {
        selectedObjs.Remove(catchedUnit);
        return true;
    }

    private void SelectAllVisibleUnits() {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
        List<Unit> unitsOnArea = UnitCreator.Inst.Spawned[0];
        for (int i = 0; i < unitsOnArea.Count; i++) {
            if (IsVisibleForCamera(unitsOnArea[i].UnitRenderer, planes)){
                SelectUnit(unitsOnArea[i], SelectionForceType.Select);
            }
        } 
    }

    private bool IsVisibleForCamera(Renderer r, Plane[] planes) {
        return GeometryUtility.TestPlanesAABB(planes, r.bounds);
    }
}
