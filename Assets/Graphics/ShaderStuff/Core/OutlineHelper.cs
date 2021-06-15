using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class OutlineHelper : MonoBehaviour {
    public class MeshData {
        public bool HasSkinnedRenderer {
            get { return renderer is SkinnedMeshRenderer; }
        }
        public List<int>[] sharedVertices;
        public Mesh mesh;
        public Renderer renderer;

        public MeshData(Renderer renderer) {
            this.renderer = renderer;
            mesh = HasSkinnedRenderer ? (renderer as SkinnedMeshRenderer).sharedMesh : renderer.GetComponent<MeshFilter>().sharedMesh;
            sharedVertices = new List<int>[mesh.vertices.Length];
        }

        public void RemapMeshNormals() {
            Dictionary<Vector3, List<int>> posToVertices = new Dictionary<Vector3, List<int>>();
            Vector3[] verts = mesh.vertices;
            Vector3 pos;
            // initialize the index array
            for(int i = 0; i < verts.Length; i++) {
                pos = verts[i];
                if(!posToVertices.ContainsKey(pos)) posToVertices[pos] = new List<int>();
                posToVertices[pos].Add(i);
            }
            // populate the index array
            for(int i = 0; i < verts.Length; i++) {
                pos = verts[i];
                (sharedVertices[i] = new List<int>()).AddRange(posToVertices[pos]);
            }
            mesh.normals = RecalculateNormals();
        }
        Vector3[] RecalculateNormals() {
            Vector3[] normals = new Vector3[mesh.vertices.Length];
            for(int i = 0; i < normals.Length; i++) {
                normals[i] = GetAverageNormal(i);
            }
            return normals;
        }
        Vector3 GetAverageNormal(int vertexIndex) {
            int[] iVerts = sharedVertices[vertexIndex].ToArray();
            Vector3 result = Vector3.zero;
            for(int i = 0; i < iVerts.Length; i++) {
                result += mesh.normals[iVerts[i]];
            }
            return (result / iVerts.Length).normalized;
        }
        public void SetupAsClone(Material outlineMat) {
            SkinnedMeshRenderer skRend = renderer as SkinnedMeshRenderer;
            MeshRenderer msRend = renderer as MeshRenderer;
            // apply material
            Material[] mats = renderer.sharedMaterials;
            for(int i = 0; i < mats.Length; i++) {
                //string log = $"{i}: {mats[i].name} to {mat.name}";
                mats[i] = Instantiate(outlineMat);
                //Debug.Log($"{log} => {mats[i].name}");
            }
            renderer.sharedMaterials = mats;

            // adjust transform
            if(HasSkinnedRenderer) {
                // apply scale to skeleton
                Vector3 scale = skRend.rootBone.localScale;
                scale.x *= -1;
                skRend.rootBone.localScale = scale;
            } else {
                // apply scale to transform
                Vector3 scale = msRend.transform.localScale;
                scale.x *= -1;
                msRend.transform.localScale = scale;
            }

            //Adjust renderer settings
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            renderer.allowOcclusionWhenDynamic = false;
        }
    }
    public static Transform pool = null;

    [HideInInspector] public GameObject cloneObj = null;
    [HideInInspector] public Renderer[] cloneRenderers = null;
    public Material outlineMaterial;

    bool HasClone {
        get { return cloneObj != null && cloneRenderers != null && cloneRenderers.Length > 0; }
    }
    Renderer[] _rends;
    MeshData[] _meshObjects;

    private void Start() {
        //Debug.Log($"{name}: cloneRenderers = {cloneRenderers} && cloneRenderers.Length = {cloneRenderers.Length}");
        if(!HasClone) CreateOutlineObject();
        ShowOutline(false);
    }
    private void Update() {
        if(HasClone) {
            if(Input.GetKeyDown(KeyCode.LeftShift)) {
                ShowOutline(true);
            } else if(Input.GetKeyUp(KeyCode.LeftShift)) {
                ShowOutline(false);
            }
        }
    }

    void Initialize() {
        _rends = GetComponentsInChildren<Renderer>();
        _meshObjects = new MeshData[_rends.Length];
        for(int i = 0; i < _rends.Length; i++) {
            //Debug.Log(_rends.Length + $"[{i}] : " + _rends[i]);
            _meshObjects[i] = new MeshData(_rends[i]);
        }
        cloneObj = null;
        cloneRenderers = null;
    }
    void CloneGameObject(bool inEditor = false) {
        if(pool == null) SetupPool();
        OutlineHelper clone = Instantiate(this, pool);
        clone.Initialize();
        clone.SetupAsClone(transform);
        cloneObj = clone.gameObject;
        cloneRenderers = clone._rends;
        if(inEditor) DestroyImmediate(clone);
        else Destroy(clone);
        //Debug.Log($"Clone of {name} created successfully.");
    }
    void SetupConstraint(IConstraint constraint, Transform t) {
        ConstraintSource source = new ConstraintSource();
        source.sourceTransform = t;
        source.weight = 1;
        constraint.AddSource(source);
        constraint.weight = 1;
        constraint.constraintActive = true;
    }
    void SetupPool() {
        string poolName = "Outline Objects";
        GameObject poolGo = GameObject.Find(poolName);
        if(poolGo != null) pool = poolGo.transform;
        else pool = new GameObject(poolName).transform;
    }
    public void ShowOutline(bool show) {
        if(!HasClone) return;
        for(int i = 0; i < cloneRenderers.Length; i++) {
            cloneRenderers[i].enabled = show;
        }
    }
    public void ResetScript(bool inEditor = false) {
        if(cloneObj != null) {
            if(inEditor) DestroyImmediate(cloneObj);
            else Destroy(cloneObj);
            cloneObj = null;
            cloneRenderers = null;
        }
    }
    public void CreateOutlineObject(bool inEditor = false) {
        Initialize();
        if(outlineMaterial == null || _rends.Length == 0) {
            Debug.LogWarning($"{name}'s {nameof(OutlineHelper)} could not start. This component needs an Outline material and to find at least one {nameof(Renderer)} in its hierarchy.");
            if(!inEditor) this.enabled = false;
            return;
        }
        //Debug.Log($"{name} prepares to create the outline obj");
        // prepare the new mesh with updated normals
        for(int i = 0; i < _meshObjects.Length; i++) {
            _meshObjects[i].RemapMeshNormals();
        }
        // clone the object
        CloneGameObject(inEditor);
    }
    public void SetupAsClone(Transform original) {
        // change name
        name += "-outline";

        // disable all colliders (if any)
        Collider[] colls = GetComponentsInChildren<Collider>();
        for(int i = 0; i < colls.Length; i++) {
            colls[i].enabled = false;
        }
        // adjust transform
        if(GetComponentInChildren<SkinnedMeshRenderer>() != null) {
            // apply constraints
            SetupConstraint(gameObject.AddComponent<PositionConstraint>(), original);
            SetupConstraint(gameObject.AddComponent<RotationConstraint>(), original);
            SetupConstraint(gameObject.AddComponent<ScaleConstraint>(), original);
        }
        // setup each renderer
        for(int i = 0; i < _meshObjects.Length; i++) {
            _meshObjects[i].SetupAsClone(outlineMaterial);
        }
    }
}