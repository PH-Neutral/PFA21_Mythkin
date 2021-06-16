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

    [HideInInspector] public bool isCloneVisible = false;
    public GameObject cloneObj = null;
    public Material outlineMaterial;

    bool HasClone {
        get { return cloneObj != null; }
    }
    Renderer[] _rends;
    MeshData[] _meshObjects;

    private void Start() {
        //Debug.Log($"{name}: cloneRenderers = {cloneRenderers} && cloneRenderers.Length = {cloneRenderers.Length}");
        //if(!HasClone) CreateOutlineObject();
        //ShowOutline(false);
    }
    private void Update() {
        /*if(HasClone) {
            if(Input.GetKeyDown(KeyCode.LeftShift)) {
                ShowOutline(true);
            } else if(Input.GetKeyUp(KeyCode.LeftShift)) {
                ShowOutline(false);
            }
        }*/
    }

    public void ShowOutline(bool show) {
        if(!HasClone) {
            isCloneVisible = false;
            return;
        }
        if(isCloneVisible == show) return;
        Renderer[] cloneRends = cloneObj.GetComponentsInChildren<Renderer>();
        for(int i = 0; i < cloneRends.Length; i++) {
            cloneRends[i].enabled = show;
        }
        isCloneVisible = show;
    }
    public void ResetScript(bool inEditor = false) {
        if(cloneObj != null) {
            if(inEditor) DestroyImmediate(cloneObj);
            else Destroy(cloneObj);
            cloneObj = null;
            isCloneVisible = false;
        }
    }
    public void CreateOutlineObject(System.Type[] typesToDelete, bool inEditor = false) {
        if(HasClone) return;
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
        CloneGameObject(typesToDelete, inEditor);
    }
    public void SetupAsClone(List<System.Type> deleteTypes, Transform original, bool inEditor = false) {
        // change name
        name += "-outline";

        // destroy unwanted components
        deleteTypes.Add(typeof(Collider));
        Component[] comps;
        for(int n = 0; n < deleteTypes.Count; n++) {
            comps = GetComponentsInChildren(deleteTypes[n], true);
            for(int i = 0; i < comps.Length; i++) {
                if(inEditor) DestroyImmediate(comps[i]);
                else Destroy(comps[i]);
            }
        }
        // apply constraints
        SetupConstraint(gameObject.AddComponent<PositionConstraint>(), original);
        SetupConstraint(gameObject.AddComponent<RotationConstraint>(), original);
        SetupConstraint(gameObject.AddComponent<ScaleConstraint>(), original);
        // setup each renderer
        for(int i = 0; i < _meshObjects.Length; i++) {
            _meshObjects[i].SetupAsClone(outlineMaterial);
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
    }
    void CloneGameObject(System.Type[] typesToDelete, bool inEditor = false) {
        if(pool == null) SetupPool();
        OutlineHelper outlineClone = Instantiate(this, transform.position, transform.rotation, pool);
        outlineClone.Initialize();
        List<System.Type> listUnwantedTypes;
        if(typesToDelete != null) {
            listUnwantedTypes = new List<System.Type>(typesToDelete);
        } else {
            listUnwantedTypes = new List<System.Type>();
        }
        
        outlineClone.SetupAsClone(listUnwantedTypes, transform, inEditor);
        cloneObj = outlineClone.gameObject;
        if(inEditor) DestroyImmediate(outlineClone);
        else Destroy(outlineClone);
        isCloneVisible = true;
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
}