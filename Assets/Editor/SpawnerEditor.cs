using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SpawnerGameObject))]
public class SpawnerEditor : Editor {
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

  public void OnSceneGUI() {
    SpawnerGameObject spawner = (SpawnerGameObject)target;

    Handles.color = Color.white;
    Vector3 corner1 = new Vector3(spawner.corner1.x, 0, spawner.corner1.y);
    Vector3 corner2 = new Vector3(spawner.corner2.x, 0, spawner.corner2.y);
    Vector3 position = (corner1+corner2)/2f;
    Handles.DrawWireCube(position, new Vector3(corner2.x-corner1.x, 10000, corner2.z-corner1.z));

    Handles hndle = new Handles();

    //Debug.Log(hndle.currentCamera);

    EditorGUI.BeginChangeCheck();
    float testRadius = Handles.RadiusHandle(Quaternion.identity, spawner.transform.position, spawner.value);
    if (EditorGUI.EndChangeCheck()) {
      Undo.RecordObject(spawner, "changed shit");
      spawner.value = testRadius;
    }
  }
}