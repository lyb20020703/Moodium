using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Artngame.INfiniDy;
namespace Artngame.TreeGEN.ProceduralIvy
{

    //v0.2
    [System.Serializable]
    public class IvyStrokeProperties //keep history for ungrow
    {
        public int randomSeed;
        public Vector3 position;
        public Vector3 normal;
        public float flowerChance;
        public float offsetRadius;
        public int branchCount;
        public GameObject leavesPrefab;
        public GameObject flowersPrefab;
        public float ivyTrailLength;
        public float ivyTrailWidth;
        public float normalMult;
        public float branchPartLength;
        public int maxBranchPositions;
        public float zigZagPower;
        public Vector4 curvingPower;
        public Vector4 gravityFactor;
        public float cutOffDistance;
        public float surfaceOffset;
        public bool addLeaves = false;
        public Vector4 directionality;//v0.9
        //v0.9
        public float FlowerScale=1;
        public float LeafScale=1;
        public Vector2 flowerScaleMinMax = new Vector2(1.0f, 1.0f);
        public Vector2 leafScaleMinMax = new Vector2(1.0f, 1.0f);
    }

    //v0.2
    public class IvyPosition
    {
        public Vector3 position;
        public Vector3 normal;
        public IvyPosition(Vector3 position, Vector3 normal)
        {
            this.position = position;
            this.normal = normal;
        }
    }

    public class IvyGeneratorTREANT : MonoBehaviour
    {
        //GENERAL OPTIONS
        [Header("------------------------------------------------------")]
        [Header("Player Setup")]
        [Header("------------------------------------------------------")]
        [Tooltip("Select player where LODs will be calculated from. If not assigned Camera.Main is used.")]
        public Transform player;//v0.2

        //LAYERS CHOICE
        public bool confineToLayers = false;
        public LayerMask plantLayerMask = 1;//v1.0

        //RUN TIME PLANTING
        [Header("------------------------------------------------------")]
        [Header("Run Time Planting")]
        [Header("------------------------------------------------------")]
        [Tooltip("Enable Run time planting")]
        public bool runTimePlanting = false; //v0.4
        [Tooltip("Choose mouse button for Run time planting, 0 is left, 1 is right")]
        public int plantMouseButton = 0; //v0.5
        public bool addColliderPerIvy = false;//v0.3
        public float colliderScale = 0.1f;//v0.3 
        [Tooltip("Enable Ivy gradual growth")]
        public bool growIvy = false;
        public bool growFlowers = false;
        public float growIvySpeed = 1f;
        public float growFlowerSpeed = 1f;
        [Tooltip("Material used for growth before model is batched with common material assigned after grown")]
        public Material growIvyMaterial;
        public List<Material> growingMaterials = new List<Material>();
        public List<float> growthAmount = new List<float>();
        public List<Transform> growingItems = new List<Transform>();
        public List<Transform> growingItemsBranch = new List<Transform>();

        //OPTIMIZATIONS
        [Header("------------------------------------------------------")]
        [Header("Batching and Optimizations")]
        [Header("------------------------------------------------------")]
        [Tooltip("Batch all the individual IVY batched models used during edit time, when game starts.")]
        public bool globalBatchEditorIvy = true; //v0.7
        [Tooltip("Use a single mesh for all Ivy of an Ivy generator script. Not applies to 3D branch case.")]
        public bool singleMeshBranch = false;//v0.3
        [Tooltip("Set seed for randomize of brances, use same when regrow the Ivy to get same branch positions")]
        public int randomSeed = 1;//v0.5
        [Tooltip("Enable LOD disable of distant Ivy generators")]
        public bool autoLOD = false; //disable after a number of plantExtends //v0.2
        [Tooltip("Limit planting around Ivy generator")]
        public bool limitPlantingArea = false; //v0.2
        [Tooltip("Distance Limit of planting around Ivy generator")]
        public float plantExtends = 100;//how far can plant with this planter, can be used to limit if a LOD system is used to disable planters//v0.2
        [Tooltip("Enable LOD at this variable, times plantExtends, distance")]
        public int plantExtendsLODLimit = 2; //v0.2
        [Tooltip("How many strokes of run time Ivy plant strokes before batch them for optimization")]
        public int maxFlowerPerHolder = 25;//flowers added to holder, if more than maxFlowerPerHolder, batch and create new holder

        //LEAVES - FLOWERS
        [Header("------------------------------------------------------")]
        [Header("Leaves and Flowers")]
        [Header("------------------------------------------------------")]
        [Tooltip("Add leaves and flowers to the Ivy.")]
        public bool addLeaves = false;
        [Tooltip("Higher will give less leaves and flowers.")]
        public int flowerDivide = 9;  //v0.6 3D branch
        [Tooltip("Lower chance of a flower appear.")]
        public float flowerChance = 0.055f;//v0.2
        [Tooltip("Offset of flowers - leaves above the branch center.")]
        public float offsetRadius = 0.03f;//v0.2

        //v0.9
        public float FlowerScale = 1;
        public float LeafScale = 1;
        public Vector2 flowerScaleMinMax = new Vector2(1.0f, 1.0f);
        public Vector2 leafScaleMinMax = new Vector2(1.0f, 1.0f);

        //BRANCH OPTIONS
        [Header("------------------------------------------------------")]
        [Header("Ivy Branch Controls")]
        [Header("------------------------------------------------------")]
        [Tooltip("Use fully 3D Branch model. If disabled will use a more optimized 2D model.")]
        public bool use3dbranch = false; //v0.6 3D branch
        [Tooltip("Size of 3D Branch model.")]
        public float branch3dSize = 0.03f; //v0.6 3D branch
        [Tooltip("Radial Detail of 3D Branch model mesh.")]
        public int branch3dDivisions = 8; //v0.6 3D branch
        [Tooltip("Make branch more narrow near edges.")]
        public float narrowFactor = 0.03f; //v0.6 3D branch
        [Tooltip("Choose spider web like branch spreading.")]
        public bool spiderweb = false; //v0.6 3D branch
        [Tooltip("Number of branches to spread from the plant point")]
        public int branchCount = 7;

        //v0.9
        public Vector4 directionality = new Vector4(0,0,0,0); //if zero not apply directionality
        public bool useCameraDirection = false;//assign directionality by camera forward
        public bool useCameraMouseDirection = false;

        [Tooltip("Length of Ivy branch parts, larger will make trail longer")]
        public float branchPartLength = 0.1f;
        [HideInInspector]
        public float ivyTrailLength = 0.5f;
        [Tooltip("Width of Ivy branch when 2D mode is active")]
        public float ivyTrailWidth = 0.1f;
        [Tooltip("Stop connecting last branch point to plant origin by check for this distance, when 2D mode is active")]
        public float cutOffDistance = 1; //for 2D branch, stop connecting last branch to planting origin by check for this distance
        [Tooltip("Move raycast points above surface along its normal by this factor when planting.")]
        public float normalMult = 0.012f;
        [Tooltip("Move branch points above surface along its normal by this factor, when 2D mode is active.")]
        public float surfaceOffset = 0.1f;
        [Tooltip("Ivy branch parts count, larger number will make for a bigger Ivy model")]
        public int maxBranchPositions = 30;
        [Tooltip("Branch angle randomizer factor")]
        public float zigZagPower = 1;        
        [Tooltip("Branch gravity factor, can use plus or minus to move Ivy up or down")]
        public Vector4 gravityFactor = Vector4.zero;
        [Tooltip("Branch curving factor, curve branches mid way, use in combination with gravity for various shapes.")]
        public Vector4 curvingPower = Vector4.zero;

        //MATERIALS - PREFABS
        [Header("------------------------------------------------------")]
        [Header("Ivy Leaves and Flowers prefabs")]
        [Header("------------------------------------------------------")]
        [Tooltip("Leaves prefab to be placed in certain intervals along the Ivy mesh.")]
        public GameObject leavesPrefab;//v0.2
        [Tooltip("Flowers prefab to be placed in certain intervals along the Ivy mesh.")]
        public GameObject flowersPrefab;//v0.2
        [Tooltip("Material to be used for the Ivy mesh.")]
        public Material branchMaterial;//v0.3                             

        //STATE
        [Header("------------------------------------------------------")]
        [Header("Ivy Generator States for Debug Purposes")]
        [Header("------------------------------------------------------")]
        [Tooltip("Ivy generated by this creator has been ungrown and no actual Ivy exist on scene. Regrow will recreate the Ivy based on IvyStrokes struct.")]
        public bool ungrown = false;
        [Tooltip("Ivy from editor time parented to this holder so can be globally batched in game start.")]
        public Transform editorIvyHolder; //v0.7
        [Tooltip("Ivy has been globally batched at game start.")]
        public bool globallyBatched = false;//signal when editor grass gets globally bached per creator script //v0.7
        //v0.2
        [Tooltip("Ivy batching groups, one Ivy stroke per group for editor Ivy, maxFlowerPerHolder strokes for run time.")]
        public List<GameObject> flowerHolders = new List<GameObject>();
        [Tooltip("Ivy strokes added currently, when reach maxFlowerPerHolder batch and create new batch group")]
        public int flowersAdded = 0; //flowers added to holder, if more than maxFlowerPerHolder, batch and create new holder
        //v0.4
        [Tooltip("Ivy strokes information saved for regrowing the Ivy, after an Ungrow operation.")]
        [SerializeField]
        public List<IvyStrokeProperties> ivyStrokes = new List<IvyStrokeProperties>();

        [HideInInspector]
        public bool create3dmesh = false;                      

        //LOCAL VARIABLES
        int ivyNumber = 0;
        private Mesh ivy_mesh;
        private Vector3 ivy_vertexL = new Vector3(0.1f / 2, 0, 0);
        private Vector3 ivy_vertexR = new Vector3(0.1f / 2, 0, 0);
        private List<Vector3> vertices = new List<Vector3>();
        private Vector2[] uvs;
        private int[] triangles;
        private int[] trianglesLEAVES;

        //v0.6
        float scaleMe(float value, float prevLow, float prevHigh, float Low, float High)
        {
            float p = Mathf.InverseLerp(prevLow, prevHigh, value);
            return Mathf.Lerp(Low, High, p);
        }       
        void generate3Dbranch(Mesh outMesh, List<IvyPosition> branches)
        {
            Vector3[] vertices = new Vector3[4 * branches.Count * branch3dDivisions];
            int[] triangles = new int[6 * (branches.Count - 1) * branch3dDivisions];
            Vector2[] uv = new Vector2[4 * branches.Count * branch3dDivisions];
            Vector3[] normals = new Vector3[4 * branches.Count * branch3dDivisions];

            for (int i = 0; i < branches.Count; i++)
            {
                float step = Mathf.PI * 2 / branch3dDivisions;

                Vector3 vectorAlongBranch = Vector3.zero;
                if (i != 0)
                {
                    vectorAlongBranch = branches[i - 1].position - branches[i].position;
                }
                if (i != branches.Count - 1)
                {
                    vectorAlongBranch += branches[i].position - branches[i + 1].position;
                }
                if (vectorAlongBranch == Vector3.zero)
                {
                    vectorAlongBranch = Vector3.forward;
                }
                vectorAlongBranch.Normalize();

                Vector3 normVector = branches[i].normal.normalized;

                for (int k = 0; k < branch3dDivisions; k++)
                {
                    Quaternion direction = Quaternion.LookRotation(vectorAlongBranch, normVector);
                    Vector3 xA = Vector3.up;
                    Vector3 yA = Vector3.right;
                    Vector3 position = branches[i].position;
                    position += direction * xA * (branch3dSize * Mathf.Sin(k * step));
                    position += direction * yA * (branch3dSize * Mathf.Cos(k * step));
                    vertices[k + i * branch3dDivisions] = position;

                    Vector3 offset = position - branches[i].position;
                    normals[k + i * branch3dDivisions] = offset.normalized;

                    float scaledUVs = scaleMe(i, 0, branches.Count - 1, 0, 1);
                    uv[k + i * branch3dDivisions] = new Vector2((float)k / branch3dDivisions, scaledUVs);
                    position = position - normals[i * branch3dDivisions + k] * narrowFactor * scaledUVs;
                    vertices[k + i * branch3dDivisions] = position;
                }

                if (branches.Count > i + 1)
                {
                    for (int e = 0; e < branch3dDivisions; e++)
                    {
                        int triIndex = i * branch3dDivisions * 6 + e * 6;
                        int baseVertexIndex = branch3dDivisions * i;
                        int nextVertexIndex = branch3dDivisions * (i + 1);
                        int div = (e + 1) % branch3dDivisions;
                        int divP = e % branch3dDivisions;
                        if (spiderweb)
                        {
                            triangles[triIndex + 0] = div + baseVertexIndex;
                            triangles[triIndex + 1] = e + baseVertexIndex;
                            triangles[triIndex + 2] = div + nextVertexIndex;
                            triangles[triIndex + 3] = div + nextVertexIndex;
                            triangles[triIndex + 4] = e + branch3dDivisions;
                            triangles[triIndex + 5] = divP + nextVertexIndex;
                        }
                        else
                        {
                            triangles[triIndex + 0] = div + baseVertexIndex;
                            triangles[triIndex + 1] = e + baseVertexIndex;
                            triangles[triIndex + 2] = div + nextVertexIndex;
                            triangles[triIndex + 3] = div + nextVertexIndex;
                            triangles[triIndex + 4] = e + i * branch3dDivisions;
                            triangles[triIndex + 5] = divP + nextVertexIndex;
                        }
                    }
                }
            }
            outMesh.vertices = vertices;
            outMesh.triangles = triangles;
            outMesh.normals = normals;
            outMesh.uv = uv;
            outMesh.RecalculateBounds();
            outMesh.RecalculateNormals();
            outMesh.RecalculateTangents();
        }

        
       

       

        public void Start()
        {
            //v0.1
            ivy_vertexL = new Vector3(ivyTrailWidth / 2, 0, 0);
            ivy_vertexR = new Vector3(-ivyTrailWidth / 2, 0, 0);
            if (Application.isPlaying)
            {
                ivy_mesh = GetComponent<MeshFilter>().mesh;               
            }
            else
            {
                ivy_mesh = GetComponent<MeshFilter>().sharedMesh;               
            }           
            ivy_mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            //vertices = new List<Vector3>();
        }
        int items_grown = 0;
        Transform currentHolderGrown;
        public void Update()
        {

            //v0.9
            if (growIvy && Application.isPlaying)
            {
                for (int j = growingItems.Count - 1; j >= 0; j--)
                {
                    if (growthAmount[j] < 0.2f * (FlowerScale * UnityEngine.Random.Range(flowerScaleMinMax.x, flowerScaleMinMax.y) )) //v0.9
                    {
                        growthAmount[j] += 1.0f * growIvySpeed * Time.deltaTime;
                        growingMaterials[j].SetFloat("_growStage", growthAmount[j]);

                        if (growFlowers)
                        {
                            Transform[] flowers = growingItems[j].GetComponentsInChildren<Transform>();

                            for (int k = flowers.Length - 1; k >= 0; k--)
                            {
                                if (flowers[k] != growingItemsBranch[j] && flowers[k] != growingItems[j])
                                {
                                    flowers[k].localScale += new Vector3(1, 1, 1) * 0.3f * growFlowerSpeed * Time.deltaTime;
                                }
                            }
                        }
                    }
                    else
                    {
                        //growingItems[j].SetParent(flowerHolders[flowerHolders.Count - 1].transform);

                        growingItemsBranch[j].GetComponent<MeshRenderer>().sharedMaterial = branchMaterial;

                        if (items_grown == 0)
                        {
                            GameObject holderGROWN = new GameObject();
                            holderGROWN.name = "Grown";
                            holderGROWN.transform.parent = transform;
                            flowerHolders.Add(holderGROWN);
                            currentHolderGrown = holderGROWN.transform;
                            growingItems[j].parent = currentHolderGrown;
                            //flowerHolders.Add(growingItems[j].gameObject);
                        }
                        else
                        {
                            growingItems[j].parent = currentHolderGrown;
                        }

                        items_grown++;
                        
                        growingItems.RemoveAt(j);
                        growingItemsBranch.RemoveAt(j);
                        growingMaterials.RemoveAt(j);
                        growthAmount.RemoveAt(j);
                        flowersAdded++;

                        if (flowerHolders.Count > 0 && items_grown == branchCount) //if (flowersAdded == maxFlowerPerHolder * branchCount && flowerHolders.Count > 0)
                        {
                            //get last holder
                            ControlCombineChildrenINfiniDyGrassLAND batcher = flowerHolders[flowerHolders.Count - 1].AddComponent<ControlCombineChildrenINfiniDyGrassLAND>();
                            batcher.MakeActive = true;
                            batcher.Auto_Disable = true;

                            //batch and create new holder
                            flowersAdded = 0;

                            items_grown = 0;
                        }
                    }

                }
            }
            //v0.8
            //if (growIvy && Application.isPlaying)
            //{
            //    for (int j = growingItems.Count - 1; j >= 0; j--)
            //    {
            //        if (growthAmount[j] < 0.2f)
            //        {
            //            growthAmount[j] += 1.0f *growIvySpeed*Time.deltaTime;
            //            growingMaterials[j].SetFloat("_growStage", growthAmount[j]);

            //            if (growFlowers)
            //            {
            //                Transform[] flowers = growingItems[j].GetComponentsInChildren<Transform>();

            //                for (int k = flowers.Length - 1; k >= 0; k--)
            //                {
            //                    if (flowers[k] != growingItemsBranch[j] && flowers[k] != growingItems[j])
            //                    {
            //                        flowers[k].localScale += new Vector3(1,1,1)*0.3f * growFlowerSpeed * Time.deltaTime;
            //                    }
            //                }
            //            }
            //        }
            //        else
            //        {
            //            //growingItems[j].SetParent(flowerHolders[flowerHolders.Count - 1].transform);

            //            growingItemsBranch[j].GetComponent<MeshRenderer>().sharedMaterial = branchMaterial;
            //            flowerHolders.Add(growingItems[j].gameObject);
            //            growingItems.RemoveAt(j);
            //            growingItemsBranch.RemoveAt(j);
            //            growingMaterials.RemoveAt(j);
            //            growthAmount.RemoveAt(j);                        
            //            flowersAdded++;

            //            if (flowerHolders.Count > 0) //if (flowersAdded == maxFlowerPerHolder * branchCount && flowerHolders.Count > 0)
            //            {
            //                //get last holder
            //                ControlCombineChildrenINfiniDyGrassLAND batcher = flowerHolders[flowerHolders.Count - 1].AddComponent<ControlCombineChildrenINfiniDyGrassLAND>();
            //                batcher.MakeActive = true;
            //                batcher.Auto_Disable = true;

            //                //batch and create new holder
            //                flowersAdded = 0;
            //            }
            //        }

            //    }
            //}

            if (autoLOD)
            {
                if(player == null)
                {
                    if(Camera.main != null)
                    {
                        player = Camera.main.transform;
                    }
                }

                //v0.7
                if (globallyBatched)
                {
                    if (editorIvyHolder != null)
                    {
                        if (Vector2.Distance(
                            new Vector2(player.position.x, player.position.z), 
                            new Vector2(editorIvyHolder.transform.position.x, editorIvyHolder.transform.position.z)) > plantExtendsLODLimit * plantExtends)
                        {
                            if (editorIvyHolder.gameObject.activeInHierarchy)
                            {
                                editorIvyHolder.gameObject.SetActive(false);
                            }
                        }
                        else
                        {
                            if (!editorIvyHolder.gameObject.activeInHierarchy)
                            {
                                editorIvyHolder.gameObject.SetActive(true);
                            }
                        }
                    }
                }
                else
                {
                    if (Vector2.Distance(new Vector2(player.position.x, player.position.z), new Vector2(transform.position.x, transform.position.z)) > plantExtendsLODLimit * plantExtends)
                    {
                        for (int i = 0; i < flowerHolders.Count; i++)
                        {
                            if (flowerHolders[i].activeInHierarchy)
                            {
                                flowerHolders[i].SetActive(false);
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < flowerHolders.Count; i++)
                        {
                            if (!flowerHolders[i].activeInHierarchy)
                            {
                                flowerHolders[i].SetActive(true);
                            }
                        }
                    }
                }
            }

            //v0.5
            //https://stackoverflow.com/questions/63636944/unity-type-or-namespace-name-inputsystem-does-not-exist-in-the-namespace-unit
            bool mouse_pressed = false;
            Vector3 mousePos = Vector3.zero;
#if ENABLE_INPUT_SYSTEM
            if(plantMouseButton == 0){
mouse_pressed = UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
            }
            if(plantMouseButton == 1){
mouse_pressed = UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame;
            }
mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            // Old input backends are enabled.
            mouse_pressed = Input.GetMouseButtonDown(plantMouseButton);
            mousePos = Input.mousePosition;
#endif

            if (runTimePlanting && mouse_pressed)//if (runTimePlanting && Input.GetMouseButtonDown(0)) //v0.5
            {
                Ray ray = Camera.main.ScreenPointToRay(mousePos); //(Input.mousePosition); //v0.5
                RaycastHit hit;
                LayerMask allLayers = ~0;
                if (Physics.Raycast(ray, out hit, 100, confineToLayers?plantLayerMask:allLayers)) //v1.0
                {
                    mouseHitPoint = hit.point;//v1.0
                    generateIvyA(hit);                    
                }
            }            
        }

        //v1.0
        public Vector3 mouseHitPoint = new Vector3(-1,-1,-1);//-1 if not hit

        public void generateIvyA(RaycastHit hit)
        {
            if (!limitPlantingArea || (limitPlantingArea &&
                         hit.point.x < transform.position.x + plantExtends &&
                         hit.point.x > transform.position.x - plantExtends &&
                         hit.point.z < transform.position.z + plantExtends &&
                         hit.point.z > transform.position.z - plantExtends
                   ))
            {
                generateIvy(hit);
            }
        }

        //v0.2a
        Vector3 findCenterPoint(Vector3 p0, Vector3 p1, Vector3 normal)
        {
            Vector3 center = (p0 + p1) / 2;           
            return center + (p0 - p1).magnitude * normal;
        }

        //v1.0.9
        Vector3 prevLeafPos = new Vector3(0,0,0);

        void createLeaves(List<IvyPosition> ivys, bool startPlace, int fixCounter, float scaleFlowers, float scaleLeaves)
        {
            for (int i = 0; i < ivys.Count; i++)
            {
                var randomize = Random.Range(0, flowerDivide);
                if (startPlace || i > 0)
                {
                    Vector3 test = ivys[i].position + ivys[i].normal * (offsetRadius);//v1.0.9
                    if (randomize < 12 && randomize > 2 && (prevLeafPos - test).magnitude > 0.04f)//v1.0.9
                    {
                        prevLeafPos = test;//v1.0.9

                        Vector3 upNormal = Vector3.up;
                        Vector3 ivyPosNormal = ivys[i].normal;
                        Vector3 headingVector = Vector3.forward;
                        if (i > 0)
                        {
                            headingVector = ivys[i - 1].position - ivys[i].position;
                            upNormal = ivys[i - 1].normal;
                        }
                        else if (i <= ivys.Count - 2)
                        {
                            headingVector = ivys[i].position - ivys[i + 1].position;
                            upNormal = ivys[i + 1].normal;
                        }
                        bool createFlower = (randomize == 5) && Vector3.Dot(upNormal, ivyPosNormal) >= 1-flowerChance && Random.Range(0, flowerChance+0.5f) > 0.5f;//v1.0.9
                        var prefab = leavesPrefab;
                        float scaler = scaleLeaves * UnityEngine.Random.Range(leafScaleMinMax.x, leafScaleMinMax.y);
                        if (createFlower)
                        {
                            prefab = flowersPrefab;
                            scaler = scaleFlowers * UnityEngine.Random.Range(flowerScaleMinMax.x, flowerScaleMinMax.y);
                        }

                        //Quaternion rotation = Quaternion.LookRotation(headingVector.normalized, ivyPosNormal);
                        Quaternion rotation = headingVector == Vector3.zero
                                  ? Quaternion.identity
                                  : Quaternion.LookRotation(headingVector.normalized, ivyPosNormal);

                        float flowerOffset = createFlower ? 0.02f : 0;                       
                        GameObject item = Instantiate(prefab, ivys[i].position + ivys[i].normal * (flowerOffset + offsetRadius), rotation); //
                        item.transform.localScale = Vector3.one * scaler;
                        item.transform.SetParent(flowerHolders[flowerHolders.Count - fixCounter].transform);
                        //// END BATCHING
                    }
                }
            }
        }                           
       

        public void sanitizeHoldersList()
        {
            for (int i = flowerHolders.Count - 1; i > -1; i--)
            {
                if (flowerHolders[i] == null) {
                    flowerHolders.RemoveAt(i);

                    //v0.4
                    if (maxFlowerPerHolder == 1 && !ungrown) //Only for Editor use !!
                    {
                        ivyStrokes.RemoveAt(i);
                    }
                }                
            }
        }
        void OnDrawGizmos()
        {
#if UNITY_EDITOR
            // Draws the Light bulb icon at position of the object.
            // Because we draw it inside OnDrawGizmos the icon is also pickable
            // in the scene view.
            for (int i = 0; i < flowerHolders.Count; i++)
            {
                BoxCollider boxCol = flowerHolders[i].GetComponent<BoxCollider>();
                if (boxCol != null)
                {
                    Vector3 gizmPos = boxCol.center;                    
                    Gizmos.DrawIcon(gizmPos, "ivyGizmo.png", true);
                }
            }
#endif
        }
        //public Texture2D gizmoIcon;

        

        //v0.4
        public void UngrowIvy()
        {
            //erase all under the grower - only use in editor
            for (int i = flowerHolders.Count - 1; i > -1; i--)
            {
                DestroyImmediate(flowerHolders[i]);
            }
            for (int i = flowerHolders.Count - 1; i > -1; i--)
            {
                if (flowerHolders[i] == null)
                {
                    flowerHolders.RemoveAt(i);
                }
            }
            //    sanitizeHoldersList();
            ungrown = true;
        }
        public void RegrowIvy()
        {
            List<IvyStrokeProperties> copyList = new List<IvyStrokeProperties>();
            for (int i = 0; i < ivyStrokes.Count; i++)
            {
                copyList.Add(ivyStrokes[i]);
            }
            ivyStrokes.Clear(); //clear global list

            //save current params
            IvyStrokeProperties strokeSAVED = new IvyStrokeProperties();
            RaycastHit hitB = new RaycastHit();
            loadParametersToStroke(hitB, strokeSAVED, directionality);

            //v0.9
            bool savedUseCamDir = useCameraDirection;//if regrow not take camera direction !
            useCameraDirection = false;

            for (int i = copyList.Count - 1; i > -1; i--)
            {
                RaycastHit hitA = new RaycastHit();
                hitA.point = copyList[i].position;
                hitA.normal = copyList[i].normal;
                loadParametersFromStroke(copyList[i]);
                generateIvyA(hitA);
            }

            //RESTORE PARAMS
            loadParametersFromStroke(strokeSAVED);
            useCameraDirection = savedUseCamDir;

            //regrow from ivyStrokes - only use in editor
            ungrown = false;
        }

        //v0.9
        public void ReplantLastStroke(int strokeID)
        {
            List<IvyStrokeProperties> copyList = new List<IvyStrokeProperties>();
            //for (int i = 0; i < ivyStrokes.Count; i++)
            //{
                copyList.Add(ivyStrokes[strokeID]);// ivyStrokes.Count-1]);
            //}
            // ivyStrokes.Clear(); //clear global list
            ivyStrokes.RemoveAt(strokeID);// ivyStrokes.Count - 1);

            //save current params
            IvyStrokeProperties strokeSAVED = new IvyStrokeProperties();
            RaycastHit hitB = new RaycastHit();
            loadParametersToStroke(hitB, strokeSAVED, directionality);

            for (int i = copyList.Count - 1; i > -1; i--)
            {
                RaycastHit hitA = new RaycastHit();
                hitA.point = copyList[i].position;
                //Debug.Log(hitA.point);
                hitA.normal = copyList[i].normal;
                loadParametersFromStroke(copyList[i]);
                generateIvyA(hitA);
            }

            //RESTORE PARAMS
            loadParametersFromStroke(strokeSAVED);

            //regrow from ivyStrokes - only use in editor
            //ungrown = false;
        }

        public void loadParametersFromStroke(IvyStrokeProperties stroke)
        {
            randomSeed = stroke.randomSeed;
            flowerChance = stroke.flowerChance;
            offsetRadius = stroke.offsetRadius;
            branchCount = stroke.branchCount;
            leavesPrefab = stroke.leavesPrefab;
            flowersPrefab = stroke.flowersPrefab;
            ivyTrailLength = stroke.ivyTrailLength;
            ivyTrailWidth = stroke.ivyTrailWidth;
            normalMult = stroke.normalMult;
            branchPartLength = stroke.branchPartLength;
            maxBranchPositions = stroke.maxBranchPositions;
            zigZagPower = stroke.zigZagPower;
            curvingPower = stroke.curvingPower;
            gravityFactor = stroke.gravityFactor;
            cutOffDistance = stroke.cutOffDistance;
            surfaceOffset = stroke.surfaceOffset;
            addLeaves = stroke.addLeaves;

            //v0.9
            directionality = stroke.directionality;

            FlowerScale=stroke.FlowerScale;
            LeafScale=stroke.LeafScale;
            flowerScaleMinMax=stroke.flowerScaleMinMax;
            leafScaleMinMax=stroke.leafScaleMinMax;
        }
        public void loadParametersToStroke(RaycastHit hit, IvyStrokeProperties stroke, Vector4 directionality)
        {
            stroke.position = hit.point;
            stroke.normal = hit.normal;
            stroke.randomSeed = randomSeed;
            stroke.flowerChance = flowerChance;
            stroke.offsetRadius = offsetRadius;
            stroke.branchCount = branchCount;
            stroke.leavesPrefab = leavesPrefab;
            stroke.flowersPrefab = flowersPrefab;
            stroke.ivyTrailLength = ivyTrailLength;
            stroke.ivyTrailWidth = ivyTrailWidth;
            stroke.normalMult = normalMult;
            stroke.branchPartLength = branchPartLength;
            stroke.maxBranchPositions = maxBranchPositions;
            stroke.zigZagPower = zigZagPower;
            stroke.curvingPower = curvingPower;
            stroke.gravityFactor = gravityFactor;
            stroke.cutOffDistance = cutOffDistance;
            stroke.surfaceOffset = surfaceOffset;
            stroke.addLeaves = addLeaves;

            //v0.9
            stroke.directionality = directionality;

            stroke.FlowerScale = FlowerScale;
            stroke.LeafScale = LeafScale;
            stroke.flowerScaleMinMax = flowerScaleMinMax;
            stroke.leafScaleMinMax = leafScaleMinMax;
        }

        public void generateIvy(RaycastHit hit)
        {
            //v0.9
            Vector4 finalDirectionality = directionality;
            if (useCameraDirection)
            {
                if (useCameraMouseDirection)//v1.0
                {
                    finalDirectionality = //new Vector4(mouseHitPoint.x, mouseHitPoint.y,mouseHitPoint.z,0) 
                        //+  
                        new Vector4(mouseHitPoint.x - Camera.main.transform.position.x,
                        mouseHitPoint.y - Camera.main.transform.position.y,
                        mouseHitPoint.z - Camera.main.transform.position.z, 
                        0).normalized;
                    //Debug.DrawLine(mouseHitPoint,
                    //    mouseHitPoint
                    //    + new Vector3(finalDirectionality.x, finalDirectionality.y, finalDirectionality.z), Color.blue, 10);
                    //Debug.DrawLine(new Vector3(Camera.main.transform.position.x,0, Camera.main.transform.position.z),
                    //    new Vector3(Camera.main.transform.position.x, 0, Camera.main.transform.position.z) 
                    //    + new Vector3(finalDirectionality.x, finalDirectionality.y, finalDirectionality.z), Color.red,10 );
                }
                else
                {
                    finalDirectionality = new Vector4(Camera.main.transform.forward.x, Camera.main.transform.forward.y, Camera.main.transform.forward.z, directionality.w);
                }
            }

            //v0.4
            IvyStrokeProperties stroke = new IvyStrokeProperties();
            loadParametersToStroke(hit, stroke, finalDirectionality);
            ivyStrokes.Add(stroke);

            //v0.3
            Random.InitState(randomSeed);

            //v0.2
            Vector3 findPrallelPlane = Vector3.Cross(hit.normal, Vector3.up);
            Vector3 otherParallel = Vector3.Cross(hit.normal, Vector3.forward);
            if (otherParallel.magnitude > findPrallelPlane.magnitude)
            {
                findPrallelPlane = otherParallel;
            }
            //MESH
            Vector3 worldSpaceDisp = transform.position;
            if (singleMeshBranch && ivy_mesh == null)
            {
                ivy_vertexL = new Vector3(ivyTrailWidth / 2, 0, 0);
                ivy_vertexR = new Vector3(-ivyTrailWidth / 2, 0, 0);
                if (Application.isPlaying)
                {
                    ivy_mesh = new Mesh();
                    GetComponent<MeshFilter>().mesh = ivy_mesh;
                }
                else
                {
                    ivy_mesh = new Mesh();
                    GetComponent<MeshFilter>().sharedMesh = ivy_mesh;
                }
                ivy_mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                GetComponent<MeshFilter>().sharedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }
            if (ivy_mesh != null) { 
                ivy_mesh.Clear();
            }
            //END MESH

            for (int i = 0; i < branchCount; i++)
            {
                Vector3 direction = Quaternion.AngleAxis(360 / branchCount * i + Random.Range(0, 360 / branchCount), hit.normal) * findPrallelPlane;

                //v0.9
                if(finalDirectionality != Vector4.zero)
                {
                    direction = Quaternion.AngleAxis(10 / branchCount * i + Random.Range(0, 10 / branchCount), hit.normal) * 
                        new Vector3(finalDirectionality.x, finalDirectionality.y, finalDirectionality.z);// * (finalDirectionality.w+1);
                       // Quaternion.AngleAxis(360 / branchCount * i + Random.Range(0, 360 / branchCount), hit.normal) * findPrallelPlane;
                }

                List<IvyPosition> ivyPlaces = makeBranches(maxBranchPositions, hit.point, hit.normal, direction);

                //
                if (create3dmesh)
                {
                    //TO ADD IN LATER VERSIONS - 2D is much more optimized
                }
                else
                {
                    if (!singleMeshBranch)
                    {
                        worldSpaceDisp = Vector3.zero;
                    }

                    ivy_vertexL = ivyPlaces[0].position + ivyPlaces[0].normal * surfaceOffset + new Vector3(ivyTrailWidth / 2, 0, 0) - worldSpaceDisp;
                    ivy_vertexR = ivyPlaces[0].position + ivyPlaces[0].normal * surfaceOffset + new Vector3(-ivyTrailWidth / 2, 0, 0) - worldSpaceDisp;
                    vertices.Insert(0, ivy_vertexL);
                    vertices.Insert(0, ivy_vertexR);

                    for (int j = 1; j < ivyPlaces.Count - 1; j++)
                    {
                        ivy_vertexL = ivyPlaces[j].position + ivyPlaces[j].normal * surfaceOffset + new Vector3(ivyTrailWidth / 2, 0, 0) - worldSpaceDisp;
                        ivy_vertexR = ivyPlaces[j].position + ivyPlaces[j].normal * surfaceOffset + new Vector3(-ivyTrailWidth / 2, 0, 0) - worldSpaceDisp;
                        vertices.Add(ivy_vertexL);
                        vertices.Add(ivy_vertexR);
                    }//end createmesh                        




                    ///// ADD LEAVES
                    int fixCounter = 1;
                    if (flowerHolders.Count == 0 || flowersAdded == 0)
                    {

                        //if (flowersAdded == maxFlowerPerHolder * branchCount && flowerHolders.Count > 0)
                        //{
                        //    //get last holder
                        //    ControlCombineChildrenINfiniDyGrassLAND batcher = flowerHolders[flowerHolders.Count - 1].AddComponent<ControlCombineChildrenINfiniDyGrassLAND>();
                        //    batcher.MakeActive = true;
                        //    batcher.Auto_Disable = true;

                        //    //batch and create new holder
                        //    flowersAdded = 0;
                        //}

                        GameObject holder = new GameObject();
                        flowerHolders.Add(holder);
                        holder.transform.parent = transform;

                        //v0.3
                        if (addColliderPerIvy)
                        {
                            BoxCollider collider = holder.AddComponent<BoxCollider>();
                            collider.size = new Vector3(1, 1, 1) * colliderScale;
                            collider.center = hit.point;
                            holder.name = "IVY";
                        }
                        if (flowerHolders.Count > 0)
                        {
                            fixCounter = 1;
                        }
                    }
                    flowersAdded++;
                    if (addLeaves)
                    {
                        //// START BATCHING
                        //flowersAdded++;

                        //v0.8
                        if (growIvy && Application.isPlaying && growFlowers)
                        {
                            createLeaves(ivyPlaces, i == 0, fixCounter, 0.01f, 0.01f); //v0.9
                        }
                        else
                        {
                            createLeaves(ivyPlaces, i == 0, fixCounter, FlowerScale, LeafScale); //1); //v0.9
                        }
                    }
                    ////END ADD LEAVES



                    //Create separate branch meshes
                    if (!singleMeshBranch)
                    {
                        if (vertices.Count >= 3)
                        {
                            GameObject branchPart = new GameObject();
                            MeshFilter meshBranch = branchPart.AddComponent<MeshFilter>();
                            MeshRenderer meshBranchRenderer = branchPart.AddComponent<MeshRenderer>();
                            Mesh ivy_meshA;
                            if (Application.isPlaying)
                            {
                                ivy_meshA = new Mesh();
                                meshBranch.mesh = ivy_meshA;
                            }
                            else
                            {
                                ivy_meshA = new Mesh();
                                meshBranch.sharedMesh = ivy_meshA;
                            }
                            meshBranchRenderer.material = branchMaterial;
                            //ivy_meshA.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

                            //v0.8
                            if (growIvy && Application.isPlaying)
                            {
                                meshBranchRenderer.material = new Material(growIvyMaterial.shader);//make instance
                                meshBranchRenderer.material.CopyPropertiesFromMaterial(growIvyMaterial);
                                meshBranchRenderer.material.SetFloat("_growStage", -1.0f);
                                growingMaterials.Add(meshBranchRenderer.material);
                                growthAmount.Add(-1.0f);
                            }

                            //v0.6
                            if (use3dbranch)
                            {
                                generate3Dbranch(ivy_meshA, ivyPlaces);
                            }
                            else
                            {
                                createFlatMesh(ivy_meshA, vertices);
                            }

                            branchPart.transform.SetParent(flowerHolders[flowerHolders.Count - fixCounter].transform);

                            //v0.8
                            if (growIvy && Application.isPlaying)
                            {
                                //branchPart.transform.SetParent(null);
                                growingItems.Add(flowerHolders[flowerHolders.Count - 1].transform);
                                growingItemsBranch.Add(branchPart.transform);
                                flowerHolders.RemoveAt(flowerHolders.Count - 1);
                                flowersAdded--;
                            }
                        }
                    }

                    //v0.8
                    //if (growIvy && Application.isPlaying)
                    //{
                    //    for (int j = growingItems.Count-1; j >=0; j--)
                    //    {
                    //        if(growthAmount[j] < 2)
                    //        {
                    //            //growthAmount[j] += 0.01f *growIvySpeed;
                    //            //growingMaterials[j].SetFloat("_growStage", growthAmount[j]);
                    //        }
                    //        else
                    //        {
                    //            growingItems[j].SetParent(flowerHolders[flowerHolders.Count - fixCounter].transform);
                    //            growingItems.RemoveAt(j);
                    //            growingMaterials.RemoveAt(j);
                    //            growthAmount.RemoveAt(j);
                    //            growingItems[j].GetComponent<MeshRenderer>().sharedMaterial = branchMaterial;
                    //            flowersAdded++;
                    //        }
                            
                    //    }
                    //}

                    //v0.9
                    if ( (!Application.isPlaying || (Application.isPlaying && !growIvy)) && flowersAdded == maxFlowerPerHolder * branchCount && flowerHolders.Count > 0)
                    {
                        //get last holder
                        ControlCombineChildrenINfiniDyGrassLAND batcher = flowerHolders[flowerHolders.Count - 1].AddComponent<ControlCombineChildrenINfiniDyGrassLAND>();
                        batcher.MakeActive = true;
                        batcher.Auto_Disable = true;

                        //batch and create new holder
                        flowersAdded = 0;
                    }

                }
                if (!singleMeshBranch)
                {
                    vertices.Clear();
                }
            }
            //FINISH MESH
            if (singleMeshBranch && vertices.Count >= 3)
            {
                createFlatMesh(ivy_mesh, vertices);
                
            }//End vertices check          

            ivyNumber++;
        }

        public void createFlatMesh(Mesh ivy_meshB, List<Vector3> vertices)
        {
            // add mesh vertices 
            ivy_meshB.vertices = vertices.ToArray();

            if (vertices.Count >= 3)
            {
                //  triangles
                triangles = new int[3 * (vertices.Count - 2)];
                trianglesLEAVES = new int[3 * (vertices.Count - 2)];

                // starting triangle
                int trianglesCounter = 3;
                triangles[0] = 0;
                triangles[1] = 2;
                triangles[2] = 1;

                for (int iB = 1; iB < vertices.Count - 2; iB++) // Triangle count will always be (vertex.count-2)*3
                {
                    if (iB % 2 == 0)
                    {
                        triangles[trianglesCounter] = iB;
                        triangles[trianglesCounter + 1] = iB + 1;
                        triangles[trianglesCounter + 2] = iB + 2;
                    }
                    else
                    {
                        triangles[trianglesCounter] = iB;
                        triangles[trianglesCounter + 1] = iB + 2;
                        triangles[trianglesCounter + 2] = iB + 1;
                    }

                    if (Vector3.Distance(vertices[iB + 2], vertices[iB + 1]) > cutOffDistance)
                    {
                        triangles[trianglesCounter] = 0;
                        triangles[trianglesCounter + 1] = 0;
                        triangles[trianglesCounter + 2] = 0;
                    }
                    if (Vector3.Distance(vertices[iB + 0], vertices[iB + 1]) > cutOffDistance)
                    {
                        triangles[trianglesCounter] = 0;
                        triangles[trianglesCounter + 1] = 0;
                        triangles[trianglesCounter + 2] = 0;
                    }
                    if (Vector3.Distance(vertices[iB + 0], vertices[iB + 2]) > cutOffDistance)
                    {
                        triangles[trianglesCounter] = 0;
                        triangles[trianglesCounter + 1] = 0;
                        triangles[trianglesCounter + 2] = 0;
                    }
                    trianglesCounter += 3;
                }
                ivy_meshB.triangles = triangles;


                Color[] colored = new Color[vertices.Count];
                for (int iB = 1; iB < vertices.Count; iB++)
                {
                    colored[iB] = Color.black;
                }

                uvs = new Vector2[vertices.Count];
                for (int iB = 1; iB < vertices.Count; iB++)
                {
                    if (iB % 2 == 0)
                    {
                        uvs[iB] = new Vector2(iB / ((float)vertices.Count - 2), 0);
                    }
                    else
                    {
                        uvs[iB] = new Vector2((iB - 1) / ((float)vertices.Count - 2), 1);
                    }
                }
                ivy_meshB.uv = uvs;

                ivy_meshB.colors = colored;
                ivy_meshB.RecalculateBounds();
                ivy_meshB.RecalculateNormals();
                ivy_meshB.RecalculateTangents();
            }
        }

        //v0.2
        bool findObstacle(Vector3 start, Vector3 end)
        {
            LayerMask allLayers = ~0;
            Ray ray = new Ray(start, (end - start) / (end - start).magnitude);
            return Physics.Raycast(ray, (end - start).magnitude, confineToLayers ? plantLayerMask : allLayers);//v1.0
        }        
        
        List<IvyPosition> makeBranches(int branchesCount, Vector3 position, Vector3 normal, Vector3 direction)
        {
            if (branchesCount == maxBranchPositions)//END condition
            {
                IvyPosition startNode = new IvyPosition(position, normal);
                List<IvyPosition> lista = new List<IvyPosition>();
                lista.Add(startNode);
                List<IvyPosition> listb = makeBranches(branchesCount-1, position, normal, direction);
                if (listb != null)
                {
                    lista.AddRange(listb);
                }
                return lista;
            }
            else if (branchesCount < maxBranchPositions && branchesCount > 0)
            {
                if (branchesCount % 2 == 0)
                {
                    direction = Quaternion.AngleAxis(Random.Range(-25.0f * zigZagPower, 25.0f * zigZagPower) * zigZagPower + curvingPower.y * branchesCount * 0.1f, normal) * direction; 
                        //- gravityFactor * Vector3.up * 0.01f/ (maxBranchPositions-branchesCount);//5
                }

                RaycastHit hit;

                //SEARCH UP of surface !!!!!!!!!!!!!!!!!!
                Ray ray = new Ray(position, normal);
                Vector3 p1 = position + normal * branchPartLength;

                LayerMask allLayers = ~0;
                if (Physics.Raycast(ray, out hit, branchPartLength, confineToLayers ? plantLayerMask : allLayers))//v1.0
                {
                    p1 = hit.point;
                }

                //SEARCH RIGHT parallel to surface from new point for collision (collision or free area) !!!!!!!!!!!!!!!!!!
                ray = new Ray(p1, direction);

                if (Physics.Raycast(ray, out hit, branchPartLength, confineToLayers ? plantLayerMask : allLayers))//v1.0
                {
                    Vector3 p2 = hit.point;
                    IvyPosition p2Node = new IvyPosition(p2, -direction);
                    List<IvyPosition> lista = new List<IvyPosition>();
                    lista.Add(p2Node);
                    List<IvyPosition> listb = makeBranches(branchesCount-1, p2, -direction, normal);//IF hit found, start new search - branching on p2
                    if (listb != null)
                    {
                        lista.AddRange(listb);
                    }
                    return lista;
                }
                else
                {
                    //SEARCH RIGHT to full extend if collision not found
                    Vector3 p2 = p1 + direction * branchPartLength;

                    //v0.7
                    if (gravityFactor.x != 0)
                    {
                        //p2.y -= gravityFactor * branchesCount * 0.01f;
                    }

                    //LayerMask allLayers = ~0;

                    ray =new Ray((p2 + normalMult*normal), -normal); 
                    if (Physics.Raycast(ray, out hit, branchPartLength, confineToLayers ? plantLayerMask : allLayers))//v1.0
                    {
                        Vector3 p3 = hit.point;
                        IvyPosition p3Node = new IvyPosition(p3, normal);

                        if (findObstacle(p3 + normalMult * normal, position + normalMult * normal))////
                        {
                            Vector3 middle = findCenterPoint(p3, position, (normal + direction) / 2);////
                            List<IvyPosition> lista = new List<IvyPosition>();
                            lista.Add(new IvyPosition((position + middle) / 2, normal));
                            lista.Add(new IvyPosition((p3 + middle) / 2, normal));
                            lista.Add(p3Node);

                            List<IvyPosition> listb = makeBranches(branchesCount - 3, p3, normal, direction);
                            if (listb != null)
                            {
                                lista.AddRange(listb);
                            }
                            return lista;
                        }                        
                        List<IvyPosition> listaA = new List<IvyPosition>();
                        listaA.Add(p3Node);
                        List<IvyPosition> listbb = makeBranches(branchesCount-1, p3, normal, direction);
                        if (listbb != null)
                        {
                            listaA.AddRange(listbb);
                        }
                        return listaA;
                    }
                    else
                    {
                        Vector3 p3 = p2 - normal * branchPartLength;

                        //v0.7
                        if (gravityFactor.y != 0 && !findObstacle(p3 + new Vector3(0, 0.065f, 0), new Vector3(p3.x, p3.y - gravityFactor.y * branchesCount * 0.01f - 0.065f, p3.z)))
                        {
                            p3.y -= gravityFactor.y * branchesCount * 0.001f;
                        }

                        ray = new Ray(p3 + normalMult* normal, -normal); 

                        if (Physics.Raycast(ray, out hit, branchPartLength, confineToLayers ? plantLayerMask : allLayers))//v1.0
                        {
                            Vector3 p4 = hit.point;
                            IvyPosition p4Node = new IvyPosition(p4, normal);

                            if (findObstacle(p4 + normalMult * normal, position + normalMult * normal))
                            {
                                Vector3 middle = findCenterPoint(p4, position, (normal + direction) / 2);
                                List<IvyPosition> lista = new List<IvyPosition>();
                                lista.Add(new IvyPosition((position + middle) / 2, normal));
                                lista.Add(new IvyPosition((p4 + middle) / 2, normal));
                                lista.Add(p4Node);

                                List<IvyPosition> listb = makeBranches(branchesCount - 3, p4, normal, direction);
                                if (listb != null)
                                {
                                    lista.AddRange(listb);
                                }
                                return lista;
                            }

                            //v0.7
                            if (gravityFactor.y != 0)
                            {
                                //p4.y -= gravityFactor.y * branchesCount * 0.01f;
                            }

                            List<IvyPosition> listaA = new List<IvyPosition>();
                            listaA.Add(p4Node);
                            List<IvyPosition> listbb = makeBranches(branchesCount-1, p4, normal, direction);
                            if (listbb != null)
                            {
                                listaA.AddRange(listbb);
                            }
                            return listaA;
                        }
                        else
                        {
                            Vector3 p4 = p3 - normal * branchPartLength;

                         

                            IvyPosition p4Node = new IvyPosition(p4, direction);

                            

                            if (findObstacle(p4 + normalMult * normal, position + normalMult * normal))
                            {
                                Vector3 middle = findCenterPoint(p4, position, (normal + direction) / 2);
                                List<IvyPosition> lista = new List<IvyPosition>();
                                lista.Add(new IvyPosition((position + middle) / 2, direction));
                                lista.Add(new IvyPosition((p4 + middle) / 2, direction));
                                lista.Add(p4Node);
                                List<IvyPosition> listb = makeBranches(branchesCount - 3, p4, direction, -normal);
                                if (listb != null)
                                {
                                    lista.AddRange(listb);
                                }
                                return lista;
                            }

                            //v0.7
                            if (gravityFactor.z != 0 && !findObstacle(p4 + new Vector3(0,0.065f,0), new Vector3(p4.x, p4.y - gravityFactor.z * 1 * 0.01f - 0.065f, p4.z)))
                            {
                                if (gravityFactor.z > 0)
                                {
                                    if (branchesCount > 8)
                                    {
                                        p4.y -= gravityFactor.z * 1 * 0.01f;
                                    }
                                    //p4.y -= gravityFactor * branchesCount * 0.01f;
                                    if (branchesCount < 15) //larger goes towards plant point, smaller to branch edge
                                    {
                                        p4.x -= Random.Range(-1, 1) * curvingPower.z * branchesCount * 0.01f;
                                        p4.z += Random.Range(-1, 1) * curvingPower.z * branchesCount * 0.01f;
                                        p4.y -= gravityFactor.z * 1 * 0.01f;
                                    }
                                    if (branchesCount < 10) //larger goes towards plant point, smaller to branch edge
                                    {
                                        p4.x -= Random.Range(-1, 1) * curvingPower.z * branchesCount * 0.003f;
                                        p4.z += Random.Range(-1, 1) * curvingPower.z * branchesCount * 0.003f;
                                        p4.y -= gravityFactor.z * 1 * 0.003f;
                                    }
                                }
                                else
                                {
                                    //curve it
                                    if (branchesCount > 22)
                                    {
                                        p4.y -= gravityFactor.z * 1 * 0.01f;
                                    }
                                    else
                                    {
                                        p4.y += gravityFactor.z * 1 * 0.002f;
                                    }
                                    p4.x -= Random.Range(-1, 1)* curvingPower.z * 1 * 0.01f;
                                    p4.z += Random.Range(-1, 1) * curvingPower.z * 1 * 0.01f;
                                }
                            }

                            List<IvyPosition> listaA = new List<IvyPosition>();
                            listaA.Add(p4Node);
                            List<IvyPosition> listbb = makeBranches(branchesCount-1, p4, direction, -normal);
                            if (listbb != null)
                            {
                                listaA.AddRange(listbb);
                            }
                            return listaA;
                        }
                    }
                }
            }
            return null;
        }   
    }
}