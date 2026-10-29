using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
using Unity.XR.CoreUtils;
using Moodium.Interaction;

namespace Moodium.NatureWorld
{
    // A bounded companion cloud: never rewrites the half-million-point scan.
    public sealed class BubbleWorldHandRipple : MonoBehaviour
    {
        const int Capacity = 768;
        readonly Vector3[] positions = new Vector3[Capacity];
        readonly Vector3[] velocities = new Vector3[Capacity];
        readonly float[] lives = new float[Capacity];
        readonly Vector3[] vertices = new Vector3[Capacity * 4];
        readonly Color[] colors = new Color[Capacity * 4];
        readonly Vector3[] previous = new Vector3[2];
        readonly bool[] tracked = new bool[2];
        Mesh mesh;
        Material material;
        Texture2D atlas;
        XRHandSubsystem hands;
        XROrigin origin;
        Camera viewer;
        int cursor;
        float next;

        public void Initialize(string folder, Camera camera)
        {
            viewer = camera;
            atlas = Resources.Load<Texture2D>(BubbleWorldData.RoundColorsResource(folder));
            material = BubbleWorldData.CreateUnlit("Hand Ripple Points", atlas, true, CullMode.Off, Color.white);
            BubbleWorldData.ConfigureRoundPoints(material);
            BubbleWorldController.ConfigureTransparentPoints(material);
            mesh = new Mesh { name = "Bounded Hand Ripple" };
            mesh.MarkDynamic();
            var uv = new Vector2[Capacity * 4];
            var triangles = new int[Capacity * 6];
            for (int i = 0; i < Capacity; i++)
            {
                // Sample the same round colour atlas as the surrounding scan.
                int sample = (i * 647) % 500000;
                int row = atlas.width / 4;
                float x = (sample % row * 4 + .5f) / atlas.width;
                float y = 1f - (sample / row * 4 + 3.5f) / atlas.height;
                float s = 3f / atlas.width;
                int v = i * 4, t = i * 6;
                uv[v] = new Vector2(x,y); uv[v+1] = new Vector2(x+s,y);
                uv[v+2] = new Vector2(x+s,y+s); uv[v+3] = new Vector2(x,y+s);
                triangles[t]=v; triangles[t+1]=v+1; triangles[t+2]=v+2;
                triangles[t+3]=v; triangles[t+4]=v+2; triangles[t+5]=v+3;
            }
            mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles;
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        void Update()
        {
            if (mesh == null || Time.unscaledTime < next) return;
            float dt = Mathf.Min(.06f, Mathf.Max(.025f, Time.unscaledTime - next + .025f));
            next = Time.unscaledTime + .025f;
            if (hands == null || !hands.running)
                hands = XRGeneralSettings.Instance?.Manager?.activeLoader?.GetLoadedSubsystem<XRHandSubsystem>();
            if (origin == null) origin = FindFirstObjectByType<XROrigin>();
            if (hands != null && hands.running) { Emit(hands.leftHand,0,dt); Emit(hands.rightHand,1,dt); }
            else { tracked[0]=tracked[1]=false; }
            if (viewer == null) viewer = Camera.main;
            Vector3 right = viewer != null ? viewer.transform.right : Vector3.right;
            Vector3 up = viewer != null ? viewer.transform.up : Vector3.up;
            for (int i=0;i<Capacity;i++)
            {
                lives[i]=Mathf.Max(0,lives[i]-dt);
                positions[i]+=velocities[i]*dt;
                velocities[i]*=Mathf.Exp(-2f*dt);
                float size=lives[i]>0 ? .0035f * Mathf.SmoothStep(0,1,Mathf.Min(lives[i],2f-lives[i])/.3f) : 0;
                int v=i*4;
                vertices[v]=positions[i]-right*size-up*size;
                vertices[v+1]=positions[i]+right*size-up*size;
                vertices[v+2]=positions[i]+right*size+up*size;
                vertices[v+3]=positions[i]-right*size+up*size;
                for(int j=0;j<4;j++) colors[v+j]=new Color(1,1,1,Mathf.Clamp01(lives[i]));
            }
            mesh.vertices=vertices; mesh.colors=colors; mesh.RecalculateBounds();
        }

        void Emit(XRHand hand,int index,float dt)
        {
            if (!hand.isTracked || !hand.GetJoint(XRHandJointID.Palm).TryGetPose(out var pose))
            { tracked[index]=false; return; }
            var world=PalmPressGestureController.TransformTrackingPose(pose,origin != null ? origin.Origin.transform : null).position;
            var velocity=tracked[index] ? (world-previous[index])/dt : Vector3.zero;
            previous[index]=world; tracked[index]=true;
            float speed=velocity.magnitude;
            if(speed<.08f || speed>3f) return;
            int count=Mathf.Clamp(Mathf.CeilToInt(speed*10),2,16);
            for(int j=0;j<count;j++)
            {
                int i=cursor++%Capacity;
                positions[i]=transform.InverseTransformPoint(world+Random.insideUnitSphere*.12f);
                velocities[i]=transform.InverseTransformVector(Vector3.ClampMagnitude(velocity,.7f)*.22f+Random.insideUnitSphere*.035f+Vector3.up*.018f);
                lives[i]=2f;
            }
        }
        void OnDestroy() { if(mesh!=null) Destroy(mesh); if(material!=null) Destroy(material); }
    }
}
