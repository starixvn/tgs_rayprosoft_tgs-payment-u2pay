using Microsoft.Extensions.Logging;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Payment.U2Pay.MessageQueue;

internal class DepositTimeoutCheckHandler : IMessageHandler
{
    private readonly IPaymentServiceClient _paymentServiceClient;
    private readonly IPaymentClient _paymentClient;
    private readonly ILogger<DepositTimeoutCheckHandler> _logger;

    public DepositTimeoutCheckHandler(
        IPaymentServiceClient paymentServiceClient,
        IPaymentClient paymentClient,
        ILogger<DepositTimeoutCheckHandler> logger)
    {
        _paymentServiceClient = paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));
        _paymentClient = paymentClient ?? throw new ArgumentNullException(nameof(paymentClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var request = ScheduleRequest.Parser.ParseFrom(data.Span);

        _logger.LogInformation(
            "Deposit Schedule Handler Receive request Form Id:{request.FormId.ToGuid()}, "
                + "Schedule Time:{request.ScheduleTime.ToDateTime():yyyy/MM/dd HH:mm:ss}",
            request.FormId.ToGuid(),
            request.ScheduleTime.ToDateTime());

        var formId = request.FormId.ToGuid();

        try
        {
            var depositForm = await _paymentServiceClient
                .GetDepositFormAsync(formId, cancellationToken)
                .ConfigureAwait(false);

            if (depositForm is null)
            {
                _logger.LogWarning("Deposit Form {request.FormId.ToGuid()} not found.", request.FormId.ToGuid());
                return;
            }

            var brandId = depositForm.BrandId;

            _logger.LogInformation(
                "GetDepositFormAsync Form Id:{depositForm.Id}, Brand Id:{brandId}, Status:{depositForm.Status}",
                depositForm.Id,
                brandId,
                depositForm.Status);

            if (depositForm.Status >= PaymentFlowState.Reject)
                return;

            var fsm = PaymentStateMachineFactory.CreateDepositStateMachine(depositForm.Status);

            if (fsm.CanFire(PaymentTrigger.Reject))
            {
                await fsm.FireAsync(PaymentTrigger.Reject).ConfigureAwait(false);

                await _paymentClient
                    .RejectDepositAsync(formId, brandId, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Process deposit timeout error!");

            await _paymentClient.ScheduleAsync(
                "tgs.payment.u2pay.deposit.schedule.reject",
                formId,
                DateTime.UtcNow.AddMinutes(10D),
                cancellationToken).ConfigureAwait(false);
        }
    }
}
