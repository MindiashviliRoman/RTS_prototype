using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyUnit : Unit {
    [SerializeField] private int healthStartValue = 1;
    [SerializeField] int stepPatrol = 3;
    private float sizePatrol = 0f;
    private Vector3 spawnPos = Vector3.zero;


    //Common States
    private bool isSettedNwTargetPath = false;


    //Patrolling
    [SerializeField] private Vector2 MinMaxTimeWaitingForPatrol = new Vector2(2, 7); //Using like structure with float field
    [SerializeField] private float curTimeWatingForPatrolMove = 0f;
    private float startWaitingTime = 0f;
    [SerializeField] private bool isStartedWaitingTime = false;


    private void OnEnable() {
        uOnEnable();
        isSettedNwTargetPath = false;

        curTimeWatingForPatrolMove = 0f;
        startWaitingTime = 0f;
        isStartedWaitingTime = false;
   

        catchedVision = false;
        startChacingTime = 0f;
        isStartChacingTime = false;
        isCatch = false;

        curAttackingTime = 0f;
        isStartAttack = false;
        isCurEnemyUnitDead = false;

        ChangeState(UnitStates.Patroling);
        HealthBarSetVisible(true);
        spawnPos = transform.position;

        this.startHealth = healthStartValue;
    }

    private void Awake() {

        uAwake();
        this.gameObject.name = "Enemy_" + this.gameObject.name;

        Vector3 v = GameManager.Inst.SizeCell * stepPatrol;
        sizePatrol = new Vector2(v.x, v.z).magnitude;

        targetPlayers = new Collider[1];
        targetPlayersUnit = new Unit[1];
    }
    private void Start() {
        uStart();
    }

    private void Update() {
 //       uUpdate();
        CallBehaviour();
    }
    private void LateUpdate() {
        uLateUpdate();
    }

    protected override void Patroling(){
        if (!isCurEnemyUnitDead) {
            if (!isSettedNwTargetPath) {
                curTimeWatingForPatrolMove = Random.Range(MinMaxTimeWaitingForPatrol.x, MinMaxTimeWaitingForPatrol.y);
                float dX = Random.Range(1, sizePatrol);
                Vector3 nwPos = new Vector3(spawnPos.x + dX, spawnPos.y, spawnPos.z);
                float maxZ = Mathf.Sqrt(sizePatrol * sizePatrol - dX * dX);
                nwPos.z = spawnPos.z + Random.Range(-maxZ, maxZ);
                thisAgent.SetDestination(nwPos);
                isSettedNwTargetPath = true;
            } else {
                if (thisAgent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathComplete) {
                    if (!isStartedWaitingTime) {
                        startWaitingTime = Time.time;
                        isStartedWaitingTime = true;
                    }
                    if (isStartedWaitingTime && (Time.time - startWaitingTime) > curTimeWatingForPatrolMove) {
                        isStartedWaitingTime = false;
                        isSettedNwTargetPath = false;
                    }
                }
            }
        }

        int cntVisionPlayers = CheckVision();
        if (cntVisionPlayers > 0) {
            ChangeState(UnitStates.Chase);
        }
    }

    private int CheckVision() {
        int cntCatched = Physics.OverlapSphereNonAlloc(transform.position, radiusView, targetPlayers, GameManager.Inst.OnlyUnitPlayerMask);
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

                if((this.transform.position - playerPos).sqrMagnitude < cathedQuadDist) {
                    isCatch = true;
                }
            } else {
                if (!isSettedNwTargetPath) {
                    thisAgent.SetDestination(spawnPos);
                    isSettedNwTargetPath = true;
                }
                //return to spawn
                //mb to create UnitState.Return to spawn?
                if (isSettedNwTargetPath && thisAgent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathComplete) {
                    isSettedNwTargetPath = false;
                    isStartChacingTime = false;
                    ChangeState(UnitStates.Patroling);
                }
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
                        if (!isSettedNwTargetPath) {
                            thisAgent.SetDestination(spawnPos);
                            isSettedNwTargetPath = true;
                            isStartAttack = false;
                        }
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
            //return to spawn
            //mb to create UnitState.Return to spawn?
            if (isSettedNwTargetPath && thisAgent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathComplete) {
                isSettedNwTargetPath = false;
                isCurEnemyUnitDead = false;
                ChangeState(UnitStates.Patroling);
            }
        }
    }

}
