using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Tgs.Payment.U2Pay.ViewModels;

public class CallbackDepositViewModel
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
    /// 商户客户号
    /// </summary>
    [FromForm(Name = "userNo")]
    [JsonPropertyName("userNo")]
    public int? UserNo { get; set; }

    /// <summary>
    /// 商户客户名
    /// </summary>
    [FromForm(Name = "userName")]
    [JsonIgnore]
    public string? UserName { get; set; }

    /// <summary>
    /// 支付通道编号 (纯数字格式; MomoPay:0 | ZaloPay:1 | 银行扫码:2 | 直連:3 | 网关:4 |VTPay:5 )
    /// </summary>
    [FromForm(Name = "channelNo")]
    [JsonIgnore]
    public int ChannelNo { get; set; }

    /// <summary>
    /// 订单金额
    /// </summary>
    [FromForm(Name = "amount")]
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    /// <summary>
    /// 立减金额
    /// </summary>
    [FromForm(Name = "discount")]
    [JsonPropertyName("discount")]
    public decimal Discount { get; set; }

    /// <summary>
    /// 优惠金额
    /// </summary>
    [FromForm(Name = "lucky")]
    [JsonPropertyName("lucky")]
    public decimal Lucky { get; set; }

    /// <summary>
    /// 实付金额
    /// </summary>
    [FromForm(Name = "paid")]
    [JsonPropertyName("paid")]
    public decimal Paid { get; set; }

    /// <summary>
    /// 附加信息
    /// </summary>
    [FromForm(Name = "extra")]
    [JsonPropertyName("extra")]
    public string? Extra { get; set; }

    [FromForm(Name = "sign")]
    [JsonIgnore]
    public string Sign { get; set; } = default!;
}
