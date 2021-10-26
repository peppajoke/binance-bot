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
        private HashSet<string> _existingSockets = new HashSet<string>();

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
            if (_existingSockets.Contains(symbol))
            {
                return new Task(() => {});
            }
            return _socketClient.Spot.SubscribeToTradeUpdatesAsync(symbol + "USD",
                data => 
                {
                    HandleTradeSocket(data.Data);
                    _existingSockets.Add(symbol);
                });
        }

        private async void HandleTradeSocket(BinanceStreamTrade trade)
        {
            _marketPrices[trade.Symbol] = trade.Price;
        }

        private void UpdatePrice(string symbol, decimal price)
        {
            _marketPrices[symbol] = price;
        }

        public decimal GetPrice(string symbol)
        {
            return _marketPrices[symbol];
        }
    }
}