using Mediapipe;
using Mediapipe.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Stopwatch = System.Diagnostics.Stopwatch;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;


public class FaceMeshGizmos : MonoBehaviour
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
     RepeatedField<NormalizedLandmark> landmarks;


     IEnumerator Start()
     {

          Admin.GoToSceneView();


          // set up webcam
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
          yield return resourceMgr.PrepareAssetAsync("face_detection_short_range.bytes");
          yield return resourceMgr.PrepareAssetAsync("face_landmark_with_attention.bytes");


          // graph
          graph = new CalculatorGraph(graphConfigTxt.text);
          var stopwatch = new Stopwatch(); // timestamp

          //
          outputLandmarkStream = new OutputStream<List<NormalizedLandmarkList>>(graph, "multi_face_landmarks");
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

               graph.AddPacketToInputStream("input", Packet.CreateImageFrameAt(imageFrame, timestamp));


               //  -------------------------------------------------------------------
               var task = outputLandmarkStream.WaitNextAsync();

               yield return new WaitUntil(() => (task.IsCompleted));
               if (!task.Result.ok)
                    throw new Exception("???");


               // output
               var packet = task.Result.packet;
               if (packet != null)
               {
                    var _list = packet.Get(NormalizedLandmarkList.Parser);
                    landmarks = _list[0].Landmark; // there is only 1 element

                    if (printCoords)
                         Debug.Log(landmarks);
               }

          }
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
                    graph.CloseInputStream("input");
                    graph.WaitUntilDone();
               }
               finally
               {
                    graph.Dispose();
                    graph = null;
               }
          }

     }


     //  -------------------------------------------------------------------

     [Header("Gizmos")]
     public float gizmosSize = 2;
     [Range(-1, 478)]
     public int heightLightDot = -1;
     int _eyes = 468;
     int _halfFace = 250;


     void OnDrawGizmos()
     {
          if (landmarks == null)
               return;

          int j = 0;
          foreach (var l in landmarks)
          {

               if (j <= heightLightDot)
                    Gizmos.color = new UnityEngine.Color(0, 0, 0, 0.2f);
               else if (j >= _eyes)
                    Gizmos.color = UnityEngine.Color.magenta;
               else
                    Gizmos.color = UnityEngine.Color.cyan;

               var pos = new Vector3(l.X * width, (1 - l.Y) * height, l.Z * width);
               Gizmos.DrawWireSphere(pos, gizmosSize);


               if (j == heightLightDot)
               {
                    Gizmos.color = UnityEngine.Color.green;
                    Gizmos.DrawWireSphere(pos, gizmosSize * 4);
               }

               j++;
          }

     }




}
