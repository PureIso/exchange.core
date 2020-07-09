﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using exchange.binance;
using exchange.core;
using exchange.core.models;
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
        #endregion

        [TestInitialize]
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
        [TestMethod]
        public void UpdateProductOrderBook_ShouldReturnOrderBook_WhenProductExists()
        {
            //Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        $@"{{""lastUpdateId"": 77366060,
                            ""bids"": [
                            [""8241.75000000"",""0.03877400""],[""8240.72000000"",""0.00488600""],[""8237.41000000"",""0.06057400""],[""8233.08000000"",""0.06218100""],
                            [""8228.42000000"",""0.12178900""],[""8227.54000000"",""0.14200000""],[""8225.16000000"",""0.07890000""],[""8223.75000000"",""0.15092500""],
                            [""8222.48000000"",""0.34890000""],[""8219.08000000"",""0.12264200""],[""8218.30000000"",""0.00158000""],[""8215.32000000"",""0.06045600""],
                            [""8214.42000000"",""0.13581600""],[""8209.89000000"",""0.00133900""],[""8209.76000000"",""0.36892100""],[""8209.45000000"",""0.22500000""],
                            [""8207.76000000"",""2.16220000""],[""8207.51000000"",""0.00189800""],[""8205.53000000"",""0.00827300""],[""8205.10000000"",""0.24619600""]],
                            ""asks"": [
                            [""8243.05000000"",""0.00488100""],[""8252.34000000"",""0.00395500""],[""8254.10000000"",""0.33372100""],[""8254.72000000"",""0.16300000""],
                            [""8255.00000000"",""0.21194800""],[""8255.01000000"",""0.00732400""],[""8257.02000000"",""0.11936300""],[""8257.70000000"",""0.55723800""],
                            [""8259.84000000"",""0.15700000""],[""8260.00000000"",""0.05294700""],[""8260.78000000"",""0.15030900""],[""8262.30000000"",""0.00608300""],
                            [""8263.96000000"",""0.00137700""],[""8264.54000000"",""0.21096800""],[""8264.57000000"",""0.00999100""],[""8266.08000000"",""0.00144300""],
                            [""8267.76000000"",""0.00133500""],[""8268.31000000"",""0.11518000""],[""8272.07000000"",""0.33551200""],[""8274.12000000"",""0.22590000""]]}}")
                }))
                .Verifiable();
            HttpClient httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            ConnectionAdapter connectionFactory = new ConnectionAdapter(httpClient, _exchangeSettings);
            Binance subjectUnderTest = new Binance(connectionFactory);
            Product product = new Product {ID = "BTCEUR"};

            //Act
            subjectUnderTest.UpdateProductOrderBookAsync(product,20).Wait();
            //Assert
            Assert.IsNotNull(subjectUnderTest.OrderBook);
            Assert.AreEqual(20, subjectUnderTest.OrderBook.Bids.ToOrderList().Count);
            Assert.AreEqual(20, subjectUnderTest.OrderBook.Asks.ToOrderList().Count);
            Assert.AreEqual((decimal)8241.75000000, subjectUnderTest.OrderBook.Bids.ToOrderList()[0].Price.ToDecimal());
            Assert.AreEqual((decimal)0.00488100, subjectUnderTest.OrderBook.Asks.ToOrderList()[0].Quantity);
        }
        [TestMethod]
        public void UpdateProductHistoricCandles_ShouldReturnOrderBook_WhenAccountExists()
        {
            //Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        $@"[
                    [1594131000000,""8244.44000000"",""8247.71000000"",""8234.56000000"",""8247.53000000"",""0.57541000"",1594131299999,""4743.74677830"",29,""0.51348500"",""4233.45949059"",""0""],
                    [1594131300000,""8251.92000000"",""8263.73000000"",""8235.49000000"",""8261.31000000"",""0.73944300"",1594131599999,""6104.99268016"",73,""0.44151600"",""3646.66755442"",""0""],
                    [1594131600000,""8258.99000000"",""8266.70000000"",""8253.56000000"",""8265.01000000"",""0.21730200"",1594131899999,""1795.04159624"",61,""0.12557200"",""1037.27246541"",""0""]]")
                }))
                .Verifiable();
            HttpClient httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            ConnectionAdapter connectionFactory = new ConnectionAdapter(httpClient, _exchangeSettings);
            Binance subjectUnderTest = new Binance(connectionFactory);
            Product product = new Product
            {
                ID = "BTCEUR"
            };
            DateTime startingDateTime = new DateTime(2015, 4, 23).Date.ToUniversalTime();
            DateTime endingDateTime = startingDateTime.AddMonths(6).ToUniversalTime();
            //Act
            subjectUnderTest.UpdateProductHistoricCandlesAsync(product, startingDateTime, endingDateTime).Wait();
            //Assert
            Assert.IsNotNull(subjectUnderTest.HistoricRates);
            Assert.AreEqual(3, subjectUnderTest.HistoricRates.Count);
            Assert.AreEqual((decimal)8234.56000000, subjectUnderTest.HistoricRates[0].Low);
            Assert.AreEqual((decimal)0.57541000, subjectUnderTest.HistoricRates[0].Volume);
        }
        [TestMethod]
        public void UpdateTickers_ShouldReturnProductsAndTickers_WhenProductsAndTickersExists()
        {
            //Arrange
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        $@"{{""symbol"":""BTCEUR"",""price"":""8287.76000000""}}")
                }))
                .Verifiable();
            HttpClient httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            ConnectionAdapter connectionFactory = new ConnectionAdapter(httpClient, _exchangeSettings);
            Binance subjectUnderTest = new Binance(connectionFactory);
            List<Product> products = new List<Product>
            {
                new Product {ID = "BTCEUR"},
            };
            //Act
            subjectUnderTest.UpdateTickersAsync(products).Wait();
            //Assert
            Assert.IsNotNull(subjectUnderTest.Tickers);
            Assert.AreEqual(1, subjectUnderTest.Tickers.Count);
            Assert.AreEqual("BTCEUR", subjectUnderTest.Tickers[0].ProductID);
            Assert.AreEqual((decimal)8287.76000000, subjectUnderTest.Tickers[0].Price.ToDecimal());
            Assert.IsNotNull(subjectUnderTest.CurrentPrices);
            Assert.AreEqual(subjectUnderTest.CurrentPrices[subjectUnderTest.Tickers[0].ProductID], subjectUnderTest.Tickers[0].Price.ToDecimal());
        }
    }
}