using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Unit : MonoBehaviour, IDamageble, IDataBar, ISelectable {
    private static string[] someNames = new string[10] { "Pukka", "Chester", "Kingkong", "Felicia", "Devastrator", "Organizator", "Bill", "Splinter", "Mr.Agent", "Ms.Agetn" };
    public Renderer UnitRenderer{ private set; get;}
    protected NavMeshAgent thisAgent;
    protected Rigidbody rigitBody;
    protected Collider thisCollider;

    //poolLink setting control
    private int poolLink = -1;
    private bool isPoolLinkSetted = false;
    public int PoolLink {
        set {
            if (isPoolLinkSetted) {
                throw new System.Exception("poolLink is setted yet");
            } else {
                poolLink = value;
                isPoolLinkSetted = true;
            }
        }
        get {
            return poolLink;
        }
    }

    public int MaxDamag { set; get; } = 1;
    public int startHealth { protected set; get; }
    public int curHealth { protected set; get; }
    public DataBarLinks InfoBarLinks { private set; get; }

    public CamMover CamDirector { protected set; get; }

    public System.Func<Unit, bool> ReleaseFromSelecterCam { private set;  get; }
    public bool IsSelected { protected set; get; }
    protected Material curMat;
    protected float startSmoothness = 0;
    protected Color starColor;



    //stateMachine Anim
    [SerializeField] protected UnitStates curBehaviourState = UnitStates.Idle;
    [SerializeField] protected UnitStates prevBehaviourStat = UnitStates.Idle;
    //Animator
    protected Animator unitAnimator;

    //vision
    [SerializeField] protected float radiusView = 4;
    protected Collider[] targetPlayers;
    protected Unit[] targetPlayersUnit;
    protected bool catchedVision = false;

    //Chasing
    [SerializeField] protected float chacingTime = 10f;
    [SerializeField] protected float cathedQuadDist = 9f; //dist = 3
    protected float startChacingTime = 0;
    protected bool isStartChacingTime = false;
    protected bool isCatch = false;


    //Att
    [SerializeField] protected float pauseBetweenAttack = 1f;//sec
    protected float targetEscapedQuadDistance = 20f;// dist = 2sqrt(5)
    protected float curAttackingTime = 0f;
    protected bool isStartAttack = false;
    protected bool isCurEnemyUnitDead = false;



    protected void uOnEnable() {
        thisCollider.enabled = true;
        thisAgent.enabled = true;
        curHealth = startHealth;
        SetProgressBarValue((float)curHealth / startHealth);
    }

    protected void uAwake() {
        rigitBody = this.GetComponent<Rigidbody>();
        Transform capsuleObjTr = this.transform.GetChild(0);
        InfoBarLinks = this.transform.GetChild(1).GetComponent<DataBarLinks>();
        UnitRenderer = capsuleObjTr.GetComponent<Renderer>();
        thisCollider = this.GetComponent<Collider>();

        this.gameObject.name = someNames[Random.Range(0, someNames.Length)];
        curMat = UnitRenderer.material;

        startSmoothness = curMat.GetFloat("_Glossiness");
        starColor = curMat.color;

        thisAgent = this.GetComponent<NavMeshAgent>();

        CamDirector = CamMover.cam.GetComponent<CamMover>();
        unitAnimator = capsuleObjTr.GetComponent<Animator>();


        AnimationClip[] clips = unitAnimator.runtimeAnimatorController.animationClips;// DeadAnim
        for(int i = 0; i < clips.Length; i++) {
            if(clips[i].name == "DeadAnim") {
                capsuleObjTr.GetComponent<AnimationFuncs>().unitDaying = Dying;

                float clipEnd = clips[i].length;
                AnimationEvent evt = new AnimationEvent();
                evt.time = clipEnd;
                evt.functionName = "CallUnitDaying";
                clips[i].AddEvent(evt);
            }
            if (clips[i].name == "AttackAnim") {
                pauseBetweenAttack = clips[i].length;
            }
        }
    }

    protected void uStart() {
        thisAgent.avoidancePriority = Random.Range(10, 100);
    }
//    protected void uUpdate() {
//        SetViewBarToCamera(CamMover.cam);
//    }

    protected void uLateUpdate() {
        SetViewBarToCamera(CamMover.cam); //this should be on LateUpdate
    }
    public void SetProgressBarValue(float f) {
        InfoBarLinks.SetValue(f, startHealth, "");
    }

    private void SetViewBarToCamera(Camera cam) {
//      we can call SetViewBarToCamera when camera change rotation......
        InfoBarLinks.transform.rotation = Quaternion.Euler(cam.transform.rotation.eulerAngles);
    }


    protected void CallBehaviour() {
        switch (curBehaviourState) {
            case UnitStates.Idle:
                Idle();
                break;
            case UnitStates.Moving:
                Moving();
                break;
            case UnitStates.Attack:
                Attacking();
                break;
            case UnitStates.Chase:
                Chasing();
                break;
            case UnitStates.Patroling:
                Patroling();
                break;
            case UnitStates.Death:
                Death();
                break;
            default:
                break;
        }
    }

    protected virtual void Idle() { }
    protected virtual void Moving() { }
    protected virtual void Attacking() { }
    protected virtual void Chasing() { }
    protected virtual void Patroling() { }
    protected virtual void Death() { }

    public bool Damaged(int damag) {
        bool isKilled = false;
        curHealth -= damag;
        SetProgressBarValue((float)curHealth / startHealth);

        isKilled = curHealth <= 0;
        if (isKilled) {
            ChangeState(UnitStates.Death);
            thisAgent.enabled = false;
            thisCollider.enabled = false;
        }
        return isKilled;
    }
    virtual protected void Dying() {
        if (IsSelected) {
            SelectSwitch(null, SelectionForceType.Deselect);
        }
        ChangeState(UnitStates.Idle);
        UnitCreator.Inst.UnitReturnToPool(this, PoolLink);
        poolLink = -1;
        isPoolLinkSetted = false;
        Debug.Log("Unit " + this.name + " was end game");
    }


    public bool SelectSwitch(System.Func<Unit, bool> releaseSelecterFunc, SelectionForceType forceSelection = SelectionForceType.Default) {
        //forceSelection = default
        bool deselect = forceSelection == SelectionForceType.Deselect;
        bool select = forceSelection == SelectionForceType.Select;
        switch (forceSelection) {
            case SelectionForceType.Deselect:
                ReleaseFromSelecterCam?.Invoke(this);//Clear from cam
                ReleaseFromSelecterCam = null;
                Deselect();
                break;
            case SelectionForceType.Select:
                ReleaseFromSelecterCam = releaseSelecterFunc;
                StartSelect();
                break;
            default:
                if (IsSelected) {
                    Deselect();
                } else {
                    StartSelect();
                }
                break;
        }

        return IsSelected;
    }

    public void SetAgentDestination(Vector3 dPos) {
        thisAgent.SetDestination(dPos);
    }

    virtual protected void StartSelect() {
        IsSelected = true;
        HealthBarSetVisible(true);
    }
    virtual protected void Deselect() {
        IsSelected = false;
        HealthBarSetVisible(false);
    }

    protected void HealthBarSetVisible(bool flg) {
        InfoBarLinks.gameObject.SetActive(flg);
    }

    protected void ChangeState(UnitStates nwState) {
        if (this.enabled) {
            int stat = (int)nwState;
            if (stat == 0) {
                Debug.Log("curUnit: " + name + " go to Idle");
            }
            if (stat == 2) {
                Debug.Log("curUnit: " + name + " go to Attack");
            }
            unitAnimator.SetInteger("State", stat);
            prevBehaviourStat = curBehaviourState;
            curBehaviourState = nwState;
        }
    }
}

public enum UnitStates {
    Idle,
    Moving,
    Attack,
    Chase,
    Patroling,
    Death
}


