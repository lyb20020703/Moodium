using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Artngame.TreeGEN.ProceduralIvy
{
    [CustomEditor(typeof(IvyObjectsManager))]
    public class IvyObjectsManagerEditor : Editor
    {

        //v0.2
        SerializedProperty sourceGenerators;
        SerializedProperty runTimeIvyGenerators;
        SerializedProperty maxIvysPerHolder;
        SerializedProperty addRunTimeColliders;
        SerializedProperty gridTiles;
        SerializedProperty coveredLandPerTile;
        SerializedProperty useSingleBranchMesh;

        //v0.1
        SerializedProperty ivyGenerators;
        public void OnEnable()
        {
            ivyGenerators = serializedObject.FindProperty("ivyGenerators");

            sourceGenerators = serializedObject.FindProperty("sourceGenerators");
            runTimeIvyGenerators = serializedObject.FindProperty("runTimeIvyGenerators");
            maxIvysPerHolder = serializedObject.FindProperty("maxIvysPerHolder");
            addRunTimeColliders = serializedObject.FindProperty("addRunTimeColliders");
            gridTiles = serializedObject.FindProperty("gridTiles");
            coveredLandPerTile = serializedObject.FindProperty("coveredLandPerTile");

            useSingleBranchMesh = serializedObject.FindProperty("useSingleBranchMesh");
        }

        void OnSceneGUI()
        {

        }
        

        private IvyObjectsManager script;
        void Awake()
        {
            script = (IvyObjectsManager)target; //script.GrassPrefabs
        }

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Lauch Ivy Painter"))
            {
                IvyPainterWindow.LauchVertexPainter();
            }

            if (GUILayout.Button("Create Editor Ivy Generators Grid"))
            {
                script.createGridEditorIvys(false);
            }
            if (GUILayout.Button("Create Run Time Ivy Generators Grid"))
            {
                script.createGridEditorIvys(true);
            }

            if (GUILayout.Button("Ungrow Ivy"))
            {
                script.ungrowIvy();
                EditorUtility.SetDirty(script);
            }

            if (GUILayout.Button("Regrow Ivy"))
            {
                script.regrowIvy();
                EditorUtility.SetDirty(script);
            }

            serializedObject.Update();

            float GapLeft = 15;
          


            //v0.2
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(GapLeft);
            EditorGUILayout.PropertyField(sourceGenerators, true);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(GapLeft);
            EditorGUILayout.PropertyField(ivyGenerators, true);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(GapLeft);
            EditorGUILayout.PropertyField(runTimeIvyGenerators, true);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(GapLeft);
            EditorGUILayout.PropertyField(maxIvysPerHolder, true);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(GapLeft);
            EditorGUILayout.PropertyField(addRunTimeColliders, true);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(GapLeft);
            EditorGUILayout.PropertyField(gridTiles, true);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(GapLeft);
            EditorGUILayout.PropertyField(coveredLandPerTile, true);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(GapLeft);
            EditorGUILayout.PropertyField(useSingleBranchMesh, true);
            EditorGUILayout.EndHorizontal(); 

            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();

        }
    }
}