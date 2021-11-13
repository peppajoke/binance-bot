using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BinanceBot.Configuration;

namespace BinanceBot.Service
{
    public class DynamicBuyService
    {
        private readonly BinanceAccountService _accountService;
        private readonly OrderService _orderService;
        private PriceService _priceService;
        private readonly CostBasisService _costBasisService;
        private readonly BotConfig _config;

        public DynamicBuyService(BinanceAccountService accountService, OrderService orderService, CostBasisService costBasisService, PriceService priceService, BotConfig config)
        {
            _accountService = accountService;
            _orderService = orderService;
            _costBasisService = costBasisService;
            _priceService = priceService;
            _config = config;
        }

        public async Task TryBuy(string symbol)
        {
            await Buy(symbol);
        }

        private async Task Buy(string symbol)
        {

            var marketPrice = _priceService.GetPrice(symbol);
            try
            {
                var targetSpend = _config.HoardCoins.Contains(symbol) ? 30 : 11;
                var boughtPrice = _costBasisService.GetAveragePriceBought(symbol);
                var cash = _accountService.GetAvailableCash();
                if (cash < targetSpend)
                {
                    return;
                }
                var heldQuantity = _accountService.GetHeldQuantity(symbol);
                decimal targetPrice;
                if (boughtPrice > 0)
                {
                    targetPrice = Math.Min(boughtPrice, marketPrice) * .99M;
                }
                else
                {
                    targetPrice = marketPrice * .99M;
                }

                var targetQuantity = targetSpend / targetPrice;

                Console.WriteLine("Buying " + symbol);
                await _orderService.Buy(symbol, targetPrice, targetQuantity);
            }
            catch(Exception e)
            {
                await _orderService.Buy(symbol, marketPrice, 11 / marketPrice);
            }
        }
    }
}