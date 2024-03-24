using Mediapipe;
using Mediapipe.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Stopwatch = System.Diagnostics.Stopwatch;


public class FaceMesh2 : MonoBehaviour
{

     // setting
     [SerializeField] TextAsset graphConfigTxt;
     [SerializeField] RawImage displayer;
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
     Texture2D outputTexture;
     Color32[] outputPixel;

     OutputStream<ImageFrame> outputVideoStream;


     IEnumerator Start()
     {
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
          outputTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
          outputPixel = new Color32[width * height];

          displayer.rectTransform.sizeDelta = new Vector2(width, height);
          displayer.texture = outputTexture;


          // model
          resourceMgr = new LocalResourceManager();
          yield return resourceMgr.PrepareAssetAsync("face_detection_short_range.bytes");
          yield return resourceMgr.PrepareAssetAsync("face_landmark_with_attention.bytes");


          // graph
          graph = new CalculatorGraph(graphConfigTxt.text);
          var stopwatch = new Stopwatch(); // timestamp

          //
          outputVideoStream = new OutputStream<ImageFrame>(graph, "output");
          outputVideoStream.StartPolling();
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



               //   ----------------------------------------------------------------
               var task = outputVideoStream.WaitNextAsync();

               yield return new WaitUntil(() => task.IsCompleted);
               if (!task.Result.ok)
                    throw new Exception("???");



               // output
               var outputPacket = task.Result.packet;
               if (outputPacket != null)
               {
                    var outputVideo = outputPacket.Get();
                    if (outputVideo.TryReadPixelData(outputPixel))
                    {
                         outputTexture.SetPixels32(outputPixel);
                         outputTexture.Apply();
                    }
               }

          }
     }


     void OnDestroy()
     {

          if (camTexture != null)
               camTexture.Stop();


          outputVideoStream?.Dispose();
          outputVideoStream = null;

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


}
