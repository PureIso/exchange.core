using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using exchange.binance;
using exchange.coinbase;
using exchange.core;
using exchange.core.models;
using exchange.core.Models;
using exchange.service;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace exchange.test
{
    [TestClass]
    public class BinanceUnitTest
    {
        #region Fields
        private ExchangeSettings _exchangeSettings;
        private Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private Request _request;
        #endregion

        [TestInitialize()]
        public void Initialize()
        {
            _exchangeSettings = new ExchangeSettings
            {
                APIKey = "api_key",
                PassPhrase = "pass_phrase",
                Secret = "NiWaGaqmhB3lgI/tQmm/gQ==",
                EndpointUrl = "https://api.binance.com",
                Uri = "wss://stream.binance.com:9443"
            };
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            _request = new Request(_exchangeSettings.EndpointUrl, "GET", "");
        }

        [TestMethod]
        public void UpdateServerTime_ShouldReturnServerTime()
        {
            //Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent($@"{{""serverTime"":1592395836992}}")
                }))
                .Verifiable();
            HttpClient httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            ConnectionAdapter connectionFactory = new ConnectionAdapter(httpClient, _exchangeSettings);
            Binance subjectUnderTest = new Binance(connectionFactory);
            //Act
            subjectUnderTest.UpdateTimeServerAsync().Wait();
            //Assert
            Assert.IsNotNull(subjectUnderTest.ServerTime);
            Assert.AreEqual(1592395836992, subjectUnderTest.ServerTime.ServerTimeLong);
            Assert.IsTrue(subjectUnderTest.ServerTime.GetDelay() > 0);
        }

        [TestMethod]
        public void UpdateExchangeInfo_ShouldReturnExchangeInfo()
        {
            //Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent($@"{{""timezone"": ""UTC"",
                    ""serverTime"": 1594060749279,
                    ""rateLimits"": [
                    {{
                    ""rateLimitType"": ""REQUEST_WEIGHT"",
                    ""interval"": ""MINUTE"",
                    ""intervalNum"": 1,
                    ""limit"": 1200}},
                    {{
                    ""rateLimitType"": ""ORDERS"",
                    ""interval"": ""SECOND"",
                    ""intervalNum"": 10,
                    ""limit"": 100}},
                    {{
                    ""rateLimitType"": ""ORDERS"",
                    ""interval"": ""DAY"",
                    ""intervalNum"": 1,
                    ""limit"": 200000}}],
                    ""exchangeFilters"": [],
                    ""symbols"": [
                    {{
                    ""symbol"": ""ETHBTC"",
                    ""status"": ""TRADING"",
                    ""baseAsset"": ""ETH"",
                    ""baseAssetPrecision"": 8,
                    ""quoteAsset"": ""BTC"",
                    ""quotePrecision"": 8,
                    ""quoteAssetPrecision"": 8,
                    ""baseCommissionPrecision"": 8,
                    ""quoteCommissionPrecision"": 8,
                    ""orderTypes"": [
                    ""LIMIT"",
                    ""LIMIT_MAKER"",
                    ""MARKET"",
                    ""STOP_LOSS_LIMIT"",
                    ""TAKE_PROFIT_LIMIT""
                        ],
                    ""icebergAllowed"": true,
                    ""ocoAllowed"": true,
                    ""quoteOrderQtyMarketAllowed"": true,
                    ""isSpotTradingAllowed"": true,
                    ""isMarginTradingAllowed"": true,
                    ""filters"": [
                    {{
                    ""filterType"": ""PRICE_FILTER"",
                    ""minPrice"": ""0.00000100"",
                    ""maxPrice"": ""100000.00000000"",
                    ""tickSize"": ""0.00000100""}},
                    {{
                    ""filterType"": ""PERCENT_PRICE"",
                    ""multiplierUp"": ""5"",
                    ""multiplierDown"": ""0.2"",
                    ""avgPriceMins"": 5
                }},
                {{
                    ""filterType"": ""LOT_SIZE"",
                    ""minQty"": ""0.00100000"",
                    ""maxQty"": ""100000.00000000"",
                    ""stepSize"": ""0.00100000""
                    }},
                    {{
                        ""filterType"": ""MIN_NOTIONAL"",
                        ""minNotional"": ""0.00010000"",
                        ""applyToMarket"": true,
                        ""avgPriceMins"": 5
                    }},
                    {{
                        ""filterType"": ""ICEBERG_PARTS"",
                        ""limit"": 10
                    }},
                    {{
                        ""filterType"": ""MARKET_LOT_SIZE"",
                        ""minQty"": ""0.00000000"",
                        ""maxQty"": ""15815.50494448"",
                        ""stepSize"": ""0.00000000""
                    }},
                    {{
                        ""filterType"": ""MAX_NUM_ALGO_ORDERS"",
                        ""maxNumAlgoOrders"": 5
                    }},
                    {{
                        ""filterType"": ""MAX_NUM_ORDERS"",
                        ""maxNumOrders"": 200
                    }}
                    ],
                    ""permissions"": [
                    ""SPOT"",
                    ""MARGIN""]}}]}}")
                }))
                .Verifiable();
            HttpClient httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            ConnectionAdapter connectionFactory = new ConnectionAdapter(httpClient, _exchangeSettings);
            Binance subjectUnderTest = new Binance(connectionFactory);
            //Act
            subjectUnderTest.UpdateExchangeInfoAsync().Wait();
            //Assert
            Assert.IsNotNull(subjectUnderTest.ExchangeInfo);
        }

        [TestMethod]
        public void UpdateAccounts_ShouldReturnAccounts_WhenAccountExists()
        {
            //Arrange
            //Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        $@"{{""serverTime"":1592395836992}}")
                }))
                .Verifiable();
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        $@"{{""makerCommission"": 10,""takerCommission"": 10,""buyerCommission"": 0,""sellerCommission"": 0,
                        ""canTrade"": true,""canWithdraw"": true,""canDeposit"": true,""updateTime"": 1561284536404,""accountType"": ""MARGIN"",
                        ""balances"": [
                        {{""asset"": ""BTC"",""free"": ""0.00000000"",""locked"": ""0.00000000""}},
                        {{""asset"": ""LTC"",""free"": ""0.00000000"",""locked"": ""0.00000000""}},
                        {{""asset"": ""ETH"",""free"": ""0.00000000"",""locked"": ""0.00000000""}}],
                        ""permissions"": [""SPOT""]}}")
                }))
                .Verifiable();
            HttpClient httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            ConnectionAdapter connectionFactory = new ConnectionAdapter(httpClient, _exchangeSettings);
            Binance subjectUnderTest = new Binance(connectionFactory);
            //Act
            subjectUnderTest.UpdateBinanceAccountAsync().Wait();
            Assert.IsNotNull(subjectUnderTest.BinanceAccount);
        }
    }
}
