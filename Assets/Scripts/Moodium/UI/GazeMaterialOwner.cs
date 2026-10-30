using UnityEngine;
namespace Moodium.UI
{
    public sealed class GazeMaterialOwner:MonoBehaviour
    {
        public Material Material;
        void OnDestroy(){if(Material==null)return;if(Application.isPlaying)Destroy(Material);else DestroyImmediate(Material);}
    }
}
