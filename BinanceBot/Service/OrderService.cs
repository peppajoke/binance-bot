using System;
using System.Threading.Tasks;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Interfaces.SubClients.Spot;

namespace BinanceBot.Service
{
    public class OrderService
    {

        private readonly IBinanceClient _client;
        private readonly SymbolService _symbolService;

        public OrderService(IBinanceClient client, SymbolService symbolService)
        {
            _client = client;
            _symbolService = symbolService;
        }
        
        public async Task Buy(string symbol, decimal price, decimal quantity)
        {
            var quantityPrecision = _symbolService.GetQuantityPrecision(symbol);
            var pricePrecision = _symbolService.GetPricePrecision(symbol);
            var response = await _client.Spot.Order.PlaceOrderAsync(
                symbol+"USD", OrderSide.Buy, OrderType.Limit, quantity: Math.Round(quantity, quantityPrecision), 
                price: Math.Round(price, pricePrecision), timeInForce: TimeInForce.GoodTillCancel
            );
            if (!response.Success)
            {
                Console.WriteLine("Failed to buy " + symbol + ": " + response.Error.Message);
            }
        }

        public async Task Sell(string symbol, decimal price, decimal quantity)
        {
            var quantityPrecision = _symbolService.GetQuantityPrecision(symbol);
            var pricePrecision = _symbolService.GetPricePrecision(symbol);
            var response = await _client.Spot.Order.PlaceOrderAsync(
                symbol+"USD", OrderSide.Sell, OrderType.Limit, quantity: Math.Round(quantity, quantityPrecision), 
                price: Math.Round(price, pricePrecision), timeInForce: TimeInForce.GoodTillCancel
            );
            var thing = true;
        }
        public async Task CancelAllOrders()
        {
            Console.WriteLine("Cancelling all open orders...");
            foreach (var symbol in _symbolService.GetAllCoins())
            {
                await _client.Spot.Order.CancelAllOpenOrdersAsync(symbol + "USD");
            }
        }
    }
}