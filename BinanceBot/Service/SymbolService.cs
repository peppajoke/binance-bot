using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Binance.Net.Interfaces;
using Binance.Net.Interfaces.SubClients.Spot;

namespace BinanceBot.Service
{
    public class SymbolService
    {
        private readonly IBinanceClient _client;
        private HashSet<string> _supportedCoins = new HashSet<string>();
        private Dictionary<string, int> _pricePrecision = new Dictionary<string, int>();
        private Dictionary<string, int> _quantityPrecision = new Dictionary<string, int>();

        public SymbolService(IBinanceClient client)
        {
            _client = client;
        }

        public async Task Awaken()
        {
            await Task.WhenAll(InitializeSymbolPrecision(), InitializeSupportedCoins());   
        }

        private async Task InitializeSymbolPrecision()
        {
            var symbols = (await _client.Spot.System.GetExchangeInfoAsync()).Data.Symbols;
            foreach(var symbol in symbols)
            {
                var cleanSymbol = symbol.Name.Replace("USD", "");
                if (symbol.QuoteAsset == "USD")
                {
                    
                    var stepSize = symbol.LotSizeFilter.StepSize;
            
                    var quantityPrecision = 0;
                    while(stepSize != 1)
                    {
                        quantityPrecision++;
                        stepSize *= 10;
                    }

                    var priceStepSize = symbol.PriceFilter.TickSize;
                    
                    var pricePrecision = 0;
                    while(priceStepSize != 1)
                    {
                        pricePrecision++;
                        priceStepSize *= 10;
                    }
                    _pricePrecision[cleanSymbol] = pricePrecision;
                    _quantityPrecision[cleanSymbol] = quantityPrecision;
                }
            }
        }

        private async Task InitializeSupportedCoins()
        {
            var exchangeInfo = await _client.Spot.System.GetExchangeInfoAsync();
            _supportedCoins = exchangeInfo.Data.Symbols.Where(x => x.QuoteAsset == "USD" && !x.BaseAsset.Contains("USD")).Select(x => x.BaseAsset).ToHashSet();
        }

        public IEnumerable<string> GetAllCoins()
        {
            return _supportedCoins;
        }

        public int GetPricePrecision(string symbol)
        {
            return _pricePrecision[symbol];
        }

        public int GetQuantityPrecision(string symbol)
        {
            return _quantityPrecision[symbol];
        }
    }
}