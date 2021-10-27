using System;
using System.Threading.Tasks;
using BinanceBot.Configuration;
using BinanceBot.Service;

namespace BinanceBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var config = new BotConfig();
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
            
            await SetUpServices(config);
        }

        private static async Task SetUpServices(BotConfig config)
        {
            // i know this sucks. I'll clean it up later
            var clientProvider = new BinanceClientProvider(config);
            var client = clientProvider.GetClient();
            var socketClient = clientProvider.GetSocketClient();

            var symbolService = new SymbolService(client);
            var accountService = new BinanceAccountService(client, socketClient, symbolService);
            var priceService = new PriceService(client, socketClient, symbolService);
            var orderService = new OrderService(client, symbolService);
            var costBasisService = new CostBasisService(client, socketClient, symbolService, accountService);
            var dynamicSellService = new DynamicSellService(accountService, orderService, priceService, costBasisService);

            var botService = new BinanceBotService(priceService, orderService, costBasisService, symbolService, accountService, dynamicSellService);

            await botService.Awaken();
        }
    }
}
