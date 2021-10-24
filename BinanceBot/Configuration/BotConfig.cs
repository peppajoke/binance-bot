using System;
using System.Collections.Generic;

namespace BinanceBot.Configuration
{
    [Serializable]
    public class BotConfig
    {
        // Coins that you want to hang on to indefinitely.
        public IEnumerable<string> HoardCoins { get; set; }

        public string BinanceApiKey {get;set;}
        
        public string BinanceApiSecret {get;set;}
    }
}