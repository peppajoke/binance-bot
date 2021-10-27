using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Binance.Net.Interfaces;
using Binance.Net.Interfaces.SubClients;

namespace BinanceBot.Service
{
    public class BinanceAccountService
    {
        private decimal _freeCash = 0;
        private Dictionary<string, decimal> _coins = new Dictionary<string, decimal>();

        private readonly IBinanceClient _client;
        private readonly IBinanceSocketClient _socketClient;
        private readonly SymbolService _symbolService;
        public BinanceAccountService(IBinanceClient client, IBinanceSocketClient socketClient, SymbolService symbolService)
        {
            _client = client;
            _socketClient = socketClient;
            _symbolService = symbolService;
        }

        public async Task Awaken()
        {
            await InitializeAssets();
            await SetUpSockets();
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
            null,
            data =>
            {
                UpdateBalance(data.Data.Asset, data.Data.BalanceDelta);
            });

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

        private void UpdateBalance(string symbol, decimal delta)
        {
            if (symbol == "USD")
            {
                _freeCash += delta;
            }
            else
            {
                lock(_coins)
                {
                    _coins[symbol] += delta;
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

        private void UpdateHolding(string symbol, decimal delta)
        {
            lock(_coins)
            {
                _coins[symbol] += delta;
            }
        }
    }
}