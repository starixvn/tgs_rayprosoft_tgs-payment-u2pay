using System.Text.Json;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Tgs.Payment.U2Pay.Client;
using Valhalla.MessageQueue;

namespace Tgs.Payment.U2Pay.MessageQueue;

internal class WithdrawProcessor : IMessageProcessor
{
    private readonly U2PayClient _u2PayClient;
    private readonly IPaymentClient _paymentClient;
    private readonly U2PayOptions _options;

    public WithdrawProcessor(
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
        var request = WithdrawRequest.Parser.ParseFrom(data.Span);
        var settings = JsonSerializer.Deserialize<U2PayWithdrawSettings>(request.ApplyInfo, U2PayUtility.SerializerOptions)!;

        var order = new WithdrawOrder(
            settings.FormId,
            decimal.Parse(request.Amount) * settings.ExchangeRate,
            new Uri($"{_options.CallbackBaseUrl}/withdraw/callback"),
            settings.ReceiverName,
            settings.BankName,
            settings.BankAccount);

        var withdrawResponse = await _u2PayClient.WithdrawAsync(
            settings.MerchantNo,
            settings.AppSecret,
            settings.PrivateKey,
            order,
            cancellationToken).ConfigureAwait(false);

        var applyInfo = new U2PayWithdrawApplyInfo(
            settings,
            new U2PayWithdrawResponse(withdrawResponse.FormId, withdrawResponse.ThirdPartyOrderNo));

        await _paymentClient.SaveWithdrawFormThirdPartyOrderNoAsync(
            settings.FormId,
            withdrawResponse.ThirdPartyOrderNo,
            cancellationToken);

        var msg = new WithdrawResponse
        {
            Result = JsonSerializer.Serialize(applyInfo, U2PayUtility.SerializerOptions)
        };

        return msg.ToByteArray();
    }
}
