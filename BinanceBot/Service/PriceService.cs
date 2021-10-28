using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Binance.Net.Interfaces;
using Binance.Net.Interfaces.SubClients.Spot;
using Binance.Net.Objects.Spot.MarketStream;

namespace BinanceBot.Service
{
    public class PriceService
    {
        private ConcurrentDictionary<string, decimal> _marketPrices = new ConcurrentDictionary<string, decimal>();
        private readonly IBinanceClient _client;
        private readonly IBinanceSocketClient _socketClient;

        private readonly SymbolService _symbolService;

        public PriceService(IBinanceClient client, IBinanceSocketClient socketClient, SymbolService symbolService)
        {
            _client = client;
            _socketClient = socketClient;
            _symbolService = symbolService;
        }

        public async Task Awaken()
        {
            var prices = await _client.Spot.Market.GetPricesAsync();
            foreach (var price in prices.Data)
            {
                var cleanedSymbol = price.Symbol.Replace("USD", "");
                if (_symbolService.GetAllCoins().Contains(cleanedSymbol))
                {
                    UpdatePrice(cleanedSymbol, price.Price);
                }
            }
            // wait 10 seconds for orders to get wiped out... this is hacky i know
            await Task.Delay(10000);
            await SetUpSockets();
        }

        private async Task SetUpSockets()
        {
            var socketTasks = new List<Task>();
            foreach(var price in _marketPrices)
            {
                socketTasks.Add(SetUpSocket(price.Key));
            }
            await Task.WhenAll(socketTasks);
        }

        private Task SetUpSocket(string symbol)
        {
            return _socketClient.Spot.SubscribeToTradeUpdatesAsync(symbol + "USD",
                data => 
                {
                    Task.Run(() => {HandleTradeSocket(data.Data);});
                });
        }

        private async void HandleTradeSocket(BinanceStreamTrade trade)
        {
            UpdatePrice(trade.Symbol.Replace("USD", ""), trade.Price);
        }

        private async void UpdatePrice(string symbol, decimal price)
        {
            _marketPrices[symbol] = price;
        }

        public decimal GetPrice(string symbol)
        {
            return _marketPrices[symbol];
        }
    }
}