namespace Tgs.Payment.U2Pay.Client;
public record U2PayDepositResponse(
    decimal Amount,
    Guid FormId,
    Uri RedirectUrl,
    string ThirdPartyOrderNo);
