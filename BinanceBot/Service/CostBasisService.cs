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

        private Dictionary<string, decimal> _costBasis = new Dictionary<string, decimal>();
        public CostBasisService(IBinanceClient client, IBinanceSocketClient socketClient, SymbolService symbolService, BinanceAccountService accountService)
        {
            _client = client;
            _socketClient = socketClient;
            _symbolService = symbolService;
            _accountService = accountService;
        }

        public async Task Awaken()
        {
            await InitializeCostBasis();
            await SetUpSockets();
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
                if (data.Data.Status == OrderStatus.Filled || data.Data.Status == OrderStatus.PartiallyFilled)
                {
                    HandleOrderUpdate(data.Data.Symbol, data.Data.LastPriceFilled, data.Data.LastQuantityFilled, data.Data.Side);
                }
            },
            data => 
            {
            },
            data => 
            { 
            },
            data =>
            {
                
            });
        }

        private void HandleOrderUpdate(string symbol, decimal totalPrice, decimal totalQuantity, OrderSide side)
        {
            if (side == OrderSide.Buy)
            {
                _costBasis[symbol] += totalPrice;
            }
            else
            {
                _costBasis[symbol] -= totalPrice;
            }
        }

        public decimal GetCostBasis(string symbol)
        {
            return 0M;
        }

    }
}