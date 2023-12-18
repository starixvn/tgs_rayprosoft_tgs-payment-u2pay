using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Tgs.Payment.Clients;
using Tgs.Payment.U2Pay.Client;
using Xunit.Abstractions;

namespace Tgs.Payment.U2Pay.IntegrationTests;

public class DepositTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public DepositTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
    }

    [Fact]
    public async Task 代收回調_訂單狀態成功()
    {
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var fakePaymentClient = Substitute.For<IPaymentClient>();

        await using var web = U2PayTestHost.Factory
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureLogging(logging => logging.AddXUnit(_testOutputHelper))
                .ConfigureServices(services => services
                    .AddSingleton(fakePaymentServiceClient)
                    .AddSingleton(fakePaymentClient)));

        var httpClient = web.CreateClient();

        var formId = Guid.Parse("ef828c68a93449519ffd5e7a457ac94b");
        var brandId = Guid.NewGuid();
        var merchantNo = "110630";
        var appSecret = "ageDUOgK6kumEoTb2tjraA";
        var privateKey = "ClLtElIsn0O2VDFYYbTMZg";

        var dict = new Dictionary<string, string>
        {
            ["status"] = "PAID",
            ["tradeNo"] = "888884",
            ["orderNo"] = "ef828c68a93449519ffd5e7a457ac94b",
            ["userNo"] = "",
            ["userName"] = "",
            ["channelNo"] = "2",
            ["amount"] = "1000000.00",
            ["discount"] = "0.00",
            ["lucky"] = "0.00",
            ["paid"] = "1000000.00",
            ["extra"] = "",
            ["sign"] = "285D877C88C73F77D4B83459D4953CE1",
        };

        var content = new FormUrlEncodedContent(dict);

        var applyInfo = new U2PayDepositApplyInfo(
            new U2PayDepositSettings(
                merchantNo,
                appSecret,
                privateKey,
                formId,
                2,
                "AGR",
                ExchangeRate: 1m),
             new U2PayDepositResponse(
                1000000m,
                formId,
                new Uri("https://bank.xr.hmpas.com/?oid=888884\u0026c=-1162300592\u0026bc=AGR"),
                "888884"));

        _ = fakePaymentServiceClient.GetDepositFormAsync(Arg.Is(formId), Arg.Any<CancellationToken>())
            .Returns(new DepositFormInfo
            {
                Id = formId,
                BrandId = brandId,
                Status = PaymentFlowState.WaitPayment,
                Amount = 1000000m,
                ThirdPartyOrderNo = "888884",
                ApplyInfo = JsonSerializer.Serialize(applyInfo, U2PayUtility.SerializerOptions)
            });

        var response = await httpClient.PostAsync("/deposit/callback", content);

        var actual = await response.Content.ReadAsStringAsync();

        Assert.Equal("success", actual);

        var paymentReturn = new U2PayDepositReturn()
        {
            Status = "PAID",
            FormId = formId,
            ThirdPartyOrderNo = "888884",
            RealAmount = decimal.Parse("1000000.00"),
            OrderAmount = decimal.Parse("1000000.00"),
            Sign = dict["sign"],
            AfterExchangeRealAmount = decimal.Parse("1000000.00"),
            AfterExchangeOrderAmount = decimal.Parse("1000000.00")
        };

        _ = fakePaymentServiceClient.Received(1)
            .SubmitDepositFormStatusAsync(
                Arg.Is(formId),
                paymentReturn: Arg.Is(JsonSerializer.Serialize(paymentReturn, U2PayUtility.SerializerOptions)),
                cancellationToken: Arg.Any<CancellationToken>());

        _ = fakePaymentClient.Received(1)
            .ApproveDepositAsync(
                Arg.Is(formId),
                Arg.Is(brandId),
                Arg.Is(paymentReturn.AfterExchangeRealAmount),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task 代收回調_訂單狀態成功_匯率轉換()
    {
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
        var fakePaymentClient = Substitute.For<IPaymentClient>();

        await using var web = U2PayTestHost.Factory
            .WithWebHostBuilder(hostBuilder => hostBuilder
                .ConfigureLogging(logging => logging.AddXUnit(_testOutputHelper))
                .ConfigureServices(services => services
                    .AddSingleton(fakePaymentServiceClient)
                    .AddSingleton(fakePaymentClient)));

        var httpClient = web.CreateClient();

        var formId = Guid.Parse("ef828c68a93449519ffd5e7a457ac94b");
        var brandId = Guid.NewGuid();
        var merchantNo = "110630";
        var appSecret = "ageDUOgK6kumEoTb2tjraA";
        var privateKey = "ClLtElIsn0O2VDFYYbTMZg";

        var dict = new Dictionary<string, string>
        {
            ["status"] = "PAID",
            ["tradeNo"] = "888884",
            ["orderNo"] = "ef828c68a93449519ffd5e7a457ac94b",
            ["userNo"] = "",
            ["userName"] = "",
            ["channelNo"] = "2",
            ["amount"] = "1000000.00",
            ["discount"] = "0.00",
            ["lucky"] = "0.00",
            ["paid"] = "1000000.00",
            ["extra"] = "",
            ["sign"] = "285D877C88C73F77D4B83459D4953CE1",
        };

        var content = new FormUrlEncodedContent(dict);

        var applyInfo = new U2PayDepositApplyInfo(
            new U2PayDepositSettings(
                merchantNo,
                appSecret,
                privateKey,
                formId,
                2,
                "AGR",
                ExchangeRate: 1000m),
             new U2PayDepositResponse(
                1000000m,
                formId,
                new Uri("https://bank.xr.hmpas.com/?oid=888884\u0026c=-1162300592\u0026bc=AGR"),
                "888884"));

        _ = fakePaymentServiceClient.GetDepositFormAsync(Arg.Is(formId), Arg.Any<CancellationToken>())
            .Returns(new DepositFormInfo
            {
                Id = formId,
                BrandId = brandId,
                Status = PaymentFlowState.WaitPayment,
                Amount = 1000m,
                ThirdPartyOrderNo = "888884",
                ApplyInfo = JsonSerializer.Serialize(applyInfo, U2PayUtility.SerializerOptions)
            });

        var response = await httpClient.PostAsync("/deposit/callback", content);

        var actual = await response.Content.ReadAsStringAsync();

        Assert.Equal("success", actual);

        var paymentReturn = new U2PayDepositReturn()
        {
            Status = "PAID",
            FormId = formId,
            ThirdPartyOrderNo = "888884",
            RealAmount = decimal.Parse("1000000.00"),
            OrderAmount = decimal.Parse("1000000.00"),
            Sign = dict["sign"],
            AfterExchangeRealAmount = decimal.Parse("1000.00"),
            AfterExchangeOrderAmount = decimal.Parse("1000.00")
        };

        _ = fakePaymentServiceClient.Received(1)
            .SubmitDepositFormStatusAsync(
                Arg.Is(formId),
                paymentReturn: Arg.Is(JsonSerializer.Serialize(paymentReturn, U2PayUtility.SerializerOptions)),
                cancellationToken: Arg.Any<CancellationToken>());

        _ = fakePaymentClient.Received(1)
            .ApproveDepositAsync(
                Arg.Is(formId),
                Arg.Is(brandId),
                Arg.Is(paymentReturn.AfterExchangeRealAmount),
                Arg.Any<CancellationToken>());
    }
}
