namespace Tgs.Payment.U2Pay.Client;
public record U2PayDepositSettings(
    string MerchantNo,
    string AppSecret,
    string PrivateKey,
    Guid FormId,
    int ChannelNo,
    string BankName);
