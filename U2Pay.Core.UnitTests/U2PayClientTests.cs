using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RichardSzalay.MockHttp;
using Tgs.Payment.U2Pay;
using Tgs.SerialNumberGenerator;

namespace U2Pay.Core.UnitTests;

public class U2PayClientTests
{
    [Fact]
    public async Task 查詢商戶餘額()
    {
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://xr.hmpas.com");

        var signer = new U2PayHasher(NullLogger<U2PayHasher>.Instance);
        var fakeSystemClock = Substitute.For<ISystemClock>();

        var sut = new U2PayClient(httpClient, signer, fakeSystemClock, NullLogger<U2PayClient>.Instance);

        var merchantNo = "110630";
        var appSecret = "ageDUOgK6kumEoTb2tjraA";
        var privateKey = "ClLtElIsn0O2VDFYYbTMZg";

        _ = fakeSystemClock.UtcNow
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

        var actual = await sut.QueryBalanceAsync(merchantNo, appSecret, privateKey);

        Assert.Equal(0, actual);
    }

    [Fact]
    public async Task 存款()
    {
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://xr.hmpas.com");

        var signer = new U2PayHasher(NullLogger<U2PayHasher>.Instance);
        var fakeSystemClock = Substitute.For<ISystemClock>();

        var sut = new U2PayClient(httpClient, signer, fakeSystemClock, NullLogger<U2PayClient>.Instance);

        var merchantNo = "110630";
        var appSecret = "ageDUOgK6kumEoTb2tjraA";
        var privateKey = "ClLtElIsn0O2VDFYYbTMZg";
        var formId = Guid.Parse("ef828c68a93449519ffd5e7a457ac94b");

        var order = new DepositOrder(
            Amount: 1000000m,
            FormId: formId,
            CallbackUrl: new Uri("https://dev-payment.rpgp.cc/u2pay/deposit/callback"),
            ChannelNo: 2,
            BankName: "AGR");

        _ = fakeSystemClock.UtcNow
            .Returns(DateTimeOffset.FromUnixTimeSeconds(1701935918));

        _ = mockHttp
            .Expect(HttpMethod.Post, "https://xr.hmpas.com/order/create")
            .WithHeaders(new Dictionary<string, string>
            {
                { "Content-Type","application/x-www-form-urlencoded" }
            })
            .WithContent(await new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "merchantNo", merchantNo },
                { "orderNo", "ef828c68a93449519ffd5e7a457ac94b" },
                { "userNo", "" },
                { "channelNo", "2" },
                { "amount", "1000000" },
                { "discount", "" },
                { "bankName", "AGR" },
                { "extra", "" },
                { "datetime", "2023-12-07 07:58:38" },
                { "notifyUrl", "https://dev-payment.rpgp.cc/u2pay/deposit/callback"},
                { "time","1701935918" },
                { "appSecret", appSecret },
                { "sign", "A01FBBF8375BD2A0F09F8AB542AB2945" }
            }).ReadAsStringAsync().ConfigureAwait(false))
            .Respond(HttpStatusCode.OK, new StringContent(
                """
                {"code":0,"text":"success","tradeNo":"888884","orderNo":"ef828c68a93449519ffd5e7a457ac94b","userNo":null,"userName":null,"channelNo":2,"amount":1000000,"discount":0,"lucky":0,"payable":1000000,"kfNo":"F22B4","targetUrl":"https://bank.xr.hmpas.com/?oid=888884\u0026c=-1162300592\u0026bc=AGR","qrcode":"data:image/gif;base64, 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","extra":""}
                """));

        var actual = await sut.DepositAsync(merchantNo, appSecret, privateKey, order);

        Assert.Equal(1000000m, actual.Amount);
        Assert.Equal(formId, actual.FormId);
        Assert.Equal("888884", actual.ThirdPartyOrderNo);
        Assert.Equal(new Uri("https://bank.xr.hmpas.com/?oid=888884\u0026c=-1162300592\u0026bc=AGR"), actual.RedirectUrl);
    }

    [Fact]
    public async Task 查詢存款單()
    {
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://xr.hmpas.com");

        var signer = new U2PayHasher(NullLogger<U2PayHasher>.Instance);
        var fakeSystemClock = Substitute.For<ISystemClock>();

        var sut = new U2PayClient(httpClient, signer, fakeSystemClock, NullLogger<U2PayClient>.Instance);

        var merchantNo = "110630";
        var appSecret = "ageDUOgK6kumEoTb2tjraA";
        var privateKey = "ClLtElIsn0O2VDFYYbTMZg";
        var formId = Guid.Parse("fc20591611624e8fb67b050c2233b930");

        _ = fakeSystemClock.UtcNow
            .Returns(DateTimeOffset.FromUnixTimeSeconds(-62135596800));

        _ = mockHttp
            .Expect(HttpMethod.Post, "https://xr.hmpas.com/order/status")
            .WithHeaders(new Dictionary<string, string>
            {
                { "Content-Type","application/x-www-form-urlencoded" }
            })
            .WithContent(await new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "merchantNo", merchantNo },
                { "tradeNo", "" },
                { "orderNo", "fc20591611624e8fb67b050c2233b930" },
                { "time", "-62135596800" },
                { "appSecret", appSecret },
                { "sign", "4C3DF0BF9FF4515A54B0CFF3BB8EEEC2" }
            }).ReadAsStringAsync().ConfigureAwait(false))
            .Respond(HttpStatusCode.OK, new StringContent(
                """
                {"code":0,"text":"success","status":"PENDING","paid":100000,"tradeNo":"","orderNo":"fc20591611624e8fb67b050c2233b930"}
                """));

        var actual = await sut.QueryDepositFormStatusAsync(merchantNo, appSecret, privateKey, formId);

        Assert.Equal("PENDING", actual);
    }

    [Fact]
    public async Task 提款()
    {
        var mockHttp = new MockHttpMessageHandler();

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://xr.hmpas.com");

        var signer = new U2PayHasher(NullLogger<U2PayHasher>.Instance);
        var fakeSystemClock = Substitute.For<ISystemClock>();

        var sut = new U2PayClient(httpClient, signer, fakeSystemClock, NullLogger<U2PayClient>.Instance);

        var merchantNo = "110630";
        var appSecret = "ageDUOgK6kumEoTb2tjraA";
        var privateKey = "ClLtElIsn0O2VDFYYbTMZg";
        var formId = Guid.Parse("2a1a56a5ebf54547acf92ada121acf7f");

        var order = new WithdrawOrder(
            FormId: formId,
            Amount: 100000m,
            CallbackUrl: new Uri("https://dev-payment.rpgp.cc/u2pay/withdraw/callback"),
            ReceiverName: "NGUYEN",
            BankName: "VCB",
            BankAccount: "testBankAccount");

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

        var actual = await sut.WithdrawAsync(merchantNo, appSecret, privateKey, order);

        Assert.Equal(100000m, actual.Amount);
        Assert.Equal(formId, actual.FormId);
        Assert.Equal("159421", actual.ThirdPartyOrderNo);
    }
}
