using UnityEngine;
using Mediapipe;


public class HelloWorld2 : MonoBehaviour
{
     void Start()
     {
          Protobuf.SetLogHandler(Protobuf.DefaultLogHandler);

          // set up graph
          var graphConfig = @"
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

          var graph = new CalculatorGraph(graphConfig);
          var poller = graph.AddOutputStreamPoller<string>("out"); // NOTE: don't change order
          graph.StartRun();


          // input
          for (var i = 0; i < 10; i++)
          {
               var input = Packet.CreateStringAt("Hello World! " + i, i); // +timestamp
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
          graph.Dispose(); // guess garbage collection won't apply to native c++?
          poller.Dispose();
          output.Dispose();

     }


     void OnApplicationQuit()
     {
          Protobuf.ResetLogHandler(); // avoid SIGSEGV
     }
}
