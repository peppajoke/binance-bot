using System;
using System.IO;
using BinanceBot.Service;
using NUnit.Framework;

namespace BinanceBot.Tests
{
    public class BotConfigurationServiceTest
    {
        [SetUp]
        public void Setup()
        {
            
        }

        [Test]
        public void TestSaveAndLoad()
        {
            var svc = new BotConfigurationService();

            var output = new StringWriter();
            Console.SetOut(output);

            var input = new StringReader(@"ALFCOIN,BURTCOIN
APITHANG
SECRETSAREFUN");
            Console.SetIn(input);

            svc.BuildConfig();

            var config = svc.GetConfig();
            Assert.AreEqual(config.BinanceApiKey, "APITHANG");
            Assert.AreEqual(config.BinanceApiSecret, "SECRETSAREFUN");
            Assert.AreEqual(config.HoardCoins, new string[] {"ALFCOIN", "BURTCOIN"});
        }
    }
}