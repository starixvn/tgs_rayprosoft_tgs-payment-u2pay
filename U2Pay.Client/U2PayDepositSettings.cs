using System.ComponentModel;

namespace Tgs.Payment.U2Pay.Client;
public record U2PayDepositSettings(
    string MerchantNo,
    string AppSecret,
    string PrivateKey,
    Guid FormId,
    int ChannelNo,
    string BankName,
    [property: Description("匯率轉換")]
    decimal ExchangeRate);
