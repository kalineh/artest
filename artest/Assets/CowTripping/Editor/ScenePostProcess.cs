using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScenePostProcess
    : MonoBehaviour
{
    [UnityEditor.Callbacks.PostProcessScene]
    public static void OnPostProcessScene()
    {
        var platformAndroid = GameObject.FindGameObjectsWithTag("PlatformAndroid");
        var platformWindows = GameObject.FindGameObjectsWithTag("PlatformWindows");

        var enableAndroid = false;
        var enableWindows = false;

#if UNITY_ANDROID
        enableAndroid = false;
#endif

#if UNITY_EDITOR
        enableWindows = true;
#endif

        foreach (var obj in platformAndroid)
            obj.SetActive(enableAndroid);
        foreach (var obj in platformWindows)
            obj.SetActive(enableWindows);
    }
}
