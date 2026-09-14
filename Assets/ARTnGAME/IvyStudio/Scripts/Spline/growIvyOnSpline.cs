using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Artngame.CommonTools;
using Artngame.INfiniDy;

namespace Artngame.TreeGEN.ProceduralIvy
{
    public class growIvyOnSpline : MonoBehaviour
    {
        //v1.0.9
        public bool plantPerTime = false;
        public float plantTimeInterval = 0.3f;
        float prevPlantTime = 0;

        public SplinerPTREANT spline;
        public int addPerSegments = 2;
        public IvyGeneratorTREANT ivyGen;
        public bool plantAtStart = true;

        public bool followSplineDirection = false;
        public bool growSplineGradual = true;

        public bool createColliders = false;
        public float colliderSize = 0.1f;
        public float colliderDistance = 0.1f;
        Vector3 prevColliderPos;
        List<GameObject> collidersAdded = new List<GameObject>();

        Vector3 prevPos;
        public float distanceThreshold = 1;//plant no sooner than this distance

        // Start is called before the first frame update
        void Start()
        {
            if (plantAtStart && spline != null)
            {
                //v1.0.7
                if (createColliders)
                {
                    for (int i = 0; i < spline.Curve.Count; i = i + 1)
                    {
                        //add colliders
                        if ((prevColliderPos - spline.Curve[i].position).magnitude > colliderDistance)
                        {
                            GameObject colliderOBJ = new GameObject();
                            colliderOBJ.transform.position = spline.Curve[i].position;
                            colliderOBJ.AddComponent<SphereCollider>();
                            colliderOBJ.GetComponent<SphereCollider>().radius = colliderSize;
                            collidersAdded.Add(colliderOBJ);
                            prevColliderPos = spline.Curve[i].position;

                        }
                       
                    }
                }

                for (int i = 0; i < spline.Curve.Count; i = i + addPerSegments)
                {                    
                    ivyGen.growFlowers = growSplineGradual;
                    ivyGen.growIvy = growSplineGradual;
                    
                    RaycastHit hit = new RaycastHit();
                    hit.point = spline.Curve[i].position;

                    if (followSplineDirection && i > 3)
                    {
                        hit.normal =- (spline.Curve[i].position - spline.Curve[i - 2].position);
                    }
                    else
                    {
                        hit.normal = Vector3.up;
                    }

                    if ((prevPos - spline.Curve[i].position).magnitude > distanceThreshold)
                    {
                        ivyGen.generateIvy(hit);
                    }

                    prevPos = spline.Curve[i].position;
                }

               
            }
        }
        int currentPlanted = 0;
        public float destroyCollidersTime = 4;
        bool collidersDestroyed = false;

        //v1.0.9
        GameObject collidersGroup;
        public int destroyCollidersRate = 2;
        //GameObject colliderOBJtemp;
        //GameObject colliderOBJtempPREV1;
        //GameObject colliderOBJtempAFTER1;
        //int destoyedColliders = 0;

        // Update is called once per frame
        void Update()
        {

            //v1.0.7
            if (!plantPerTime && createColliders && Time.fixedTime > destroyCollidersTime && !collidersDestroyed) //v1.1
            {
                for (int i = 0; i < collidersAdded.Count; i++)
                {
                     Destroy(collidersAdded[i]);
                }
                collidersAdded.Clear();
                collidersDestroyed = true;
            }


            //v1.0.9 - GENERATE coliders for run time use
            //v1.0.7
            if (plantPerTime && !plantAtStart && spline != null && createColliders && collidersAdded.Count == 0 && !collidersDestroyed) //v1.1
            {
                if(collidersGroup == null)
                {
                    collidersGroup = new GameObject();
                    collidersGroup.name = "Helper Ivy Grow Colliders Group";
                }
                for (int i = 0; i < spline.Curve.Count; i = i + 1)
                {
                    //add colliders
                    if ((prevColliderPos - spline.Curve[i].position).magnitude > colliderDistance)
                    {
                        GameObject colliderOBJ = new GameObject();
                        colliderOBJ.transform.parent = collidersGroup.transform;
                        colliderOBJ.transform.position = spline.Curve[i].position;
                        colliderOBJ.AddComponent<SphereCollider>();
                        colliderOBJ.GetComponent<SphereCollider>().radius = colliderSize;
                        collidersAdded.Add(colliderOBJ);
                        prevColliderPos = spline.Curve[i].position;
                    }
                }
            }
            if (plantPerTime && !plantAtStart && createColliders && collidersAdded.Count != 0 && currentPlanted >= spline.Curve.Count && !collidersDestroyed) //v1.1
            {
                for (int i = 0; i < collidersAdded.Count; i++)
                {
                    Destroy(collidersAdded[i]);
                }
                collidersAdded.Clear();
                collidersDestroyed = true;
            }
            if (plantPerTime && !plantAtStart && spline != null && currentPlanted < spline.Curve.Count && Time.fixedTime - prevPlantTime > plantTimeInterval && !collidersDestroyed) //v1.1
            {
                //destroy colliders
                if (createColliders)
                { 
                    if(collidersAdded.Count == 0)
                    {
                        collidersAdded.Clear();
                        collidersDestroyed = true;
                    }
                    else
                    {
                        for (int i = 0; i <destroyCollidersRate; i = i + 1)
                        {
                            Destroy(collidersAdded[0]);
                            collidersAdded.RemoveAt(0);
                        }
                        //destoyedColliders++;
                    }
                    //collidersAdded.Clear();
                    //collidersDestroyed = true;
                }

                //add colliders
                //if ((prevColliderPos - spline.Curve[currentPlanted].position).magnitude > colliderDistance && currentPlanted > 3 && currentPlanted < spline.Curve.Count-3)
                //if ( currentPlanted > 4 && currentPlanted < spline.Curve.Count - 4)
                //{
                //    if (colliderOBJtemp == null)
                //    {
                //        colliderOBJtemp = new GameObject();
                //    }
                //    if (colliderOBJtempPREV1 == null)
                //    {
                //        colliderOBJtempPREV1 = new GameObject();
                //    }
                //    if (colliderOBJtempAFTER1 == null)
                //    {
                //        colliderOBJtempAFTER1 = new GameObject();
                //    }

                //    colliderOBJtemp.transform.position = spline.Curve[currentPlanted].position;
                //    colliderOBJtempPREV1.transform.position = spline.Curve[currentPlanted-2].position;
                //    colliderOBJtempAFTER1.transform.position = spline.Curve[currentPlanted-3].position;

                //    if (colliderOBJtemp.GetComponent<SphereCollider>() == null)
                //    {
                //        colliderOBJtemp.AddComponent<SphereCollider>();
                //    }
                //    if (colliderOBJtempPREV1.GetComponent<SphereCollider>() == null)
                //    {
                //        colliderOBJtempPREV1.AddComponent<SphereCollider>();
                //    }
                //    if (colliderOBJtempAFTER1.GetComponent<SphereCollider>() == null)
                //    {
                //        colliderOBJtempAFTER1.AddComponent<SphereCollider>();
                //    }
                //    colliderOBJtemp.GetComponent<SphereCollider>().radius = colliderSize;
                //    colliderOBJtempPREV1.GetComponent<SphereCollider>().radius = colliderSize;
                //    colliderOBJtempAFTER1.GetComponent<SphereCollider>().radius = colliderSize;
                //    //collidersAdded.Add(colliderOBJ);
                //    prevColliderPos = spline.Curve[currentPlanted].position;
                //}


                prevPlantTime = Time.fixedTime;

                ivyGen.growFlowers = growSplineGradual;
                ivyGen.growIvy = growSplineGradual;

                RaycastHit hit = new RaycastHit();
                hit.point = spline.Curve[currentPlanted].position;

                if (followSplineDirection && currentPlanted > 3)
                {
                    hit.normal = -(spline.Curve[currentPlanted].position - spline.Curve[currentPlanted - 2].position);
                }
                else
                {
                    hit.normal = Vector3.up;
                }

                if ((prevPos - spline.Curve[currentPlanted].position).magnitude > distanceThreshold)
                {
                    ivyGen.generateIvy(hit);
                }

                prevPos = spline.Curve[currentPlanted].position;
                currentPlanted += addPerSegments;
            }



            if (!plantPerTime && !plantAtStart && spline != null && currentPlanted < spline.Curve.Count)
            {
                for (int i = 0; i < spline.Curve.Count; i = i + addPerSegments)
                {
                    if (currentPlanted < i)
                    {
                        RaycastHit hit = new RaycastHit();
                        hit.point = spline.Curve[i].position;

                        if (followSplineDirection)
                        {

                        }
                        else
                        {
                            hit.normal = Vector3.up;
                        }

                        ivyGen.generateIvy(hit);

                        currentPlanted++;
                    }
                }
            }
        }
    }
}
