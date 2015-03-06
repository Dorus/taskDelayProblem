﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Server {

    public class Server {
        public static void Main() {
            IPAddress ipAd = IPAddress.Parse("127.0.0.1");
            TcpListener myList = new TcpListener(ipAd, 8001);
            myList.Start();
            Console.WriteLine("The server local End point is: " +
                              myList.LocalEndpoint);
            Thread.Sleep(1000);
            Console.WriteLine("Waiting for a connection.....");
            while (true) {
                new ServerConnection(myList).connect();
            }
        }
    }
}
