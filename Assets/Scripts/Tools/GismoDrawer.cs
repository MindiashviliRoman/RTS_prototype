using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GismoDrawer : MonoBehaviour
{
    void OnDrawGizmosSelected() {
        Gizmos.color = Color.red;
        for (int i = -250; i < 250; i++) {
            Vector3 direction = new Vector3(500, 0, 0);
            Gizmos.DrawRay(new Vector3(-250, 0, i), direction);
        }
        for (int i = -250; i < 250; i++) {
            Vector3 direction = new Vector3(0, 0, 500);
            Gizmos.DrawRay(new Vector3(i, 0, -250), direction);
        }

    }
}
