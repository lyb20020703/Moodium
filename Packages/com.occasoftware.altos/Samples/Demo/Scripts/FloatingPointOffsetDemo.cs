using UnityEngine;

namespace OccaSoftware.Altos.Runtime
{
    public class FloatingPointOffsetDemo : MonoBehaviour
    {
        public float distance = 512;

        private Vector3 origin = Vector3.zero;

        private void OnEnable()
        {
            origin = Vector3.zero;
            AltosSkyDirector.Instance.SetOrigin(origin);
        }

        private void OnDisable()
        {
            origin = Vector3.zero;
            if (AltosSkyDirector.Instance)
            {
                AltosSkyDirector.Instance.SetOrigin(origin);
            }
        }

        void LateUpdate()
        {
            if (transform.position.magnitude > distance)
            {
                origin -= transform.position;
                AltosSkyDirector.Instance.SetOrigin(origin);

                transform.position = Vector3.zero;
            }
        }
    }
}
