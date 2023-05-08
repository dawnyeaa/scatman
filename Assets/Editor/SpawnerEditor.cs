using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(SpawnerGameObject))]
public class SpawnerEditor : Editor {
  private Vector3 storedCorner1, storedCorner2;
  public Vector3[] storedCornerNeighbors = new Vector3[8];
  private float[] heights = new float[4];
  
  [Flags]
  private enum Corner {
    None = 0,
    SW = 1,
    SE = 2,
    NE = 4,
    NW = 8
  }

  float neighborOffsetDistance = 15;
  float lineThickness = 2f;
  float handleSize = 0.035f;

  public override void OnInspectorGUI() {
    base.OnInspectorGUI();
    SpawnerGameObject spawner = (SpawnerGameObject)target;

    if (GUILayout.Button("do it")) {
      spawner.Spawn();
    }

    if (GUILayout.Button("cleanup")) {
      spawner.ResetGenerated();
    }
  }

  // public void OnDrawGizmos() {
  //   Handles.color = new Color(1f, 1f, 1f, 0.2f);
  //   Handles.DrawCube(position, new Vector3(corner2.x-corner1.x, 10000, corner2.z-corner1.z));
  // }

  [DrawGizmo(GizmoType.InSelectionHierarchy)]
  static void DrawGizmos(SpawnerGameObject spawner, GizmoType gizmotype) {
    Gizmos.color = new Color(0.8f, 0f, 0.2f, 0.2f);
    Vector3 corner1 = new Vector3(spawner.corner1.x, 0, spawner.corner1.y);
    Vector3 corner2 = new Vector3(spawner.corner2.x, 0, spawner.corner2.y);
    Vector3 position = (corner1+corner2)/2f;
    Gizmos.DrawCube(new Vector3(position.x, 0, corner2.z), new Vector3(corner2.x-corner1.x, 10000, 0));
    Gizmos.DrawCube(new Vector3(corner1.x, 0, position.z), new Vector3(0, 10000, corner2.z-corner1.z));
    Gizmos.DrawCube(new Vector3(position.x, 0, corner1.z), new Vector3(corner2.x-corner1.x, 10000, 0));
    Gizmos.DrawCube(new Vector3(corner2.x, 0, position.z), new Vector3(0, 10000, corner2.z-corner1.z));
  }

  public void OnSceneGUI() {
    SpawnerGameObject spawner = (SpawnerGameObject)target;

    Handles.color = new Color(0.8f, 0f, 0.2f, 1f);
    Vector3 corner1 = new Vector3(spawner.corner1.x, 0, spawner.corner1.y);
    Vector3 corner2 = new Vector3(spawner.corner2.x, 0, spawner.corner2.y);
    Vector3 boxCenter = (corner1+corner2)/2f;
    //Handles.DrawWireCube(position, new Vector3(corner2.x-corner1.x, 10000, corner2.z-corner1.z));

    //spawner.raycastTerrainPoint();

    Handles hndle = new Handles();

    if (!storedCorner1.Equals(corner1) || !storedCorner2.Equals(corner2)) {
      Corner Wflag = (storedCorner1.x != corner1.x)? Corner.NW | Corner.SW : Corner.None;
      Corner Sflag = (storedCorner1.z != corner1.z)? Corner.SW | Corner.SE : Corner.None;
      Corner Eflag = (storedCorner2.x != corner2.x)? Corner.SE | Corner.NE : Corner.None;
      Corner Nflag = (storedCorner2.z != corner2.z)? Corner.NE | Corner.NW : Corner.None;
      storedCorner1 = corner1;
      storedCorner2 = corner2;
      updateCornerNeighbors(Wflag | Sflag | Eflag | Nflag);
    }
    
    drawCornerNeighbors();

    /*
      for handle drawing location, plane intersection with line from center of area
      define plane with view vector at height, cross product with left or right from view
    */
    Camera currentCam = hndle.currentCamera;
    Vector3 heightPointWS = currentCam.ViewportToWorldPoint(new Vector3(0.5f, 0.6f, 1));
    Vector3 heightPointFromCam = heightPointWS - currentCam.transform.position;
    Vector3 planeNormal = Vector3.Cross(heightPointFromCam, currentCam.transform.right).normalized;

    float handleHeight = 0;
    float planeDotLine = Vector3.Dot(Vector3.up, planeNormal);
    if (planeDotLine != 0) {
      handleHeight = Vector3.Dot((heightPointWS-boxCenter), planeNormal)/planeDotLine;
    }

    Vector3 handleWpos = new Vector3(corner1.x, handleHeight, boxCenter.z);
    spawner.corner1.x = Handles.FreeMoveHandle(handleWpos, Quaternion.identity, HandleUtility.GetHandleSize(handleWpos)*handleSize, Vector3.zero, Handles.DotHandleCap).x;
    Vector3 handleSpos = new Vector3(boxCenter.x, handleHeight, corner1.z);
    spawner.corner1.y = Handles.FreeMoveHandle(handleSpos, Quaternion.identity, HandleUtility.GetHandleSize(handleSpos)*handleSize, Vector3.zero, Handles.DotHandleCap).z;
    Vector3 handleEpos = new Vector3(corner2.x, handleHeight, boxCenter.z);
    spawner.corner2.x = Handles.FreeMoveHandle(handleEpos, Quaternion.identity, HandleUtility.GetHandleSize(handleEpos)*handleSize, Vector3.zero, Handles.DotHandleCap).x;
    Vector3 handleNpos = new Vector3(boxCenter.x, handleHeight, corner2.z);
    spawner.corner2.y = Handles.FreeMoveHandle(handleNpos, Quaternion.identity, HandleUtility.GetHandleSize(handleNpos)*handleSize, Vector3.zero, Handles.DotHandleCap).z;
    //Debug.Log(hndle.currentCamera);

    // EditorGUI.BeginChangeCheck();
    // float testRadius = Handles.RadiusHandle(Quaternion.identity, spawner.transform.position, spawner.value);
    // if (EditorGUI.EndChangeCheck()) {
    //   Undo.RecordObject(spawner, "changed shit");
    //   spawner.value = testRadius;
    // }
  }

  private void updateCornerNeighbors(Corner updatedCorners) {
    SpawnerGameObject spawner = (SpawnerGameObject)target;

    Vector3 offsetX = new Vector3(neighborOffsetDistance, 0, 0);
    Vector3 offsetZ = new Vector3(0, 0, neighborOffsetDistance);
    Vector3 cornerSW = new Vector3(spawner.corner1.x, 500000, spawner.corner1.y);
    Vector3 cornerSE = new Vector3(spawner.corner2.x, 500000, spawner.corner1.y);
    Vector3 cornerNE = new Vector3(spawner.corner2.x, 500000, spawner.corner2.y);
    Vector3 cornerNW = new Vector3(spawner.corner1.x, 500000, spawner.corner2.y);

    if ((updatedCorners & Corner.SW) != 0) {
      heights[0] = spawner.raycastTerrainPoint(cornerSW).y;
      // storedCornerNeighbors[0] = spawner.raycastTerrainPoint(cornerSW + offsetZ);
      // storedCornerNeighbors[1] = spawner.raycastTerrainPoint(cornerSW + offsetX);
      storedCornerNeighbors[0] = cornerSW + offsetZ;
      storedCornerNeighbors[0].y = heights[0];
      storedCornerNeighbors[1] = cornerSW + offsetX;
      storedCornerNeighbors[1].y = heights[0];
    }
    if ((updatedCorners & Corner.SE) != 0) {
      heights[1] = spawner.raycastTerrainPoint(cornerSE).y;
      // storedCornerNeighbors[2] = spawner.raycastTerrainPoint(cornerSE - offsetX);
      // storedCornerNeighbors[3] = spawner.raycastTerrainPoint(cornerSE + offsetZ);
      storedCornerNeighbors[2] = cornerSE - offsetX;
      storedCornerNeighbors[2].y = heights[1];
      storedCornerNeighbors[3] = cornerSE + offsetZ;
      storedCornerNeighbors[3].y = heights[1];
    }
    if ((updatedCorners & Corner.NE) != 0) {
      heights[2] = spawner.raycastTerrainPoint(cornerNE).y;
      // storedCornerNeighbors[4] = spawner.raycastTerrainPoint(cornerNE - offsetZ);
      // storedCornerNeighbors[5] = spawner.raycastTerrainPoint(cornerNE - offsetX);
      storedCornerNeighbors[4] = cornerNE - offsetZ;
      storedCornerNeighbors[4].y = heights[2];
      storedCornerNeighbors[5] = cornerNE - offsetX;
      storedCornerNeighbors[5].y = heights[2];
    }
    if ((updatedCorners & Corner.NW) != 0) {
      heights[3] = spawner.raycastTerrainPoint(cornerNW).y;
      // storedCornerNeighbors[6] = spawner.raycastTerrainPoint(cornerNW + offsetX);
      // storedCornerNeighbors[7] = spawner.raycastTerrainPoint(cornerNW - offsetZ);
      storedCornerNeighbors[6] = cornerNW + offsetX;
      storedCornerNeighbors[6].y = heights[3];
      storedCornerNeighbors[7] = cornerNW - offsetZ;
      storedCornerNeighbors[7].y = heights[3];
    }

  }

  private void drawCornerNeighbors() {
    Vector3 cornerSW = new Vector3(storedCorner1.x, heights[0], storedCorner1.z);
    Vector3 cornerSE = new Vector3(storedCorner2.x, heights[1], storedCorner1.z);
    Vector3 cornerNE = new Vector3(storedCorner2.x, heights[2], storedCorner2.z);
    Vector3 cornerNW = new Vector3(storedCorner1.x, heights[3], storedCorner2.z);

    Handles.DrawLine(cornerSW, storedCornerNeighbors[0], lineThickness);
    Handles.DrawLine(cornerSW, storedCornerNeighbors[1], lineThickness);
    Handles.DrawLine(cornerSE, storedCornerNeighbors[2], lineThickness);
    Handles.DrawLine(cornerSE, storedCornerNeighbors[3], lineThickness);
    Handles.DrawLine(cornerNE, storedCornerNeighbors[4], lineThickness);
    Handles.DrawLine(cornerNE, storedCornerNeighbors[5], lineThickness);
    Handles.DrawLine(cornerNW, storedCornerNeighbors[6], lineThickness);
    Handles.DrawLine(cornerNW, storedCornerNeighbors[7], lineThickness);

    Handles.DrawLine(cornerSW, cornerSW + (Vector3.up * 10000), lineThickness);
    Handles.DrawLine(cornerSE, cornerSE + (Vector3.up * 10000), lineThickness);
    Handles.DrawLine(cornerNE, cornerNE + (Vector3.up * 10000), lineThickness);
    Handles.DrawLine(cornerNW, cornerNW + (Vector3.up * 10000), lineThickness);
  }
}