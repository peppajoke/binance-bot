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

        private Dictionary<string, CostBasis> _costBasis = new Dictionary<string, CostBasis>();
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
                _costBasis[symbol] = new CostBasis () {Spend = costBasis, Quantity = quantityHeld};
            }
            MessageCostBasis();
        }

        private void MessageCostBasis()
        {
            var totalCost = 0M;
            foreach (var cost in _costBasis)
            {
                var symbol = cost.Key;
                var costBasis = cost.Value;

                if (costBasis.Spend == 0)
                {
                    continue;
                }

                totalCost += costBasis.Spend;
                var marketPrice = _priceService.GetPrice(symbol);
                var heldQuantity = _accountService.GetHeldQuantity(symbol);

                var profit = (heldQuantity * marketPrice) - costBasis.Spend;
                var profitPercent = profit / costBasis.Spend;

                Console.WriteLine(symbol + " cost basis: $" + costBasis.Spend + " current value: $" + heldQuantity * marketPrice + " proft: $"+ profit + " (" + profitPercent + "%)" );    
            }
            Console.WriteLine("Total Cost: $" + totalCost);
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

        private void HandleOrderUpdate(string symbol, decimal price, decimal quantity, OrderSide side)
        {
            if (!_costBasis.ContainsKey(symbol))
            {
                return;
            }
            if (side == OrderSide.Buy)
            {
                _costBasis[symbol].AddCoins(quantity, price);
            }
            else
            {
                _costBasis[symbol].SubtractCoins(quantity);
            }
            Console.WriteLine("Captured order: " + symbol + ": " + price+ "...." + quantity);
            try 
            {
                Console.WriteLine("Cost basis for " + symbol + " is now " + _costBasis[symbol] + " min sell price: $" + _costBasis[symbol].AveragePriceBought);
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
            return _costBasis[symbol].Spend;       
        }

        public decimal GetAveragePriceBought(string symbol)
        {
            if (symbol is null || symbol == "")
            {
                return 0M;
            }
            return _costBasis[symbol].AveragePriceBought;       
        }
    }

    class CostBasis
    {
        public decimal Quantity {get;set;}
        public decimal Spend { get;set; }

        public decimal AveragePriceBought => Spend/Quantity;

        public void SubtractCoins(decimal quantity)
        {
            Spend -= AveragePriceBought * quantity;
            Quantity -= quantity;
        }

        public void AddCoins(decimal quantity, decimal price)
        {
            Spend += price * quantity;
            Quantity += quantity;
        }
    }
}