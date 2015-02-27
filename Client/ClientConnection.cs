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
        public static int openConnections = 0;
        public static int openingConnections = 0;

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
            int con2 = Interlocked.Increment(ref openingConnections);
            try {
                await tcpclnt.ConnectAsync("127.0.0.1", 8001);
            } catch (SocketException e) {
                if (e.ErrorCode == 10061) { // connection refused, retry once.
                    tcpclnt.Connect("127.0.0.1", 8001);
                    Console.WriteLine("Retry connection");
                } else {
                    throw;
                }
            }
            Interlocked.Decrement(ref openingConnections);
            try {
                printConnecting();

                await Task.Delay(startDelay * 1000);
                Console.WriteLine("startDelay {0}: {1}", delay, startDelay);
                using (Stream stream = tcpclnt.GetStream()) {

                    int count = 0;
                    while (true) {
                        var beginRead = sw.Elapsed;
                        int r = await stream.ReadAsync(b, 0, 750);

                        var readTime = sw.Elapsed.Subtract(beginRead).TotalMilliseconds;
                        if (readTime > 20) {
                            Console.WriteLine("Slow read {0}, {1:0.0}ms", delay, readTime);
                        }

                        if (r < 750 || b[0] == (Byte)'S') break;
                        if (count == 0) start = sw.Elapsed;
                        count++;
                        var preDelay = sw.Elapsed;
                        if (preDelay.Subtract(beginRead).TotalMilliseconds > 50) {
                            Console.WriteLine("Slowly {0}", sw.Elapsed.Subtract(beginRead).TotalMilliseconds);
                        }
                        double sleep = start.Add(TimeSpan.FromMilliseconds(count * delay + 1)).Subtract(sw.Elapsed).TotalMilliseconds;
                        if (sleep > 0) {
                            await Task.Delay((int)sleep); // Task.Delay is very inaccurate, could take 15-30ms.
                            //await TaskExt.Delay((int)sleep);
                            //await Task.Run(() => Thread.Sleep((int)sleep));

                            var late = sw.Elapsed.Subtract(start).Subtract(TimeSpan.FromMilliseconds(count * delay + 1)).TotalSeconds;
                            if (late > 1) Console.WriteLine("Slow! {0} time {2:00.00} late {1:00.00} read {4:0.00} sleep {3:0.00}ms", delay, late, sw.Elapsed.Subtract(beginRead).TotalSeconds, sleep, preDelay.Subtract(beginRead).TotalMilliseconds);
                        }
                        if (sw.Elapsed.Subtract(preDelay).TotalMilliseconds > 100) {
                            Console.WriteLine("Sleep {2}: expect {0:0.0}ms got: {1:0.00}s", sleep, sw.Elapsed.Subtract(preDelay).TotalSeconds, delay);
                        }
                    }
                    // bstm.Flush(); // kinda optional
                    printClose();
                }
            } catch (Exception e) {
                if (e is SocketException && ((SocketException)e).ErrorCode == 10061) { // connection refused, retry once.
                    Console.WriteLine("Refused {0} {1}", delay, tcpclnt.Client.LocalEndPoint);
                } else {
                    Console.WriteLine("Error1..... {0} {1}", delay, e);
                }
            } finally {
                int con = Interlocked.Decrement(ref openConnections);
                tcpclnt.Close();
            }
        }

        private void printConnecting() {
            int con = Interlocked.Increment(ref openConnections);
            Console.WriteLine("Connected {0} {1} connections {2}, open {4} time {3:0.000}", delay, tcpclnt.Client.LocalEndPoint, con, sw.Elapsed.Subtract(start).TotalSeconds, openingConnections);
        }

        private void printClose() {
            Console.WriteLine("Closing con {0} {1} con {2} open {4} time {3:0.000}", delay, tcpclnt.Client.LocalEndPoint, openConnections, sw.Elapsed.Subtract(start).TotalSeconds, openingConnections);
        }

        public void Dispose() {
            if (tcpclnt != null)
                tcpclnt.Close();
        }
    }
}
