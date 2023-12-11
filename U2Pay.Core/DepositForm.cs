using Tgs.Payment.Clients;
using Tgs.Payment.U2Pay.Client;

namespace Tgs.Payment.U2Pay;
public record DepositForm(
    Guid Id,
    string PlayerCode,
    Guid BrandId,
    Guid BrandPaymentId,
    decimal Amount,
    PaymentFlowState Status,
    U2PayDepositApplyInfo? ApplyInfo,
    string? PaymentReturn = null);
