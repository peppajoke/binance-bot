using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Binance.Net.Interfaces;
using Binance.Net.Interfaces.SubClients;
using Binance.Net.Objects.Spot.UserStream;

namespace BinanceBot.Service
{
    public class BinanceAccountService
    {
        private decimal _freeCash = 0;
        private Dictionary<string, decimal> _coins = new Dictionary<string, decimal>();

        private readonly IBinanceClient _client;
        private readonly IBinanceSocketClient _socketClient;
        private readonly SymbolService _symbolService;
        private readonly PriceService _priceService;
        public BinanceAccountService(IBinanceClient client, IBinanceSocketClient socketClient, SymbolService symbolService, PriceService priceService)
        {
            _client = client;
            _socketClient = socketClient;
            _symbolService = symbolService;
            _priceService = priceService;
        }

        public async Task Awaken()
        {
            await InitializeAssets();
            await SetUpSockets();
        }

        public decimal Liquidity()
        {
            var totalValue = 0M;
            foreach(var coin in _coins)
            {
                totalValue += coin.Value * _priceService.GetPrice(coin.Key);
            }
            totalValue += _freeCash;

            return _freeCash / totalValue;
        }

        private async Task InitializeAssets()
        {
            var account = await _client.General.GetAccountInfoAsync();
            _freeCash = account.Data.Balances.First(x => x.Asset == "USD").Free;
            foreach (var symbol in _symbolService.GetAllCoins())
            {
                _coins[symbol] = 0;
                var matchingAsset = account.Data.Balances.FirstOrDefault(x=> x.Free > 0 && x.Asset == symbol);
                if (matchingAsset is not null)
                {
                    _coins[symbol] = matchingAsset.Free;
                }
            }
        }

        private async Task SetUpSockets()
        {
            var listenKeyResultAccount = await _client.Spot.UserStream.StartUserStreamAsync();

            if(!listenKeyResultAccount.Success)
            {
                Console.WriteLine("Failed to start user session.");
                return;
            }
            var subscribeResult = await _socketClient.Spot.SubscribeToUserDataUpdatesAsync(listenKeyResultAccount.Data, 
            null,
            null,
            data =>
            {
                UpdateBalances(data.Data.Balances);
            },
            null);

            if (!subscribeResult.Success)
            {
                Console.WriteLine("Failed to listen for account updates.");
                return;
            }
            else
            {
                Console.WriteLine("Listening for account updates...");
            }
        }

        private void UpdateBalances(IEnumerable<BinanceStreamBalance> balances)
        {
            foreach(var balance in balances)
            {
                if (balance.Asset == "USD")
                {
                    _freeCash = balance.Free;
                    Console.WriteLine("Free cash is now: $" + _freeCash);
                }
                else
                {
                    lock(_coins)
                    {
                        _coins[balance.Asset] = balance.Free;
                    }
                    Console.WriteLine("You now hold " + _coins[balance.Asset] + " " + balance.Asset);
                }
            }
        }

        public decimal GetAvailableCash()
        {
            return _freeCash;
        }

        public decimal GetHeldQuantity(string symbol)
        {
            return _coins[symbol];
        }
    }
}