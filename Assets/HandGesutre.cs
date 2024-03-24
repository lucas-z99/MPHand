using Mediapipe;
using Mediapipe.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Stopwatch = System.Diagnostics.Stopwatch;
using Google.Protobuf.Collections;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;


public class HandGesutre : MonoBehaviour
{

     // setting
     [SerializeField] TextAsset graphConfigTxt;
     [SerializeField] RawImage displayer;
     public bool printCoords;
     [Header("WebCam")]
     [SerializeField] int width = 1280; // requested size, may be adjust by webcam
     [SerializeField] int height = 720;
     [SerializeField] int fps = 30;


     // private
     WebCamTexture camTexture;
     CalculatorGraph graph;
     ResourceManager resourceMgr;

     Texture2D inputTexture;
     Color32[] inputPixel;

     OutputStream<List<NormalizedLandmarkList>> outputLandmarkStream;
     List<NormalizedLandmarkList> handsLandmarks;


     IEnumerator Start()
     {

          Admin.GoToSceneView();


          // webcam
          if (WebCamTexture.devices.Length == 0)
               throw new System.Exception("WebCam missing!");


          var device = WebCamTexture.devices[0];

          camTexture = new WebCamTexture(device.name, width, height, fps);
          camTexture.Play();

          yield return new WaitUntil(() => camTexture.width > 16);

          width = camTexture.width; // adjust to actual size
          height = camTexture.height;


          // texture
          inputTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
          inputPixel = new Color32[width * height];

          displayer.rectTransform.sizeDelta = new Vector2(width, height);
          displayer.texture = camTexture;


          // model
          resourceMgr = new LocalResourceManager();
          yield return resourceMgr.PrepareAssetAsync("hand_landmark_full.bytes");
          //yield return resourceMgr.PrepareAssetAsync("hand_landmark_lite.bytes");
          //yield return resourceMgr.PrepareAssetAsync("hand_landmarker.bytes");

          //yield return resourceMgr.PrepareAssetAsync("palm_detection_full.bytes");
          //yield return resourceMgr.PrepareAssetAsync("palm_detection_lite.bytes");

          //yield return resourceMgr.PrepareAssetAsync("hand_recrop.bytes");


          // graph
          graph = new CalculatorGraph(graphConfigTxt.text);
          var stopwatch = new Stopwatch(); // timestamp

          //
          outputLandmarkStream = new OutputStream<List<NormalizedLandmarkList>>(graph, "hand_landmarks");
          outputLandmarkStream.StartPolling();
          //

          graph.StartRun();
          stopwatch.Start();


          // go
          while (true)
          {
               // input
               inputTexture.SetPixels32(camTexture.GetPixels32(inputPixel));

               var imageFrame = new ImageFrame(
                    ImageFormat.Types.Format.Srgba,
                    width,
                    height,
                    width * 4,
                    inputTexture.GetRawTextureData<byte>());

               var timestamp = stopwatch.ElapsedTicks / (TimeSpan.TicksPerMillisecond / 1000);

               graph.AddPacketToInputStream("input_video", Packet.CreateImageFrameAt(imageFrame, timestamp));


               //   ----------------------------------------------------------------
               var task = outputLandmarkStream.WaitNextAsync();

               yield return new WaitUntil(() => (task.IsCompleted));
               if (!task.Result.ok)
                    throw new Exception("???");


               // output
               var packet = task.Result.packet;
               if (packet != null)
               {
                    handsLandmarks = packet.Get(NormalizedLandmarkList.Parser);


                    // "the hand"
                    physicalHand.Clear();
                    foreach (var fun in handsLandmarks[0].Landmark)
                    {
                         var pos = ToVector3(fun);
                         physicalHand.Add(pos);
                    }
                    TheHand.inst.SetPos(physicalHand);


                    if (printCoords)
                         foreach (var h in handsLandmarks)
                              Debug.Log(h);
               }

          }
     }


     //  -------------------------------------------------------------------

     [Header("Gizmos")]
     public float gizmosSize = 2;
     [Range(-1, 21)]
     public int debugIndex = -1;
     int _landmark_count = 21;
     List<Vector3> physicalHand = new List<Vector3>();


     void OnDrawGizmos()
     {
          if (handsLandmarks == null)
               return;

          Gizmos.color = UnityEngine.Color.cyan;


          foreach (var hand in handsLandmarks)
          {
               int index = 0;
               var lastPos = new Vector3();
               foreach (var l in hand.Landmark)
               {
                    // dots
                    var pos = ToVector3(l);
                    Gizmos.DrawWireSphere(pos, gizmosSize);

                    // fingers
                    if (index != 0 && index != 5 && index != 9 && index != 13 && index != 17)
                         Gizmos.DrawLine(pos, lastPos);


                    //
                    if (index == debugIndex)
                         Gizmos.DrawWireSphere(pos, gizmosSize * 4);


                    lastPos = pos;
                    index++;
               }


               // palm triangle
               var a = ToVector3(hand.Landmark[0]);
               var b1 = ToVector3(hand.Landmark[5]);
               var b2 = ToVector3(hand.Landmark[9]);
               var b3 = ToVector3(hand.Landmark[13]);
               var c = ToVector3(hand.Landmark[17]);
               Gizmos.DrawLine(a, b1);
               Gizmos.DrawLine(b1, b2);
               Gizmos.DrawLine(b2, b3);
               Gizmos.DrawLine(b3, c);
               Gizmos.DrawLine(c, a);

          }
     }


     Vector3 ToVector3(NormalizedLandmark l)
     {
          return new Vector3(l.X * width, (1 - l.Y) * height, l.Z * width);
     }



     void OnDestroy()
     {

          if (camTexture != null)
               camTexture.Stop();

          outputLandmarkStream?.Dispose();
          outputLandmarkStream = null;

          if (graph != null)
          {
               try
               {
                    graph.CloseInputStream("input_video");
                    graph.WaitUntilDone();
               }
               finally
               {
                    graph.Dispose();
                    graph = null;
               }
          }

     }


}
