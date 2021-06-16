using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrassZoneGenerator : MonoBehaviour {
    const string meshName = "GrassZone Mesh";

    [SerializeField] GameObject prefabGrass = null;
    [SerializeField] Material grassMat = null;
    [SerializeField] float spacing = 1f, meshScale = 8;
    [SerializeField] bool addLOD = false;
    [Range(0f, 1f)][SerializeField] float lodCullThreshold = 0.2f;
    [SerializeField] float noiseStrength = 0.1f, noiseScale = 1f;
    [SerializeField] Vector2 noiseOffset = Vector2.zero;
    [SerializeField] Transform[] grassZones;

    public void PopulateAll(bool inEditor = false) {
        for(int i=0; i<grassZones.Length; i++) {
            Populate(grassZones[i], inEditor);
        }
    }
    public void ClearAll(bool inEditor = false) {
        for(int i = 0; i < grassZones.Length; i++) {
            Clear(grassZones[i], inEditor);
            ShowRendererInChildren(grassZones[i], true);
        }
    }
    void Populate(Transform tObj, bool inEditor = false) {
        Clear(tObj, inEditor); // empty the zone to avoid cluttering
        ShowRendererInChildren(tObj, false);
        Collider[] zoneColliders = CalculateBounds(tObj, out Bounds box); // add zone bounds together
        Transform meshObj = CreateMeshObject(tObj); // prepare a transform to put the mesh in
        // Place grass at regular intervals offsetted with noise
        Vector3 point;
        for(float x = box.min.x; x <= box.max.x; x += spacing) {
            for(float z = box.min.z; z <= box.max.z; z += spacing) {
                point = new Vector3(x, box.max.y, z);
                if(CheckForPlacement(AddNoise(point), box.size.y, out RaycastHit hit, zoneColliders)) {
                    PlaceGrass(hit.point, meshObj);
                }
            }
        }
        // Combine grass meshs together ??
        CombineMeshes(meshObj, inEditor);
        if(addLOD) SetupLOD(meshObj.gameObject);
    }
    void Clear(Transform tObj, bool inEditor = false) {
        for(int i=0; i<tObj.childCount; i++) {
            if(tObj.GetChild(i).name.Equals(meshName)) {
                if(inEditor) DestroyImmediate(tObj.GetChild(i).gameObject);
                else Destroy(tObj.GetChild(i).gameObject);
                break;
            }
        }
    }
    void ShowRendererInChildren(Transform parent, bool show) {
        Renderer rend;
        for(int i = 0; i < parent.childCount; i++) {
            if(parent.GetChild(i).TryGetComponent(out rend)) {
                rend.enabled = show;
            }
        }
    }
    Collider[] CalculateBounds(Transform tObj, out Bounds box) {
        Collider[] zoneCollider = tObj.GetComponentsInChildren<Collider>();
        // generate bounding box
        box = new Bounds(tObj.position, Vector3.zero);
        for(int i = 0; i < zoneCollider.Length; i++) {
            box.SetMinMax(Vector3.Min(box.min, zoneCollider[i].bounds.min), Vector3.Max(box.max, zoneCollider[i].bounds.max));
        }
        return zoneCollider;
    }
    Transform CreateMeshObject(Transform parent) {
        Transform meshObject = new GameObject(meshName).transform;
        meshObject.SetParent(parent);
        return meshObject;
    }
    Vector3 AddNoise(Vector3 point) {
        Vector2 noise = Vector2.zero;
        noise.x = Mathf.Clamp01(Mathf.PerlinNoise((point.x + noiseOffset.x * 500) * noiseScale, (point.z + noiseOffset.y * 500) * noiseScale));
        noise.y = Mathf.Clamp01(Mathf.PerlinNoise((point.x + noiseOffset.x) * noiseScale, (point.z + noiseOffset.y) * noiseScale));
        noise = (noise * 2 - Vector2.one) * noiseStrength;
        return new Vector3(point.x + noise.x, point.y, point.z + noise.y);
    }
    bool CheckForPlacement(Vector3 pos, float maxHeight, out RaycastHit hit, Collider[] zoneColliders) {
        Vector3 origin = pos + Vector3.up * 0.1f;
        Vector3 dir = Vector3.down * maxHeight;
        if(Physics.Raycast(origin, dir, out hit, dir.magnitude, Utils.l_Environment.ToLayerMask())) {
            // we are indeed in the correct xz pos
            if(zoneColliders.Contains(hit.collider)) {
                // if the collider does, in fact, belong to this object
                if(Physics.Raycast(hit.point, dir, out hit, dir.magnitude, Utils.l_Terrain.ToLayerMask())) {
                    // there is a terrain surface in the zone
                    return true;
                }
            }
        }
        return false;
    }
    void PlaceGrass(Vector3 position, Transform parent) {
        // instanciate grass prefab at pos with random rotation and defined parent
        if(prefabGrass == null) return;
        Quaternion rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
        GameObject grassInstance = Instantiate(prefabGrass, position, rotation, parent);
        grassInstance.transform.localScale = Vector3.one * meshScale;
    }
    void CombineMeshes(Transform parent, bool inEditor = false) {
        MeshFilter[] meshFilters = parent.GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];

        for(int i = 0; i < meshFilters.Length; i++) {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            //meshFilters[i].gameObject.SetActive(false);
        }
        MeshRenderer rend = parent.gameObject.AddComponent<MeshRenderer>();
        MeshFilter mf = parent.gameObject.AddComponent<MeshFilter>();
        mf.sharedMesh = new Mesh();
        mf.sharedMesh.name = name;
        mf.sharedMesh.CombineMeshes(combine);
        if(grassMat != null) rend.material = grassMat;
        parent.gameObject.SetActive(true);
        // destroy children
        while(parent.childCount > 0) {
            if(inEditor) DestroyImmediate(parent.GetChild(0).gameObject);
            else Destroy(parent.GetChild(0).gameObject);
        }
    }
    void SetupLOD(GameObject obj) {
        LODGroup lod = obj.AddComponent<LODGroup>();
        lod.fadeMode = LODFadeMode.None;
        LOD[] lods = new LOD[1];
        lods[0] = new LOD(lodCullThreshold, new Renderer[] { obj.GetComponent<Renderer>() });
        lod.SetLODs(lods);
    }
}