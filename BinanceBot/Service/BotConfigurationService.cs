using System.Xml;
using BinanceBot.Configuration;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;

namespace BinanceBot.Service
{
    public interface IBotConfigurationService
    {
        void BuildConfig();
        BotConfig GetConfig();
    }
    public class BotConfigurationService : IBotConfigurationService
    {
        private const string CONFIG_PATH = "../config.json";
        public void BuildConfig()
        {
            var config = new BotConfig();
            Console.WriteLine("Any coins you want to avoid selling? Example: ADA,BTC,ETH");
            
            var hoardCoinString = Console.ReadLine();

            config.HoardCoins = hoardCoinString.Split(",");

            Console.WriteLine("What is your Binance US API Key?");
            config.BinanceApiKey = Console.ReadLine();

            Console.WriteLine("What is your Binance US API Secret?");
            config.BinanceApiSecret = Console.ReadLine();

            Console.WriteLine("Saving config...");

            string json = JsonSerializer.Serialize<BotConfig>(config);
            File.WriteAllText(CONFIG_PATH, json);
            Console.WriteLine("Success!");
        }

        public BotConfig GetConfig()
        {
            var json = File.ReadAllText(CONFIG_PATH);
            return JsonSerializer.Deserialize<BotConfig>(json);
        }
    }
}