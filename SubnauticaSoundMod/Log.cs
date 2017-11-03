using System;
using System.Diagnostics;

namespace SubnauticaSoundMod
{
    public class Log
    {
        public static void Error(string message, params object[] args)
        {
			Console.WriteLine("[SoundMod] ERROR: " + message, args);
        }

        public static void Info(string message, params object[] args)
        {
			Console.WriteLine("[SoundMod]: " + message, args);
        }
    }
}
