using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Admin : MonoBehaviour
{

     public static Action onForceSceneView;


     public static void GoToSceneView()
     {
          onForceSceneView?.Invoke();
     }



}
