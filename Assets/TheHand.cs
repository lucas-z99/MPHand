using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TheHand : MonoBehaviour
{

     public static TheHand inst;
     List<Vector3> posDesired;
     public float speed = 20f;


     void Awake()
     {
          inst = this;
     }


     void FixedUpdate()
     {
          if (posDesired == null)
               return;

          Rigidbody rb = null;



          for (int i = 0; i < posDesired.Count; i++)
          {
               var child = transform.GetChild(i);
               var pos = Vector3.Lerp(child.position, posDesired[i], speed * Time.fixedDeltaTime);

               child.GetComponent<Rigidbody>().MovePosition(pos);



               //child.position = Vector3.Lerp(child.position, posDesired[i], speed * Time.fixedDeltaTime);

          }
     }


     public void SetPos(List<Vector3> posList)
     {
          posDesired = posList;
     }



}
