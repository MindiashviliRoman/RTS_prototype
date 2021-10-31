using System.Collections.Generic;
using UnityEngine;

public class Pool<T> where T : MonoBehaviour {
    public T curPrefab { private set; get; }
    private Stack<T> objs;
    private Transform tParent;
    public int CountObjects {private set; get;}

    public Pool(T prefab, Transform parentTransform, int startCnt = 32){
        curPrefab = prefab;
        tParent = parentTransform;
        CreatePool(startCnt);
    }

    private void CreatePool(int cnt) {
        objs = new Stack<T>(cnt);
        for (int i = 0; i <  cnt; i++) {
            objs.Push(CreateNewObject());
        }
    }

    private T CreateNewObject(bool isActivated = false) {
        T curObj = Object.Instantiate(curPrefab, tParent);
        curObj.gameObject.SetActive(isActivated);
        CountObjects++;
        return curObj;
    }

    public T GetObjectFromPool(Vector3 pos, Vector3 scale, Quaternion rot) {
        T curObj = null;
        if (objs.Count == 0) {
            curObj = CreateNewObject(true);
        } else {
            curObj = objs.Pop();
            curObj.gameObject.SetActive(true);
        }
        curObj.transform.position = pos;
        curObj.transform.localScale = scale;
        curObj.transform.rotation = rot;
        return curObj;
    }

    public void ReturnToPool(T obj) {
        obj.gameObject.SetActive(false);
        objs.Push(obj);
    }

    public bool IsAllReturned() {
        return objs.Count == CountObjects;
    }
}
