using System;
using BinanceBot.Configuration;
using BinanceBot.Service;

namespace BinanceBot
{
    class Program
    {
        static void Main(string[] args)
        {
            BotConfig config;
            var bcs = new BotConfigurationService();
            var hasValidConfig = false;
            while (!hasValidConfig)
            {
                try
                {
                    config = bcs.GetConfig();
                    hasValidConfig = true;
                }
                catch(Exception ex) 
                {
                    Console.WriteLine("No crypto configuration found. Let's get you set up.");
                    bcs.BuildConfig();
                }
            }
            
            
            Console.WriteLine("Hello World!");
        }
    }
}
