using System.Text.Json;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Tgs.Payment.U2Pay.Client;
using Valhalla.MessageQueue;

namespace Tgs.Payment.U2Pay.MessageQueue;
internal class DepositProcessor : IMessageProcessor
{
    private readonly U2PayClient _u2PayClient;
    private readonly IPaymentClient _paymentClient;
    private readonly U2PayOptions _options;

    public DepositProcessor(
        U2PayClient u2PayClient,
        IPaymentClient paymentClient,
        IOptions<U2PayOptions> optionsAccessor)
    {
        _u2PayClient = u2PayClient ?? throw new ArgumentNullException(nameof(u2PayClient));
        _paymentClient = paymentClient ?? throw new ArgumentNullException(nameof(paymentClient));
        _options = optionsAccessor.Value;
    }

    public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var request = DepositRequest.Parser.ParseFrom(data.Span);

        var settings = JsonSerializer.Deserialize<U2PayDepositSettings>(
            request.ApplyInfo,
            U2PayUtility.SerializerOptions)!;

        var order = new DepositOrder(
            decimal.Parse(request.Amount) * settings.ExchangeRate,
            settings.FormId,
            new Uri($"{_options.CallbackBaseUrl}/deposit/callback"),
            settings.ChannelNo,
            settings.BankName);

        var depositResponse = await _u2PayClient.DepositAsync(
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

        await _paymentClient.ScheduleAsync(
            "tgs.payment.u2pay.deposit.schedule.reject",
            settings.FormId,
            DateTime.UtcNow.AddMinutes(11D),
            cancellationToken).ConfigureAwait(false);

        var msg = new DepositResponse
        {
            Result = JsonSerializer.Serialize(applyInfo, U2PayUtility.SerializerOptions)
        };

        return msg.ToByteArray();
    }
}
