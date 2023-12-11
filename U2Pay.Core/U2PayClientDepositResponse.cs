namespace Tgs.Payment.U2Pay;
public record U2PayClientDepositResponse(
    decimal Amount,
    Guid FormId,
    string ThirdPartyOrderNo,
    Uri RedirectUrl);
