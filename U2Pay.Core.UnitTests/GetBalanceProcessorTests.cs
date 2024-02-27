using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RichardSzalay.MockHttp;
using Tgs.Payment.Protos;
using Tgs.Payment.U2Pay;
using Tgs.Payment.U2Pay.MessageQueue;

namespace U2Pay.Core.UnitTests;
public class GetBalanceProcessorTests
{
    [Fact]
    public async Task GetBalanceProcessor_取得商戶餘額()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://xr.hmpas.com");

        var fakeTimeProvider = Substitute.For<TimeProvider>();

        var fakeU2PayClient = new U2PayClient(
            httpClient,
            new U2PayHasher(NullLogger<U2PayHasher>.Instance),
            fakeTimeProvider,
            NullLogger<U2PayClient>.Instance);

        var sut = new GetBalanceProcessor(fakeU2PayClient);

        var request = new PaymentBalanceRequest
        {
            BrandPaymentSettings = """
                {
                    "merchantNo": "110630",
                    "appSecret": "ageDUOgK6kumEoTb2tjraA",
                    "privateKey": "ClLtElIsn0O2VDFYYbTMZg"
                }
                """
        };

        var merchantNo = "110630";
        var appSecret = "ageDUOgK6kumEoTb2tjraA";        

        _ = fakeTimeProvider.GetUtcNow()
            .Returns(DateTimeOffset.FromUnixTimeSeconds(1701847072));

        _ = mockHttp
            .Expect(HttpMethod.Post, "https://xr.hmpas.com/payout/balance")
            .WithHeaders(new Dictionary<string, string>
            {
                { "Content-Type","application/x-www-form-urlencoded" }
            })
            .WithContent(await new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "merchantNo", merchantNo },
                { "time", "1701847072" },
                { "appSecret", appSecret },
                { "sign", "90464B09231DA5CF96B7B016125BF719" }
            }).ReadAsStringAsync().ConfigureAwait(false))
            .Respond(HttpStatusCode.OK, new StringContent(
                """
                {
                    "code": 0,
                    "text": "success",
                    "balance": 0.0,
                    "balance0": 0.0,
                    "balance1": 0.0,
                    "balance2": 0.0,
                    "balance3": 0.0,
                    "balance4": 0.0,
                    "balance5": 0.0,
                    "datetime": "2023-12-06 15:33:36"
                }
                """));

        // Act
        var actual = await sut.HandleAsync("test", request, null);

        // Assert
        Assert.Equal(0M, actual.Balance.ToDecimal());
    }
}
