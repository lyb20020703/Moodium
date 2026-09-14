//
// Wall - object array animator
//
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using Artngame.CommonTools;

namespace Artngame.WallGEN
{
    [ExecuteInEditMode, AddComponentMenu("ARTnGAME/WallGEN/WallARC")]
    public partial class WallARC : MonoBehaviour
    {
        //v0.3 - Spline
        public bool useSplinePath = false;
        public SplinerPTREANT splinePath;

        //v0.2
        public bool recreateWall = false;
        public List<GameObject> bricks = new List<GameObject>();
        public List<GameObject> brickSource = new List<GameObject>();
        public bool createSceneMesh = false;
        public bool sceneMeshOnlyColliders = false;
        public float scaleSceneBricks = 1;

        //v0.1
        public bool worldSpace = true;// false;
        public bool makeArcs = false;
        public bool isArc = false;
        public float scaleArcX = 10;
        public float scaleArcY = 10;
        public float arcBorderHeight = 0.5f;
        public Vector3 shiftArbBorderPos = Vector3.zero;
        public List<Transform> arcs = new List<Transform>(); //position and scale of arcs

        #region Basic Properties

        [SerializeField]
        int _columns = 50;

        public int columns {
            get { return _columns; }
        }

        [SerializeField]
        int _rows = 50;

        public int rows {
            get { return _rows; }
        }

        [SerializeField]
        Vector2 _extent = new Vector2(100, 100);

        public Vector2 extent {
            get { return _extent; }
            set { _extent = value; }
        }

        [SerializeField]
        Vector2 _offset = Vector2.zero;

        public Vector2 offset {
            get { return _offset; }
            set { _offset = value; }
        }

        #endregion

        #region Noise-To-Position Parameters

        public enum PositionNoiseMode { Disabled, ZOnly, XYZ, Random }

        [SerializeField]
        PositionNoiseMode _positionNoiseMode = PositionNoiseMode.ZOnly;

        public PositionNoiseMode positionNoiseMode {
            get { return _positionNoiseMode; }
            set { _positionNoiseMode = value; }
        }

        [SerializeField]
        float _positionNoiseAmplitude = 5.0f;

        public float positionNoiseAmplitude {
            get { return _positionNoiseAmplitude; }
            set { _positionNoiseAmplitude = value; }
        }

        [SerializeField]
        float _positionNoiseFrequency = 1.0f;

        public float positionNoiseFrequency {
            get { return _positionNoiseFrequency; }
            set { _positionNoiseFrequency = value; }
        }

        [SerializeField]
        float _positionNoiseSpeed = 0.2f;

        public float positionNoiseSpeed {
            get { return _positionNoiseSpeed; }
            set { _positionNoiseSpeed = value; }
        }

        #endregion

        #region Noise-To-Rotation Parameters

        public enum RotationNoiseMode { Disabled, XAxis, YAxis, ZAxis, Random }

        [SerializeField]
        RotationNoiseMode _rotationNoiseMode = RotationNoiseMode.Disabled;

        public RotationNoiseMode rotationNoiseMode {
            get { return _rotationNoiseMode; }
            set { _rotationNoiseMode = value; }
        }

        [SerializeField]
        float _rotationNoiseAmplitude = 45.0f;

        public float rotationNoiseAmplitude {
            get { return _rotationNoiseAmplitude; }
            set { _rotationNoiseAmplitude = value; }
        }

        [SerializeField]
        float _rotationNoiseFrequency = 1.0f;

        public float rotationNoiseFrequency {
            get { return _rotationNoiseFrequency; }
            set { _rotationNoiseFrequency = value; }
        }

        [SerializeField]
        float _rotationNoiseSpeed = 0.2f;

        public float rotationNoiseSpeed {
            get { return _rotationNoiseSpeed; }
            set { _rotationNoiseSpeed = value; }
        }

        #endregion

        #region Noise-To-Scale Parameters

        public enum ScaleNoiseMode { Disabled, Uniform, XYZ }

        [SerializeField]
        ScaleNoiseMode _scaleNoiseMode = ScaleNoiseMode.Disabled;

        public ScaleNoiseMode scaleNoiseMode {
            get { return _scaleNoiseMode; }
            set { _scaleNoiseMode = value; }
        }

        [SerializeField, Range(0, 1)]
        float _scaleNoiseAmplitude = 0.5f;

        public float scaleNoiseAmplitude {
            get { return _scaleNoiseAmplitude; }
            set { _scaleNoiseAmplitude = value; }
        }

        [SerializeField]
        float _scaleNoiseFrequency = 1.0f;

        public float scaleNoiseFrequency {
            get { return _scaleNoiseFrequency; }
            set { _scaleNoiseFrequency = value; }
        }

        [SerializeField]
        float _scaleNoiseSpeed = 0.2f;

        public float scaleNoiseSpeed {
            get { return _scaleNoiseSpeed; }
            set { _scaleNoiseSpeed = value; }
        }

        #endregion

        #region Render Settings

        [SerializeField]
        Mesh[] _shapes;

        [SerializeField]
        Vector3 _baseScale = Vector3.one;

        public Vector3 baseScale {
            get { return _baseScale; }
            set { _baseScale = value; }
        }

        [SerializeField, Range(0, 1)]
        float _scaleRandomness = 0.1f;

        public float scaleRandomness {
            get { return _scaleRandomness; }
            set { _scaleRandomness = value; }
        }

        [SerializeField]
        Material _material;
        bool _owningMaterial; // whether owning the material

        public Material sharedMaterial {
            get { return _material; }
            set { _material = value; }
        }

        public Material material {
            get {
                if (!_owningMaterial) {
                    _material = Instantiate<Material>(_material);
                    _owningMaterial = true;
                }
                return _material;
            }
            set {
                if (_owningMaterial) Destroy(_material, 0.1f);
                _material = value;
                _owningMaterial = false;
            }
        }

        [SerializeField]
        ShadowCastingMode _castShadows;

        public ShadowCastingMode castShadows {
            get { return _castShadows; }
            set { _castShadows = value; }
        }

        [SerializeField]
        bool _receiveShadows = false;

        public bool receiveShadows {
            get { return _receiveShadows; }
            set { _receiveShadows = value; }
        }

        #endregion

        #region Editor Properties

        [SerializeField]
        bool _debug;

        #endregion

        #region Built-in Resources

        [SerializeField] Mesh _defaultShape;
        [SerializeField] Material _defaultMaterial;
        [SerializeField] Shader _kernelShader;
        [SerializeField] Shader _debugShader;

        #endregion

        #region Private Variables And Properties

        RenderTexture _positionBuffer;
        RenderTexture _rotationBuffer;
        RenderTexture _scaleBuffer;
        BulkMeshARC _bulkMesh;
        Material _kernelMaterial;
        Material _debugMaterial;
        bool _needsReset = true;

        Mesh[] SourceShapes {
            get {
                if (_shapes != null)
                    foreach (var m in _shapes)
                        if (m != null) return _shapes;
                return new Mesh[]{ _defaultShape };
            }
        }

        float XOffset {
            get { return Mathf.Repeat(_offset.x, _extent.x / _columns); }
        }

        float YOffset {
            get { return Mathf.Repeat(_offset.y, _extent.y / _rows); }
        }

        Vector2 UVOffset {
            get {
                return new Vector2(
                    -(_offset.x - XOffset) / _extent.x,
                    -(_offset.y - YOffset) / _extent.y);
            }
        }

        #endregion

        #region Resource Management

        public void NotifyConfigChange()
        {
            _needsReset = true;
        }

        Material CreateMaterial(Shader shader)
        {
            var material = new Material(shader);
            material.hideFlags = HideFlags.DontSave;
            return material;
        }

        RenderTexture CreateBuffer()
        {
            var buffer = new RenderTexture(_columns, _rows, 0, RenderTextureFormat.ARGBFloat);
            buffer.hideFlags = HideFlags.DontSave;
            buffer.filterMode = FilterMode.Point;
            buffer.wrapMode = TextureWrapMode.Repeat;
            return buffer;
        }

        void UpdateKernelShader()
        {
            var m = _kernelMaterial;

            // Shader uniforms

            m.SetVector("_ColumnRow", new Vector2(_columns, _rows));
            m.SetVector("_Extent", _extent);
            m.SetVector("_UVOffset", UVOffset);
            m.SetVector("_BaseScale", _baseScale);
            m.SetVector("_RandomScale", new Vector2(1 - _scaleRandomness, 1));

            var no_position = (_positionNoiseMode == PositionNoiseMode.Disabled);
            var pnoise = new Vector4(
                _positionNoiseFrequency * _extent.x,
                _positionNoiseFrequency * _extent.y,
                no_position ? 0.0f : _positionNoiseAmplitude,
                _positionNoiseSpeed * Time.time);
            m.SetVector("_PositionNoise", pnoise);

            var no_rotation = (_rotationNoiseMode == RotationNoiseMode.Disabled);
            var rnoise = new Vector4(
                _rotationNoiseFrequency * _extent.x,
                _rotationNoiseFrequency * _extent.y,
                no_rotation ? 0.0f : _rotationNoiseAmplitude * Mathf.Deg2Rad,
                _rotationNoiseSpeed * Time.time);
            m.SetVector("_RotationNoise", rnoise);

            var no_scale = (_scaleNoiseMode == ScaleNoiseMode.Disabled);
            var snoise = new Vector4(
                _scaleNoiseFrequency * _extent.x,
                _scaleNoiseFrequency * _extent.y,
                no_scale ? 0.0f : _scaleNoiseAmplitude,
                _scaleNoiseSpeed * Time.time);
            m.SetVector("_ScaleNoise", snoise);

            // Shader keywords

            if (_positionNoiseMode == PositionNoiseMode.XYZ)
            {
                m.EnableKeyword("POSITION_XYZ");
                m.DisableKeyword("POSITION_RANDOM");
            }
            else if (_positionNoiseMode == PositionNoiseMode.Random)
            {
                m.DisableKeyword("POSITION_XYZ");
                m.EnableKeyword("POSITION_RANDOM");
            }
            else
            {
                m.DisableKeyword("POSITION_XYZ");
                m.DisableKeyword("POSITION_RANDOM");
            }

            if (_rotationNoiseMode == RotationNoiseMode.Random)
            {
                m.EnableKeyword("ROTATION_RANDOM");
            }
            else
            {
                m.DisableKeyword("ROTATION_RANDOM");
                if (_rotationNoiseMode == RotationNoiseMode.XAxis)
                    m.SetVector("_RotationAxis", Vector3.right);
                else if (_rotationNoiseMode == RotationNoiseMode.YAxis)
                    m.SetVector("_RotationAxis", Vector3.up);
                else // ZAxis or Disabled
                    m.SetVector("_RotationAxis", Vector3.forward);
            }

            if (_scaleNoiseMode == ScaleNoiseMode.XYZ)
                m.EnableKeyword("SCALE_XYZ");
            else
                m.DisableKeyword("SCALE_XYZ");
        }

        void ResetResources()
        {
            if (_bulkMesh == null)
            {
                _bulkMesh = new BulkMeshARC(SourceShapes, 1);// _columns);
            }
            else
            {
                _bulkMesh.Rebuild(SourceShapes, 1);// _columns);
            }

            if (_positionBuffer) DestroyImmediate(_positionBuffer);
            if (_rotationBuffer) DestroyImmediate(_rotationBuffer);
            if (_scaleBuffer) DestroyImmediate(_scaleBuffer);

            _positionBuffer = CreateBuffer();
            _rotationBuffer = CreateBuffer();
            _scaleBuffer = CreateBuffer();

            if (!_kernelMaterial) _kernelMaterial = CreateMaterial(_kernelShader);
            if (!_debugMaterial) _debugMaterial = CreateMaterial(_debugShader);

            _needsReset = false;
        }

        #endregion

        #region MonoBehaviour Functions

        void Reset()
        {
            _needsReset = true;
        }

        void OnDestroy()
        {
            if (_bulkMesh != null) _bulkMesh.Release();
            if (_positionBuffer) DestroyImmediate(_positionBuffer);
            if (_rotationBuffer) DestroyImmediate(_rotationBuffer);
            if (_scaleBuffer)    DestroyImmediate(_scaleBuffer);
            if (_kernelMaterial) DestroyImmediate(_kernelMaterial);
            if (_debugMaterial)  DestroyImmediate(_debugMaterial);
        }

        void Update()
        {

            //v0.7
            if (recreateWall)
            {
                for(int i = 0; i < bricks.Count; i++)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(bricks[i]);
                    }
                    else
                    {
                        DestroyImmediate(bricks[i]);
                    }
                }
                bricks.Clear();

                recreateWall = false;
            }


            if (_needsReset) ResetResources();

            // Call the kernels.
            UpdateKernelShader();
            Graphics.Blit(null, _positionBuffer, _kernelMaterial, 0);
            Graphics.Blit(null, _rotationBuffer, _kernelMaterial, 1);
            Graphics.Blit(null, _scaleBuffer, _kernelMaterial, 2);

            // Make a material property block for the following drawcalls.
            var props = new MaterialPropertyBlock();
            props.SetTexture("_PositionTex", _positionBuffer);
            props.SetTexture("_RotationTex", _rotationBuffer);
            props.SetTexture("_ScaleTex", _scaleBuffer);
            props.SetVector("_ColumnRow", new Vector2(_columns, _rows));
            props.SetVector("_UVOffset", UVOffset);

            // Temporary variables.
            var mesh = _bulkMesh.mesh;
            var position = transform.position;
            var rotation = transform.rotation;
            var material = _material ? _material : _defaultMaterial;
            var uv = new Vector2(0.5f / _positionBuffer.width, 0);

            position += transform.right * XOffset;
            position += transform.up * YOffset;


            int maxCount = _positionBuffer.height;
            if (worldSpace)
            {
                maxCount = _rows;
            }

            //bricks.Clear();
            int bricksCount = 0;

            // Draw mesh segments.
            for (var i = 0; i < maxCount; i++)// _positionBuffer.height; i++)
            {
                for (var i1 = 0; i1 < _columns; i1++)//  _positionBuffer.height; i1++)
                {
                    //v0.1
                    //uv.x = (0.25f + i1) / _positionBuffer.height;
                    uv.x = (0.25f + i1) / _columns;// _positionBuffer.height;

                    uv.y = (0.5f + i) / _positionBuffer.height;


                    if (worldSpace)
                    {
                        props.SetVector("_BufferOffset", uv);// Vector2.zero * 1.0f);
                    }
                    else
                    {
                        props.SetVector("_BufferOffset", uv);
                    }


                    //v0.3
                    if (useSplinePath)
                    {
                        int splinePointsCount = splinePath.Curve.Count;
                        float pointOnSpline = (uv.x) * scaleArcX * splinePointsCount / ((1) * scaleArcX);
                        int splinePointID = (int)pointOnSpline;
                        //if (i == 0 && splinePointID < splinePath.Curve.Count)
                        //{
                        //    Debug.Log(splinePointID + ", curve IDs = " + splinePath.Curve.Count + ", Pos = " + splinePath.Curve[splinePointID].position);
                        //}
                        if (splinePointID > 0 && splinePointID < splinePath.Curve.Count)
                        {
                            // realPos.z = position.z + splinePath.Curve[splinePointID].position.z;
                            position.z =  splinePath.Curve[splinePointID].position.z;
                        }
                    }


                    Vector3 realPos = position + new Vector3((uv.x - 0.5f) * scaleArcX, (uv.y - 0.5f) * scaleArcY, 0);

                  

                    if (makeArcs)
                    {
                        bool draw_mesh = true;

                        for (var j = 0; j < arcs.Count; j++)
                        {
                            float arc_scale = arcs[j].localScale.x / 2;

                            realPos = position + new Vector3((uv.x - 0.5f) * scaleArcX * arc_scale, (uv.y - 0.5f) * scaleArcY * arc_scale, 0);
                            if (worldSpace)
                            {
                                uv.x = (0.5f + i1) / _columns;
                                uv.y = (0.5f + i) / _rows;// _positionBuffer.height;
                                realPos = position + new Vector3((uv.x - 0.0f) * scaleArcX, (uv.y - 0.0f) * scaleArcY, 0);
                            }

                            if (Vector3.Distance(realPos, arcs[j].position) > arc_scale)
                            //    if (Mathf.Abs(realPos.x - arcs[j].position.x) > arcs[j].localScale.x && Mathf.Abs(realPos.y - arcs[j].position.y) > arcs[j].localScale.y)//
                            {
                                //Graphics.DrawMesh(
                                //    mesh, position, rotation,
                                //    material, 0, null, 0, props,
                                //    _castShadows, _receiveShadows);
                            }
                            else
                            {
                                draw_mesh = false;
                                if (Vector3.Distance(realPos, arcs[j].position) > arc_scale - 0.5f * arcBorderHeight)
                                {
                                    float angle = ((180 / Mathf.PI) * Mathf.Acos(Vector3.Dot((realPos - arcs[j].position).normalized, Vector3.right)));
                                    //Debug.Log("Angle " + i + ", " + i1 + " = " + angle);
                                    float correctAngle = 1;
                                    if (realPos.x < arcs[j].position.x)//   angle > 90)
                                    {
                                        // correctAngle = 1;
                                    }

                                    Vector3 finPos = position;
                                    if (worldSpace)
                                    {
                                        finPos = realPos;
                                    }

                                    Graphics.DrawMesh(
                                      // mesh, position, Quaternion.LookRotation(realPos - arcs[j].position, Vector3.forward) * rotation,// rotation *,
                                      mesh, finPos + shiftArbBorderPos,//Quaternion.AngleAxis(45, Vector3.forward)*position, 
                                      Quaternion.AngleAxis(angle - 180 * correctAngle,
                                      -Vector3.forward),
                                       // *Quaternion.LookRotation( 
                                       //     Vector3.Cross((realPos - arcs[j].position).normalized, Vector3.forward) , 
                                       //    (realPos - arcs[j].position).normalized),// rotation *,
                                       material, 0, null, 0, props,
                                       _castShadows, _receiveShadows);
                                }
                            }
                        }


                        if (draw_mesh)
                        {
                            bricksCount++;

                            if (worldSpace)
                            {
                                Vector3 realPosA = position + new Vector3((uv.x - 0.5f) * scaleArcX, (uv.y - 0.5f) * scaleArcY, 0);
                                Graphics.DrawMesh(
                                mesh, realPos, rotation,
                                material, 0, null, 0, props,
                                _castShadows, _receiveShadows);

                                //if (createSceneMesh)
                                //{
                                //    if (bricks.Count < bricksCount) // maxCount * _columns)
                                //    {
                                //        GameObject bricky = Instantiate(brickSource[0], realPos, Quaternion.identity);
                                //        bricky.name = "B"+i+","+i1;
                                //        bricky.transform.parent = transform;
                                //        bricky.transform.localScale = baseScale * scaleSceneBricks;
                                //        bricks.Add(bricky);
                                //        if (sceneMeshOnlyColliders)
                                //        {
                                //            bricky.GetComponent<MeshRenderer>().enabled = false;
                                //        }
                                //        else
                                //        {
                                //            bricky.GetComponent<MeshRenderer>().enabled = true;
                                //        }
                                //    }
                                //    else
                                //    {
                                //        bricks[i * _columns + i1].transform.position = realPos;
                                //        bricks[i * _columns + i1].transform.parent = transform;
                                //        bricks[i * _columns + i1].transform.localScale = baseScale * scaleSceneBricks;
                                //        if (sceneMeshOnlyColliders)
                                //        {
                                //            bricks[i * _columns + i1].GetComponent<MeshRenderer>().enabled = false;
                                //        }
                                //        else
                                //        {
                                //            bricks[i * _columns + i1].GetComponent<MeshRenderer>().enabled = true;
                                //        }
                                //    }
                                //}

                            }
                            else
                            {
                                Graphics.DrawMesh(
                                mesh, position, rotation,
                                material, 0, null, 0, props,
                                _castShadows, _receiveShadows);
                            }
                        }

                        ////v0.7
                        if (createSceneMesh)
                        {
                            if (bricks.Count < maxCount * _columns)
                            {
                                GameObject bricky; 

                                if (draw_mesh)
                                {
                                    bricky = Instantiate(brickSource[0], realPos, Quaternion.identity);
                                    bricky.name = "B" + i + "," + i1;
                                }
                                else
                                {
                                    bricky = new GameObject();
                                    bricky.name = "Door" + i + "," + i1; ;
                                }
                                    
                                bricky.transform.parent = transform;
                                bricky.transform.localScale = baseScale * scaleSceneBricks;
                                bricks.Add(bricky);
                                if (sceneMeshOnlyColliders)
                                {
                                    MeshRenderer renderer = bricky.GetComponent<MeshRenderer>();
                                    if (renderer != null)
                                    {
                                        renderer.enabled = false;
                                    }
                                }
                                else
                                {
                                    MeshRenderer renderer = bricky.GetComponent<MeshRenderer>();
                                    if (renderer != null)
                                    {
                                        renderer.enabled = true;
                                    }
                                    //bricky.GetComponent<MeshRenderer>().enabled = true;
                                }
                            }
                            else
                            {
                                bricks[i * _columns + i1].transform.position = realPos;
                                bricks[i * _columns + i1].transform.parent = transform;
                                bricks[i * _columns + i1].transform.localScale = baseScale * scaleSceneBricks;
                                if (sceneMeshOnlyColliders)
                                {
                                    MeshRenderer renderer = bricks[i * _columns + i1].GetComponent<MeshRenderer>();
                                    if (renderer != null)
                                    {
                                        renderer.enabled = false;
                                    }
                                    //bricks[i * _columns + i1].GetComponent<MeshRenderer>().enabled = false;
                                }
                                else
                                {
                                    MeshRenderer renderer = bricks[i * _columns + i1].GetComponent<MeshRenderer>();
                                    if (renderer != null)
                                    {
                                        renderer.enabled = true;
                                    }
                                    //bricks[i * _columns + i1].GetComponent<MeshRenderer>().enabled = true;
                                }
                            }
                        }
                        ////v0.7

                    }
                    else
                    {
                        Graphics.DrawMesh(
                            mesh, position, rotation,
                            material, 0, null, 0, props,
                            _castShadows, _receiveShadows);
                    }
                }
            }


            for (var j = 0; j < bricks.Count; j++)
            {
                if (sceneMeshOnlyColliders)
                {
                    MeshRenderer renderer = bricks[j].GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        renderer.enabled = false;
                    }
                    //bricks[j].GetComponent<MeshRenderer>().enabled = false;
                }
                else
                {
                    MeshRenderer renderer = bricks[j].GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        renderer.enabled = true;
                    }
                    //bricks[j].GetComponent<MeshRenderer>().enabled = true;
                }
            }
        }

        void OnGUI()
        {
            if (_debug && Event.current.type.Equals(EventType.Repaint))
            {
                if (_debugMaterial && _positionBuffer && _rotationBuffer && _scaleBuffer)
                {
                    var rect = new Rect(0, 0, _columns, _rows);
                    Graphics.DrawTexture(rect, _positionBuffer, _debugMaterial);

                    rect.y += _rows;
                    Graphics.DrawTexture(rect, _rotationBuffer, _debugMaterial);

                    rect.y += _rows;
                    Graphics.DrawTexture(rect, _scaleBuffer, _debugMaterial);
                }
            }
        }

        #endregion
    }
}
