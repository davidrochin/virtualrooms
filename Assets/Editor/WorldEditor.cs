﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Util;

[CustomEditor(typeof(World))]
public class WorldEditor : Editor {

    public static RoomEditMode editMode;
    public static bool editModeEnabled;
    public static int floorLevel = 0;

    //Edit Plane
    public static Plane editPlane;

    //Mouse position in plane and grid-locked mouse positions
    Vector3 mouseWorldPos;
    Vector3 mouseLocalPos;
    Vector3 mouseFloorGridPos;
    Vector3 mouseWallGridPos;

    //Walls and Floor managers (do not use directly)
    Walls walls;
    Floor floor;

    //Wall points for drawing walls
    List<Vector3> wallEditPoints = new List<Vector3>();

    //Materials selected
    int floorMaterialId;

    public override void OnInspectorGUI() {

        //Draw default inspector
        base.OnInspectorGUI();

        GUILayout.Label("Editor", EditorStyles.boldLabel);

        //Edit mode selector
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Toggle(editMode == RoomEditMode.None, "None", EditorStyles.miniButtonLeft)) editMode = RoomEditMode.None;
        if (GUILayout.Toggle(editMode == RoomEditMode.Floor, "Floor", EditorStyles.miniButtonMid)) editMode = RoomEditMode.Floor;
        if (GUILayout.Toggle(editMode == RoomEditMode.Walls, "Walls", EditorStyles.miniButtonMid)) editMode = RoomEditMode.Walls;
        if (GUILayout.Toggle(editMode == RoomEditMode.Furniture, "Furniture", EditorStyles.miniButtonRight)) editMode = RoomEditMode.Furniture;
        EditorGUILayout.EndHorizontal();

        //Floor selector
        if(editMode != RoomEditMode.None) {

            //Floor level selector
            EditorGUILayout.BeginHorizontal();
            //EditorGUILayout.PrefixLabel("Floor Level");
            //floorLevel = EditorGUILayout.IntSlider(floorLevel, 1, 3);
            editPlane = new Plane(Vector3.up, Vector3.zero + Vector3.up * (floorLevel));
            EditorGUILayout.EndHorizontal();

            //Floor material selector
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Floor Material");
            //floorMaterial = EditorGUILayout.ObjectField(floorMaterial, typeof(Material), false) as Material;
            floorMaterialId = EditorGUILayout.IntField(floorMaterialId);
            EditorGUILayout.EndHorizontal();
            
        }

        //Debug buttons
        GUILayout.Label("Debug", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Rebuild all")) {
            ((World)target).RebuildFloor();
            ((World)target).RebuildWalls();
            SetSceneDirty();
        }
        if (GUILayout.Button("Load Test Room")) {
            ((World)target).LoadTestRoom();
            SetSceneDirty();
        }
        GUILayout.EndHorizontal();
    }

    private void OnSceneGUI() {

        //Obtain the World that this Editor is editing
        World world = ((World)target);

        //Calculate mouse position on imaginary plane
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        float rayHitDistance = 0f; bool onPlane = editPlane.Raycast(ray, out rayHitDistance);
        mouseWorldPos = ray.origin + ray.direction * rayHitDistance;
        mouseLocalPos = mouseWorldPos - ((World)target).transform.position;

        //Calculate mouse position locked to mouse and wall grids
        mouseFloorGridPos = new Vector3(Math.RoundDown(mouseLocalPos.x) + 0.5f, mouseLocalPos.y, Math.RoundDown(mouseLocalPos.z) + 0.5f);
        mouseWallGridPos = new Vector3(Math.RoundNearest(mouseWorldPos.x), mouseWorldPos.y, Math.RoundNearest(mouseWorldPos.z));

        //Draw all previously calculated positions
        //Handles.BeginGUI();
        //Handles.color = Color.red; Handles.DrawSolidDisc(mouseEditPos, Vector3.up, 0.1f);
        //Handles.color = Color.cyan; Handles.DrawSolidDisc(mouseFloorGridPos, Vector3.up, 0.1f);
        //Handles.color = Color.green; Handles.DrawSolidDisc(mouseWallGridPos, Vector3.up, 0.1f);
        //Handles.EndGUI();

        //Prevent lose of focus on GameObject if we are in any edit mode
        if(editMode != RoomEditMode.None) {
            Selection.activeObject = target;
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }

        //When in Floor Edit Mode
        if (editMode == RoomEditMode.Floor) {
            Handles.DrawWireCube(mouseFloorGridPos + ((World)target).transform.position, new Vector3(1f, 0f, 1f));

            //If mouse clicked
            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseDrag) {

                //Left click
                if (currentEvent.button == 0) {

                    //If pressing CTRL erase floor. Else, add floor
                    if(currentEvent.control) {
                        world.floorGrid[Math.RoundDown(mouseFloorGridPos.x), Math.RoundDown(mouseFloorGridPos.z)] = new FloorInfo(false, 0, 0);
                        world.RebuildFloor();
                        SetSceneDirty();
                    } else {
                        world.floorGrid[Math.RoundDown(mouseFloorGridPos.x), Math.RoundDown(mouseFloorGridPos.z)] = new FloorInfo(true, 0, floorMaterialId);
                        world.RebuildFloor();
                        SetSceneDirty();
                    }  
                }    
            }
        }

        //When in Walls Edit Mode
        if (editMode == RoomEditMode.Walls) {

            //Draw the marker
            Handles.DrawLine(mouseWallGridPos, mouseWallGridPos + Vector3.up * 3f);

            //If mouse clicked or dragged
            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseDrag) {
                if (currentEvent.button == 0 && wallEditPoints.Exists(a => a == mouseWallGridPos) == false) {
                    wallEditPoints.Add(mouseWallGridPos);
                }
            }

            //If mouse released
            if(currentEvent.button == 0 && currentEvent.type == EventType.MouseUp) {

                //Check that there is at least two wallEditPoints
                if(wallEditPoints.Count >= 2) {

                    //Add walls from wallEditPoints
                    for (int i = 0; i < wallEditPoints.Count - 1; i++) {

                        //Add a wall between this point and the next
                        SetSceneDirty();

                    }
                }

                //Clear point list and rebuild
                wallEditPoints.Clear();
            }

            //Draw wallEditPoints if any
            foreach (Vector3 editPoint in wallEditPoints) {
                Handles.DrawSolidDisc(editPoint, Vector3.up, 0.05f);
            }
        }

    }

    public static bool SetSceneDirty() {
        if (!EditorApplication.isPlaying) {
            return UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        } else {
            return false;
        }  
    }

    public enum RoomEditMode { None, Floor, Walls, Furniture }
}
