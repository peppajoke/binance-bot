using System;
using System.Threading.Tasks;
using Binance.Net.Interfaces;
using Binance.Net.Enums;
using Binance.Net.Objects.Spot.UserStream;
using System.Text;

namespace BinanceBot.Service
{
    public class OrderWatchService
    {
        private readonly IBinanceClient _client;
        private readonly IBinanceSocketClient _socketClient;
        private readonly DynamicSellService _dynamicSellService;
        private readonly DynamicBuyService _dynamicBuyService;
        public OrderWatchService(IBinanceClient client, IBinanceSocketClient socketClient, DynamicSellService dynamicSellService, DynamicBuyService dynamicBuyService)
        {
            _client = client;
            _socketClient = socketClient;
            _dynamicSellService = dynamicSellService;
            _dynamicBuyService = dynamicBuyService;
        }

        public async Task Awaken()
        {
            var listenKeyResultAccount = await _client.Spot.UserStream.StartUserStreamAsync();

            if(!listenKeyResultAccount.Success)
            {
                Console.WriteLine("Failed to start user session.");
                return;
            }

            var exchangeInfo = await _client.Spot.System.GetExchangeInfoAsync();

            Console.WriteLine("Subscribing to updates...");
            var subscribeResult = await _socketClient.Spot.SubscribeToUserDataUpdatesAsync(listenKeyResultAccount.Data, 
            data => 
            {
                Task.Run(() => { HandleOrderUpdate(data.Data.Symbol.Replace("USD" , ""), data.Data.Side, data.Data.Status); MessageOrder(data.Data);});
            },
            null,
            null,
            null
            );

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
        private void MessageOrder(BinanceStreamOrderUpdate order)
        {
            if (order.Status != OrderStatus.Filled)
            {
                return;
            }

            var sb  = new StringBuilder();
            sb.Append(order.Side == OrderSide.Buy ? "Bought " : "Sold ");
            sb.Append(order.QuantityFilled + " " + order.Symbol + " at $" + order.Price);

            Console.WriteLine(sb.ToString());
        }

        private async void HandleOrderUpdate(string symbol, OrderSide side, OrderStatus status)
        {
            if (status != OrderStatus.Filled)
            {
                // not a filled order, we don't care about it.
                return;
            }

            if (side == OrderSide.Sell)
            {
                await _dynamicSellService.TrySell(symbol);
            }
            else
            {
                await _dynamicBuyService.TryBuy(symbol);
            }
        }
    }
}