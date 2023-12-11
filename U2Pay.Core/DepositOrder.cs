namespace Tgs.Payment.U2Pay;
public record DepositOrder(
    decimal Amount,
    Guid FormId,
    Uri CallbackUrl,
    int ChannelNo,
    string BankName);
