using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
//using SVTXPainter;

namespace Artngame.TreeGEN.ProceduralIvy
{
    public class IvyPainterWindow : EditorWindow
    {
        #region Variables
        private GUIStyle titleStyle;
        private bool allowPainting = false;
        //private bool changingBrushValue = false;
        private bool allowSelect = false;
        private bool isPainting = false;
        private bool enableErase = false;
        private int enableGizmos = 2;//0 no gizmos, 1 icons, 2 plus box
        private bool isRecord = false;

        private Vector2 mousePos = Vector2.zero;
        private Vector2 lastMousePos = Vector2.zero;
        private RaycastHit curHit;

        //v0.8
        private float iconsSize = 0.5f;
        private const float MinIconsSize = 0.2f;
        public const float MaxIconsSize = 2f;

        private float brushSize = 0.5f;
        private float brushOpacity = 1f;
        private float brushFalloff = 0.1f;

        private Color brushColor;
        private float brushIntensity;

        private const float MinBrushSize = 0.01f;
        public const float MaxBrushSize = 10f;


        private int curColorChannel = (int)PaintType.All;

        private Mesh curMesh;
        private IvyObjectsManager m_target;
        private GameObject m_active;

        #endregion

        #region Main Method
        [MenuItem("Window/ARTnGAME/Ivy Painter")]
        public static void LauchVertexPainter()
        {
            var window = EditorWindow.GetWindow<IvyPainterWindow>();
            window.titleContent = new GUIContent("Ivy Painter");
            window.Show();
            window.OnSelectionChange();
            window.GenerateStyles();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui -= this.OnSceneGUI;
            SceneView.duringSceneGui += this.OnSceneGUI;
           // SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
            //SceneView.onSceneGUIDelegate += this.OnSceneGUI;
            if (titleStyle == null)
            {
                GenerateStyles();
            }
        }

        private void OnDestroy()
        {
             SceneView.duringSceneGui -= this.OnSceneGUI;
            //SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
        }

        private void OnSelectionChange()
        {
            m_target = null;
            m_active = null;
            curMesh = null;
            if (Selection.activeGameObject != null)
            {
                m_target = Selection.activeGameObject.GetComponent<IvyObjectsManager>();
                curMesh = IvyPainterUtils.GetMesh(Selection.activeGameObject);

                var activeGameObject = Selection.activeGameObject;
                if (curMesh != null)
                {
                    m_active = activeGameObject;
                }
            }
            allowSelect = (m_target == null);
           
            Repaint();
        }

        #endregion

        #region GUI Methods
        private void OnGUI()
        {
            //Header
            GUILayout.BeginHorizontal();
            GUILayout.Box("Ivy Painter", titleStyle, GUILayout.Height(60), GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            //Body
            GUILayout.BeginVertical(GUI.skin.box);

            if (m_target != null)
            {
                if (!m_target.isActiveAndEnabled)
                {
                    EditorGUILayout.LabelField("(Enable " + m_target.name + " to show Ivy Painter)");
                }
                else
                {
                    //bool lastAP = allowPainting;
                    allowPainting = GUILayout.Toggle(allowPainting, "Paint Mode");


                    //v0.1
                    enableErase = GUILayout.Toggle(enableErase, "Erase Mode");

                    //v0.9
                    //enableGizmos = (int)GUILayout.HorizontalSlider(enableGizmos, 0,2, GUILayout.Width(90));
                    enableGizmos = (int)EditorGUILayout.Slider("Gizmos:", enableGizmos, 0, 2);


                    if (allowPainting)
                    {
                        //Selection.activeGameObject = null;
                        Tools.current = Tool.None;
                    }


                    //GUILayout.BeginHorizontal();
                    //GUILayout.Label("Paint Type:", GUILayout.Width(90));
                    //string[] channelName = { "All", "R", "G", "B", "A" };
                    //int[] channelIds = { 0, 1, 2, 3, 4 };
                    //curColorChannel = EditorGUILayout.IntPopup(curColorChannel, channelName, channelIds, GUILayout.Width(50));
                    //GUILayout.EndHorizontal();
                    //GUILayout.BeginHorizontal();
                    //if (curColorChannel == (int)PaintType.All)
                    //{
                    //    brushColor = EditorGUILayout.ColorField("Brush Color:", brushColor);
                    //}
                    //else
                    //{
                    //    brushIntensity = EditorGUILayout.Slider("Intensity:", brushIntensity, 0, 1);
                    //}
                    //if (GUILayout.Button("Fill"))
                    //{
                    //    FillVertexColor();
                    //}
                    //GUILayout.EndHorizontal();
                    brushSize = EditorGUILayout.Slider("Brush Size:", brushSize, MinBrushSize, MaxBrushSize);

                    //v0.8
                    iconsSize = EditorGUILayout.Slider("Icons Size:", iconsSize, MinIconsSize, MaxIconsSize);

                    //brushOpacity = EditorGUILayout.Slider("Brush Opacity:", brushOpacity, 0, 1);
                    //brushFalloff = EditorGUILayout.Slider("Brush Falloff:", brushFalloff, MinBrushSize, brushSize);

                    //if (GUILayout.Button("Export .asset file") && curMesh != null)
                    //{
                    //    string path = EditorUtility.SaveFilePanel("Export .asset file", "Assets", IvyPainterUtils.SanitizeForFileName(curMesh.name), "asset");
                    //    if (path.Length > 0)
                    //    {
                    //        var dataPath = Application.dataPath;
                    //        if (!path.StartsWith(dataPath))
                    //        {
                    //            Debug.LogError("Invalid path: Path must be under " + dataPath);
                    //        }
                    //        else
                    //        {
                    //            path = path.Replace(dataPath, "Assets");
                    //            AssetDatabase.CreateAsset(Instantiate(curMesh), path);
                    //            Debug.Log("Asset exported: " + path);
                    //        }
                    //    }
                    //}




                    //Footer
                    //GUILayout.Label("Key Z:Turn on or off\nRight mouse button:Paint\nRight mouse button+Shift:Opacity\nRight mouse button+Ctrl:Size\nRight mouse button+Shift+Ctrl:Falloff\n", EditorStyles.helpBox);
                    Repaint();
                }
            }
            else if (m_active != null)
            {
                //if (GUILayout.Button("Add SVTX Object to " + m_active.name))
                //{
                //    m_active.AddComponent<IvyObjectsManager>();
                //    OnSelectionChange();
                //}
            }
            else
            {
                EditorGUILayout.LabelField("Please select an Ivy Editor gameobject");
                EditorGUILayout.LabelField("with the Ivy Objects Manager script.");
            }
            GUILayout.EndVertical();
        }

        //https://forum.unity.com/threads/camera-worldtoscreenpoint-bug.85311/
        //public static Vector2 WorldToScreenPointProjected(Camera camera, Vector3 worldPos)
        //{
        //    Vector3 camNormal = camera.transform.forward;
        //    Vector3 vectorFromCam = worldPos - camera.transform.position;
        //    float camNormDot = Vector3.Dot(camNormal, vectorFromCam);
        //    if (camNormDot <= 0)
        //    {
        //        // we are behind the camera forward facing plane, project the position in front of the plane
        //        Vector3 proj = (camNormal * camNormDot * 1.01f);
        //        worldPos = camera.transform.position + (vectorFromCam - proj);
        //    }

        //    return RectTransformUtility.WorldToScreenPoint(camera, worldPos);
        //}

        void OnSceneGUI(SceneView sceneView)
        {

            if (m_target != null)
            {

                //v0.8
                if (enableGizmos == 2)
                {
                    for (int i = 0; i < m_target.runTimeIvyGenerators.Count; i++)
                    {

                        //v0.8 - draw bounds iflimited
                        if (m_target.runTimeIvyGenerators[i].limitPlantingArea)
                        {
                            Handles.color = Color.magenta;
                            Handles.DrawWireCube(m_target.runTimeIvyGenerators[i].transform.position, new Vector3(1, 0.4f, 1) * m_target.runTimeIvyGenerators[i].plantExtends * 2);
                        }
                        else
                        {
                            Handles.color = Color.red;
                            Handles.DrawWireCube(m_target.runTimeIvyGenerators[i].transform.position, new Vector3(1, 0.7f, 1) * 1 * 2);
                            Handles.DrawWireCube(m_target.runTimeIvyGenerators[i].transform.position, new Vector3(1, 0.7f, 1) * 1.2f * 2);
                        }
                    }
                }

                for (int i = 0; i < m_target.ivyGenerators.Count; i++)
                {
                    if (enableGizmos == 2)
                    {
                        //v0.8 - draw bounds iflimited
                        if (m_target.ivyGenerators[i].limitPlantingArea)
                        {
                            Handles.color = Color.white;
                            Handles.DrawWireCube(m_target.ivyGenerators[i].transform.position, new Vector3(1, 0.5f, 1) * m_target.ivyGenerators[i].plantExtends * 2);
                        }
                        else
                        {
                            Handles.color = Color.yellow;
                            Handles.DrawWireCube(m_target.ivyGenerators[i].transform.position, new Vector3(1, 1, 1) * 1 * 2);
                            Handles.DrawWireCube(m_target.ivyGenerators[i].transform.position, new Vector3(1, 1, 1) * 1.2f * 2);
                        }
                    }
                    if (enableGizmos == 1 || enableGizmos == 2)
                    {
                        if (m_target.ivyGenerators[i].ungrown)
                        {
                            //show ungrown positions
                            for (int j = 0; j < m_target.ivyGenerators[i].ivyStrokes.Count; j++)
                            {
                                ////GUI
                                Handles.BeginGUI();
                                Vector3 gizmPos = m_target.ivyGenerators[i].ivyStrokes[j].position;
                                var sceneCamera = SceneView.currentDrawingSceneView.camera;
                                if (sceneCamera != null)
                                {
                                    Vector3 screenPos = sceneCamera.WorldToScreenPoint(gizmPos);
                                    var pixelRatio = UnityEditor.HandleUtility.GUIPointToScreenPixelCoordinate(Vector2.right).x
                                        - UnityEditor.HandleUtility.GUIPointToScreenPixelCoordinate(Vector2.zero).x;
                                    Vector2 size = Vector2.one * 50 * pixelRatio;
                                    Vector2 anchor = Vector2.zero;
                                    var alignedPosition = ((Vector2)screenPos + size * ((anchor + Vector2.left + Vector2.up) / 2f)) * (Vector2.right + Vector2.down) +
                                                            Vector2.up * sceneCamera.pixelHeight;

                                    Vector3 camF = sceneCamera.transform.forward;
                                    Vector3 posToCam = gizmPos - sceneCamera.transform.position;
                                    float camDot = Vector3.Dot(camF, posToCam);
                                    if (camDot > 0)
                                    {
                                        GUI.Label(new Rect(alignedPosition / pixelRatio, size * iconsSize / pixelRatio), (Texture2D)Resources.Load("ivyGizmoUNGROWN"));
                                    }
                                }
                                Handles.EndGUI();
                                //// END GUI
                            }
                        }


                        for (int j = 0; j < m_target.ivyGenerators[i].flowerHolders.Count; j++)
                        {
                            //Handles.Label(m_target.ivyGenerators[i].flowerHolders[j].transform.position, (Texture2D)Resources.Load("ivyGizmo"));
                            // m_target.ivyGenerators[i].sanitizeHoldersList();
                            //if (m_target.ivyGenerators[i].flowerHolders.Count > 0)
                            {
                                BoxCollider boxCol = m_target.ivyGenerators[i].flowerHolders[j].GetComponent<BoxCollider>();
                                if (boxCol != null)
                                {
                                    Vector3 gizmPos = boxCol.center;
                                    //Handles.Label(gizmPos, (Texture2D)Resources.Load("ivyGizmo"));
                                    //Gizmos.DrawIcon(gizmPos, "ivyGizmo.png", true);
                                    Handles.BeginGUI();
                                    //if (GUILayout.Button( ("Reset Area", GUILayout.Width(100)))
                                    //{                            
                                    //}
                                    var sceneCamera = SceneView.currentDrawingSceneView.camera;
                                    if (sceneCamera != null) //(Camera.current != null)
                                    {
                                        //Vector2 poss = WorldToScreenPointProjected(sceneCamera, gizmPos);
                                        Vector3 screenPos = sceneCamera.WorldToScreenPoint(gizmPos);// Camera.current.WorldToScreenPoint(gizmPos); //WorldToViewportPoint(gizmPos);// WorldToScreenPoint(gizmPos);
                                                                                                    //GUI.Label(new Rect(screenPos.x, Camera.current.pixelHeight - screenPos.y, 100, 100), (Texture2D)Resources.Load("ivyGizmo"));
                                                                                                    // GUI.Label(new Rect(screenPos.x, SceneView.currentDrawingSceneView.position.height - screenPos.y, 100, 100), (Texture2D)Resources.Load("ivyGizmo"));
                                                                                                    // GUI.Label(new Rect(poss.x, SceneView.currentDrawingSceneView.position.height - poss.y, 100, 100), (Texture2D)Resources.Load("ivyGizmo"));

                                        //https://gist.github.com/Arakade/9dd844c2f9c10e97e3d0
                                        var pixelRatio = UnityEditor.HandleUtility.GUIPointToScreenPixelCoordinate(Vector2.right).x - UnityEditor.HandleUtility.GUIPointToScreenPixelCoordinate(Vector2.zero).x;
                                        Vector2 size = Vector2.one * 50 * pixelRatio;
                                        Vector2 anchor = Vector2.zero;
                                        var alignedPosition = ((Vector2)screenPos + size * ((anchor + Vector2.left + Vector2.up) / 2f)) * (Vector2.right + Vector2.down) +
                                                                Vector2.up * sceneCamera.pixelHeight;

                                        Vector3 camF = sceneCamera.transform.forward;
                                        Vector3 posToCam = gizmPos - sceneCamera.transform.position;
                                        float camDot = Vector3.Dot(camF, posToCam);
                                        if (camDot > 0)
                                        {
                                            GUI.Label(new Rect(alignedPosition / pixelRatio, size * iconsSize / pixelRatio), (Texture2D)Resources.Load("ivyGizmo"));
                                        }

                                    }
                                    Handles.EndGUI();
                                }
                            }
                        }

                    }//END check Gizomos level
                }
            }

            if (allowPainting )
            {
                //bool isHit = false; //v0.9
                if (!allowSelect)
                {
                    HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                }
                Ray worldRay = HandleUtility.GUIPointToWorldRay(mousePos);
               
                //v0.1
                if (Selection.activeGameObject != null)
                {
                    m_target = Selection.activeGameObject.GetComponent<IvyObjectsManager>();

                    //MASS ERASE
                    RaycastHit hit2 = new RaycastHit();
                    if (Physics.Raycast(worldRay, out hit2, Mathf.Infinity))
                    {
                        Handles.color = Color.white;

                        //v0.2
                        if (enableErase)
                        {
                            Handles.color = Color.red;
                        }

                        Handles.DrawWireDisc(hit2.point, hit2.normal, brushSize);
                        Handles.DrawWireDisc(hit2.point, hit2.normal, brushSize*0.95f);

                        Handles.DrawWireDisc(hit2.point, hit2.normal, brushSize*0.05f);
                        Handles.DrawWireDisc(hit2.point, hit2.normal, brushSize * 0.1f);
                    }


                    if (m_target != null && isPainting)
                    {
                        //Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                        RaycastHit hit;
                        if (Physics.Raycast(worldRay, out hit, 1000))
                        {
                            if (!enableErase) //PLANT IVY
                            {
                                for (int i = 0; i < m_target.ivyGenerators.Count; i++)
                                {

                                    //if (m_target.ivyGenerators[i].plantLayerMask == hit.collider.gameObject.layer) { //v1.0
                                    //if ((m_target.ivyGenerators[i].plantLayerMask & (1 << hit.collider.gameObject.layer)) != 0) { 
                                    if (!m_target.ivyGenerators[i].confineToLayers || (m_target.ivyGenerators[i].plantLayerMask & (1 << hit.collider.gameObject.layer)) != 0)
                                    {

                                        //v0.9
                                        if (!m_target.ivyGenerators[i].limitPlantingArea || (m_target.ivyGenerators[i].limitPlantingArea &&
                                             hit.point.x < m_target.ivyGenerators[i].transform.position.x + m_target.ivyGenerators[i].plantExtends &&
                                             hit.point.x > m_target.ivyGenerators[i].transform.position.x - m_target.ivyGenerators[i].plantExtends &&
                                             hit.point.z < m_target.ivyGenerators[i].transform.position.z + m_target.ivyGenerators[i].plantExtends &&
                                             hit.point.z > m_target.ivyGenerators[i].transform.position.z - m_target.ivyGenerators[i].plantExtends
                                    ))
                                    {
                                        Undo.RegisterCompleteObjectUndo(m_target.ivyGenerators[i], "Ivy Painter [" + i + "]");
                                        //Undo.RegisterCompleteObjectUndo(m_target.ivyGenerators[i].flowerHolders[m_target.ivyGenerators[i].flowerHolders.Count - 1].gameObject, "Ivy Painter Obj [" + i + "]");
                                    }

                                    m_target.ivyGenerators[i].generateIvyA(hit);

                                    //v0.9
                                    //if (!m_target.ivyGenerators[i].limitPlantingArea || (m_target.ivyGenerators[i].limitPlantingArea &&
                                    //         hit.point.x < m_target.ivyGenerators[i].transform.position.x + m_target.ivyGenerators[i].plantExtends &&
                                    //         hit.point.x > m_target.ivyGenerators[i].transform.position.x - m_target.ivyGenerators[i].plantExtends &&
                                    //         hit.point.z < m_target.ivyGenerators[i].transform.position.z + m_target.ivyGenerators[i].plantExtends &&
                                    //         hit.point.z > m_target.ivyGenerators[i].transform.position.z - m_target.ivyGenerators[i].plantExtends
                                    //))
                                    //{
                                    //    //Undo.RegisterCompleteObjectUndo(m_target.ivyGenerators[i], "Ivy Painter [" + i + "]");
                                    //    //Undo.RegisterCompleteObjectUndo(m_target.ivyGenerators[i].flowerHolders[m_target.ivyGenerators[i].flowerHolders.Count-1].gameObject, "Ivy Painter Obj [" + i + "]");
                                    //}

                                    //v0.9
                                    //IvyStrokeProperties push
                                }
                                }
                            }
                            else //ERASE IVY
                            {
                                RaycastHit[] hits = Physics.SphereCastAll(worldRay, brushSize, Mathf.Infinity);
                                if (hits != null & hits.Length > 0)
                                {
                                    for (int j = 0; j < hits.Length; j++)
                                    {
                                        RaycastHit hit1 = hits[j];
                                        if (hit1.collider != null && hit1.collider.gameObject.name == "IVY")
                                        {
                                            //destroy
                                            DestroyImmediate(hit1.collider.gameObject);
                                        }
                                    }
                                }
                                for (int i = 0; i < m_target.ivyGenerators.Count; i++)
                                {
                                    m_target.ivyGenerators[i].sanitizeHoldersList();
                                }
                            }
                        }
                    }
                }
                   

                //v0.9
                //if (m_target != null && curMesh != null)
                //{
                //    Matrix4x4 mtx = m_target.transform.localToWorldMatrix;
                //    RaycastHit tempHit = new RaycastHit();
                //    isHit = false;// RXLookingGlass.IntersectRayMesh(worldRay, curMesh, mtx, out tempHit);
                //    if (isHit)
                //    {
                //        if (!changingBrushValue)
                //        {
                //            curHit = tempHit;
                //        }
                //        //Debug.Log("ray cast success");
                //        if (isPainting && m_target.isActiveAndEnabled && !changingBrushValue)
                //        {
                //            PaintVertexColor();
                //        }
                //    }
                //}

                //if (isHit|| changingBrushValue)
                //{

                //    Handles.color = getSolidDiscColor((PaintType)curColorChannel);
                //    Handles.DrawSolidDisc(curHit.point, curHit.normal, brushSize);
                //    Handles.color = getWireDiscColor((PaintType)curColorChannel);
                //    Handles.DrawWireDisc(curHit.point, curHit.normal, brushSize);
                //    Handles.DrawWireDisc(curHit.point, curHit.normal, brushFalloff);
                //}
            }


            ProcessInputs();

            sceneView.Repaint();

        }

        private void OnInspectorUpdate()
        {
            OnSelectionChange();
        }
        #endregion

        #region TempPainter Method
        void PaintVertexColor()
        {
            if (m_target&&m_active)
            {
                curMesh = IvyPainterUtils.GetMesh(m_active);
                if (curMesh)
                {
                    if (isRecord)
                    {
                        m_target.PushUndo();
                        isRecord = false;
                    }
                    Vector3[] verts = curMesh.vertices;
                    Color[] colors = new Color[0];
                    if (curMesh.colors.Length > 0)
                    {
                        colors = curMesh.colors;
                    }
                    else
                    {
                        colors = new Color[verts.Length];
                    }
                    for (int i = 0; i < verts.Length; i++)
                    {
                        Vector3 vertPos = m_target.transform.TransformPoint(verts[i]);
                        float mag = (vertPos - curHit.point).magnitude;
                        if (mag > brushSize)
                        {
                            continue;
                        }
                        float falloff = IvyPainterUtils.LinearFalloff(mag, brushSize);
                        falloff = Mathf.Pow(falloff, Mathf.Clamp01(1 - brushFalloff / brushSize)) * brushOpacity;
                        if (curColorChannel == (int)PaintType.All)
                        {
                            colors[i] = IvyPainterUtils.VTXColorLerp(colors[i], brushColor, falloff);
                        }
                        else
                        {
                            colors[i] = IvyPainterUtils.VTXOneChannelLerp(colors[i], brushIntensity, falloff, (PaintType)curColorChannel);
                        }
                        //Debug.Log("Blend");
                    }
                    curMesh.colors = colors;
                }
                else
                {
                    OnSelectionChange();
                    Debug.LogWarning("Nothing to paint!");
                }
               
            }
            else
            {
                OnSelectionChange();
                Debug.LogWarning("Nothing to paint!");
            }
        }

        void FillVertexColor()
        {
            if (curMesh)
            {
                Vector3[] verts = curMesh.vertices;
                Color[] colors = new Color[0];
                if (curMesh.colors.Length > 0)
                {
                    colors = curMesh.colors;
                }
                else
                {
                    colors = new Color[verts.Length];
                }
                for (int i = 0; i < verts.Length; i++)
                {
                    if (curColorChannel == (int)PaintType.All)
                    {
                        colors[i] = brushColor;
                    }
                    else
                    {
                        colors[i] = IvyPainterUtils.VTXOneChannelLerp(colors[i], brushIntensity, 1, (PaintType)curColorChannel);
                    }
                    //Debug.Log("Blend");
                }
                curMesh.colors = colors;
            }
            else
            {
                Debug.LogWarning("Nothing to fill!");
            }
        }
        #endregion

        #region Utility Methods
        void ProcessInputs()
        {
            if (m_target == null)
            {
                return;
            }
            Event e = Event.current;
            mousePos = e.mousePosition;
            if (e.type == EventType.KeyDown)
            {
                if (e.isKey)
                {
                    if (e.keyCode == KeyCode.Z)
                    {
                        allowPainting = !allowPainting;
                        if (allowPainting)
                        {
                            Tools.current = Tool.None;
                        }
                    }

                    //v0.2
                    if (e.keyCode == KeyCode.LeftShift)
                    {
                        if (enableErase)
                        {
                            enableErase = false;
                        }
                        else
                        {
                            enableErase = true;
                        }
                    }

                }
            }
            if (e.type == EventType.MouseUp)
            {
                //changingBrushValue = false;
                isPainting = false;

            }
            if (lastMousePos == mousePos)
            {
                isPainting = false;
            }
            if (allowPainting)
            {
                if (e.type == EventType.MouseDrag && e.control && e.button == 0 && !e.shift)
                {
                    brushSize += e.delta.x * 0.005f;
                    brushSize = Mathf.Clamp(brushSize, MinBrushSize, MaxBrushSize);
                    brushFalloff = Mathf.Clamp(brushFalloff, MinBrushSize, brushSize);
                    //changingBrushValue = true;
                }
                if (e.type == EventType.MouseDrag && !e.control && e.button == 0 && e.shift)
                {
                    brushOpacity += e.delta.x * 0.005f;
                    brushOpacity = Mathf.Clamp01(brushOpacity);
                    //changingBrushValue = true;
                }
                if (e.type == EventType.MouseDrag && e.control && e.button == 0 && e.shift)
                {
                    brushFalloff += e.delta.x * 0.005f;
                    brushFalloff = Mathf.Clamp(brushFalloff, MinBrushSize, brushSize);
                    //changingBrushValue = true;
                }
                if ((e.type == EventType.MouseDrag || e.type == EventType.MouseDown) && !e.control && e.button == 0 && !e.shift && !e.alt)
                {
                    isPainting = true;
                    if (e.type == EventType.MouseDown)
                    {
                        isRecord = true;
                    }
                }
            }
            lastMousePos = mousePos;
        }
        void GenerateStyles()
        {
            titleStyle = new GUIStyle();
            titleStyle.border = new RectOffset(3, 3, 3, 3);
            titleStyle.margin = new RectOffset(2, 2, 2, 2);
            titleStyle.fontSize = 25;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleCenter;
        }

        Color getSolidDiscColor(PaintType pt)
        {
            switch (pt)
            {
                case PaintType.All:
                    return new Color(brushColor.r, brushColor.g, brushColor.b, brushOpacity);
                case PaintType.R:
                    return new Color(brushIntensity, 0, 0, brushOpacity);
                case PaintType.G:
                    return new Color(0, brushIntensity, 0, brushOpacity);
                case PaintType.B:
                    return new Color(0, 0, brushIntensity, brushOpacity);
                case PaintType.A:
                    return new Color(brushIntensity, 0, brushIntensity, brushOpacity);

            }
            return Color.white;
        }
        Color getWireDiscColor(PaintType pt)
        {
            switch (pt)
            {
                case PaintType.All:
                    return new Color(1 - brushColor.r, 1 - brushColor.g, 1 - brushColor.b, 1);
                case PaintType.R:
                    return Color.white;
                case PaintType.G:
                    return Color.white;
                case PaintType.B:
                    return Color.white;
                case PaintType.A:
                    return Color.white;
            }
            return Color.white;
        }
        #endregion

    }

}