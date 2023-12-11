namespace Tgs.Payment.U2Pay;
public record WithdrawOrder(
    Guid FormId,
    decimal Amount,
    Uri CallbackUrl,
    string ReceiverName,
    string BankName,
    string BankAccount);
