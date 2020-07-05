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
        public void UpdateServerTime_ShouldReturnServerTime_WhenServerTimeIsValid()
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
