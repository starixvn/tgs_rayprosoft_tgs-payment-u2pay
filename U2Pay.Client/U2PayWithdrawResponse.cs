namespace Tgs.Payment.U2Pay.Client;
public record U2PayWithdrawResponse(
    Guid FormId,
    string ThirdPartyOrderNo);
