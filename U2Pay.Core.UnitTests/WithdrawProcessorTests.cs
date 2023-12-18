using System.Net;
using System.Text.Json;
using Google.Protobuf;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using RichardSzalay.MockHttp;
using Tgs.Payment.Clients;
using Tgs.Payment.U2Pay;
using Tgs.Payment.U2Pay.Client;
using Tgs.Payment.U2Pay.MessageQueue;
using Tgs.Payment.Protos;
using Tgs.SerialNumberGenerator;

namespace U2Pay.Core.UnitTests;

public class WithdrawProcessorTests
{
    [Fact]
    public async Task 處理提款要求()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://xr.hmpas.com");
        var fakeSystemClock = Substitute.For<ISystemClock>();

        var fakeU2PayClient = new U2PayClient(
            httpClient,
            new U2PayHasher(NullLogger<U2PayHasher>.Instance),
            fakeSystemClock,
            NullLogger<U2PayClient>.Instance);

        var fakePaymentClient = Substitute.For<IPaymentClient>();
        var options = new U2PayOptions
        {
            CallbackBaseUrl = new Uri("https://dev-payment.rpgp.cc/u2pay"),
            PaymentApiUrl = new Uri("https://xr.hmpas.com"),
            PaymentServiceBaseUrl = new Uri("http://localhost:5541")
        };

        var sut = new WithdrawProcessor(
            fakeU2PayClient,
            fakePaymentClient,
            new OptionsWrapper<U2PayOptions>(options));

        var formId = Guid.Parse("2a1a56a5ebf54547acf92ada121acf7f");
        var merchantNo = "110630";
        var appSecret = "ageDUOgK6kumEoTb2tjraA";
        var privateKey = "ClLtElIsn0O2VDFYYbTMZg";

        var applySettings = new U2PayWithdrawSettings(
            merchantNo,
            appSecret,
            privateKey,
            formId,
            "NGUYEN",
            BankName: "VCB",
            BankAccount: "testBankAccount",
            ExchangeRate: 1m);

        var request = new WithdrawRequest
        {
            Amount = "100000",
            ApplyInfo = JsonSerializer.Serialize(applySettings, U2PayUtility.SerializerOptions)
        };

        _ = fakeSystemClock.UtcNow
            .Returns(DateTimeOffset.FromUnixTimeSeconds(1701935136));

        _ = mockHttp
            .Expect(HttpMethod.Post, "https://xr.hmpas.com/payout/create")
            .WithHeaders(new Dictionary<string, string>
            {
                { "Content-Type","application/x-www-form-urlencoded" }
            })
            .WithContent(await new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "merchantNo", merchantNo },
                { "orderNo", "2a1a56a5ebf54547acf92ada121acf7f" },
                { "amount", "100000" },
                { "name", "NGUYEN" },
                { "bankName", "VCB" },
                { "bankAccount","testBankAccount" },
                { "mobile","" },
                { "datetime", "2023-12-07 07:45:36" },
                { "notifyUrl", "https://dev-payment.rpgp.cc/u2pay/withdraw/callback" },
                { "reverseUrl", "https://dev-payment.rpgp.cc/u2pay/withdraw/callback" },
                { "extra", "" },
                { "time","1701935136" },
                { "appSecret", appSecret },
                { "sign", "E7633F7AC038E51626655BCC24432D3F" }
            }).ReadAsStringAsync().ConfigureAwait(false))
            .Respond(HttpStatusCode.OK, new StringContent(
                """
                {"code":0,"text":"success","tradeNo":"159421","orderNo":"2a1a56a5ebf54547acf92ada121acf7f","amount":100000,"checkName":null}
                """));

        // Act
        var actual = await sut.HandleAsync(request.ToByteArray());
        var response = WithdrawResponse.Parser.ParseFrom(actual.Span);
        var applyInfo = JsonSerializer.Deserialize<U2PayWithdrawApplyInfo>(response.Result, U2PayUtility.SerializerOptions);

        // Assert
        _ = fakePaymentClient.Received(1)
            .SaveWithdrawFormThirdPartyOrderNoAsync(
                Arg.Is(applySettings.FormId),
                Arg.Is("159421"));
        Assert.NotNull(applyInfo);
        Assert.Equal(applySettings, applyInfo.Settings);
    }

    [Fact]
    public async Task 處理提款要求_匯率轉換()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://xr.hmpas.com");
        var fakeSystemClock = Substitute.For<ISystemClock>();

        var fakeU2PayClient = new U2PayClient(
            httpClient,
            new U2PayHasher(NullLogger<U2PayHasher>.Instance),
            fakeSystemClock,
            NullLogger<U2PayClient>.Instance);

        var fakePaymentClient = Substitute.For<IPaymentClient>();
        var options = new U2PayOptions
        {
            CallbackBaseUrl = new Uri("https://dev-payment.rpgp.cc/u2pay"),
            PaymentApiUrl = new Uri("https://xr.hmpas.com"),
            PaymentServiceBaseUrl = new Uri("http://localhost:5541")
        };

        var sut = new WithdrawProcessor(
            fakeU2PayClient,
            fakePaymentClient,
            new OptionsWrapper<U2PayOptions>(options));

        var formId = Guid.Parse("2a1a56a5ebf54547acf92ada121acf7f");
        var merchantNo = "110630";
        var appSecret = "ageDUOgK6kumEoTb2tjraA";
        var privateKey = "ClLtElIsn0O2VDFYYbTMZg";

        var applySettings = new U2PayWithdrawSettings(
            merchantNo,
            appSecret,
            privateKey,
            formId,
            "NGUYEN",
            BankName: "VCB",
            BankAccount: "testBankAccount",
            ExchangeRate: 1000m);

        var request = new WithdrawRequest
        {
            Amount = "100",
            ApplyInfo = JsonSerializer.Serialize(applySettings, U2PayUtility.SerializerOptions)
        };

        _ = fakeSystemClock.UtcNow
            .Returns(DateTimeOffset.FromUnixTimeSeconds(1701935136));

        _ = mockHttp
            .Expect(HttpMethod.Post, "https://xr.hmpas.com/payout/create")
            .WithHeaders(new Dictionary<string, string>
            {
                { "Content-Type","application/x-www-form-urlencoded" }
            })
            .WithContent(await new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "merchantNo", merchantNo },
                { "orderNo", "2a1a56a5ebf54547acf92ada121acf7f" },
                { "amount", "100000" },
                { "name", "NGUYEN" },
                { "bankName", "VCB" },
                { "bankAccount","testBankAccount" },
                { "mobile","" },
                { "datetime", "2023-12-07 07:45:36" },
                { "notifyUrl", "https://dev-payment.rpgp.cc/u2pay/withdraw/callback" },
                { "reverseUrl", "https://dev-payment.rpgp.cc/u2pay/withdraw/callback" },
                { "extra", "" },
                { "time","1701935136" },
                { "appSecret", appSecret },
                { "sign", "E7633F7AC038E51626655BCC24432D3F" }
            }).ReadAsStringAsync().ConfigureAwait(false))
            .Respond(HttpStatusCode.OK, new StringContent(
                """
                {"code":0,"text":"success","tradeNo":"159421","orderNo":"2a1a56a5ebf54547acf92ada121acf7f","amount":100000,"checkName":null}
                """));

        // Act
        var actual = await sut.HandleAsync(request.ToByteArray());
        var response = WithdrawResponse.Parser.ParseFrom(actual.Span);
        var applyInfo = JsonSerializer.Deserialize<U2PayWithdrawApplyInfo>(response.Result, U2PayUtility.SerializerOptions);

        // Assert
        _ = fakePaymentClient.Received(1)
            .SaveWithdrawFormThirdPartyOrderNoAsync(
                Arg.Is(applySettings.FormId),
                Arg.Is("159421"));
        Assert.NotNull(applyInfo);
        Assert.Equal(applySettings, applyInfo.Settings);
    }
}
