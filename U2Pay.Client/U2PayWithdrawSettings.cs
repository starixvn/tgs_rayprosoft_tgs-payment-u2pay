namespace Tgs.Payment.U2Pay.Client;
public record U2PayWithdrawSettings(
    string MerchantNo,
    string AppSecret,
    string PrivateKey,
    Guid FormId,
    string ReceiverName,
    string BankName,
    string BankAccount);
