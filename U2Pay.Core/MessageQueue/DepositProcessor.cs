using System.Text.Json;
using Microsoft.Extensions.Options;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Tgs.Payment.U2Pay.Client;
using Valhalla.MessageQueue;

namespace Tgs.Payment.U2Pay.MessageQueue;

internal class DepositProcessor(
    U2PayClient u2PayClient,
    IPaymentClient paymentClient,
    IOptions<U2PayOptions> optionsAccessor)
    : IMessageProcessor<DepositRequest, DepositResponse>
{
    private readonly U2PayOptions _options = optionsAccessor.Value;

    public async ValueTask<DepositResponse> HandleAsync(
        string subject,
        DepositRequest data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default)
    {
        var settings = JsonSerializer.Deserialize<U2PayDepositSettings>(
            data.ApplyInfo,
            U2PayUtility.SerializerOptions)!;

        var order = new DepositOrder(
            decimal.Parse(data.Amount) * settings.ExchangeRate,
            settings.FormId,
            new Uri($"{_options.CallbackBaseUrl}/deposit/callback"),
            settings.ChannelNo,
            settings.BankName);

        var depositResponse = await u2PayClient.DepositAsync(
            settings.MerchantNo,
            settings.AppSecret,
            settings.PrivateKey,
            order,
            cancellationToken).ConfigureAwait(false);

        var applyInfo = new U2PayDepositApplyInfo(
            settings,
            new U2PayDepositResponse(
                depositResponse.Amount,
                depositResponse.FormId,
                depositResponse.RedirectUrl,
                depositResponse.ThirdPartyOrderNo));

        await paymentClient.ScheduleAsync(
            "tgs.payment.u2pay.deposit.schedule.reject",
            settings.FormId,
            DateTime.UtcNow.AddMinutes(60D),
            cancellationToken).ConfigureAwait(false);

        return new DepositResponse
        {
            Result = JsonSerializer.Serialize(applyInfo, U2PayUtility.SerializerOptions)
        };
    }
}
