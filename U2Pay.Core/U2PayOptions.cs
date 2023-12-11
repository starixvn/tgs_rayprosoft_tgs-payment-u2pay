namespace Tgs.Payment.U2Pay;
public class U2PayOptions
{
    public required Uri CallbackBaseUrl { get; set; }

    public required Uri PaymentApiUrl { get; set; }

    public required Uri PaymentServiceBaseUrl { get; set; }
}
