using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SpawnerGameObject))]
public class SpawnerEditor : Editor {
  private Vector3 storedCorner1, storedCorner2;
  public Vector3[] storedCornerNeighbors = new Vector3[8];

  float neighborOffsetDistance = 5;

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
    Vector3 position = (corner1+corner2)/2f;
    Handles.DrawWireCube(position, new Vector3(corner2.x-corner1.x, 10000, corner2.z-corner1.z));

    //spawner.raycastTerrain();

    Handles hndle = new Handles();

    if (!storedCorner1.Equals(corner1) || !storedCorner2.Equals(corner2)) {
      updateCornerNeighbors();
      storedCorner1 = corner1;
      storedCorner2 = corner2;
    }

    //Debug.Log(hndle.currentCamera);

    // EditorGUI.BeginChangeCheck();
    // float testRadius = Handles.RadiusHandle(Quaternion.identity, spawner.transform.position, spawner.value);
    // if (EditorGUI.EndChangeCheck()) {
    //   Undo.RecordObject(spawner, "changed shit");
    //   spawner.value = testRadius;
    // }
  }

  private void updateCornerNeighbors() {
    SpawnerGameObject spawner = (SpawnerGameObject)target;

    Vector3 offsetX = new Vector3(neighborOffsetDistance, 0, 0);
    Vector3 offsetZ = new Vector3(0, 0, neighborOffsetDistance);
    Vector3 cornerSW = new Vector3(spawner.corner1.x, 500000, spawner.corner1.y);
    Vector3 cornerSE = new Vector3(spawner.corner2.x, 500000, spawner.corner1.y);
    Vector3 cornerNE = new Vector3(spawner.corner2.x, 500000, spawner.corner2.y);
    Vector3 cornerNW = new Vector3(spawner.corner1.x, 500000, spawner.corner2.y);

    storedCornerNeighbors[0] = spawner.raycastTerrain(cornerSW + offsetZ);
    storedCornerNeighbors[1] = spawner.raycastTerrain(cornerSW + offsetX);
    storedCornerNeighbors[2] = spawner.raycastTerrain(cornerSE - offsetX);
    storedCornerNeighbors[3] = spawner.raycastTerrain(cornerSE + offsetZ);
    storedCornerNeighbors[4] = spawner.raycastTerrain(cornerNE - offsetZ);
    storedCornerNeighbors[5] = spawner.raycastTerrain(cornerNE - offsetX);
    storedCornerNeighbors[6] = spawner.raycastTerrain(cornerNW + offsetX);
    storedCornerNeighbors[7] = spawner.raycastTerrain(cornerNW - offsetZ);

  }
}