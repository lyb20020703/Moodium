using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace Artngame.TreeGEN.ProceduralIvy
{
    public class LocationToShaderIvyStudio : MonoBehaviour
    {
        //pass light v0.2
        public bool passSunLight = false;
        public Transform sun;

        Transform this_tranf;
        Vector3 prev_pos;
        public float InteractSpeed = 2;

        // Use this for initialization
        void Start()
        {
            this_tranf = this.transform;
            prev_pos = this_tranf.position;
        }
        public List<Material> IvyMaterials = new List<Material>();
        // Update is called once per frame
        void LateUpdate()
        {
            if (Application.isPlaying)
            {
                Vector3 Direction = prev_pos - this_tranf.position;
                Vector3 SpeedVec = (Direction).normalized * ((prev_pos - this_tranf.position).magnitude / Time.deltaTime);
                prev_pos = this_tranf.position;

                for (int i = 0; i < IvyMaterials.Count; i++)
                {
                    IvyMaterials[i].SetVector("_InteractPos", this_tranf.position);
                    if (IvyMaterials[i].HasProperty("_InteractSpeed"))
                    {
                        IvyMaterials[i].SetVector("_InteractSpeed", Vector3.Lerp(IvyMaterials[i].GetVector("_InteractSpeed"), SpeedVec, InteractSpeed * Time.deltaTime));
                    }
                    if (passSunLight && sun != null)
                    {
                        IvyMaterials[i].SetVector("_LightDirection", sun.forward);
                    }
                }
            }
        }
    }
}