using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TianParameterModelForOpt._6_antiBreak
{
    internal  static class AntiBreak
    {
        public static async Task RunWithTimeout(Action action, TimeSpan timeout)
        {
            using (var cts = new CancellationTokenSource(timeout))
            {
                var task = Task.Run(action, cts.Token);

                try
                {
                    await task;
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Operation timed out.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An exception occurred: {ex.Message}");
                }
            }
        }
    }

}
