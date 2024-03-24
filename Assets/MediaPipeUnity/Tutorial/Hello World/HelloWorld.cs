using UnityEngine;

namespace Mediapipe.Unity.Tutorial
{
     public class HelloWorld : MonoBehaviour
     {
          void Start()
          {
               Protobuf.SetLogHandler(Protobuf.DefaultLogHandler);

               // set up graph
               var configText = @"
                    input_stream: ""in""
                    output_stream: ""out""
                    node {
                      calculator: ""PassThroughCalculator""
                      input_stream: ""in""
                      output_stream: ""out1""
                    }
                    node {
                      calculator: ""PassThroughCalculator""
                      input_stream: ""out1""
                      output_stream: ""out""
                    }
                    ";

               var graph = new CalculatorGraph(configText);
               //var graph = new CalculatorGraph("invalid format yeti yeti yeti");


               var poller = graph.AddOutputStreamPoller<string>("out"); // NOTE: don't change order of set "output" and StartRun()
               graph.StartRun();


               // input
               for (var i = 0; i < 10; i++)
               {
                    // var input = Packet.CreateString("Hello World!");
                    var input = Packet.CreateStringAt("Hello World!", i); // + timestamp!
                    graph.AddPacketToInputStream("in", input);
               }


               // output
               graph.CloseInputStream("in");

               var output = new Packet<string>();
               while (poller.Next(output))
               {
                    Debug.Log(output.Get());
               }

               graph.WaitUntilDone();
               Debug.Log("Done");


               // clean up
               graph.Dispose(); // guess since it's native c++, garbage collection won't apply?
               poller.Dispose();
               output.Dispose();

          }


          void OnApplicationQuit()
          {
               Protobuf.ResetLogHandler(); // witout this can cause SIGSEGV
          }
     }
}