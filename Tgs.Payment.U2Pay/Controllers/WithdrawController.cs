using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Tgs.Payment.Clients;
using Tgs.Payment.U2Pay.ApplicationServices;
using Tgs.Payment.U2Pay.Client;
using Tgs.Payment.U2Pay.ViewModels;
using Tgs.ServiceUtility;

namespace Tgs.Payment.U2Pay.Controllers;

[Route("[controller]")]
[ApiController]
public class WithdrawController : ControllerBase
{
    private readonly ILogger<WithdrawController> _logger;

    public WithdrawController(ILogger<WithdrawController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("callback", Name = "NotifyWithdraw")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<string> WithdrawCallbackAsync(
    [FromServices] U2PayHasher hasher,
        [FromServices] IGetService<GetWithdrawFormQuery, WithdrawForm?> getService,
        [FromServices] IPaymentServiceClient paymentServiceClient,
        [FromServices] IPaymentClient paymentClient,
        [FromForm] CallbackWithdrawViewModel source)
    {
        _logger.LogInformation("{source}", source);
        var formId = Guid.Parse(source.OrderNo);

        var withdrawForm = await getService
            .GetAsync(new(formId), HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (withdrawForm == null)
        {
            _logger.LogWarning("Can't found withdraw form({id}).", formId);
            return "fail";
        }

        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(source))!;

        var sign = source.Sign;
        var hash = hasher.ComputeHash(
            withdrawForm.ApplyInfo!.Settings.PrivateKey,
            dict.Select(kv => new KeyValuePair<string, string?>(
                kv.Key,
                kv.Value == null ? "" : kv.Value.ToString())));

        _logger.LogInformation("Compute hash: {hash}, sign: {sign}", hash, sign);

        if (sign != hash)
        {
            _logger.LogWarning("Sign value invalid. input: {sign}, compute: {hash}", sign, hash);
            return "fail";
        }

        var status = source.Status;

        var paymentReturn = new U2PayWithdrawReturn()
        {
            FormId = formId,
            Amount = source.Amount,
            Sign = sign,
            ThirdPartyOrderNo = source.TradeNo,
            Status = status,
            AfterExchangeAmount = source.Amount / withdrawForm.ApplyInfo.Settings.ExchangeRate
        };

        var fsm = PaymentStateMachineFactory.CreateWithdrawStateMachine(withdrawForm.Status);

        switch (status)
        {
            case "PAID":
                if (fsm.CanFire(PaymentTrigger.PaymentWithdrawCompleted))
                {
                    await fsm.FireAsync(PaymentTrigger.PaymentWithdrawCompleted).ConfigureAwait(false);

                    await paymentServiceClient.SubmitWithdrawFormStatusAsync(
                        withdrawForm.Id,
                        paymentReturn: JsonSerializer.Serialize(
                            paymentReturn,
                            U2PayUtility.SerializerOptions),
                        cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

                    await paymentClient.PaidWithdrawAsync(
                        withdrawForm.Id,
                        withdrawForm.BrandId).ConfigureAwait(false);
                }

                break;

            case "CANCELLED":
                if (fsm.CanFire(PaymentTrigger.Reject))
                {
                    await fsm.FireAsync(PaymentTrigger.Reject).ConfigureAwait(false);

                    await paymentServiceClient.SubmitWithdrawFormStatusAsync(
                        withdrawForm.Id,
                        paymentReturn: JsonSerializer.Serialize(
                            paymentReturn,
                            U2PayUtility.SerializerOptions),
                        cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

                    await paymentClient.RejectWithdrawAsync(
                        withdrawForm.Id,
                        withdrawForm.BrandId,
                        HttpContext.RequestAborted).ConfigureAwait(false);
                }

                break;

            default:
                _logger.LogWarning("FormId:{formId}, ThirdPartyOrderNo:{ThirdPartyOrderNo} , payment Status:{status}",
                    formId,
                    paymentReturn.ThirdPartyOrderNo,
                    paymentReturn.Status);
                return "fail";
        }

        return "success";
    }
}
