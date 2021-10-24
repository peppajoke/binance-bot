using NUnit.Framework;

namespace BinanceBot.Tests
{
    public class BinanceBotServiceTest
    {
        [SetUp]
        public void Setup()
        {
            var svc = new BinanceBotService();
        }

        [Test]
        public void Test1()
        {
            Assert.Pass();
        }
    }
}