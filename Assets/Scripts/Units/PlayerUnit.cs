using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerUnit : Unit {
    [SerializeField] private int healthStartValue = 2;

    //Common States
    private bool isSettedNwTargetPath = false;


    //Moving
    private Vector3 destTargetPos = Vector3.zero;
    private bool isChangetPointDest = false;
    private float timerStationary = 1.5f;
    private float curSationaryTimer = 0f;
    private Vector3 lastPos = Vector3.zero;
    private bool isStartStationary = false;


    private void OnEnable() {
        uOnEnable();
        isSettedNwTargetPath = false;

        catchedVision = false;
        startChacingTime = 0f;
        isStartChacingTime = false;
        isCatch = false;

        curAttackingTime = 0f;
        isStartAttack = false;
        isCurEnemyUnitDead = false;
        ChangeState(UnitStates.Idle);
        this.startHealth = healthStartValue;

        if (thisAgent.enabled && thisAgent.isOnNavMesh) {
            thisAgent.SetDestination(this.transform.position - 2 * UnitCreator.Inst.GetSizePlayer().z * Vector3.forward);
        }
    }

    private void Awake() {
        uAwake();
        this.gameObject.name = "Player_" + this.gameObject.name;
        targetPlayers = new Collider[1];
        targetPlayersUnit = new Unit[1];
    }
    private void Start() {
        uStart();
        HealthBarSetVisible(false);
    }

    private void Update() {
//        uUpdate();
        CallBehaviour();
    }
    private void LateUpdate() {
        uLateUpdate();
    }

    protected override void Idle() {
        int cntVisionEnemy = CheckVision();
        if (cntVisionEnemy > 0) {
            ChangeState(UnitStates.Chase);
        } 
    }



    protected override void Moving() {
        int cntVisionEnemy = CheckVision();
        if (cntVisionEnemy > 0) {
            ChangeState(UnitStates.Chase);
        } else if (isSettedNwTargetPath && thisAgent.remainingDistance <= thisAgent.stoppingDistance) {
            isSettedNwTargetPath = false;
            ChangeState(UnitStates.Idle);
            isStartStationary = false;
        } else if (isStartStationary && Time.time > curSationaryTimer) { //if agents are obstacles one with other
            thisAgent.SetDestination(this.transform.position);
            isStartStationary = false;
        }

        if (isSettedNwTargetPath) {
            float dist = Vector3.Distance(this.transform.position, lastPos);
            if (dist > 0.1f) {
                curSationaryTimer = Time.time + timerStationary;
                isStartStationary = false;
            } else {
                isStartStationary = true;
            }
        }

        if (isChangetPointDest) {
            if (!isSettedNwTargetPath && thisAgent.enabled) {
                isSettedNwTargetPath = true;
                curSationaryTimer = Time.time + timerStationary;
            }
            thisAgent.SetDestination(destTargetPos);
            isChangetPointDest = false;
        }

        if (isSettedNwTargetPath) {
            lastPos = this.transform.position;
        }
    }

    private int CheckVision() {
        int cntCatched = Physics.OverlapSphereNonAlloc(transform.position, radiusView, targetPlayers, GameManager.Inst.OnlyEnemyPlayerMask);
        if (cntCatched > 0) {
            targetPlayersUnit[0] = targetPlayers[0].GetComponent<Unit>();
        }
        return cntCatched;
    }

    protected override void Chasing() {
        if (!isCatch) {
            if (!isStartChacingTime) {
                startChacingTime = Time.time;
                isStartChacingTime = true;
            }
            if (isStartChacingTime && Time.time - startChacingTime < chacingTime) {
                Vector3 playerPos = targetPlayers[0].transform.position;
                thisAgent.SetDestination(playerPos);

                if ((this.transform.position - playerPos).sqrMagnitude < cathedQuadDist) {
                    isCatch = true;
                }
            } else {
                ChangeState(UnitStates.Idle);
            }
        } else {
            isCatch = false;
            isStartChacingTime = false;
            ChangeState(UnitStates.Attack);
        }
    }

    protected override void Attacking() {
        if (!isCurEnemyUnitDead) {
            Vector3 playerPos = targetPlayers[0].transform.position;
            if ((this.transform.position - playerPos).sqrMagnitude < targetEscapedQuadDistance) {
                if (!isStartAttack) {
                    curAttackingTime = Time.time;
                    int damag = Random.Range(0, MaxDamag + 1);
                    isCurEnemyUnitDead = targetPlayersUnit[0].Damaged(damag);

                    //attack
                    if (isCurEnemyUnitDead) {
                        //mb to create UnitState.Return to spawn?
                        isStartAttack = false;
                    }
                    isStartAttack = true;
                }
                if (!isCurEnemyUnitDead && Time.time - curAttackingTime > pauseBetweenAttack) {
                    isStartAttack = false;
                }
            } else {
                //chase
                ChangeState(UnitStates.Chase);
            }
            transform.LookAt(playerPos, Vector3.up);
        } else {
            isCurEnemyUnitDead = false;
            ChangeState(UnitStates.Idle);
        }
    }


    public void MoveTo(Vector3 pos) {
        destTargetPos = pos;
        isChangetPointDest = true;
        ChangeState(UnitStates.Moving);
    }


    override protected void StartSelect() {
        IsSelected = true;
        HealthBarSetVisible(true);
        curMat.SetFloat("_Glossiness", 0.0f);
        curMat.color = new Color(0.5f, 1f, 0f);
    }
     override protected void Deselect() {
        IsSelected = false;
        HealthBarSetVisible(false);
        curMat.SetFloat("_Glossiness", startSmoothness);
        curMat.color = starColor;
    }


}
