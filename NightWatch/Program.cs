using System;
using System.Threading;

namespace CaTracker
{
    public static class Program
    {
        public static Main MainInstance;

        public static void Main(string[] args)
        {
            MainInstance = new Main();
            Console.WriteLine("Starting Tracker");
            MainInstance.Start();
            while (true) Thread.Sleep(10000);
        }
    }
}