using System.Threading.Tasks;
using BinanceBot.Configuration;

namespace BinanceBot.Service
{
    public class BinanceBotService
    {
        private readonly PriceService _priceService;
        private readonly OrderService _orderService;
        private readonly CostBasisService _costBasisService;
        private readonly SymbolService _symbolService;
        private readonly BinanceAccountService _accountService;
        public BinanceBotService(
            PriceService priceService, 
            OrderService orderService, 
            CostBasisService costBasisService, 
            SymbolService symbolService, 
            BinanceAccountService accountService)
        {
            _priceService = priceService;
            _orderService = orderService;
            _costBasisService = costBasisService;
            _symbolService = symbolService;
            _accountService = accountService;
        }

        public async Task Awaken()
        {
            await _symbolService.Awaken();
            await _accountService.Awaken();
            await Task.WhenAll(
                _priceService.Awaken(),
                _orderService.CancelAllOrders()
            );

            await _costBasisService.Awaken();

        }
    }
}