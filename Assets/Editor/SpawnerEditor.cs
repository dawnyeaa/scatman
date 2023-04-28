using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SpawnerGameObject))]
public class SpawnerEditor : Editor {
  public override void OnInspectorGUI() {
    SpawnerGameObject spawner = (SpawnerGameObject)target;
    if (GUILayout.Button("do it")) {
      spawner.Spawn();
    }

    if (GUILayout.Button("cleanup")) {
      spawner.ResetGenerated();
    }
  }
}