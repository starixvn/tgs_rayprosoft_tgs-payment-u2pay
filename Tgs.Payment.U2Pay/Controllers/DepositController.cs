using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Tgs.Payment.Clients;
using Tgs.Payment.U2Pay.ApplicationServices;
using Tgs.Payment.U2Pay.Client;
using Tgs.Payment.U2Pay.ViewModels;
using Tgs.ServiceUtility;

namespace Tgs.Payment.U2Pay.Controllers;

[Route("[controller]")]
[ApiController]
public class DepositController : ControllerBase
{
    private readonly ILogger<DepositController> _logger;

    public DepositController(ILogger<DepositController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("callback", Name = "NotifyDeposit")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<string> DepositCallbackAsync(
    [FromServices] U2PayHasher hasher,
        [FromServices] IGetService<GetDepositFormQuery, DepositForm?> getService,
        [FromServices] IPaymentServiceClient paymentServiceClient,
        [FromServices] IPaymentClient paymentClient,
        [FromForm] CallbackDepositViewModel source)
    {
        var formId = Guid.Parse(source.OrderNo);

        var depositForm = await getService
            .GetAsync(new(formId), HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (depositForm == null)
        {
            _logger.LogWarning("Can't found deposit form({id}).", formId);
            return "fail";
        }

        var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(source))!;

        var sign = source.Sign;
        var hash = hasher.ComputeHash(
            depositForm.ApplyInfo!.Settings.PrivateKey,
            dict.Select(kv => new KeyValuePair<string, string?>(
                kv.Key,
                kv.Value == null ? "" : kv.Value.ToString())));

        _logger.LogInformation("Compute hash: {hash}, sign: {sign}", hash, sign);

        if (sign != hash)
        {
            _logger.LogWarning("Sign value invalid. input: {sign}, compute: {hash}", sign, hash);
            return "fail";
        }

        var paymentReturn = new U2PayDepositReturn()
        {
            Status = source.Status,
            FormId = formId,
            ThirdPartyOrderNo = source.TradeNo,
            RealAmount = source.Paid,
            OrderAmount = source.Amount,
            Sign = sign,
        };

        var fsm = PaymentStateMachineFactory.CreateDepositStateMachine(depositForm.Status);

        switch (paymentReturn.Status)
        {
            case "PAID":
                if (fsm.CanFire(PaymentTrigger.PaymentDepositCompleted))
                {
                    await fsm.FireAsync(PaymentTrigger.PaymentDepositCompleted).ConfigureAwait(false);

                    await paymentServiceClient.SubmitDepositFormStatusAsync(
                        depositForm.Id,
                        paymentReturn: JsonSerializer.Serialize(
                            paymentReturn,
                            U2PayUtility.SerializerOptions),
                        cancellationToken: HttpContext.RequestAborted).ConfigureAwait(false);

                    await paymentClient.ApproveDepositAsync(
                        depositForm.Id,
                        depositForm.BrandId,
                        paymentReturn.RealAmount,
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
