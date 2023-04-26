using UnityEditor;
using UnityEngine;

public static class Test {
 
    [MenuItem("GameObject/MyMenu/Do Something", false, 0)]
    static void Init() {
        Debug.Log("here");
    }
}