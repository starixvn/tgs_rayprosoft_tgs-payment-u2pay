using Tgs.Payment.Clients;
using Tgs.Payment.U2Pay.Client;

namespace Tgs.Payment.U2Pay;
public record WithdrawForm(
    Guid Id,
    string? UserId,
    UserType UserType,
    Guid BrandId,
    Guid? BrandPaymentId,
    decimal Amount,
    PaymentFlowState Status,
    Guid BankInfoId,
    U2PayWithdrawApplyInfo? ApplyInfo,
    string? PaymentReturn = null);
