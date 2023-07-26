//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//namespace TianParameterModelForOpt._4_repair
//{
//    internal static class Wait
//    {
//        public WaitForInitialization(dynamic needChechNull)
//        {
//            const int bail_milliseconds = 15 * 1000;
//            int time_waiting = 0;
//            while (0 == needChechNull.IsInitialized())
//            {
//                Thread.Sleep(100);
//                time_waiting += 100;
//                if (time_waiting > bail_milliseconds)
//                {
//                    Console.WriteLine("Rhino initialization failed");
//                    return;
//                }
//            }
//        }
//    }
//}
