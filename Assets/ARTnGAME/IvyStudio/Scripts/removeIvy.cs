using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Artngame.TreeGEN.ProceduralIvy
{
    public class removeIvy : MonoBehaviour
    {
        // //GROW
        //_growStage("_growStage", Range(-1.0, 0.2)) = 0
        //   _Thickness("_Thickness", float) = 0

        //plant in positions
        public bool plantInPositions = false;
        public int RaysCount = 36;
        public float maxPlantDistance = 0.1f;
        public List<Transform> plantPositions = new List<Transform>();
        public List<IvyGeneratorTREANT> generators = new List<IvyGeneratorTREANT>();
        public bool debugRayCast = false;

        // Start is called before the first frame update
        void Start()
        {
            IvyGeneratorTREANT[] generatorsA = FindObjectsOfType<IvyGeneratorTREANT>();
            generators.AddRange(generatorsA);
        }
        public float growRate = 1;
        IvyGeneratorTREANT ivyGen;

        public float maxEraseDistance = 100;
        public float maxEraseSphereCastRadius = 2f;

        public List<Material> ungrowMats = new List<Material>();

        public List<GameObject> ungrowObjects = new List<GameObject>();

        // Update is called once per frame
        void Update()
        {

            if (plantInPositions)
            {
                plantInPositions = false;

                //Debug.Log("plantPositions.Count =" + plantPositions.Count);

                for (int k = 0; k < plantPositions.Count; k++)
                {
                    RaycastHit hitA;
                    RaycastHit hitClosest = new RaycastHit();
                    hitClosest.distance = 1000000;
                    float prevDist = 1000000;

                    float angle = 0;
                    float angle2 = -Mathf.PI;
                    //Debug.Log("RaysCount.Count =" + RaysCount);
                    for (int i1 = 0; i1 <= RaysCount; i1++)
                    {
                        angle2 = -Mathf.PI/2 + i1 * Mathf.PI / RaysCount;
                        //Debug.Log("angle2 =" + angle2);
                        float yA = Mathf.Sin(angle2);
                        //Debug.Log("angle2 SIN =" + yA);
                        for (int i = 0; i < RaysCount; i++)
                        {
                            float xA = Mathf.Sin(angle);
                            float zA = Mathf.Cos(angle);
                            angle = angle + Mathf.PI * 2 / RaysCount;

                            Vector3 direction = new Vector3(plantPositions[k].position.x + xA, plantPositions[k].position.y + yA, plantPositions[k].position.z + zA);
                            Vector3 directionA = new Vector3(xA, yA, zA);
                            //if (debugRayCast)
                            //{
                                // Debug.DrawLine(plantPositions[k].position, direction, Color.blue, 6);
                                //Debug.DrawLine(plantPositions[k].position, direction, Color.red, 6);
                            //}
                            if (Physics.Raycast(plantPositions[k].position, directionA, out hitA, maxPlantDistance))
                            {
                                if (debugRayCast)
                                {
                                    //Debug.DrawLine(plantPositions[k].position, direction, Color.blue, 6);
                                    Debug.DrawLine(plantPositions[k].position, hitA.point, Color.blue, 6);
                                }
                               // Debug.Log("New hit found in i = " + i + " and i1 = " + i1 + " for plant pos: " + k + " with dist:" + hitA.distance + " Closest dist:" + hitClosest.distance);
                                //here is how to do your cool stuff ;)
                                if (hitA.distance < prevDist)//if (hitClosest.distance > hitA.distance)
                                {
                                    //Debug.Log("New dist " + hitA.distance + " found in i = " + i + " and i1 = " + i1 + " for plant pos: " + k);
                                    prevDist = hitA.distance;
                                    hitClosest = hitA;
                                    hitClosest.point = hitA.point;
                                    hitClosest.normal = hitA.normal;
                                    hitClosest.distance = hitA.distance;
                                    hitClosest.barycentricCoordinate = hitA.barycentricCoordinate;
                                }
                                //break;
                            }
                        }
                    }

                    //plant here
                    //Debug.Log("hitClosest =" + hitClosest + "  Gen count: " + generators.Count);
                    if (debugRayCast)
                    {
                        Debug.DrawLine(hitClosest.point, hitClosest.point + hitClosest.normal * 0.5f, Color.cyan, 9);
                    }
                    //
                    if (hitClosest.distance <= maxPlantDistance)
                    {
                        IvyGeneratorTREANT closesetGenerator = generators[0];
                        for (int j1 = 1; j1 < generators.Count; j1 = j1 + 1)
                        {
                            if ((hitClosest.point - closesetGenerator.transform.position).magnitude > (hitClosest.point - generators[j1].transform.position).magnitude)
                            {
                                closesetGenerator = generators[j1];
                                //break;
                            }
                            //break;
                        }
                        closesetGenerator.generateIvyA(hitClosest);
                    }

                }
            }

            for (int i = 0; i < ungrowMats.Count; i++)
            {
                float growStage = ungrowMats[i].GetFloat("_growStage");
                if (growStage > -1f)
                {
                    //register its material for ungrow
                    ungrowMats[i].SetFloat("_growStage", growStage - growRate * 0.01f*Time.deltaTime);
                }
                else
                {                   
                    ungrowMats.RemoveAt(i);
                }
            }

            for (int i = 0; i < ungrowObjects.Count; i++)
            {
                if (ungrowObjects[i] != null)
                {
                    float growStage = ungrowObjects[i].GetComponent<MeshRenderer>().material.GetFloat("_growStage");
                    if (growStage <= -1f)
                    {
                        if (ungrowObjects[i] != null)
                        {
                            for (int i1 = 0; i1 < ivyGen.flowerHolders.Count; i1++)
                            {
                                if (ungrowObjects[i].transform.parent.gameObject == ivyGen.flowerHolders[i1].gameObject)
                                {
                                    ivyGen.flowerHolders.RemoveAt(i1);
                                    break;
                                }
                            }
                            Destroy(ungrowObjects[i].transform.parent.gameObject);
                        }
                        //Destroy(ungrowObjects[i]);
                        ungrowObjects.RemoveAt(i);
                       
                    }
                }
                else {
                    ungrowObjects.RemoveAt(i);
                }
            }


            //check if collider found name is IVY and Grab 2ond parent for finding grower, find item with Branch Material and named Combined mesh and change to Grow Ivy Material instance
            //register the instance to a list and gradually ungrow in shader
            //do same for leaves (need assign another leaf grow shader probably)

            RaycastHit hit;
            

            Vector3 p1 = Camera.main.transform.position;
            //float distanceToObstacle = 0;

            

            // Cast a sphere wrapping character controller 10 meters forward
            // to see if it is about to hit anything.
            if (Physics.SphereCast(p1, maxEraseSphereCastRadius, Camera.main.transform.forward, out hit, maxEraseDistance))
            {
                //Debug.Log("hit = " + hit.point);
                //distanceToObstacle = hit.distance;
                if (hit.transform.gameObject.name == "IVY")
                {
                    if (hit.transform.parent.parent != null)
                    {
                        ivyGen = hit.transform.parent.parent.GetComponent<IvyGeneratorTREANT>();
                    }
                    if(ivyGen != null)
                    {
                        
                        MeshRenderer[] meshRenderersList = hit.transform.parent.GetComponentsInChildren<MeshRenderer>(true);
                       
                        //GameObject[] objects = hit.transform.parent.Find("Combined mesh")
                        for (int i=0; i < meshRenderersList.Length; i++){
                            if (meshRenderersList[i].transform.gameObject.name== "Combined mesh")
                            {
                                //Debug.Log("Found manager " + meshRenderersList.Length);
                                //register its material for ungrow

                                float growStage = meshRenderersList[i].material.GetFloat("_growStage");
                                if (growStage > -1)//if (growStage > -1)
                                {
                                    bool matFound = false;
                                    for (int i1 = 0; i1 < ungrowMats.Count; i1++)
                                    {
                                        if (ungrowMats[i1] == meshRenderersList[i].material)
                                        {
                                            matFound = true;
                                        }
                                    }
                                    if (!matFound)
                                    {
                                        ungrowMats.Add(meshRenderersList[i].material);

                                        if (meshRenderersList[i].material.name.Contains("Branch") || meshRenderersList[i].material.name.Contains("branch"))
                                        {
                                            ungrowObjects.Add(meshRenderersList[i].transform.gameObject);
                                        }
                                    }
                                }
                                else
                                {
                                    //for (int i1 = 0; i1 < ivyGen.flowerHolders.Count; i1++)
                                    //{
                                    //    if (hit.transform.parent.gameObject == ivyGen.flowerHolders[i1].gameObject)
                                    //    {
                                    //        ivyGen.flowerHolders.RemoveAt(i1);
                                    //        break;
                                    //    }
                                    //}
                                    //Destroy(hit.transform.parent.gameObject);
                                }
                            }
                        }
                    }
                }

            }
        }
    }
}