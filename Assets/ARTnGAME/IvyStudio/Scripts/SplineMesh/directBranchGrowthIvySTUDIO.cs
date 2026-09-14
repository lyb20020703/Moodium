using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Artngame.CommonTools;

namespace Artngame.TreeGEN
{
    public class directBranchGrowthIvySTUDIO : MonoBehaviour
    {
        public Transform MainBranch;

        //v0.1 
        public bool followSpline = false;
        public SplinerPTREANT spline;
        public float followRate = 1;
        public float currentSplinePoint=0;

        public List<Transform> followerBranches = new List<Transform>();

        // Start is called before the first frame update
        void Start()
        {

        }

        public float directionThres = 0.01f;
        public float posDiffthresh = 0.01f;

        Vector3 prevPos;
        public float offsetX = 1; public float offsetY = 1; public float offsetZ = 1;
        // Update is called once per frame
        void Update()
        {
            //v0.1
            //Debug.Log(spline.Curve.Count);
            if (followSpline && spline != null && (int)currentSplinePoint < spline.Curve.Count)
            {
                MainBranch.transform.position = spline.Curve[(int)currentSplinePoint].position;
                currentSplinePoint += followRate;
            }

            if (MainBranch != null)
            {
                for (int i = 0; i < followerBranches.Count; i++)
                {
                    Vector3 direction = MainBranch.transform.position - prevPos;
                    Vector3 posDiff = MainBranch.transform.position - followerBranches[i].position;
                    Vector3 adder = Vector3.zero;
                    if (direction != Vector3.zero && posDiff != Vector3.zero && direction.magnitude > directionThres && posDiff.magnitude > posDiffthresh)
                    {
                        adder = Vector3.Cross(direction, posDiff).normalized;
                    }
                    //followerBranches[i].position = MainBranch.transform.position + adder;
                    followerBranches[i].position = followerBranches[i].position + direction * offsetY + posDiff * offsetX + adder * offsetZ;

                }
            }

            prevPos = MainBranch.transform.position;
        }
    }
}