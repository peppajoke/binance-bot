using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BinanceBot.Configuration;

namespace BinanceBot.Service
{
    public class DynamicSellService
    {
        private readonly BinanceAccountService _accountService;
        private readonly OrderService _orderService;
        private readonly PriceService _priceService;
        private readonly CostBasisService _costBasisService;

        private readonly BotConfig _config;

        public DynamicSellService(BinanceAccountService accountService, OrderService orderService, PriceService priceService, CostBasisService costBasisService, BotConfig config)
        {
            _accountService = accountService;
            _orderService = orderService;
            _priceService = priceService;
            _costBasisService = costBasisService;
            _config = config;
        }
        public async Task TrySell(string symbol)
        {
            await Sell(symbol);
        }

        private async Task Sell(string symbol)
        {
            if (_config.HoardCoins.Contains(symbol))
            {
                // don't sell this coin
                return;
            }
            try 
            {
                var boughtPrice = _costBasisService.GetAveragePriceBought(symbol);
                if (boughtPrice == 0) 
                {
                    return;
                }
                var heldQuantity = _accountService.GetHeldQuantity(symbol);
                var marketPrice = _priceService.GetPrice(symbol);

                var targetPrice = Math.Max(boughtPrice * 1.04M, marketPrice);

                if (_accountService.Liquidity() > .5M)
                {
                    // our liquidity is good, set an additional 1% above market sell rate
                    targetPrice *= 1.01M;
                }

                var targetQuantity = heldQuantity * Math.Min(1M, (targetPrice / 5) / boughtPrice);

                if (targetQuantity * targetPrice < 11M)
                {
                    targetQuantity = 11M / targetPrice;
                }

                if (targetQuantity * targetPrice < 10M)
                {
                    return;
                }
                Console.WriteLine("Selling " + symbol);
                await _orderService.Sell(symbol, targetPrice, targetQuantity);
            }
            catch(Exception e)
            {
                Console.WriteLine("failed to sell " + symbol+ ": " + e.Message);
            }
        }
    }
}