namespace Tgs.Payment.U2Pay;
public record U2PayClientWithdrawResponse(
    Guid FormId,
    string ThirdPartyOrderNo,
    decimal Amount);
