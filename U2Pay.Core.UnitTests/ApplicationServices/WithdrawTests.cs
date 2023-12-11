using NSubstitute;
using Tgs.Payment.Clients;
using Tgs.Payment.U2Pay.ApplicationServices;

namespace U2Pay.Core.UnitTests.ApplicationServices;

public class WithdrawTests
{
    [Fact]
    public async Task 取得提款單資料()
    {
        // Arrange
        var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();

        var sut = new GetWithdrawFormQueryService(fakePaymentServiceClient);

        var query = new GetWithdrawFormQuery(Guid.NewGuid());

        // Act
        _ = await sut.GetAsync(query);

        // Assert
        _ = fakePaymentServiceClient.Received(1)
            .GetWithdrawFormAsync(Arg.Is(query.FormId));
    }
}
