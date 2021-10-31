using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public class GameManager : MonoBehaviour
{
    public static GameManager Inst;

    //LayersMask
    public LayerMask OnlyGroundMask;
    public LayerMask OnlyBuildMask;
    public LayerMask OnlyUnitPlayerMask;
    public LayerMask OnlyEnemyPlayerMask;

    [HideInInspector] public GameObject mainBuildGO = null;
    [SerializeField] private Terrain mainTerrain;
    [SerializeField] private GameObject barrierPrefab;
    [SerializeField] private GameObject barriersGO;
    [SerializeField] private GameObject centralBuildPrefab; //истинные размеры 1х1х1

    private Camera cam;
    private CamMover camMover;
    private Vector3 buildPos;

    private byte[,] cells;
    private List<LinkToCell> accessibleCells = new List<LinkToCell>();
    private int sizeBarrierCellX;
    private int sizeBarrierCellZ;

    private int sizeBuildX;
    private int sizeBuildZ;
    private Vector3 sizeBuild;

    private int sizeForExitUnitFromBuild = 0;
    public Vector3 SizeCell { private set; get; } = new Vector3(1, 1, 1);

    //players and enemies units
    [SerializeField] private Unit greenUnitPrefab;
    [SerializeField] private Unit blueUnitPrefab;
    private Transform unitsParent;

    [SerializeField] private int CountEnemies = 40;

    //navmesh
    [SerializeField] private GameObject navMeshSurfacesGO;
    private NavMeshSurface navMeshSurfaces;
    public Bounds UnitSizeBounds { private set; get; }



    private void Awake() {
        if(Inst == null) {
            Inst = this;
        }
        cam = Camera.main;
        camMover = cam.GetComponent<CamMover>();
        if(camMover == null) {
            throw new System.Exception("not CamMover component on main camera");
        }
        if (navMeshSurfacesGO == null) {
            throw new System.Exception("not navmesh object in field \"navMeshSurfacesGO\"");
        }
        navMeshSurfaces = navMeshSurfacesGO.GetComponent<NavMeshSurface>();

        int cellsCntX = Mathf.RoundToInt(mainTerrain.terrainData.size.x / SizeCell.x);
        int cellsCntZ = Mathf.RoundToInt(mainTerrain.terrainData.size.z / SizeCell.z);
        int offstPivotX = cellsCntX / 2;
        int offstPivotZ = cellsCntZ / 2;
        CreateParents();
        CreateUnitsPools();
        InitializeLevel(cellsCntX, cellsCntZ);

        buildPos = SpawnMainBuild(offstPivotX, offstPivotZ);

        InitEmemies(CountEnemies, offstPivotX, offstPivotZ);

        RebakeNavMeshSufraces();

    }

    private void Start() {
        cam.transform.position = camMover.GetCamPosFromShowedObject(buildPos);
    }

    private void Update() {
        if(Input.GetKey(KeyCode.Escape)){
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
    }

    private void CreateParents() {
        unitsParent = new GameObject("unitsParent").transform;
    }

    private void CreateUnitsPools() {
        UnitCreator.Inst.InitializePools(unitsParent.transform, 
            new Unit[] { greenUnitPrefab, blueUnitPrefab }, 
            new int[] { CountEnemies, CountEnemies });
    }

    private void InitializeLevel(int cellsCntX, int cellsCntZ) {

        cells = new byte[cellsCntX , cellsCntZ];
        int offstPivotX = cellsCntX / 2;
        int offstPivotZ = cellsCntZ / 2;

        //barrier size is a multiple of sizeCell
        sizeBarrierCellX = Mathf.RoundToInt(barrierPrefab.transform.localScale.x / SizeCell.x);
        sizeBarrierCellZ = Mathf.RoundToInt(barrierPrefab.transform.localScale.z / SizeCell.z);


        //Build size will be considering
        sizeBuild = centralBuildPrefab.transform.localScale;
        sizeBuildX = Mathf.RoundToInt(sizeBuild.x / SizeCell.x);
        //units will exit on front of build
        sizeBuildZ = Mathf.RoundToInt(sizeBuild.z / SizeCell.z) + sizeForExitUnitFromBuild;

        //Cashing Map
        int chldCnt = barriersGO.transform.childCount;
        for (int i = 0; i < chldCnt; i++) {
            Transform curBarrierParent = barriersGO.transform.GetChild(i);
            for (int j = 0; j < curBarrierParent.childCount; j++) {
                Transform curBarrierTransform = curBarrierParent.GetChild(j);
                int offstX = Mathf.RoundToInt((curBarrierTransform.position.x - curBarrierTransform.localScale.x / 2) / SizeCell.x) + offstPivotX;
                int offstZ = Mathf.RoundToInt((curBarrierTransform.position.z - curBarrierTransform.localScale.z / 2) / SizeCell.z) + offstPivotZ;
                int cntX = Mathf.RoundToInt(curBarrierTransform.localScale.x / SizeCell.x);
                int cntZ = Mathf.RoundToInt(curBarrierTransform.localScale.z / SizeCell.z);
                SetReservedCell(offstX, offstZ, cntX, cntZ, sizeBuildX, sizeBuildZ, ref cells);
            }
        }
        //Save all accesed cells
        //5 cells on perimeter are not available
        int cntCellsX = cells.GetLength(0) - sizeBuildX + 1;
        int cntCellsZ = cells.GetLength(1) - sizeBuildZ + 1;
        for (int i = 5; i < cntCellsX-5; i++) {
            for (int j = 5; j < cntCellsZ-5; j++) {
                if (cells[i, j] != 1) {
                    accessibleCells.Add(new LinkToCell(i, j));
                }
            }
        }
    }

    private Vector3 SpawnMainBuild(int offstPivotX, int offstPivotZ) {
        //spawn of build
        LinkToCell LCell = TakeAccessibleCell();
        mainBuildGO = SpawnBuildOnCell(LCell, offstPivotX, offstPivotZ);
        BuildBehaviour buildBehaviourLink = mainBuildGO.GetComponent<BuildBehaviour>();
        return mainBuildGO.transform.position;
    }

    private LinkToCell TakeAccessibleCell() {
        int indx = (int)Random.Range(0, accessibleCells.Count);
        LinkToCell LCell = accessibleCells[indx];
        accessibleCells.RemoveAt(indx);
        return LCell;
    }

    private GameObject SpawnBuildOnCell(LinkToCell cellCoord, int offstPivotX, int offstPivotZ) {
        Vector3 pos = new Vector3(cellCoord.x - offstPivotX + (sizeBuild.x) / 2, sizeBuild.y / 2, cellCoord.y - offstPivotZ + sizeBuild.z / 2);
        GameObject go = GameObject.Instantiate(centralBuildPrefab, pos, Quaternion.identity);
        return go;
    }

    /*
    private Vector3 GetCamPosFromShowedObject(Vector3 posShowedObject) {
        Vector3 camPos = cam.transform.position;
        Vector3 camRot = cam.transform.rotation.eulerAngles;
        float startDistToPlane = camPos.y / Mathf.Sin(camRot.x * Mathf.Deg2Rad);
        Vector3 destCamPos = new Vector3(posShowedObject.x - startDistToPlane * Mathf.Sin(camRot.y * Mathf.Deg2Rad)
            , camPos.y
            , posShowedObject.z - startDistToPlane * Mathf.Cos(camRot.y * Mathf.Deg2Rad));
        return destCamPos;
    }
    */

    private void SetReservedCell(int offstX, int offstZ, int cntX, int cntZ, int szBuildX, int szBuildZ, ref byte[,] inCells) {
        int reSizedOffstX = offstX - szBuildX + 1;
        if(reSizedOffstX < 0) {
            reSizedOffstX = 0;
        }
        int reSizedOffstZ = offstZ - szBuildZ + 1;
        if (reSizedOffstZ < 0) {
            reSizedOffstZ = 0;
        }
        cntX += offstX;
        cntZ += offstZ;
        for (int i = reSizedOffstX; i < cntX; i++) {
            for(int z = reSizedOffstZ; z < cntZ; z++) {
                inCells[i, z] = 1;
            }
        }
    }



    private void RebakeNavMeshSufraces() {
        navMeshSurfaces.BuildNavMesh();
    }

    private void InitEmemies(int Cnt, int offstPivotX, int offstPivotZ) {
        for(int i =0; i < Cnt; i++) {
            LinkToCell LCell = TakeAccessibleCell();
            SpawnEnemyOnCell(LCell, offstPivotX, offstPivotZ);
        }
    }
    private void SpawnEnemyOnCell(LinkToCell cellCoord, int offstPivotX, int offstPivotZ) {
        Vector3 unitSize = UnitCreator.Inst.GetSizeEnemy();
        Vector3 pos = new Vector3(cellCoord.x - offstPivotX + (unitSize.x) / 2, unitSize.y / 2, cellCoord.y - offstPivotZ + unitSize.z / 2);
        UnitCreator.Inst.SpawnEnemy(pos, Vector3.one, Quaternion.identity);
    }


    public void EnemyDying() {
        CountEnemies--;
        if (CountEnemies <= 0) {
            YouWin();
        }
    }

    private void YouWin() {
        Debug.Log("GameOver. You win!");
    }
}
