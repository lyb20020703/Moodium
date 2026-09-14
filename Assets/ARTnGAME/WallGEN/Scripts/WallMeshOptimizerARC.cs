using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Artngame.CommonTools;

namespace Artngame.WallGEN
{

    [ExecuteInEditMode]
    public class WallMeshOptimizerARC : MonoBehaviour
    {
        public GameObject metaBlobSource;
        public WallARC walls;
        public MCBlobARC blobMaker;
        public bool createOptimizedMesh = false;
        public bool resetMesh = false;
        public int maxBricks = 10;
        public List<GameObject> metaBricks = new List<GameObject>();
        // Start is called before the first frame update
        void Start()
        {

        }
        public bool useGPUBlobs = true;
        public ContainerARC GpuBlobMaker;
        public GameObject GPUblobSource;
        // Update is called once per frame
        void Update()
        {
            if (createOptimizedMesh)
            {
                metaBricks.Clear();
                int bricksCount = walls.bricks.Count;
                for (int i = 0; i < walls.bricks.Count; i++)// maxBricks; i++)
                {
                    if (walls.bricks[i] != null && walls.bricks[i].GetComponent<MeshFilter>() != null)
                    {
                        if (useGPUBlobs)
                        {
                            GameObject brick = Instantiate(GPUblobSource);
                            brick.transform.position = walls.bricks[i].transform.position;
                            brick.transform.parent = GpuBlobMaker.transform;
                            metaBricks.Add(brick);
                        }
                        else
                        {
                            GameObject brick = Instantiate(metaBlobSource);  //GameObject brick = new GameObject();
                                                                             //brick.name = "i";
                            brick.transform.position = walls.bricks[i].transform.position;
                            brick.transform.parent = blobMaker.transform;
                            metaBricks.Add(brick);
                        }
                    }
                }
                GPUblobSource.SetActive(false);
                createOptimizedMesh = false;
            }

            if (resetMesh)
            {
                int bricksCount = walls.bricks.Count;
                for (int i = 0; i < metaBricks.Count; i++)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(metaBricks[i]);
                    }
                    else
                    {
                        DestroyImmediate(metaBricks[i]);
                    }
                }
                metaBricks.Clear();
                resetMesh = false;
            }

        }
    }
}