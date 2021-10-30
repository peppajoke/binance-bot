using System.Linq;
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
        private readonly DynamicSellService _dynamicSellService;
        private readonly OrderWatchService _orderWatchService;
        private readonly DynamicBuyService _dynamicBuyService;
        public BinanceBotService(
            PriceService priceService, 
            OrderService orderService, 
            CostBasisService costBasisService, 
            SymbolService symbolService, 
            BinanceAccountService accountService,
            DynamicSellService dynamicSellService,
            OrderWatchService orderWatchService,
            DynamicBuyService dynamicBuyService)
        {
            _priceService = priceService;
            _orderService = orderService;
            _costBasisService = costBasisService;
            _symbolService = symbolService;
            _accountService = accountService;
            _dynamicSellService = dynamicSellService;
            _orderWatchService = orderWatchService;
            _dynamicBuyService = dynamicBuyService;
        }

        public async Task Awaken()
        {
            await _symbolService.Awaken();
            await _accountService.Awaken();
            await Task.WhenAll(
                _priceService.Awaken()
            );

            await _costBasisService.Awaken();

            await Cycle();

            await Task.WhenAll(_orderWatchService.Awaken());
        }

        public async Task Cycle()
        {
            await _orderService.CancelAllOrders();
            foreach (var coin in _symbolService.GetAllCoins())
            {
                await _dynamicSellService.TrySell(coin);
                await Task.Delay(250);
            }

            foreach (var coin in _symbolService.GetAllCoins())
            {
                await _dynamicBuyService.TryBuy(coin);
                await Task.Delay(250);
                // binance doesn't like more than 100 orders in 10 seconds, so we add a little delay here.
            }
        }
    }
}