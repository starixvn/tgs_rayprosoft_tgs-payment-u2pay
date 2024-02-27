using System.Text.Json;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.Payment.Clients;
using Tgs.Payment.U2Pay.Client;
using Tgs.Payment.U2Pay.MessageQueue;
using Tgs.Payment.Protos;

namespace U2Pay.Core.UnitTests;

public class DepositTimeoutCheckHandlerTests
{
    [Fact]
    public async Task 申請六十分鐘的單子會自動拒絕()
    {
        // Arrange
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();

        var fakePaymentClient = Substitute.For<IPaymentClient>();

        var sut = new DepositTimeoutCheckHandler(fakePaymentServiceClient, fakePaymentClient, NullLogger<DepositTimeoutCheckHandler>.Instance);

        var formId = Guid.Parse("13b03ac8926611ee94340242ac120003");
        var brandId = Guid.Parse("4e86719f-243f-46f6-b010-5c758b7285af");
        var request = new ScheduleRequest
        {
            FormId = formId,
            Subject = "tgs.payment.u2pay.deposit.schedule.reject",
            ScheduleTime = Timestamp.FromDateTime(DateTime.UtcNow)
        };

        var applyInfo = new U2PayDepositApplyInfo(
            new U2PayDepositSettings(
                "110630",
                "ageDUOgK6kumEoTb2tjraA",
                "ClLtElIsn0O2VDFYYbTMZg",
                formId,
                2,
                "AGR",
                ExchangeRate: 1m),
            new U2PayDepositResponse(
                1000000m,
                formId,
                new Uri("https://bank.xr.hmpas.com/?oid=888884\u0026c=-1162300592\u0026bc=AGR"),
                "888884"));

        _ = fakePaymentServiceClient.GetDepositFormAsync(Arg.Is(request.FormId.ToGuid()))
            .Returns(new DepositFormInfo
            {
                Id = formId,
                Amount = 1000000m,
                BrandId = brandId,
                ApplyInfo = JsonSerializer.Serialize(applyInfo, U2PayUtility.SerializerOptions)
            });

        // Act
        await sut.HandleAsync("test", request, null);

        // Assert
        _ = fakePaymentClient
            .Received(1)
            .RejectDepositAsync(Arg.Is(formId), Arg.Is(brandId));
    }
}
