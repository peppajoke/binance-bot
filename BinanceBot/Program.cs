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
            var orderService = new OrderService(client, symbolService);
            var costBasisService = new CostBasisService(client, socketClient, symbolService, accountService);
            var priceService = new PriceService(client, socketClient, symbolService);
            var dynamicBuyService = new DynamicBuyService(accountService, orderService, costBasisService, priceService);
            var dynamicSellService = new DynamicSellService(accountService, orderService, priceService, costBasisService);
            var orderWatchService = new OrderWatchService(client, socketClient, dynamicSellService, dynamicBuyService);

            var botService = new BinanceBotService(priceService, orderService, costBasisService, symbolService, accountService, dynamicSellService, orderWatchService, dynamicBuyService);

            await StartBotService(botService);
        }

        private static async Task StartBotService(BinanceBotService service)
        {
            await service.Awaken();
            
            while(true)
            {
                var lifetime = DateTime.Now.AddHours(24);
                while(lifetime > DateTime.Now)
                {

                }
                await service.Cycle();
            }
        }
    }
}
