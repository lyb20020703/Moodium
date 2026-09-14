using UnityEngine;
using System.Collections;
using System.Collections.Generic;
///using Artngame.CommonTools;
//using Assets.Scripts;

namespace Artngame.CommonTools {

	[System.Serializable()]

	public class SplinerPTREANT : MonoBehaviour {

        //v2.1.23 - spline changed state
        public bool splineChanged = false;
        public GameObject terrainEditors;// CustomTerrains terrainEditors;
        //public CustomTerrain terrainEditor2;
        //public CustomTerrain terrainEditor3;
        //public CustomTerrain terrainEditor4;

        //v1.9
        public void checkChange()
        {
            if (control_points_children != null)
            {
                if (control_points_children.Count != 0)
                {

                    //v2.1
                    //bool one_changed = false;
                    for (int i = 0; i < SplinePoints.Count; i++)
                    {
                        if (control_points_children[i] != null)
                        {
                            //Debug.Log("CHECKING SPLINE CHANGE");
                            //v2.1
                            if (SplinePoints[i].position == control_points_children[i].transform.position)
                            {
                                //do nothing
                            }
                            else
                            {
                                //SplinePoints[i].position = control_points_children[i].transform.position;
                                //one_changed = true;
                                //v2.1.23 - spline changed state
                                //Debug.Log("Spline change");
                                splineChanged = true;
                                //script.updateTerrainOnChange();
                                //script.SplinePoints[i].position = script.control_points_children[i].transform.position;
                            }

                            if (control_points_children[i].name != "Sphere" + i)
                            {
                                //control_points_children[i].name = "Sphere" + i;
                            }


                        }
                    }

                    //v1.1  //v2.1.23
                    if (splineChanged)
                    {
                        //updateTerrainOnChange();
                        splineChanged = false;
                    }

                    //if(one_changed){//v2.1
                    //replot = true;
                    //Debug.Log("d");
                    //}
                }
            }
        }

        //public void saveStateTexture()
        //{
        //    CustomTerrains terrainsEditors = terrainEditors.GetComponent<CustomTerrains>();
        //    terrainsEditors.terrainFluid.saveStateTexture();
        //    if (terrainsEditors.terrainFluid2 != null)
        //    {
        //        terrainsEditors.terrainFluid2.saveStateTexture();
        //    }
        //    if (terrainsEditors.terrainFluid3 != null)
        //    {
        //        terrainsEditors.terrainFluid3.saveStateTexture();
        //    }
        //    if (terrainsEditors.terrainFluid4 != null)
        //    {
        //        terrainsEditors.terrainFluid4.saveStateTexture();
        //    }
        //}
        public void setSplineMode(int mode)
        {
            //set replacing or additive
            //bool setReplacing = false;
            if(mode == 3)
            {
                mode = 2;
               // setReplacing = true;
            }

            //CustomTerrains terrainsEditors = terrainEditors.GetComponent<CustomTerrains>();
            //terrainsEditors.terrainFluid.splineMode = mode;
            //if (setReplacing)
            //{
            //    terrainsEditors.terrainFluid.setReplacingDepthStamp = true;
            //}
            //else
            //{
            //    terrainsEditors.terrainFluid.setReplacingDepthStamp = false;
            //}

            //if (terrainsEditors.terrainFluid2 != null)
            //{
            //    terrainsEditors.terrainFluid2.splineMode = mode;
            //    if (setReplacing)
            //    {
            //        terrainsEditors.terrainFluid2.setReplacingDepthStamp = true;
            //    }
            //    else
            //    {
            //        terrainsEditors.terrainFluid2.setReplacingDepthStamp = false;
            //    }
            //}
            //if (terrainsEditors.terrainFluid3 != null)
            //{
            //    terrainsEditors.terrainFluid3.splineMode = mode;
            //    if (setReplacing)
            //    {
            //        terrainsEditors.terrainFluid3.setReplacingDepthStamp = true;
            //    }
            //    else
            //    {
            //        terrainsEditors.terrainFluid3.setReplacingDepthStamp = false;
            //    }
            //}
            //if (terrainsEditors.terrainFluid4 != null)
            //{
            //    terrainsEditors.terrainFluid4.splineMode = mode;
            //    if (setReplacing)
            //    {
            //        terrainsEditors.terrainFluid4.setReplacingDepthStamp = true;
            //    }
            //    else
            //    {
            //        terrainsEditors.terrainFluid4.setReplacingDepthStamp = false;
            //    }
            //}
        }
        //public void updateTerrainOnChange()
        //{
        //    if (terrainEditors != null)
        //    {
        //        CustomTerrains terrainsEditors = terrainEditors.GetComponent<CustomTerrains>();
        //        terrainsEditors.terrainFluid.updateSpline();
        //        if (terrainsEditors.terrainFluid2 != null)
        //        {
        //            terrainsEditors.terrainFluid2.updateSpline();
        //        }
        //        if (terrainsEditors.terrainFluid3 != null)
        //        {
        //            terrainsEditors.terrainFluid3.updateSpline();
        //        }
        //        if (terrainsEditors.terrainFluid4 != null)
        //        {
        //            terrainsEditors.terrainFluid4.updateSpline();
        //        }
        //    }
        //}

        //v2.1.22
        public bool useChildren= false;
		public bool updateChildren=false;
		public List<SplinerPTREANT> childrenSplines = new List<SplinerPTREANT> ();
		public List<int> childrenAttachIDs = new List<int>();
		public enum updateModes {all, gradual, one };
		public updateModes updateMode = updateModes.gradual;
		public bool is_root = false;
		public SplinerPTREANT parentSpline;

		//v2.1.23
		public void AddPoint() {
			//Undo.RecordObject(script,"Undo add");

			Vector3 Vector_along_last_two_points = (SplinePoints[SplinePoints.Count-1].position-SplinePoints[SplinePoints.Count-2].position);
			Vector3  Moved_vector_to_last_point = SplinePoints[SplinePoints.Count-1].position + 1*Vector_along_last_two_points;

			SplinePoints.Add(new BranchSplineNode(Moved_vector_to_last_point,Quaternion.identity));

			GameObject SPHERE_SMALL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			control_points_children.Add(SPHERE_SMALL);
			//Undo.RegisterCreatedObjectUndo(SPHERE_SMALL, "create " + SPHERE_SMALL.name);

			control_points_children[control_points_children.Count-1].transform.position = Moved_vector_to_last_point;
			control_points_children[control_points_children.Count-1].transform.localScale = (1/gameObject.transform.localScale.x)*0.3f*new Vector3(Handle_scale,Handle_scale,Handle_scale);

			control_points_children[control_points_children.Count-1].transform.parent = transform;

			Overide_seg_detail.Add(0);
		}

		//v2.1.23
		public void GrowVine(){

			bool EndCriteria = false;
			while (EndCriteria == false) {

				//params
				float moveStep = 45; //move 1 meter
				float gravitateUpFactor = 1120;
				float gravitateUpDist = 1;
				float returnHeight = 250; //if going up for this distance, start going down

				//extend spline
				AddPoint ();

				//spline endpoint and forward vectors
				int LastPointID = SplinePoints.Count-1;
				Vector3 Origin = SplinePoints[SplinePoints.Count-2].position;
				Vector3 Direction =  SplinePoints[SplinePoints.Count-2].position - SplinePoints[SplinePoints.Count-3].position;

				//define sun
				Vector3 sunPos = new Vector3(1000,1000,1000);

				//raycast to see what is around, extend spline there, consider weighted move towards up or adjustent to objects, if no hit found move ahead towards forward & do weighting
				RaycastHit hit = new RaycastHit();

				float max_dist = 100;
				Vector3 gravitateBackVector = 0.16f * (SplinePoints [LastPointID - 1].position.y - returnHeight) * -Vector3.up + Vector3.right * 5;
				if (Physics.Raycast (Origin, Direction, out hit, max_dist)) {
					
					float dist = (hit.point - SplinePoints [LastPointID - 1].position).magnitude;

					if (dist > moveStep) { //if the distance is enough move ahead, otherwise move alone wall
						//DO								  Move ahead as you were goind     Look up by a factor, based on distance from obstacle					
						if (SplinePoints [LastPointID - 1].position.y < 8 * returnHeight / 7) {
							gravitateBackVector = Vector3.zero;
						}
						SplinePoints [LastPointID].position = SplinePoints [LastPointID - 1].position + Direction.normalized * moveStep + gravitateUpFactor * Vector3.up * (gravitateUpDist / dist) + gravitateBackVector;
					} else {
						SplinePoints [LastPointID].position = hit.point + hit.normal * 5 + Vector3.right * 0.5f + gravitateUpFactor * Vector3.up * (gravitateUpDist / dist) + gravitateBackVector; //move on wall with an offset using the normal
						Debug.Log ("point " + LastPointID + " collided close with:" + hit.collider.gameObject.name + " at:" + hit.point);
					}

					Debug.Log ("point " + LastPointID + " collided with" + hit.collider.gameObject.name);
				} else {
					SplinePoints [LastPointID].position = SplinePoints [LastPointID - 1].position + Direction.normalized * moveStep + Vector3.right * 1.5f + gravitateBackVector;
					
				}

				//add points between the raycast hit and origin, if distance is more than threshold

				if (SplinePoints.Count > 25) {
				
					EndCriteria = true;
				
				}

			}









			//AddPoint ();












		}

		//v2.1.22
		public void doChildrenUpdate(int parentID){

			//update self, if parentID > 0 (root pr changed will set it to -1)
			if(parentID >=0){
				//only first element adapt CASE0
				if (updateMode == updateModes.one) {
					control_points_children [0].transform.position = parentSpline.control_points_children [parentID].transform.position;
				} else if (updateMode == updateModes.all) {
					Vector3 parentPos = parentSpline.control_points_children [parentID].transform.position;
					control_points_children [0].transform.position = parentPos;
					for (int i = 1; i < control_points_children.Count; i++) {		
						Vector3 pos = control_points_children [i].transform.position;
						control_points_children [i].transform.position = new Vector3 (pos.x, parentPos.y, pos.z);
					}
				} else if (updateMode == updateModes.gradual) {
					Vector3 parentPos = parentSpline.control_points_children [parentID].transform.position;
					control_points_children [0].transform.position = parentPos;
					for (int i = 1; i < control_points_children.Count-1; i++) {		
						Vector3 pos = control_points_children [i].transform.position;
						control_points_children [i].transform.position = new Vector3 ((pos.x+0)/1, (control_points_children[control_points_children.Count-1].transform.position.y+parentPos.y)/i, (pos.z+0)/1);
					}
				}
			}

			for (int i = 0; i < childrenSplines.Count; i++) {
				childrenSplines [i].doChildrenUpdate (childrenAttachIDs[i]);
			}
		}

		//v2.1
		public bool Multithreaded = false;
		public bool DefineRotations = false;

		public List<BranchSplineNode[]> Keep_Curve;
		public BranchSplineNode[] Curve1;

		[SerializeField]
		public Vector3 last_object_position_inspector_saw;

		public List<GameObject> control_points_children = new List<GameObject>();//v2.1

		[SerializeField]
		public Vector3 last_object_rotation_inspector_saw;

		[SerializeField]
		public Vector3 last_object_scale_inspector_saw;

		[SerializeField]
		public float Handle_scale=6.5f;

		void Start () {

			//Loom tmp = Loom.Current;
			//Loom.Current.GetComponent<Loom>();
			timer = Time.fixedTime;

			if(CurveQuality <1){
				CurveQuality =1;
			}

			keep_quality=CurveQuality;

			Start_Point = this.transform.position;

			if(parent_moving !=null){
				Start_Point_object = parent_moving.transform.position;
			}

			Keep_last_pos = this.transform.position;
			Keep_last_rot = this.transform.eulerAngles;
			Sphere_particles = this.gameObject.GetComponentInChildren(typeof(ParticleSystem)) as ParticleSystem;
			//Legacy_particles = this.gameObject.GetComponentInChildren(typeof(ParticleEmitter)) as ParticleEmitter;

			if(last_object_position_inspector_saw != this.gameObject.transform.position ){ 

				for (int i=0;i<this.SplinePoints.Count;i++) 
				{
					Vector3 Distance = Keep_last_pos - last_object_position_inspector_saw;
					SplinePoints[i].position = SplinePoints[i].position + Distance;
				}
				last_object_position_inspector_saw = this.gameObject.transform.position;
			}

			if(last_object_rotation_inspector_saw != this.gameObject.transform.eulerAngles ){ 

				for (int i=0;i<this.SplinePoints.Count;i++) 
				{
					Vector3 Distance =Keep_last_rot -  last_object_rotation_inspector_saw;

					Vector3 Move_to_0 = SplinePoints[i].position - transform.position;
					SplinePoints[i].position = Quaternion.AngleAxis(Distance.y, new Vector3(0,1,0) ) * Move_to_0 + transform.position;

					Move_to_0 = SplinePoints[i].position - transform.position;
					SplinePoints[i].position = Quaternion.AngleAxis(Distance.z, new Vector3(0,0,1) ) * Move_to_0 + transform.position;

					Move_to_0 = SplinePoints[i].position - transform.position;
					SplinePoints[i].position = Quaternion.AngleAxis(Distance.x, new Vector3(1,0,0) ) * Move_to_0 + transform.position;
				}
				last_object_rotation_inspector_saw = this.gameObject.transform.eulerAngles;
			}

            #region CALC CURVE
            BranchSplineNode[]  SplinePoints1 = SplinePoints.ToArray();
			int Control_points_Count = SplinePoints1.Length;

			if(Overide_seg_detail.Count < SplinePoints.Count-1){
				Overide_seg_detail = new List<int>(); 
				for (int i=0;i<SplinePoints.Count-1;i++) 
				{					
					Overide_seg_detail.Add(0);
				}				
			}
			Keep_Overide_seg_detail=new List<int>();
			for(int i=0;i<Overide_seg_detail.Count;i++){				
				Keep_Overide_seg_detail.Add(Overide_seg_detail[i]);				
			}

			if (Keep_Curve == null ){Keep_Curve = new List<BranchSplineNode[]>();}

			if ((Multithreaded && Thead_finished) | !Multithreaded) {
				Curve.Clear();
			}

			for (int i=0;i<Control_points_Count;i++) {

				if (i<=Control_points_Count-2) {

					Draw_Curve(i, SplinePoints1, Control_points_Count,false);

					//					float detail = CurveQuality;
					//
					//					if(Overide_seg_detail[i] > 0){						
					//						detail = Overide_seg_detail[i];						
					//					}					
					//					Vector3 Handler_start  = SplinePoints1[i].position;
					//					Vector3 Handler_end  = SplinePoints1[i+1].position;
					//					
					//					Vector3 Vector_along_direction=Handler_start-Handler_end;
					//					Vector3 Vector_along_direction_INV=Handler_end-Handler_start;
					//					Vector3 Vector_along_direction_normalized  = (Vector_along_direction).normalized;
					//					
					//					Vector3 Curve_starting_direction = Vector3.zero;
					//					Vector3 Curve_ending_direction  = Vector3.zero;
					//					
					//					if (i==0 | i <= Control_points_Count-3 ) {
					//						if (i==0){
					//							Curve_starting_direction  = Vector_along_direction_INV.normalized;
					//							SplinePoints1[i].direction = Curve_starting_direction;
					//						}
					//						else{
					//							Curve_starting_direction = SplinePoints1[i].direction;
					//						}
					//						Curve_ending_direction = -1.0f*((SplinePoints1[i+2].position-Handler_end).normalized - Vector_along_direction_normalized).normalized;
					//						SplinePoints1[i+1].direction = -1.0f*Curve_ending_direction;
					//
					//						//v2.1
					//						if(SplinePoints[i+1].Type == 2){
					//							//Curve_starting_direction = Curve_starting_direction + SplinePoints[i+0].directionB;
					//							Curve_ending_direction = Curve_ending_direction - SplinePoints[i+1].directionB;			//USE TO AFFECT BOTH SIDES !!! (type 2)
					//						}
					//						if(SplinePoints[i+1].Type == 1){
					//							//Curve_starting_direction = Curve_starting_direction + SplinePoints[i+0].directionB;
					//							Curve_ending_direction = Curve_ending_direction - SplinePoints[i+1].directionB;			//USE TO AFFECT BOTH SIDES !!! (type 2)
					//						}
					//						if(SplinePoints[i+0].Type == 1 | SplinePoints[i+1].Type == 2){
					//							SplinePoints[i+0].direction = SplinePoints[i+0].direction + SplinePoints[i+0].directionB;
					//							//Curve_ending_direction = Curve_ending_direction +SplinePoints[i+0].directionB;
					//							if(SplinePoints[i+0].directionB.magnitude == 0){
					//								SplinePoints[i+0].directionB = SplinePoints[i+0].direction; 
					//							}
					//						}
					//						if(SplinePoints[i+1].Type == 1){
					//							SplinePoints[i+1].direction = SplinePoints[i+1].direction + SplinePoints[i+1].directionB;
					//							//Curve_ending_direction = Curve_ending_direction +SplinePoints[i+1].directionB;
					//							if(SplinePoints[i+1].directionB.magnitude == 0){
					//								SplinePoints[i+1].directionB = SplinePoints[i+1].direction;
					//							}
					//						}
					//						if(SplinePoints[i+1].Type == 2){
					//							SplinePoints[i+1].direction = SplinePoints[i+1].direction + SplinePoints[i+1].directionC;
					//							//Curve_ending_direction = Curve_ending_direction +SplinePoints[i+1].directionB;
					//							if(SplinePoints[i+1].directionC.magnitude == 0){
					//								SplinePoints[i+1].directionC = SplinePoints[i+1].direction;
					//							}
					//						}
					//						if(SplinePoints[i+2].Type == 1 | SplinePoints[i+1].Type == 2){
					//							SplinePoints[i+2].direction = SplinePoints[i+2].direction + SplinePoints[i+2].directionB;
					//							//Curve_ending_direction = Curve_ending_direction +SplinePoints[i+2].directionB;
					//							if(SplinePoints[i+2].directionB.magnitude == 0){
					//								SplinePoints[i+2].directionB = SplinePoints[i+2].direction;
					//							}
					//						}
					//						
					//					} else {
					//						Curve_starting_direction = SplinePoints1[i].direction;
					//						Curve_ending_direction = Vector_along_direction.normalized;
					//						SplinePoints1[i+1].direction = -1.0f*Curve_ending_direction;
					//
					//						//v2.1
					//						if(SplinePoints[i+0].Type == 1 | SplinePoints[i+0].Type == 2){
					//							Curve_starting_direction = Curve_starting_direction + SplinePoints[i+0].directionB;
					//						}
					//					} 
					//					
					//					List<LandSplineNode> a = new List<LandSplineNode>();
					//					
					//					for (float j = 0;j<1;j+= (1/detail)) {
					//						
					//						Vector3 a1 = Handler_start;
					//						Vector3 b1 = Vector_along_direction.magnitude*Curve_starting_direction/2;
					//						Vector3 c1 = Vector_along_direction.magnitude*Curve_ending_direction/2;
					//						Vector3 d1 = Handler_end;
					//						Vector3 C1 = ( d1 - (3.0f * (c1+d1)) + (3.0f * (b1+a1)) - a1 );
					//						Vector3 C2 = ( (3.0f * (c1+d1)) - (6.0f * (b1+a1)) + (3.0f * a1) );
					//						Vector3 C3 = ( (3.0f * (b1+a1)) - (3.0f * a1) );
					//						Vector3 C4 = ( a1 );
					//						Vector3 p = C1*j*j*j + C2*j*j + C3*j + C4;
					//						
					//						LandSplineNode NEW_POINT = new LandSplineNode(Vector3.zero,Quaternion.identity);
					//						NEW_POINT.position = p;
					//						
					//						a.Add (NEW_POINT);
					//					}
					//					
					//					Curve1 = a.ToArray();
					//					List<LandSplineNode> Curve_Temp =  new List<LandSplineNode>();
					//					Curve_Temp.AddRange(Curve1);
					//					Curve_Temp.Add(SplinePoints1[i+1]);
					//					Curve1 = Curve_Temp.ToArray();
					//					Keep_Curve.Add(Curve1);
					//					
					//				Curve.AddRange(Curve1);
				}
			}
			#endregion
		}



		void Draw_multithreaded(int i, BranchSplineNode[]  SplinePoints1, int Control_points_Count, bool realtime){
			float detail = CurveQuality;
			if (detail < 1) {
				detail = 1;
			}

			//Debug.Log ("in");

			if (Overide_seg_detail [i] > 0) {						
				detail = Overide_seg_detail [i];						
			}					
			Vector3 Handler_start = SplinePoints1 [i].position;
			Vector3 Handler_end = SplinePoints1 [i + 1].position;

			Vector3 Vector_along_direction = Handler_start - Handler_end;
			Vector3 Vector_along_direction_INV = Handler_end - Handler_start;
			Vector3 Vector_along_direction_normalized = (Vector_along_direction).normalized;

			Vector3 Curve_starting_direction = Vector3.zero;
			Vector3 Curve_ending_direction = Vector3.zero;

			if (i == 0 | i <= Control_points_Count - 3) {
				if (i == 0) {

					//v2.1
					if (SplinePoints [i + 0].Type == 1 | SplinePoints [i + 0].Type == 2) {
						Curve_starting_direction = Vector_along_direction_INV.normalized - SplinePoints [i + 0].directionB;
						SplinePoints1 [i].direction = Curve_starting_direction;// + SplinePoints [i + 2].directionB;
					} else {
						Curve_starting_direction = Vector_along_direction_INV.normalized;
						SplinePoints1 [i].direction = Curve_starting_direction;
					}

				} else {
					Curve_starting_direction = SplinePoints1 [i].direction;
				}
				Curve_ending_direction = -1.0f * ((SplinePoints1 [i + 2].position - Handler_end).normalized - Vector_along_direction_normalized).normalized;
				SplinePoints1 [i + 1].direction = -1.0f * Curve_ending_direction;

				//v2.1
				if (SplinePoints [i + 1].Type == 2) {
					//Curve_starting_direction = Curve_starting_direction + SplinePoints[i+0].directionB;
					Curve_ending_direction = Curve_ending_direction - SplinePoints [i + 1].directionB;			//USE TO AFFECT BOTH SIDES !!! (type 2)
				}
				if (SplinePoints [i + 1].Type == 1) {
					//Curve_starting_direction = Curve_starting_direction + SplinePoints[i+0].directionB;
					Curve_ending_direction = Curve_ending_direction - SplinePoints [i + 1].directionB;			//USE TO AFFECT BOTH SIDES !!! (type 2)
				}
				if (SplinePoints [i + 0].Type == 1 | SplinePoints [i + 1].Type == 2) {
					SplinePoints [i + 0].direction = SplinePoints [i + 0].direction + SplinePoints [i + 0].directionB;
					//Curve_ending_direction = Curve_ending_direction +SplinePoints[i+0].directionB;
					if (SplinePoints [i + 0].directionB.magnitude == 0) {
						SplinePoints [i + 0].directionB = SplinePoints [i + 0].direction; 
					}
				}
				if (SplinePoints [i + 1].Type == 1) {
					SplinePoints [i + 1].direction = SplinePoints [i + 1].direction + SplinePoints [i + 1].directionB;
					//Curve_ending_direction = Curve_ending_direction +SplinePoints[i+1].directionB;
					if (SplinePoints [i + 1].directionB.magnitude == 0) {
						SplinePoints [i + 1].directionB = SplinePoints [i + 1].direction;
					}
				}
				if (SplinePoints [i + 1].Type == 2) {
					if (i < Control_points_Count - 3) {
						SplinePoints [i + 1].direction = SplinePoints [i + 1].direction - SplinePoints [i + 1].directionC;
					}
					//Curve_ending_direction = Curve_ending_direction +SplinePoints[i+1].directionB;
					if (SplinePoints [i + 1].directionC.magnitude == 0) {
						SplinePoints [i + 1].directionC = -SplinePoints [i + 1].direction;
					}
					if (SplinePoints [i + 1].directionB.magnitude == 0) {
						SplinePoints [i + 1].directionB = SplinePoints [i + 1].direction;
					}
				}
				if (SplinePoints [i + 2].Type == 1 | SplinePoints [i + 1].Type == 2) {
					SplinePoints [i + 2].direction = SplinePoints [i + 2].direction + SplinePoints [i + 2].directionB;
					//Curve_ending_direction = Curve_ending_direction +SplinePoints[i+2].directionB;
					if (SplinePoints [i + 2].directionB.magnitude == 0) {
						SplinePoints [i + 2].directionB = SplinePoints [i + 2].direction;
					}
				}

			} else {
				Curve_starting_direction = SplinePoints1 [i].direction;
				Curve_ending_direction = Vector_along_direction.normalized;
				SplinePoints1 [i + 1].direction = -1.0f * Curve_ending_direction;

				//Debug.Log ("aa");
				//v2.1
				if (SplinePoints [i + 0].Type == 1 | SplinePoints [i + 0].Type == 2) {
					Curve_starting_direction = Curve_starting_direction - SplinePoints [i + 0].directionC;

					//if(i != Control_points_Count-2){
					Curve_ending_direction = SplinePoints1 [i].direction - SplinePoints [i + 1].directionB;
					//}
				}
			} 

			List<BranchSplineNode> a = new List<BranchSplineNode> ();

			for (float j = 0; j < 1; j += (1 / detail)) {

				Vector3 a1 = Handler_start;
				Vector3 b1 = Vector_along_direction.magnitude * Curve_starting_direction / 2;
				Vector3 c1 = Vector_along_direction.magnitude * Curve_ending_direction / 2;
				Vector3 d1 = Handler_end;
				Vector3 C1 = (d1 - (3.0f * (c1 + d1)) + (3.0f * (b1 + a1)) - a1);
				Vector3 C2 = ((3.0f * (c1 + d1)) - (6.0f * (b1 + a1)) + (3.0f * a1));
				Vector3 C3 = ((3.0f * (b1 + a1)) - (3.0f * a1));
				Vector3 C4 = (a1);
				Vector3 p = C1 * j * j * j + C2 * j * j + C3 * j + C4;

                BranchSplineNode NEW_POINT = new BranchSplineNode(Vector3.zero, Quaternion.identity);
				NEW_POINT.position = p;

				a.Add (NEW_POINT);
			}

			if (realtime) {
				a.Add (SplinePoints1 [i + 1]);
				Curve.AddRange (a);

				//v2.1
				Keep_Curve.Add (Curve1);

			} else {
				Curve1 = a.ToArray ();
				List<BranchSplineNode> Curve_Temp = new List<BranchSplineNode> ();
				Curve_Temp.AddRange (Curve1);
				Curve_Temp.Add (SplinePoints1 [i + 1]);
				Curve1 = Curve_Temp.ToArray ();
				Keep_Curve.Add (Curve1);

				Curve.AddRange (Curve1);
				//Keep_Curve = Curve.ToArray();
			}
		}


		public bool Thead_finished = true;
		//
		public void Draw_Curve(int i, BranchSplineNode[]  SplinePoints1, int Control_points_Count, bool realtime)
		{
			//if ((Multithreaded && Thead_finished) | !Multithreaded) {
			//Curve.Clear ();
			//Curve.AddRange (Curve1);

			if (!Multithreaded) {

				Draw_multithreaded (i, SplinePoints1, Control_points_Count, realtime);

			} else {

				//					Thead_finished = false;
				//					LoomANG.RunAsync (() => {

				Draw_multithreaded (i, SplinePoints1, Control_points_Count, realtime);

				//						Thead_finished = true;
				//					});
			}
			//}
		}



		public bool show_controls_play_mode=false;
		public bool show_controls_edit_mode=true;

		public int Initialized =0;

		public ParticleSystem Sphere_particles;
		//public ParticleEmitter Legacy_particles;

		public bool loop = false; 
		public GameObject parent_moving;

		[SerializeField]
		public List<BranchSplineNode> Curve = new List<BranchSplineNode>(); 

		private int count_steps;
		public int count_steps2;

		public bool follow_curve=true;
		public Vector3 Start_Point_object;

		public Vector3 Keep_last_pos;
		public Vector3 Keep_last_rot;

		[SerializeField]
		public float Scale_curve=1f;

		[SerializeField]
		public float Keep_last_scale;

		public float Motion_Speed=1;

		public bool Game_Mode_On=false;

		public bool Alter_mode=false;
		public bool Alter_mode2=false;
		public bool Accelerate=false;

		[SerializeField]
		public bool Always_on=false;

		public bool Add_point=false;
		public bool With_manip=false;

		public bool Add_mid_point=false;
		public int Add_in_Segment=1;

		public float Node_toggle_dist =1f;

		//infinidy
		public bool repainted=false;

		float timer;
		int current_frame =0;
		public bool spread_frames = false;

		//v1.8
		bool repaint=false;

		//v2.1
		public bool Desynch = false; // control desynch method

		void Update () {

            //v1.9 - terrain
            if (terrainEditors != null)
            {
                //Debug.Log("CHECKING SPLINE CHANGE");
                checkChange();
            }

            if (CurveQuality <1){				
				CurveQuality =1;				
			}

			if(Overide_seg_detail.Count < SplinePoints.Count-1 | Keep_Overide_seg_detail.Count < SplinePoints.Count-1){ //PDM v2.1
				Overide_seg_detail = new List<int>(); 
				for (int i=0;i<SplinePoints.Count-1;i++) 
				{					
					Overide_seg_detail.Add(0);
				}
				Keep_Overide_seg_detail=new List<int>();
				Keep_Overide_seg_detail = Overide_seg_detail;
			}

			#region CALC CURVE
			if(Game_Mode_On){

				//v2.1
				if(Curve == null){
					return;
				}

				//v1.8 - Define change range, so only changed curved is calculated
				//int Range_start=0;
				//int Range_end = SplinePoints.Count-1;

				//bool repaint=false;

				int more_than_one = 0;

				for (int i=0;i<Overide_seg_detail.Count-1;i++) 
				{					
					if(Keep_Overide_seg_detail[i] != Overide_seg_detail[i]){						
						Keep_Overide_seg_detail[i] = Overide_seg_detail[i];							
						repaint=true;

						//v1.8
						//Range_start = i-1;
						//Range_end = i+1;
						more_than_one++;
					}
				}

				//if quality changes
				if(keep_quality!=CurveQuality){
					keep_quality=CurveQuality;
					repaint=true;
				}

				if(Add_point){

					Add_point=false;
					repaint=true;

					Vector3 Vector_along_last_two_points = (SplinePoints[SplinePoints.Count-1].position-SplinePoints[SplinePoints.Count-2].position);

					Vector3  Moved_vector_to_last_point = SplinePoints[SplinePoints.Count-1].position + 1*Vector_along_last_two_points;					
					SplinePoints.Add(new BranchSplineNode(Moved_vector_to_last_point,Quaternion.identity));

					Overide_seg_detail.Add(0);
					Keep_Overide_seg_detail.Clear();
					for(int i=0;i<Overide_seg_detail.Count;i++){						
						Keep_Overide_seg_detail.Add(Overide_seg_detail[i]);						
					}

					GameObject SPHERE_SMALL = GameObject.CreatePrimitive(PrimitiveType.Sphere);
					control_points_children.Add(SPHERE_SMALL);

					control_points_children[control_points_children.Count-1].transform.position = Moved_vector_to_last_point;
					control_points_children[control_points_children.Count-1].transform.localScale = (1/gameObject.transform.localScale.x)*0.3f*new Vector3(Handle_scale,Handle_scale,Handle_scale);

					if(With_manip){
						control_points_children[control_points_children.Count-1].AddComponent(typeof(DragTransformInfiniLAND));
					}
					control_points_children[control_points_children.Count-1].transform.parent = transform;	

					//v1.8
					//Range_start = SplinePoints.Count-2;
					//Range_end = SplinePoints.Count-0;
				}

				if(SplinePoints.Count > 2 ){

					if (Add_mid_point) {

						Add_mid_point=false;						

						if(Add_in_Segment > 0 & Add_in_Segment < SplinePoints.Count){

							repaint=true;							

							Vector3 Moved_vector_to_last_point = SplinePoints[Add_in_Segment-1].position -(SplinePoints[Add_in_Segment-1].position-SplinePoints[Add_in_Segment].position)/2;

							SplinePoints.Insert(Add_in_Segment, new BranchSplineNode(Moved_vector_to_last_point,Quaternion.identity));							
							GameObject SPHERE_SMALL = GameObject.CreatePrimitive(PrimitiveType.Sphere);							
							control_points_children.Insert(Add_in_Segment,SPHERE_SMALL);													

							Overide_seg_detail.Insert(Add_in_Segment-1,0);
							Keep_Overide_seg_detail.Clear();
							for(int i=0;i<Overide_seg_detail.Count;i++){								
								Keep_Overide_seg_detail.Add(Overide_seg_detail[i]);								
							}								
							control_points_children[Add_in_Segment].transform.position = Moved_vector_to_last_point;
							control_points_children[Add_in_Segment].transform.localScale = (1/gameObject.transform.localScale.x)*0.3f*new Vector3(Handle_scale,Handle_scale,Handle_scale);

							if(With_manip){
								control_points_children[Add_in_Segment].AddComponent(typeof(DragTransformInfiniLAND));
							}							
							control_points_children[Add_in_Segment].transform.parent = transform;

							//v1.8
							//Range_start = Add_in_Segment-2;
							//Range_end = Add_in_Segment-0;
						}
					}
				}

				for(int i=control_points_children.Count-1;i>=0;i--){

					if(control_points_children[i] ==null){								

						if(i>0){
							Overide_seg_detail.RemoveAt(i-1);
						}

						SplinePoints.RemoveAt(i);
						control_points_children.RemoveAt(i);
						repaint=true;
					}				
				}

				if(control_points_children.Count !=0){
					for (int i=0;i<SplinePoints.Count;i++){

						if(SplinePoints[i].position == control_points_children[i].transform.position)
						{
							//do nothing 
						}
						else{
							SplinePoints[i].position = control_points_children[i].transform.position;
							repaint=true;

							//v1.8
							//Range_start = i-1;
							//Range_end = i+1;
							more_than_one++;
						}
					}
				}

				//Debug.Log(Thead_finished);

				//repainted=false;
				//Debug.Log(repainted);
				float Rand = Random.Range(0.0f,0.02f);//use this to desynchronize with other splines !!!
				//if(repaint & repainted & (Time.fixedTime - timer > (0.04f-Rand))){
				//		if(repaint & (Time.fixedTime - timer > (0.04f-Rand)| !repainted) ){
				if((Desynch & repaint & (Time.fixedTime - timer > (0.04f-Rand)| !repainted)) | (!Desynch & repaint)){
					//repainted = false;
					//Debug.Log (	repainted);

					//v2.1.22
					doChildrenUpdate(-1);

					timer = Time.fixedTime;
					//Loom tmp = Loom.Current;
					//Loom.Current.GetComponent<Loom>();
					repaint = false;

                    BranchSplineNode[]  SplinePoints1 = SplinePoints.ToArray();
					int Control_points_Count = SplinePoints1.Length;
					//List<LandSplineNode> Curve2 = new List<LandSplineNode>();

					if ((Multithreaded && Thead_finished) | !Multithreaded) {
						Curve.Clear();
					}



					//Loom.RunAsync(()=>{

					if ((Multithreaded && Thead_finished) | !Multithreaded) {

						if (!Multithreaded) {

							//v2.1
							if(Keep_Curve != null){
								Keep_Curve.Clear();
							}

							//int curve_counter=0;
							for (int i=0;i<Control_points_Count;i++) {

								if (i<=Control_points_Count-2 ) {

									Draw_Curve(i, SplinePoints1, Control_points_Count,true);

									current_frame++;
									if(current_frame > Control_points_Count-2){
										current_frame=0;
									}

								}else{

								}
							}

						}else{

							Thead_finished = false;
						//	Loom.RunAsync (() => {
							LoomANG.RunAsync (() => {

								//v2.1
								if(Keep_Curve != null){
									Keep_Curve.Clear();
								}

								//int curve_counter=0;
								for (int i=0;i<Control_points_Count;i++) {

									//if (i<=Control_points_Count-2 & ((i == current_frame & spread_frames) | !spread_frames)) {
									if (i<=Control_points_Count-2 ) {
										//if ((i<=Control_points_Count-2) & (i == current_frame)) {


										//if(i >= Range_start & i<=Range_end | more_than_one > 1 | 1==1){//confine changes, if only one node moves
										//if(1==1){

										//						float detail = CurveQuality;
										//
										//							if(Overide_seg_detail[i] > 0){								
										//								detail = Overide_seg_detail[i];								
										//							}
										//													
										//						Vector3 Handler_start  = SplinePoints1[i].position;
										//						Vector3 Handler_end  = SplinePoints1[i+1].position;
										//						
										//						Vector3 Vector_along_direction=Handler_start-Handler_end;
										//						Vector3 Vector_along_direction_INV=Handler_end-Handler_start;
										//						Vector3 Vector_along_direction_normalized  = (Vector_along_direction).normalized;
										//						
										//						Vector3 Curve_starting_direction = Vector3.zero;
										//						Vector3 Curve_ending_direction  = Vector3.zero;
										//						
										//						if (i==0 | i <= Control_points_Count-3 ) {
										//							if (i==0){
										//								Curve_starting_direction  = Vector_along_direction_INV.normalized;
										//								SplinePoints1[i].direction = Curve_starting_direction;
										//							}
										//							else{
										//								Curve_starting_direction = SplinePoints1[i].direction;
										//							}
										//							Curve_ending_direction = -1.0f*((SplinePoints1[i+2].position-Handler_end).normalized - Vector_along_direction_normalized).normalized;
										//							SplinePoints1[i+1].direction = -1.0f*Curve_ending_direction;
										//							
										//						} else {
										//							Curve_starting_direction = SplinePoints1[i].direction;
										//							Curve_ending_direction = Vector_along_direction.normalized;
										//							SplinePoints1[i+1].direction = -1.0f*Curve_ending_direction;
										//						} 
										//						
										//								List<LandSplineNode> a = new List<LandSplineNode>();
										//						
										//						for (float j = 0;j<1;j+= (1/detail)) {
										//							
										//							Vector3 a1 = Handler_start;
										//							Vector3 b1 = Vector_along_direction.magnitude*Curve_starting_direction/2;
										//							Vector3 c1 = Vector_along_direction.magnitude*Curve_ending_direction/2;
										//							Vector3 d1 = Handler_end;
										//							Vector3 C1 = ( d1 - (3.0f * (c1+d1)) + (3.0f * (b1+a1)) - a1 );
										//							Vector3 C2 = ( (3.0f * (c1+d1)) - (6.0f * (b1+a1)) + (3.0f * a1) );
										//							Vector3 C3 = ( (3.0f * (b1+a1)) - (3.0f * a1) );
										//							Vector3 C4 = ( a1 );
										//							Vector3 p = C1*j*j*j + C2*j*j + C3*j + C4;
										//							
										//									LandSplineNode NEW_POINT = new LandSplineNode(Vector3.zero,Quaternion.identity);
										//							NEW_POINT.position = p;
										//							
										//							a.Add (NEW_POINT);
										//									curve_counter++;
										//						}
										//							a.Add(SplinePoints1[i+1]);
										//		//					Curve2.AddRange(a);
										//							Curve.AddRange(a);
										//						//}


										//Debug.Log(Thead_finished);
										//	if((Multithreaded && Thead_finished) | !Multithreaded){
										Draw_Curve(i, SplinePoints1, Control_points_Count,true);
										//	}
										//							Curve1 = a.ToArray();
										//							List<LandSplineNode> Curve_Temp =  new List<LandSplineNode>();
										//							Curve_Temp.AddRange(Curve1);
										//							Curve_Temp.Add(SplinePoints1[i+1]);
										//							Curve1 = Curve_Temp.ToArray();
										//							Keep_Curve.Add(Curve1);
										//
										//							Curve.AddRange(Curve1);




										current_frame++;
										if(current_frame > Control_points_Count-2){
											current_frame=0;
										}
										//break;
									}else{
										//							if (i<=Control_points_Count-2){
										//								float detail = CurveQuality;
										//								
										//								if(Overide_seg_detail[i] > 0){								
										//									detail = Overide_seg_detail[i];	
										//								}
										//								int count_me =0;
										//								for (float j = 0;j<1;j+= (1/detail)) {
										//									count_me++;
										//								}
										//								
										//								Curve2.AddRange(Curve.GetRange(curve_counter,count_me));
										//								curve_counter = curve_counter+ count_me;
										//							}
									}
								}

								Thead_finished = true;
							});
						}//END choices multithreading

					}//END check for multithreading

					current_frame++;
					if(current_frame > Control_points_Count-2){
						current_frame=0;
					}

					//				Curve = Curve2;
					//Loom.QueueOnMainThread(()=>{
					repainted=true;//Debug.Log (Loom.);
					//},2);
					//});// END THREAD
				}//END REPAINT 
			}
			#endregion


			if(parent_moving !=null){

				if(Sphere_particles != null){
					//v2.1
					ParticleSystem.EmissionModule em = Sphere_particles.emission;
					em.enabled = true;
					//Sphere_particles.enableEmission = true;
				}
				//if(Legacy_particles != null){
				//	Legacy_particles.enabled = true;
				//	Legacy_particles.emit = true;
				//}

				if(follow_curve ){

					if(!Alter_mode & !Alter_mode2){
						if((count_steps2+1) <= Curve.Count-1 )
						{
							parent_moving.transform.position =parent_moving.transform.position+(Curve[count_steps2+1].position - Curve[count_steps2].position).normalized*5*0.35f*Motion_Speed;

							if( Vector3.Distance(Curve[count_steps2+1].position,parent_moving.transform.position) < 7 ){ 
								count_steps2=count_steps2+1;
								parent_moving.transform.position = Curve[count_steps2].position;
							}

							else if(Vector3.Distance(Curve[count_steps2+1].position,parent_moving.transform.position) > (5*Vector3.Distance(Curve[count_steps2+1].position,Curve[count_steps2].position)) )
							{	count_steps2=count_steps2+1;
								parent_moving.transform.position = Curve[count_steps2].position;
							}
						}

						if(count_steps2 > Curve.Count-2 )
						{
							if(Sphere_particles != null){
								//v2.1
								ParticleSystem.EmissionModule em = Sphere_particles.emission;
								em.enabled = false;
								//Sphere_particles.enableEmission = false;
							}						
							//if(Legacy_particles != null){
							//	Legacy_particles.emit = false;
							//	Legacy_particles.enabled=false;							
							//}
							count_steps2=0;

							if (count_steps2 < Curve.Count) {
								parent_moving.transform.position = Curve [count_steps2].position;
							}
						}
					}

					if(Alter_mode & !Alter_mode2){
						if((count_steps2+1) <= Curve.Count-1 )
						{
							if( (-parent_moving.transform.position+Curve[count_steps2+1].position).magnitude >0.0001f){

								Quaternion rotation = Quaternion.LookRotation( -Curve[count_steps2+1].position + parent_moving.transform.position);
								parent_moving.transform.rotation = Quaternion.Slerp(parent_moving.transform.rotation, rotation, 50f*Motion_Speed * Time.deltaTime);
							}else{}

							parent_moving.transform.position =parent_moving.transform.position+(Curve[count_steps2+1].position - Curve[count_steps2].position).normalized*5*0.35f*Motion_Speed;

							if( Vector3.Distance(Curve[count_steps2+1].position,parent_moving.transform.position) < Node_toggle_dist ){ 
								count_steps2=count_steps2+1;
								parent_moving.transform.position = Curve[count_steps2].position;
							}

							else if(Vector3.Distance(Curve[count_steps2+1].position,parent_moving.transform.position) > (3*Vector3.Distance(Curve[count_steps2+1].position,Curve[count_steps2].position)) )
							{	count_steps2=count_steps2+1;
								if (count_steps2 < Curve.Count) {
									parent_moving.transform.position = Curve [count_steps2].position;
								}
							}						
						}
						if(count_steps2 > Curve.Count-2 )
						{
							if(Sphere_particles != null){
								//v2.1
								ParticleSystem.EmissionModule em = Sphere_particles.emission;
								em.enabled = false;
								//Sphere_particles.enableEmission = false;
							}						
							//if(Legacy_particles != null){
							//	Legacy_particles.emit = false;
							//	Legacy_particles.enabled=false;							
							//}
							count_steps2=0;
							if (count_steps2 < Curve.Count) {
								parent_moving.transform.position = Curve [count_steps2].position;
							}
						}
					}

					if(Alter_mode2 ){
						if((count_steps2+1) <= Curve.Count-1 )
						{				
							if( (Curve[count_steps2+1].position-parent_moving.transform.position).magnitude >0.0001f){

								Quaternion rotation = Quaternion.LookRotation( -Curve[count_steps2+1].position + parent_moving.transform.position);

								if(Curve[count_steps2+1].position == Curve[count_steps2].position)
								{
									if(count_steps2 <= Curve.Count-2 )
									{									
									}
								}
								parent_moving.transform.rotation = Quaternion.Slerp(parent_moving.transform.rotation, rotation, 0.5f);						
							}

							if(Accelerate){
								parent_moving.transform.position = Vector3.Lerp(parent_moving.transform.position,(Curve[count_steps2+1].position),(Curve[count_steps2+1].position - parent_moving.transform.position).magnitude*5*0.35f*Motion_Speed);

							}
							else{
								float Speed=0.5f;
								Speed= 5*0.35f*Motion_Speed;
								if( (Curve[count_steps2+1].position - parent_moving.transform.position).sqrMagnitude !=0 ){
									Speed = 5*0.35f*Motion_Speed / (Curve[count_steps2+1].position - parent_moving.transform.position).magnitude;
								}

								if(Speed<0.01f){Speed=2.51f;}

								parent_moving.transform.position = Vector3.Lerp(parent_moving.transform.position,(Curve[count_steps2+1].position),Speed);
							}

							if( Vector3.Distance(Curve[count_steps2+1].position,parent_moving.transform.position) < Node_toggle_dist ){ 

								if( (count_steps2+2) < Curve.Count )
								{
									if(Curve[count_steps2+2].position == Curve[count_steps2+1].position & 1==1)
									{
										count_steps2=count_steps2+2;

										if(count_steps2 <= Curve.Count-2 )
										{
											parent_moving.transform.position = Vector3.Slerp(parent_moving.transform.position, Curve[count_steps2].position,0.5f);
										}								
									}else{
										count_steps2=count_steps2+1;									
										parent_moving.transform.position = Vector3.Slerp(parent_moving.transform.position, Curve[count_steps2].position,0.5f);
									}
								}else{								
									count_steps2=count_steps2+1;								
									parent_moving.transform.position = Vector3.Slerp(parent_moving.transform.position, Curve[count_steps2].position,0.5f);
								}															
							}

							else if(Vector3.Distance(Curve[count_steps2+1].position,parent_moving.transform.position) > (3.1f*Vector3.Distance(Curve[count_steps2+1].position,Curve[count_steps2].position)) )
							{	
								count_steps2=count_steps2+1;
							}

						}
						if(count_steps2 > Curve.Count-2 )
						{
							if(Sphere_particles != null){
								//v2.1
								ParticleSystem.EmissionModule em = Sphere_particles.emission;
								em.enabled = false;
								//Sphere_particles.enableEmission = false;
							}

							//if(Legacy_particles != null){
							//	Legacy_particles.emit = false;
							//	Legacy_particles.enabled=false;							
							//}
							count_steps2=0;
						}
					}
				}
				else{
					parent_moving.transform.localPosition   = parent_moving.transform.localPosition+	(SplinePoints[count_steps+1].position - SplinePoints[count_steps].position).normalized*Time.fixedTime*0.25f;

					if( Vector3.Distance(SplinePoints[count_steps+1].position,parent_moving.transform.localPosition)< 5    ){
						count_steps=count_steps+1;
					}

					if(count_steps > SplinePoints.Count-2)
					{count_steps=0;}
				}
			}

			if(!show_controls_play_mode & NODES_DISABLED ==0){
				for(int i=0;i<control_points_children.Count;i++){
					MeshRenderer TEMP = control_points_children[i].GetComponent(typeof(MeshRenderer)) as MeshRenderer;
					TEMP.enabled = false;
				}
				NODES_DISABLED=1; 
			}else if(show_controls_play_mode & NODES_DISABLED ==1){
				for(int i=0;i<control_points_children.Count;i++){
					MeshRenderer TEMP = control_points_children[i].GetComponent(typeof(MeshRenderer)) as MeshRenderer;
					TEMP.enabled = true;
				}
				NODES_DISABLED=0;
			}

          


        }


		public int NODES_DISABLED;

		[SerializeField]
		public Vector3 Start_Point;
		[SerializeField()]
		public List<BranchSplineNode> SplinePoints = new List<BranchSplineNode>();


		[SerializeField()]
		public List<int> Overide_seg_detail  = new List<int>();

		private List<int> Keep_Overide_seg_detail;

		[SerializeField]
		public bool PointGizmo_On  = true;
		[SerializeField]
		public bool extended=false;
		[Range(1,100)][SerializeField]
		public int CurveQuality  = 20;
		[SerializeField]
		public bool rotate_mode = false; 

		private int keep_quality;

	}

	#region Node
	[System.Serializable]
	public class BranchSplineNode {

		public BranchSplineNode( Vector3 pos, Quaternion rot) {
			this.position = pos;
			this.rotation = rot;
		}
		public Vector3 position   = Vector3.zero;
		public Quaternion rotation   = Quaternion.identity;
		public Vector3 direction;

		//v2.1
		public Vector3 directionB;
		public Vector3 directionC;
		public int Type;
	}
	#endregion

}
