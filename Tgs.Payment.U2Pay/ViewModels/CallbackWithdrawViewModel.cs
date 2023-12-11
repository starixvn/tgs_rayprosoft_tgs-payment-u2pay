using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Tgs.Payment.U2Pay.ViewModels;

public class CallbackWithdrawViewModel
{
    [FromForm(Name = "status")]
    [JsonPropertyName("status")]
    public string Status { get; set; } = default!;

    /// <summary>
    /// 交易单号 (支付平台的交易单号)
    /// </summary>
    [FromForm(Name = "tradeNo")]
    [JsonPropertyName("tradeNo")]
    public string TradeNo { get; set; } = default!;

    /// <summary>
    /// 商户单号
    /// </summary>
    [FromForm(Name = "orderNo")]
    [JsonPropertyName("orderNo")]
    public string OrderNo { get; set; } = default!;

    /// <summary>
    /// 订单金额
    /// </summary>
    [FromForm(Name = "amount")]
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    /// <summary>
    /// 收款人姓名
    /// </summary>
    [FromForm(Name = "name")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    /// <summary>
    /// 收款银行名称
    /// </summary>
    [FromForm(Name = "bankName")]
    [JsonPropertyName("bankName")]
    public string BankName { get; set; } = default!;

    /// <summary>
    /// 收款银行账号
    /// </summary>
    [FromForm(Name = "bankAccount")]
    [JsonPropertyName("bankAccount")]
    public string BankAccount { get; set; } = default!;

    [FromForm(Name = "bankBranch")]
    [JsonIgnore]
    public string? BankBranch { get; set; }

    [FromForm(Name = "memo")]
    [JsonIgnore]
    public string? Memo { get; set; }

    [FromForm(Name = "mobile")]
    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    [FromForm(Name = "fee")]
    [JsonPropertyName("fee")]
    public decimal Fee { get; set; }

    /// <summary>
    /// 附加信息
    /// </summary>
    [FromForm(Name = "extra")]
    [JsonPropertyName("extra")]
    public string? Extra { get; set; }

    [FromForm(Name = "sign")]
    [JsonIgnore]
    public string Sign { get; set; } = default!;

    [FromForm(Name = "reason")]
    [JsonIgnore]
    public string? Reason { get; set; }
}
