using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Artngame.INfiniDy;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace Artngame.TreeGEN.ProceduralIvy
{
    [ExecuteInEditMode]
    public class IvyObjectsManager : MonoBehaviour
    {
        //v0.1
        //SOURCE IVY GENERATORS
        public List<IvyGeneratorTREANT> sourceGenerators = new List<IvyGeneratorTREANT>();//insgantiate when making grid - samples add with "Add sample generators"

        //EDITOR IVY GENERATORS
        //maxflowerperholder = 1, so can erase per batched group
        //add collider per ivy = true if used in editor time
        //add global batcher and enable at game start for maximum optimization
        //set runTimePlanting = off for those that come from editor
        public List<IvyGeneratorTREANT> ivyGenerators = new List<IvyGeneratorTREANT>();//Editot time generators, apply global batching at game start
        public Transform EditorIvysHolder;

        //RUN TIME IVY GENERATORS
        public int maxIvysPerHolder = 5;
        public bool addRunTimeColliders = false;
        //set runTimePlanting = off for those that come from editor
        public List<IvyGeneratorTREANT> runTimeIvyGenerators = new List<IvyGeneratorTREANT>();//Run time generators, batching per N number of strokes
        public Transform RunTimeIvysHolder;
        public bool useSingleBranchMesh = false;

        //GRID - "Create Editor Ivy grid" (assign ivy generators to ivyGenerators) - "Create Run Time Ivy grid" buttons (assign ivy generators to runTimeIvyGenerators)
        public int gridTiles = 3; //3 will create 9 tiles around central
        public float coveredLandPerTile = 10; //Position = 20, plant entends = 10

        public void InitializeIvyGenerators()
        {
            for (int i = 0; i < ivyGenerators.Count; i++)
            {
                ivyGenerators[i].addColliderPerIvy = true;
                ivyGenerators[i].maxFlowerPerHolder = 1;
                ivyGenerators[i].runTimePlanting = false;
            }
            for (int i = 0; i < runTimeIvyGenerators.Count; i++)
            {
                runTimeIvyGenerators[i].addColliderPerIvy = addRunTimeColliders;
                runTimeIvyGenerators[i].maxFlowerPerHolder = maxIvysPerHolder;
                runTimeIvyGenerators[i].runTimePlanting = true;
                runTimeIvyGenerators[i].singleMeshBranch = useSingleBranchMesh;
            }
        }

        //v0.4
        public void ungrowIvy()
        {
            for (int i = 0; i < ivyGenerators.Count; i++)
            {
                ivyGenerators[i].UngrowIvy();
            }
        }
        public void regrowIvy()
        {
            for (int i = 0; i < ivyGenerators.Count; i++)
            {
                ivyGenerators[i].RegrowIvy();
            }
        }

        public void Start()
        {
            if (Application.isPlaying)
            {
                for (int i = 0; i < ivyGenerators.Count; i++)
                {
                    if (ivyGenerators[i].ungrown)
                    {
                        ivyGenerators[i].RegrowIvy();
                    }
                }
            }
        }

        public void createGridEditorIvys(bool runtime)
        {
            //instantate each of source ivy creators to the grid and setup
            for (int i = 0; i < gridTiles; i++)
            {
                for (int j = 0; j < gridTiles; j++)
                {
                    for (int k = 0; k < sourceGenerators.Count; k++)
                    {
                        Vector3 placement = new Vector3((i - (gridTiles - 1) / 2) * coveredLandPerTile * 2, 0, (j - (gridTiles - 1) / 2) * coveredLandPerTile * 2);

                        if (runtime)
                        {
                            if (RunTimeIvysHolder == null)
                            {
                                GameObject RunTimeIvyHolderOBJ = new GameObject();
                                RunTimeIvyHolderOBJ.name = "RunTimeIvysHolder";
                                RunTimeIvysHolder = RunTimeIvyHolderOBJ.transform;
                            }
                            GameObject instantiator = Instantiate(sourceGenerators[k].gameObject, placement, Quaternion.identity, RunTimeIvysHolder);
                            IvyGeneratorTREANT ivyGenerator = instantiator.GetComponent<IvyGeneratorTREANT>();

                            //RUN TIME
                            ivyGenerator.addColliderPerIvy = addRunTimeColliders;
                            ivyGenerator.maxFlowerPerHolder = maxIvysPerHolder;
                            ivyGenerator.runTimePlanting = true;
                            ivyGenerator.plantExtends = coveredLandPerTile;
                            ivyGenerator.flowersAdded = 0; //reset counter
                            ivyGenerator.singleMeshBranch = useSingleBranchMesh;
                            runTimeIvyGenerators.Add(ivyGenerator);
                        }
                        else
                        {
                            if (EditorIvysHolder == null)
                            {
                                GameObject EditorIvyHolderOBJ = new GameObject();
                                EditorIvyHolderOBJ.name = "EditorIvysHolder";
                                EditorIvysHolder = EditorIvyHolderOBJ.transform;
                            }
                            GameObject instantiator = Instantiate(sourceGenerators[k].gameObject, placement, Quaternion.identity, EditorIvysHolder);
                            IvyGeneratorTREANT ivyGenerator = instantiator.GetComponent<IvyGeneratorTREANT>();

                            //EDITOR TIME
                            ivyGenerator.addColliderPerIvy = true;
                            ivyGenerator.maxFlowerPerHolder = 1;
                            ivyGenerator.runTimePlanting = false;
                            ivyGenerator.plantExtends = coveredLandPerTile;
                            ivyGenerator.flowersAdded = 0; //reset counter
                            ivyGenerator.singleMeshBranch = false;
                            ivyGenerators.Add(ivyGenerator);
                        }
                    }
                }
            }
        }

        public void Awake()
        {
            InitializeIvyGenerators();
        }

        private void OnEnable()
        {
            InitializeIvyGenerators();
#if UNITY_EDITOR
            //UnityEditor.Undo.willFlushUndoRecord += OnUndoRedo;
            UnityEditor.Undo.undoRedoPerformed += OnUndoRedo;
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
#endif
        }

        public bool initialized = false;
        public void LateUpdate()
        {
            if (Application.isPlaying && !initialized)
            {
                //set parameters
                //add batchers and enable
                for (int i = 0; i < ivyGenerators.Count; i++)
                {
                    //v0.7
                    if (ivyGenerators[i].globalBatchEditorIvy)
                    {
                        GameObject globalBatchHolder = new GameObject();
                        globalBatchHolder.transform.parent = ivyGenerators[i].transform;
                        globalBatchHolder.transform.localPosition = new Vector3(0, 0, 0);
                        ControlCombineChildrenINfiniDyGrassLAND[] batchers = ivyGenerators[i].gameObject.GetComponentsInChildren<ControlCombineChildrenINfiniDyGrassLAND>();
                        ivyGenerators[i].editorIvyHolder = globalBatchHolder.transform;
                        for (int j = 0; j < batchers.Length; j++)
                        {
                            batchers[j].transform.parent = globalBatchHolder.transform;
                        }

                        //ControlCombineChildrenINfiniDyGrassLAND batcher = ivyGenerators[i].gameObject.AddComponent<ControlCombineChildrenINfiniDyGrassLAND>();
                        ControlCombineChildrenINfiniDyGrassLAND batcher = globalBatchHolder.AddComponent<ControlCombineChildrenINfiniDyGrassLAND>();
                        batcher.MakeActive = true;
                        batcher.Auto_Disable = true;

                        ivyGenerators[i].globallyBatched = true;
                    }
                }
                initialized = true;
            }
        }

#if UNITY_EDITOR
        [Serializable]
        public class Record
        {
            // for undo
            public int index = 0;
            public Color[] colors;
        }

        [SerializeField] Record m_record = new Record();
        int m_historyIndex = 0;

        public void PushUndo()
        {

            var mesh = IvyPainterUtils.GetMesh(gameObject);
            if (mesh != null)
            {
                m_record.index = m_historyIndex;
                m_historyIndex++;
                var colors = mesh.colors;
                m_record.colors = new Color[colors.Length];
                Array.Copy(colors, m_record.colors, colors.Length);
                Undo.RegisterCompleteObjectUndo(this, "Simple Vertex Painter [" + m_record.index + "]");
                //Debug.Log("Change Vertex Color");
            }
        }

        public void OnUndoRedoVertexPainter()
        {
            var mesh = IvyPainterUtils.GetMesh(gameObject);
            if (mesh == null) {
                return;
            }
            if (m_historyIndex != m_record.index)
            {
                m_historyIndex = m_record.index;
                if (m_record.colors != null && mesh.colors != null && m_record.colors.Length == mesh.colors.Length)
                {
                    mesh.colors = m_record.colors;
                    //Debug.Log("UndoRedo");
                }
            }
        }

        //v0.9
        public void PushUndoIvy()
        {
            //listen to growers strokes list, if item added register an undo
            Undo.RegisterCompleteObjectUndo(this, "Ivy Painter [" + "]");
        }
        public void OnUndoRedo()
        {
            int countPlants = 0;
            int countStrokes = 0;
            //bool isUndo = false;


         


            //perform last undo registered
            for (int i = 0; i < ivyGenerators.Count; i++)
            {
                foreach (Transform trans in ivyGenerators[i].transform)
                {
                    bool found = false;
                    //countPlants += ivyGenerators[i].flowerHolders.Count;
                    for (int j = 0; j < ivyGenerators[i].flowerHolders.Count; j++)
                    {
                        if (ivyGenerators[i].flowerHolders[j] == trans.gameObject)
                        {
                            found = true;
                        }
                    }
                    if (!found)
                    {
                        DestroyImmediate(trans.gameObject);
                        //isUndo = true;
                    }
                }

            }
            //Debug.Log("Plants before Undo" + countPlants);

            for (int i = 0; i < ivyGenerators.Count; i++)
            {
                for (int j = ivyGenerators[i].flowerHolders.Count - 1; j >= 0; j--)
                {
                    if (ivyGenerators[i].flowerHolders[j] == null)
                    {

                        countPlants += 1;
                        ivyGenerators[i].flowerHolders.RemoveAt(j);

                        //Undo.RegisterCompleteObjectUndo(ivyGenerators[i], "Ivy Painter Undo Redo [" + i + "]");

                        ivyGenerators[i].ReplantLastStroke(j);

                        //ivyGenerators[i].RegrowIvy();
                        //break;
                        //needRegrow = true;
                    }
                    else
                    {

                    }
                }
                countStrokes += ivyGenerators[i].ivyStrokes.Count;
                //if (needRegrow)
                //{
                //    //ivyGenerators[i].RegrowIvy();
                //}
            }

            for (int i = 0; i < ivyGenerators.Count; i++)
            {
                foreach (Transform trans in ivyGenerators[i].transform)
                {
                    bool found = false;
                    //countPlants += ivyGenerators[i].flowerHolders.Count;
                    for (int j = 0; j < ivyGenerators[i].flowerHolders.Count; j++)
                    {
                        if (ivyGenerators[i].flowerHolders[j] == trans.gameObject)
                        {
                            found = true;
                        }
                    }
                    if (!found)
                    {
                        DestroyImmediate(trans.gameObject);
                       // isUndo = true;
                    }
                }

            }


            //REDO handle
            //bool needRegrow = false;
            //if (!isUndo)
            //{

            //}
            //Debug.Log("Plants " + countPlants + ", Strokes "+ countStrokes);
        }

      

        private void OnDisable()
        {
            //UnityEditor.Undo.willFlushUndoRecord -= OnUndoRedo;
            UnityEditor.Undo.undoRedoPerformed -= OnUndoRedo;
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
#endif
    }
}
