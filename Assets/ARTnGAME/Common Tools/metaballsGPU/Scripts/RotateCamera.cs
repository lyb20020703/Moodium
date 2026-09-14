using UnityEngine;
using System.Collections;
namespace Artngame.CommonTools
{
    public class RotateCamera : MonoBehaviour
    {
        public float speed;

        void Start()
        {

        }

        void Update()
        {
            this.transform.Rotate(Vector3.up, this.speed * Time.deltaTime);
        }
    }
}