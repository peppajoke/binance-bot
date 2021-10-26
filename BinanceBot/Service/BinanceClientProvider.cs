using System;
using Binance.Net;
using Binance.Net.Objects;
using BinanceBot.Configuration;
using CryptoExchange.Net.Authentication;

namespace BinanceBot.Service
{
    public class BinanceClientProvider
    {
        private BotConfig _botConfig;
        public BinanceClientProvider(BotConfig botConfig)
        {
            _botConfig = botConfig;
        }

        public BinanceClient GetClient()
        {
            return new BinanceClient(new BinanceClientOptions() 
            { 
                ApiCredentials = new ApiCredentials(_botConfig.BinanceApiKey, _botConfig.BinanceApiSecret),
                BaseAddress = "https://api.binance.us/",
                AutoTimestamp = true
            });
        }

        public BinanceSocketClient GetSocketClient()
        {
            return new BinanceSocketClient(new BinanceSocketClientOptions()
            {
                ApiCredentials = new ApiCredentials(_botConfig.BinanceApiKey, _botConfig.BinanceApiSecret),
                BaseAddress = "wss://stream.binance.us:9443/",
                AutoReconnect = true,
                ReconnectInterval = TimeSpan.FromSeconds(15)
            });
        }
    }
}