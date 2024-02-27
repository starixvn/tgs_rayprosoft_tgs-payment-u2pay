using System.Text.Json;
using Tgs.Payment.Protos;
using Tgs.Payment.U2Pay.Client;
using Valhalla.MessageQueue;

namespace Tgs.Payment.U2Pay.MessageQueue;

internal class GetBalanceProcessor(
    U2PayClient u2PayClient)
    : IMessageProcessor<PaymentBalanceRequest, PaymentBalanceResponse>
{
    public async ValueTask<PaymentBalanceResponse> HandleAsync(
        string subject,
        PaymentBalanceRequest data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default)
    {
        var settings = JsonSerializer.Deserialize<BrandPaymentMerchantInfo>(
            data.BrandPaymentSettings,
            U2PayUtility.SerializerOptions)!;

        var result = await u2PayClient.QueryBalanceAsync(
            settings.MerchantNo,
            settings.AppSecret,
            settings.PrivateKey,
            cancellationToken).ConfigureAwait(false);

        return new PaymentBalanceResponse
        {
            Balance = result
        };
    }
}
