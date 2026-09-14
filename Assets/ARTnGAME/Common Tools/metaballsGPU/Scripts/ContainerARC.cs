using UnityEngine;
using System.Collections;
using System.Collections.Generic;
namespace Artngame.CommonTools
{
    public class ContainerARC : MonoBehaviour
    {
        public float safeZone;
        public float resolution;
        public float threshold;
        public ComputeShader computeShader;
        public bool calculateNormals;

        private CubeGridARC grid;

        public void Start()
        {
            this.grid = new CubeGridARC(this, this.computeShader);
        }

        public void Update()
        {
            this.grid.evaluateAll(this.GetComponentsInChildren<MetaBall>());

            Mesh mesh = this.GetComponent<MeshFilter>().mesh;
            mesh.Clear();
            mesh.vertices = this.grid.vertices.ToArray();
            mesh.triangles = this.grid.getTriangles();

            if (this.calculateNormals)
            {
                mesh.RecalculateNormals();
            }
        }
    }
}