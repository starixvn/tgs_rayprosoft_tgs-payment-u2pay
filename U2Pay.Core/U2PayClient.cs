using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Tgs.Payment.U2Pay;

public class U2PayClient(
    HttpClient httpClient,
    U2PayHasher hasher,
    TimeProvider timeProvider,
    IConfiguration configuration,
    ILogger<U2PayClient> logger)
{
    public async ValueTask<U2PayClientDepositResponse> DepositAsync(string merchantNo, string appSecert, string privateKey, DepositOrder order, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();

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

        var sign = hasher.ComputeHash(privateKey, dict.Where(x => !excludeKeys.Contains(x.Key)));
        dict["sign"] = sign;

        var request = new HttpRequestMessage(HttpMethod.Post, AddSubPath("/order/create"))
        {
            Content = new FormUrlEncodedContent(dict)
        };

        var response = await httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("U2PayClient Deposit Body:{body}", body);

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
            ["time"] = timeProvider.GetUtcNow().ToUnixTimeSeconds().ToString(),
            ["appSecret"] = appSecert,
        };

        var sign = hasher.ComputeHash(privateKey, dict.Where(x => x.Key != "appSecret"));
        dict["sign"] = sign;

        var request = new HttpRequestMessage(HttpMethod.Post, AddSubPath("/order/status"))
        {
            Content = new FormUrlEncodedContent(dict)
        };

        var response = await httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("U2PayClient Query Deposit Form Body:{body}", body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Query Deposit Form response can't read.");

        if (node["code"]!.GetValue<int>() != 0)
            throw new ApiFailException(node["text"]!.GetValue<string>());

        return node["status"]!.GetValue<string>();
    }

    public async ValueTask<U2PayClientWithdrawResponse> WithdrawAsync(string merchantNo, string appSecert, string privateKey, WithdrawOrder order, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();

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

        var sign = hasher.ComputeHash(privateKey, dict.Where(x => !excludeKeys.Contains(x.Key)));
        dict["sign"] = sign;

        var request = new HttpRequestMessage(HttpMethod.Post, AddSubPath("/payout/create"))
        {
            Content = new FormUrlEncodedContent(dict)
        };

        var response = await httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("U2PayClient Withdraw Body:{body}", body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Withdraw response can't read.");

        if (node["code"]!.GetValue<int>() != 0)
            throw new ApiFailException(node["text"]!.GetValue<string>());

        return new U2PayClientWithdrawResponse(
            Amount: node["amount"]!.GetValue<decimal>(),
            FormId: Guid.Parse(node["orderNo"]!.GetValue<string>()),
            ThirdPartyOrderNo: node["tradeNo"]!.GetValue<string>());
    }

    public async ValueTask<decimal> QueryBalanceAsync(string merchantNo, string appSecret, string privateKey, CancellationToken cancellationToken = default)
    {
        var dict = new Dictionary<string, string?>
        {
            ["merchantNo"] = merchantNo,
            ["time"] = timeProvider.GetUtcNow().ToUnixTimeSeconds().ToString(),
            ["appSecret"] = appSecret,
        };

        var sign = hasher.ComputeHash(privateKey, dict.Where(x => x.Key != "appSecret"));
        dict["sign"] = sign;

        var request = new HttpRequestMessage(HttpMethod.Post, AddSubPath("/order/status"))
        {
            Content = new FormUrlEncodedContent(dict)
        };

        var response = await httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("U2PayClient Query Merchant Balance Body:{body}", body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Query Merchant Balance response can't read.");

        if (node["code"]!.GetValue<int>() != 0)
            throw new ApiFailException(node["text"]!.GetValue<string>());

        var balance = node["balance"]!.GetValue<decimal>();

        return balance;
    }

    private string AddSubPath(string origin)
    {
        return $"/{configuration.GetValue<string>("PathBase")}{origin}";
    }
}
