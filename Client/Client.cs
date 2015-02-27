using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Client {

    public class Client {
        public static void Main() {
            new Client().start();
            Console.WriteLine("Press enter quit.");
            Console.ReadLine();
        }

        List<Task> tasks = new List<Task>();
        Object lockme = new Object();


        private async Task displayThreads() {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < 32; i++) {
                int counter = System.Diagnostics.Process.GetCurrentProcess().Threads.Count;
                Console.WriteLine("Threads.Count: {0} {1:0.00}s", counter, sw.ElapsedMilliseconds / 1000);
                //await Task.Delay(2000);
                Thread.Sleep(2000);
                await Task.Yield();
            }
        }

        private void makeClient(int delay, int startDelay) {
            Task task = new ClientConnection(this, delay, startDelay).connectAsync();
            task.ContinueWith(_ => {
                lock (lockme) { tasks.Remove(task); }
            }, TaskContinuationOptions.ExecuteSynchronously
            );
            lock (lockme) { tasks.Add(task); }
        }

        private void makeClient(int delay) {
            makeClient(delay, 0);
        }

        private void waitForTasks(List<Task> tasks) {
            Task[] waitFor;
            lock (lockme) {
                waitFor = tasks.ToArray();
            }
            Task.WaitAll(waitFor);
        }

        private void start() {
            DateTime start = DateTime.Now;
            Console.WriteLine("Starting clients...");

            int[] iList = new[]  { 
                0,1,1,2,
                10, 20, 30, 40};
            foreach (int delay in iList) {
                makeClient(delay, 0); ;
            }
            makeClient(15, 40);
            Console.WriteLine("Done making");

            tasks.Add(displayThreads());

            waitForTasks(tasks);
            Console.WriteLine("All done.");
        }
    }
}
