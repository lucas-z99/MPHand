using UnityEngine;
using TMPro;
using System.Collections.Generic;
using static Mediapipe.VideoPreStreamCalculatorOptions.Types;

public class FPSCounter : MonoBehaviour
{

     [SerializeField] TextMeshProUGUI ui;


     void Update()
     {
          //
          var dots = "";
          int dotCount = (int)((Time.unscaledTime * 30) % 30);

          for (int i = 0; i < dotCount; i++)
               dots += ".";

          //
          ui.text = dots;

     }


}
