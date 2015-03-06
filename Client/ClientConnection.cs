using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Client {

    class ClientConnection : IDisposable {
        private TimeSpan start;
        private Stopwatch sw = new Stopwatch();
        private TcpClient tcpclnt = new TcpClient();
        private int delay;
        private int startDelay;
        private Client clnt;

        public ClientConnection(Client clnt, int delay) {
            this.clnt = clnt;
            this.delay = delay;
            sw.Start();
        }

        public ClientConnection(Client clnt, int delay, int startDelay)
            : this(clnt, delay) {
            this.startDelay = startDelay;
        }

        private Byte[] b = new Byte[750];

        public async Task connectAsync() {
            await tcpclnt.ConnectAsync("127.0.0.1", 8001);
            try {
                await Task.Delay(startDelay * 1000);
                using (Stream stream = tcpclnt.GetStream()) {
                    int count = 0;
                    while (true) {
                        var beginRead = sw.Elapsed;
                        int r = await stream.ReadAsync(b, 0, 750);
                        if (r < 750 || b[0] == (Byte)'S') break;
                        if (count == 0) start = sw.Elapsed;
                        count++;
                        var preDelay = sw.Elapsed;
                        double sleep = start.Add(TimeSpan.FromMilliseconds(count * delay + 1)).Subtract(sw.Elapsed).TotalMilliseconds;
                        if (sleep > 0) {
                            // Use one of the three:
                            await Task.Delay((int)sleep); // Task.Delay is very inaccurate, could take 15-30ms.
                            //await TaskExt.Delay((int)sleep);
                            //await Task.Run(() => Thread.Sleep((int)sleep));
                        }
                        if (sw.Elapsed.Subtract(preDelay).TotalMilliseconds > 100) {
                            Console.WriteLine("Sleep {2}: expect {0:0.0}ms got: {1:0.00}s", sleep, sw.Elapsed.Subtract(preDelay).TotalSeconds, delay);
                        }
                    }
                }
            } finally {
                tcpclnt.Close();
            }
        }

        public void Dispose() {
            if (tcpclnt != null)
                tcpclnt.Close();
        }
    }
}
