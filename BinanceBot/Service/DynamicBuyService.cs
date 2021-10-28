using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BinanceBot.Service
{
    public class DynamicBuyService
    {
        private readonly BinanceAccountService _accountService;
        private readonly OrderService _orderService;
        private PriceService _priceService;
        private readonly CostBasisService _costBasisService;

        public DynamicBuyService(BinanceAccountService accountService, OrderService orderService, CostBasisService costBasisService, PriceService priceService)
        {
            _accountService = accountService;
            _orderService = orderService;
            _costBasisService = costBasisService;
            _priceService = priceService;
        }

        public async Task TryBuy(string symbol)
        {
            Buy(symbol);
        }

        private async Task Buy(string symbol)
        {
            var costBasis = _costBasisService.GetCostBasis(symbol);
            var cash = _accountService.GetAvailableCash();
            var marketPrice = _priceService.GetPrice(symbol);
            if (cash < 11)
            {
                return;
            }
            var heldQuantity = _accountService.GetHeldQuantity(symbol);
            decimal targetPrice;
            if (costBasis > 0)
            {
                var currentProfitability = (heldQuantity * marketPrice) / costBasis;
                targetPrice = ((currentProfitability * costBasis) / heldQuantity) * .99M;
            }
            else
            {
                targetPrice = marketPrice * .99M;
            }

            var targetQuantity = 15 / targetPrice;

            await _orderService.Buy(symbol, targetPrice, targetQuantity);
            await Task.Delay(1000);
        }
    }
}