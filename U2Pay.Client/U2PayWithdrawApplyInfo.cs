namespace Tgs.Payment.U2Pay.Client;
public record U2PayWithdrawApplyInfo(
    U2PayWithdrawSettings Settings,
    U2PayWithdrawResponse Response);