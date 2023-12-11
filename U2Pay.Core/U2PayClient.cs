using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Tgs.SerialNumberGenerator;

namespace Tgs.Payment.U2Pay;

public class U2PayClient
{
    private readonly HttpClient _httpClient;
    private readonly U2PayHasher _hasher;
    private readonly ISystemClock _systemClock;
    private readonly ILogger<U2PayClient> _logger;

    public U2PayClient(
        HttpClient httpClient,
        U2PayHasher hasher,
        ISystemClock systemClock,
        ILogger<U2PayClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
        _systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<U2PayClientDepositResponse> DepositAsync(string merchantNo, string appSecert, string privateKey, DepositOrder order, CancellationToken cancellationToken = default)
    {
        var now = _systemClock.UtcNow;

        var dict = new Dictionary<string, string?>
        {
            ["merchantNo"] = merchantNo,
            ["orderNo"] = order.FormId.ToString("N"),
            ["userNo"] = "",
            ["channelNo"] = order.ChannelNo.ToString(),
            ["amount"] = order.Amount.ToString(),
            ["discount"] = "",
            ["bankName"] = order.BankName,
            ["extra"] = "",
            ["datetime"] = now.ToString("yyyy-MM-dd HH:mm:ss"),
            ["notifyUrl"] = order.CallbackUrl.AbsoluteUri,
            ["time"] = now.ToUnixTimeSeconds().ToString(),
            ["appSecret"] = appSecert,
        };

        var excludeKeys = new string[] { "userName", "channelNo", "payeeName", "amountBeforeFixed", "returnUrl", "appSecret", "bankName" };

        var sign = _hasher.ComputeHash(privateKey, dict.Where(x => !excludeKeys.Contains(x.Key)));
        dict["sign"] = sign;

        var request = new HttpRequestMessage(HttpMethod.Post, "/order/create")
        {
            Content = new FormUrlEncodedContent(dict)
        };

        var response = await _httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("U2PayClient Deposit Body:{body}", body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Deposit response can't read.");

        if (node["code"]!.GetValue<int>() != 0)
            throw new ApiFailException(node["text"]!.GetValue<string>());

        return new U2PayClientDepositResponse(
            Amount: node["amount"]!.GetValue<decimal>(),
            FormId: Guid.Parse(node["orderNo"]!.GetValue<string>()),
            ThirdPartyOrderNo: node["tradeNo"]!.GetValue<string>(),
            RedirectUrl: new Uri(node["targetUrl"]!.GetValue<string>()));
    }

    public async ValueTask<string> QueryDepositFormStatusAsync(string merchantNo, string appSecert, string privateKey, Guid formId, CancellationToken cancellationToken = default)
    {
        var dict = new Dictionary<string, string?>
        {
            ["merchantNo"] = merchantNo,
            ["tradeNo"] = "",
            ["orderNo"] = formId.ToString("N"),
            ["time"] = _systemClock.UtcNow.ToUnixTimeSeconds().ToString(),
            ["appSecret"] = appSecert,
        };

        var sign = _hasher.ComputeHash(privateKey, dict.Where(x => x.Key != "appSecret"));
        dict["sign"] = sign;

        var request = new HttpRequestMessage(HttpMethod.Post, "/order/status")
        {
            Content = new FormUrlEncodedContent(dict)
        };

        var response = await _httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("U2PayClient Query Deposit Form Body:{body}", body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Query Deposit Form response can't read.");

        if (node["code"]!.GetValue<int>() != 0)
            throw new ApiFailException(node["text"]!.GetValue<string>());

        return node["status"]!.GetValue<string>();
    }

    public async ValueTask<U2PayClientWithdrawResponse> WithdrawAsync(string merchantNo, string appSecert, string privateKey, WithdrawOrder order, CancellationToken cancellationToken = default)
    {
        var now = _systemClock.UtcNow;

        var dict = new Dictionary<string, string?>
        {
            ["merchantNo"] = merchantNo,
            ["orderNo"] = order.FormId.ToString("N"),
            ["amount"] = order.Amount.ToString(),
            ["name"] = order.ReceiverName,
            ["bankName"] = order.BankName,
            ["bankAccount"] = order.BankAccount,
            ["mobile"] = "",
            ["datetime"] = now.ToString("yyyy-MM-dd HH:mm:ss"),
            ["notifyUrl"] = order.CallbackUrl.AbsoluteUri,
            ["reverseUrl"] = order.CallbackUrl.AbsoluteUri,
            ["extra"] = "",
            ["time"] = now.ToUnixTimeSeconds().ToString(),
            ["appSecret"] = appSecert,
        };

        var excludeKeys = new string[] { "bankBranch", "memo", "appSecret" };

        var sign = _hasher.ComputeHash(privateKey, dict.Where(x => !excludeKeys.Contains(x.Key)));
        dict["sign"] = sign;

        var request = new HttpRequestMessage(HttpMethod.Post, "/payout/create")
        {
            Content = new FormUrlEncodedContent(dict)
        };

        var response = await _httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("U2PayClient Withdraw Body:{body}", body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Withdraw response can't read.");

        if (node["code"]!.GetValue<int>() != 0)
            throw new ApiFailException(node["text"]!.GetValue<string>());

        return new U2PayClientWithdrawResponse(
            Amount: node["amount"]!.GetValue<decimal>(),
            FormId: Guid.Parse(node["orderNo"]!.GetValue<string>()),
            ThirdPartyOrderNo: node["tradeNo"]!.GetValue<string>());
    }

    public async ValueTask<decimal> QueryBalanceAsync(string merchantNo, string appSecert, string privateKey, CancellationToken cancellationToken = default)
    {
        var dict = new Dictionary<string, string?>
        {
            ["merchantNo"] = merchantNo,
            ["time"] = _systemClock.UtcNow.ToUnixTimeSeconds().ToString(),
            ["appSecret"] = appSecert,
        };

        var sign = _hasher.ComputeHash(privateKey, dict.Where(x => x.Key != "appSecret"));
        dict["sign"] = sign;

        var request = new HttpRequestMessage(HttpMethod.Post, "/payout/balance")
        {
            Content = new FormUrlEncodedContent(dict)
        };

        var response = await _httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("U2PayClient Query Merchant Balance Body:{body}", body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Query Merchant Balance response can't read.");

        if (node["code"]!.GetValue<int>() != 0)
            throw new ApiFailException(node["text"]!.GetValue<string>());

        var balance = node["balance"]!.GetValue<decimal>();

        return balance;
    }
}
