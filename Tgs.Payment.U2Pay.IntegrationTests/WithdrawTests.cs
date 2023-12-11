using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Tgs.Payment.Clients;
using Tgs.Payment.U2Pay.Client;
using Xunit.Abstractions;

namespace Tgs.Payment.U2Pay.IntegrationTests;

public class WithdrawTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public WithdrawTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
    }

    [Fact]
    public async Task 代付回調_訂單狀態成功()
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

        var formId = Guid.Parse("2a1a56a5ebf54547acf92ada121acf7f");
        var brandId = Guid.NewGuid();
        var merchantNo = "110630";
        var appSecret = "ageDUOgK6kumEoTb2tjraA";
        var privateKey = "ClLtElIsn0O2VDFYYbTMZg";

        var settings = new U2PayWithdrawSettings(
            merchantNo,
            appSecret,
            privateKey,
            formId,
            "NGUYEN",
            "VCB",
            "testBankAccount");
        var withdrawResponse = new U2PayWithdrawResponse(formId, "159421");

        var applyInfo = new U2PayWithdrawApplyInfo(settings, withdrawResponse);

        _ = fakePaymentServiceClient.GetWithdrawFormAsync(
                Arg.Is(formId),
                Arg.Any<CancellationToken>())
            .Returns(new WithdrawFormInfo
            {
                Id = formId,
                BrandId = brandId,
                Status = PaymentFlowState.Approved,
                ApplyInfo = JsonSerializer.Serialize(applyInfo, U2PayUtility.SerializerOptions)
            });

        var dict = new Dictionary<string, string>
        {
            ["status"] = "PAID",
            ["tradeNo"] = "159421",
            ["orderNo"] = "2a1a56a5ebf54547acf92ada121acf7f",
            ["amount"] = "100000.00",
            ["name"] = "NGUYEN",
            ["bankName"] = "VIETCOMBANK",
            ["bankAccount"] = "testBankAccount",
            ["bankBranch"] = "",
            ["memo"] = "",
            ["mobile"] = "",
            ["fee"] = "0",
            ["extra"] = "",
            ["sign"] = "16C5A7B788D7838483E1FCF2C8031F12",
        };

        var content = new FormUrlEncodedContent(dict);

        var response = await httpClient.PostAsync("/withdraw/callback", content);

        var actual = await response.Content.ReadAsStringAsync();

        Assert.Equal("success", actual);
        _ = fakePaymentServiceClient.Received(1)
            .SubmitWithdrawFormStatusAsync(
                Arg.Is(formId),
                paymentReturn: Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>());

        _ = fakePaymentClient.Received(1)
            .PaidWithdrawAsync(
                Arg.Is(formId),
                Arg.Is(brandId),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task 代付回調_訂單狀態撤銷()
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

        var formId = Guid.Parse("8db32d4885b94041b0de9be071793145");
        var brandId = Guid.NewGuid();
        var merchantNo = "110630";
        var appSecret = "ageDUOgK6kumEoTb2tjraA";
        var privateKey = "ClLtElIsn0O2VDFYYbTMZg";

        var settings = new U2PayWithdrawSettings(
            merchantNo,
            appSecret,
            privateKey,
            formId,
            "NGUYEN",
            BankName: "VIETCOMBANK",
            BankAccount: "testBankAccount");
        var withdrawResponse = new U2PayWithdrawResponse(formId, "161844");

        var applyInfo = new U2PayWithdrawApplyInfo(settings, withdrawResponse);

        _ = fakePaymentServiceClient.GetWithdrawFormAsync(
                Arg.Is(formId),
                Arg.Any<CancellationToken>())
            .Returns(new WithdrawFormInfo
            {
                Id = formId,
                BrandId = brandId,
                Status = PaymentFlowState.Approved,
                ApplyInfo = JsonSerializer.Serialize(applyInfo, U2PayUtility.SerializerOptions)
            });

        var dict = new Dictionary<string, string>
        {
            ["status"] = "CANCELLED",
            ["tradeNo"] = "161844",
            ["orderNo"] = "8db32d4885b94041b0de9be071793145",
            ["amount"] = "100000.00",
            ["name"] = "NGUYEN",
            ["bankName"] = "VIETCOMBANK",
            ["bankAccount"] = "testBankAccount",
            ["bankBranch"] = "",
            ["memo"] = "",
            ["mobile"] = "",
            ["fee"] = "0",
            ["extra"] = "",
            ["sign"] = "D593836D2F27542B30B999DEF53B015E",
            ["reason"] = "",
        };

        var content = new FormUrlEncodedContent(dict);

        var response = await httpClient.PostAsync("/withdraw/callback", content);

        var actual = await response.Content.ReadAsStringAsync();

        Assert.Equal("success", actual);
        _ = fakePaymentServiceClient.Received(1)
            .SubmitWithdrawFormStatusAsync(
                Arg.Is(formId),
                paymentReturn: Arg.Any<string>(),
                cancellationToken: Arg.Any<CancellationToken>());

        _ = fakePaymentClient.Received(1)
            .RejectWithdrawAsync(
                Arg.Is(formId),
                Arg.Is(brandId),
                Arg.Any<CancellationToken>());
    }
}
