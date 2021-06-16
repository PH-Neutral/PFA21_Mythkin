using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrassZoneGenerator : MonoBehaviour {
    MeshCollider[] collParts;
    List<Vector3> grassObjects = new List<Vector3>();

    public void Populate() {
        Initialize();
        for(int i=0; i<collParts.Length;i++) {
            PopulateOne(collParts[i]);
        }
    }
    public void Clear() {

    }
    void Initialize() {
        collParts = GetComponentsInChildren<MeshCollider>();
    }
    void PopulateOne(MeshCollider coll) {
        Mesh mesh = coll.sharedMesh;

    }


    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.green;
        for(int i = 0; i < grassObjects.Count; i++) {
            Gizmos.DrawSphere(grassObjects[i] + Vector3.up * 0.5f, 0.1f);
        }
    }
}