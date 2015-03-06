using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Server {
    class ServerConnection {

        private TcpClient client;
        private TcpListener myList;

        public ServerConnection(TcpListener myList) {
            this.myList = myList;
        }

        public void connect() {
            client = myList.AcceptTcpClient();
            NetworkStream stm = client.GetStream();
            Task.Run(() => {
                try {
                    Byte[] b = new Byte[750];
                    for (int i = 0; i < 500; i++) {
                        stm.Write(b, 0, 750);
                    }
                    b[0] = (Byte)'S';
                    stm.Write(b, 0, 750);
                } finally {
                    client.Close();
                }
            });
        }
    }
}
