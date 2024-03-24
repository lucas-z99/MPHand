using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class ForceSceneView
{
     static ForceSceneView()
     {
          Admin.onForceSceneView += GoToSceneView;
     }


     public static void GoToSceneView()
     {
          var sceneView = SceneView.sceneViews[0] as SceneView;

          if (sceneView != null)
               sceneView.Focus();
     }


}
