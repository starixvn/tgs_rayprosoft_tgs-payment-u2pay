using System.Text.Json;
using Tgs.Payment.Clients;
using Tgs.Payment.U2Pay.Client;
using Tgs.ServiceUtility;

namespace Tgs.Payment.U2Pay.ApplicationServices;

internal class GetWithdrawFormQueryService : IGetService<GetWithdrawFormQuery, WithdrawForm?>
{
    private readonly IPaymentServiceClient _paymentServiceClient;

    public GetWithdrawFormQueryService(IPaymentServiceClient paymentServiceClient)
    {
        _paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));
    }

    public async ValueTask<WithdrawForm?> GetAsync(GetWithdrawFormQuery query, CancellationToken cancellationToken = default)
    {
        var data = await _paymentServiceClient
            .GetWithdrawFormAsync(query.FormId, cancellationToken)
        .ConfigureAwait(false);

        return data == null
            ? null
            : new WithdrawForm(
                data.Id,
                data.UserId,
                data.UserType,
                data.BrandId,
                data.BrandPaymentId,
                data.Amount,
                data.Status,
                data.BankInfoId,
                string.IsNullOrEmpty(data.ApplyInfo)
        ? null
        : JsonSerializer.Deserialize<U2PayWithdrawApplyInfo>(data.ApplyInfo, U2PayUtility.SerializerOptions),
                data.PaymentReturn);
    }
}
