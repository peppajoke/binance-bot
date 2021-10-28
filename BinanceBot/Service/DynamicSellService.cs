using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BinanceBot.Service
{
    public class DynamicSellService
    {
        private readonly BinanceAccountService _accountService;
        private readonly OrderService _orderService;
        private readonly PriceService _priceService;
        private readonly CostBasisService _costBasisService;

        public DynamicSellService(BinanceAccountService accountService, OrderService orderService, PriceService priceService, CostBasisService costBasisService)
        {
            _accountService = accountService;
            _orderService = orderService;
            _priceService = priceService;
            _costBasisService = costBasisService;
        }
        public async Task TrySell(string symbol)
        {
            await Sell(symbol);
        }

        private async Task Sell(string symbol)
        {
            var costBasis = _costBasisService.GetCostBasis(symbol);
            if (costBasis == 0) 
            {
                return;
            }
            var heldQuantity = _accountService.GetHeldQuantity(symbol);
            var marketPrice = _priceService.GetPrice(symbol);
            var currentProfitability = (heldQuantity * marketPrice) / costBasis;
            var targetProfitability = Math.Max(1.03M, currentProfitability) + .01M;
            var targetPrice = (targetProfitability * costBasis) / heldQuantity;
            var targetQuantityRatio = Math.Min((targetProfitability - 1) / 2, 1);
            var targetQuantity = targetQuantityRatio * heldQuantity;

            if (targetQuantity * targetPrice < 10M)
            {
                targetQuantity = 11M / targetPrice;
            }

            if (targetQuantity * targetPrice < 10M)
            {
                return;
            }
            await _orderService.Sell(symbol, targetPrice, targetQuantity);
        }
    }
}