using System.Text.Json;
using Microsoft.Extensions.Options;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Tgs.Payment.U2Pay.Client;
using Valhalla.MessageQueue;

namespace Tgs.Payment.U2Pay.MessageQueue;

internal class WithdrawProcessor(
    U2PayClient u2PayClient,
    IPaymentClient paymentClient,
    IOptions<U2PayOptions> optionsAccessor) : IMessageProcessor<WithdrawRequest, WithdrawResponse>
{
    private readonly U2PayOptions _options = optionsAccessor.Value;

    public async ValueTask<WithdrawResponse> HandleAsync(
        string subject,
        WithdrawRequest data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default)
    {
        var settings = JsonSerializer.Deserialize<U2PayWithdrawSettings>(data.ApplyInfo, U2PayUtility.SerializerOptions)!;

        var order = new WithdrawOrder(
            settings.FormId,
            decimal.Parse(data.Amount) * settings.ExchangeRate,
            new Uri(_options.CallbackBaseUrl,"withdraw/callback"),
            settings.ReceiverName,
            settings.BankName,
            settings.BankAccount);

        var withdrawResponse = await u2PayClient.WithdrawAsync(
            settings.MerchantNo,
            settings.AppSecret,
            settings.PrivateKey,
            order,
            cancellationToken).ConfigureAwait(false);

        var applyInfo = new U2PayWithdrawApplyInfo(
            settings,
            new U2PayWithdrawResponse(withdrawResponse.FormId, withdrawResponse.ThirdPartyOrderNo));

        await paymentClient.SaveWithdrawFormThirdPartyOrderNoAsync(
            settings.FormId,
            withdrawResponse.ThirdPartyOrderNo,
            cancellationToken);

        return new WithdrawResponse
        {
            Result = JsonSerializer.Serialize(applyInfo, U2PayUtility.SerializerOptions)
        };
    }
}
