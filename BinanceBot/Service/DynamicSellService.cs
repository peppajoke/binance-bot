namespace BinanceBot.Service
{
    public class DynamicSellService
    {
        private readonly BinanceAccountService _accountService;
        private readonly OrderService _orderService;
        private readonly PriceService _priceService;

        public DynamicSellService(BinanceAccountService accountService, OrderService orderService, PriceService priceService)
        {
            _accountService = accountService;
            _orderService = orderService;
            _priceService = priceService;
        }

        //public async Task TrySell(string symbol)
        //{

        //}
    }
}