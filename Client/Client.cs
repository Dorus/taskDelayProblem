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
        private void makeClient(int delay, int startDelay) {
            Task task = new ClientConnection(this, delay, startDelay).connectAsync();
            tasks.Add(task);
        }

        private void makeClient(int delay) {
            makeClient(delay, 0);
        }

        private void start() {
            DateTime start = DateTime.Now;
            Console.WriteLine("Starting clients...");

            foreach (int delay in new int[] { 10, 20, 30, 40 }) {
                makeClient(delay, 0); ;
            }
            makeClient(15, 40);
            Console.WriteLine("Done making. Please wait 20 seconds.");

            Task.WaitAll(tasks.ToArray());
            Console.WriteLine("All done.");
        }
    }
}
