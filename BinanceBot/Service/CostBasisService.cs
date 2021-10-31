using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using System;

namespace BinanceBot.Service
{
    public class CostBasisService
    {
        private readonly IBinanceClient _client;
        private readonly IBinanceSocketClient _socketClient;
        private readonly SymbolService _symbolService;
        private readonly BinanceAccountService _accountService;

        private readonly PriceService _priceService;

        private Dictionary<string, decimal> _costBasis = new Dictionary<string, decimal>();
        public CostBasisService(IBinanceClient client, IBinanceSocketClient socketClient, SymbolService symbolService, BinanceAccountService accountService, PriceService priceService)
        {
            _client = client;
            _socketClient = socketClient;
            _symbolService = symbolService;
            _accountService = accountService;
            _priceService = priceService;
        }

        public async Task Awaken()
        {
            await InitializeCostBasis();
            await SetUpSockets();
            Console.WriteLine("Cost basis analysis complete.");
        }

        private async Task InitializeCostBasis()
        {
            foreach (var symbol in _symbolService.GetAllCoins())
            {
                var orders = await _client.Spot.Order.GetOrdersAsync(symbol + "USD");
                var quantityHeld = _accountService.GetHeldQuantity(symbol);
                var remainingQuantityToScan = quantityHeld;
                var costBasis = 0M;
                foreach (var order in orders.Data.OrderByDescending(x=>x.UpdateTime))
                {
                    if (remainingQuantityToScan == 0)
                    {
                        // We've captured the full cost basis, so stop.
                        break;
                    }
                
                    if (order.Side == OrderSide.Buy && (order.Status == OrderStatus.Filled || order.Status == OrderStatus.PartiallyFilled))
                    {
                        if (order.Quantity > remainingQuantityToScan)
                        {
                            var unitPrice = order.QuoteQuantityFilled / order.QuantityFilled;
                            costBasis += unitPrice * remainingQuantityToScan;
                            remainingQuantityToScan = 0;
                        }
                        else
                        {
                            remainingQuantityToScan -= order.QuantityFilled;
                            costBasis += order.QuoteQuantityFilled;
                        }
                    }
                }
                _costBasis[symbol] = costBasis;
            }
            MessageCostBasis();
        }

        private void MessageCostBasis()
        {
            foreach (var cost in _costBasis)
            {
                var symbol = cost.Key;
                var costBasis = cost.Value;

                if (costBasis == 0)
                {
                    continue;
                }
                var marketPrice = _priceService.GetPrice(symbol);
                var heldQuantity = _accountService.GetHeldQuantity(symbol);

                var profit = (heldQuantity * marketPrice) - costBasis;
                var profitPercent = profit / costBasis;

                Console.WriteLine(symbol + " cost basis: " + costBasis + " current value: $" + heldQuantity * marketPrice + " proft: $"+ profit + " (" + profitPercent + "%)" );    
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
            data => 
            {
                if (data.Data.Status == OrderStatus.Filled)
                {
                    HandleOrderUpdate(data.Data.Symbol.Replace("USD", ""), data.Data.Price, data.Data.QuantityFilled, data.Data.Side);
                }
            },
            null,
            null,
            null);
        }

        private void HandleOrderUpdate(string symbol, decimal totalPrice, decimal totalQuantity, OrderSide side)
        {
            if (!_costBasis.ContainsKey(symbol))
            {
                return;
            }
            if (side == OrderSide.Buy)
            {
                _costBasis[symbol] += totalPrice;
            }
            else
            {
                _costBasis[symbol] -= totalPrice;
            }
            try 
            {
                Console.WriteLine("Cost basis for " + symbol + " is now " + _costBasis[symbol] + " min sell price: $" + _costBasis[symbol] / _accountService.GetHeldQuantity(symbol));
            }
            catch(Exception e)
            {

            }
        }

        public decimal GetCostBasis(string symbol)
        {
            if (symbol is null || symbol == "")
            {
                return 0M;
            }
            return _costBasis[symbol];       
        }

    }
}