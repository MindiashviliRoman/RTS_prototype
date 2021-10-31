using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitCreator {
    public List<List<Unit>> Spawned {private set; get;} 

    private static UnitCreator inst;
    private List<Pool<Unit>> unitPools; //[green, blue]
    private List<Vector3> sizesUnit;
    private Transform parentTransform;

    private UnitCreator() {
        unitPools = new List<Pool<Unit>>();
        sizesUnit = new List<Vector3>();
    }

    public static UnitCreator Inst {
        get  {
            if(inst == null) {
                inst = new UnitCreator();
            }
            return inst;
        }
    }
    public void InitializePools(Transform parent, Unit[] prefabUnit, int[] Cnt) {
        Spawned = new List<List<Unit>>();
        parentTransform = parent;
        for (int i = 0; i < Cnt.Length; i++) {
            MeshFilter mf = prefabUnit[i].transform.GetChild(0).GetComponent<MeshFilter>();
            if (mf != null) {
                sizesUnit.Add(mf.sharedMesh.bounds.size);
            } else {
                sizesUnit.Add(Vector3.zero);
            }
            unitPools.Add(new Pool<Unit>(prefabUnit[i], parentTransform.transform, Cnt[i]));
            Spawned.Add(new List<Unit>());
        }
    }

    public Unit SpawnPlayer(Vector3 pos, Vector3 size, Quaternion rot) {
        return SpawnUnit(pos, size, rot, 0);
    }

    public Unit SpawnEnemy(Vector3 pos, Vector3 size, Quaternion rot) {
        return SpawnUnit(pos, size, rot, 1);
    }

    private Unit SpawnUnit(Vector3 pos, Vector3 size, Quaternion rot, int indxPool) {
        Unit curUnit = unitPools[indxPool].GetObjectFromPool(pos, size, rot);
        curUnit.gameObject.SetActive(false);
        curUnit.PoolLink = indxPool;
        curUnit.gameObject.SetActive(true);
        Spawned[indxPool].Add(curUnit);
        return curUnit;
    } 

    public void UnitReturnToPool(Unit curUnit, int indxPool) {
        unitPools[indxPool].ReturnToPool(curUnit);
        Spawned[indxPool].Remove(curUnit);
    }


    public Vector3 GetSizePlayer() {
        return sizesUnit[0];
    }
    public Vector3 GetSizeEnemy() {
        return sizesUnit[1];
    }
}
