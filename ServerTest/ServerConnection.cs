using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Server {
    class ServerConnection {
        static int openConnections = 0;

        private TcpClient client;
        private TcpListener myList;
        DateTime start = DateTime.Now;

        public ServerConnection(TcpListener myList) {
            this.myList = myList;
        }

        static int count = 0;

        public void connect() {
            int counter = Interlocked.Increment(ref count);
            client = myList.AcceptTcpClient();
            printAccept(counter);
            NetworkStream stm = client.GetStream();
            Stream bstm = new BufferedStream(stm, 1500);
            Task.Run(() => {
                try {
                    Byte[] b = new Byte[750];
                    for (int i = 0; i < 500; i++) {
                        bstm.Write(b, 0, 750);
                    }
                    b[0] = (Byte)'S';
                    bstm.Write(b, 0, 750);
                } finally {
                    client.Close();
                    printClose();
                }
            });
        }

        private void printAccept(int counter) {
            int con = Interlocked.Increment(ref openConnections);
            Console.WriteLine("Connection accepted from {0} con: {1} time {2:0.000} nr {3}", client.Client.RemoteEndPoint, con, DateTime.Now.Subtract(start).TotalSeconds, counter);
        }

        private void printClose() {
            int con = Interlocked.Decrement(ref openConnections);
            Console.WriteLine("\nClosing connection {0} connections: {1} time: {2:0.000}", client.Client.RemoteEndPoint, con, DateTime.Now.Subtract(start).TotalSeconds);
        }
    }
}
