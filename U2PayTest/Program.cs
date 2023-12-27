using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.Payment.Clients;
using Tgs.Payment.U2Pay;
using Tgs.Payment.U2Pay.Client;
using Tgs.SerialNumberGenerator;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddPaymentClient()
    .AddPhoenixToolkitsMessageQueue()
    .AddNatsMessageQueue(natsConfig => natsConfig
        .ConfigQueueOptions((opt, _) => opt.Url = "nats://localhost:4222"))
    .AddNatsGlobPatternExchange("*");

var app = builder.Build();

await app.StartAsync().ConfigureAwait(false);

var logger = app.Services.GetRequiredService<ILogger<Program>>();

var paymentClient = app.Services.GetRequiredService<IPaymentClient>();

var merchantNo = "110630";
string appSecret = "ageDUOgK6kumEoTb2tjraA";
var privateKey = "ClLtElIsn0O2VDFYYbTMZg";

//Deposit
//var depositSettings = new U2PayDepositSettings(merchantNo, appSecret, privateKey, Guid.NewGuid(), 2, "AGR");

//var depositReturn = await paymentClient.DepositAsync(
//    "tgs.payment.u2pay.deposit",
//    1000000m,
//    JsonSerializer.Serialize(depositSettings, U2PayUtility.SerializerOptions)).ConfigureAwait(false);

//logger.LogInformation("DepositReturn: {depositReturn}", depositReturn);

//Withdraw
//var withdrawSettings = new U2PayWithdrawSettings(
//    merchantNo,
//    appSecret,
//    privateKey,
//    Guid.NewGuid(),
//    "NGUYEN",
//    "VCB",
//    "testBankAccount",
//    ExchangeRate: 1m);

//var withdrawReturn = await paymentClient.WithdrawAsync(
//    "tgs.payment.u2pay.withdraw",
//    100000m,
//    JsonSerializer.Serialize(withdrawSettings, U2PayUtility.SerializerOptions)).ConfigureAwait(false);

//logger.LogInformation("WithdrawReturn: {withdrawReturn}", withdrawReturn);

//訂單查詢
//var httpClient = new HttpClient();
//httpClient.BaseAddress = new Uri("https://xr.hmpas.com");
//var fakeSystemClock = Substitute.For<ISystemClock>();
//var client = new U2PayClient(httpClient, new U2PayHasher(NullLogger<U2PayHasher>.Instance), fakeSystemClock, NullLogger<U2PayClient>.Instance);

//var queryReturn = await client.QueryDepositFormStatusAsync(merchantNo, appSecret, privateKey, Guid.Parse("fc20591611624e8fb67b050c2233b930"));
//logger.LogInformation("{return}", queryReturn);

//商戶餘額查詢
var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri("https://xr.hmpas.com");
var fakeSystemClock = Substitute.For<ISystemClock>();
var client = new U2PayClient(httpClient, new U2PayHasher(NullLogger<U2PayHasher>.Instance), fakeSystemClock, NullLogger<U2PayClient>.Instance);

var queryReturn = await client.QueryBalanceAsync(merchantNo, appSecret, privateKey);
logger.LogInformation("{return}", queryReturn);