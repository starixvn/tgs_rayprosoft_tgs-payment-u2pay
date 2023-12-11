using Microsoft.Extensions.Logging.Abstractions;
using Tgs.Payment.U2Pay;

namespace U2Pay.Core.UnitTests;

public class HasherTests
{
    [Fact]
    public void 查詢餘額()
    {
        var key = "ClLtElIsn0O2VDFYYbTMZg";

        var dict = new Dictionary<string, string?>
        {
            ["merchantNo"] = "110630",
            ["time"] = "1701847072",
            ["appSecret"] = "ageDUOgK6kumEoTb2tjraA",
        };
        var expected = "90464B09231DA5CF96B7B016125BF719";

        var sut = new U2PayHasher(NullLogger<U2PayHasher>.Instance);

        var actual = sut.ComputeHash(key, dict.Where(x => x.Key != "appSecret"));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void 存款()
    {
        var key = "ClLtElIsn0O2VDFYYbTMZg";

        var dict = new Dictionary<string, string?>
        {
            ["amount"] = "100000",
            ["datetime"] = "2023-12-06 09:07:52",
            ["discount"] = "",
            ["extra"] = "",
            ["merchantNo"] = "110630",
            ["notifyUrl"] = "",
            ["orderNo"] = "54280073a70c49118ccbfbd545513486",
            ["channelNo"] = "2",
            ["time"] = "1701853672",
            ["appSecret"] = "ageDUOgK6kumEoTb2tjraA",
            ["userNo"] = "",
        };
        var expected = "5A0D8A49C1EE4DE67E2A088096CBEB5B";

        var excludeKeys = new string[] { "userName", "channelNo", "payeeName", "amountBeforeFixed", "returnUrl", "appSecret", "bankName" };

        var sut = new U2PayHasher(NullLogger<U2PayHasher>.Instance);

        var actual = sut.ComputeHash(key, dict.Where(x => !excludeKeys.Contains(x.Key)));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void 存款回調()
    {
        var key = "ClLtElIsn0O2VDFYYbTMZg";

        var dict = new Dictionary<string, string?>
        {
            ["status"] = "PAID",
            ["tradeNo"] = "888429",
            ["orderNo"] = "5a285c40726543d6b50e24a1f0cc42cc",
            ["userNo"] = "",
            ["userName"] = "",
            ["channelNo"] = "2",
            ["amount"] = "100000.00",
            ["discount"] = "0.00",
            ["lucky"] = "0.00",
            ["paid"] = "100000.00",
            ["extra"] = "",
            ["sign"] = "5DDB8E7F671B8B2E00D44A74483B3B30",
        };
        var expected = "5DDB8E7F671B8B2E00D44A74483B3B30";

        _ = dict.Remove("sign");

        var excludeKeys = new string[] { "userName", "channelNo", "payeeName", "amountBeforeFixed", "returnUrl", "appSecret", "bankName" };

        var sut = new U2PayHasher(NullLogger<U2PayHasher>.Instance);

        var actual = sut.ComputeHash(key, dict.Where(x => !excludeKeys.Contains(x.Key)));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void 查詢存款單()
    {
        var key = "ClLtElIsn0O2VDFYYbTMZg";

        var dict = new Dictionary<string, string?>
        {
            ["merchantNo"] = "110630",
            ["tradeNo"] = "",
            ["orderNo"] = "fc20591611624e8fb67b050c2233b930",
            ["time"] = "-62135596800",
            ["appSecret"] = "ageDUOgK6kumEoTb2tjraA",
        };
        var expected = "4C3DF0BF9FF4515A54B0CFF3BB8EEEC2";

        var sut = new U2PayHasher(NullLogger<U2PayHasher>.Instance);

        var actual = sut.ComputeHash(key, dict.Where(x => x.Key != "appSecret"));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void 提款()
    {
        var key = "ClLtElIsn0O2VDFYYbTMZg";

        var dict = new Dictionary<string, string?>
        {
            ["merchantNo"] = "110630",
            ["orderNo"] = "2a1a56a5ebf54547acf92ada121acf7f",
            ["amount"] = "100000",
            ["name"] = "NGUYEN",
            ["bankName"] = "VCB",
            ["bankAccount"] = "testBankAccount",
            ["mobile"] = "",
            ["datetime"] = "2023-12-07 07:45:36",
            ["notifyUrl"] = "https://dev-payment.rpgp.cc/u2pay/withdraw/callback",
            ["reverseUrl"] = "https://dev-payment.rpgp.cc/u2pay/withdraw/callback",
            ["extra"] = "",
            ["time"] = "1701935136",
            ["appSecret"] = "ageDUOgK6kumEoTb2tjraA",
        };
        var expected = "E7633F7AC038E51626655BCC24432D3F";

        var excludeKeys = new string[] { "bankBranch", "memo", "appSecret" };

        var sut = new U2PayHasher(NullLogger<U2PayHasher>.Instance);

        var actual = sut.ComputeHash(key, dict.Where(x => !excludeKeys.Contains(x.Key)));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void 提款回調成功()
    {
        var key = "ClLtElIsn0O2VDFYYbTMZg";

        var dict = new Dictionary<string, string?>
        {
            ["status"] = "PAID",
            ["tradeNo"] = "159421",
            ["orderNo"] = "2a1a56a5ebf54547acf92ada121acf7f",
            ["amount"] = "100000.00",
            ["name"] = "NGUYEN",
            ["bankName"] = "VIETCOMBANK",
            ["bankAccount"] = "testBankAccount",
            ["bankBranch"] = "",
            ["memo"] = "",
            ["mobile"] = "",
            ["fee"] = "0",
            ["extra"] = "",
            ["sign"] = "16C5A7B788D7838483E1FCF2C8031F12",
        };
        var expected = "16C5A7B788D7838483E1FCF2C8031F12";

        _ = dict.Remove("sign");

        var excludeKeys = new string[] { "bankBranch", "memo", "appSecret" };

        var sut = new U2PayHasher(NullLogger<U2PayHasher>.Instance);

        var actual = sut.ComputeHash(key, dict.Where(x => !excludeKeys.Contains(x.Key)));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void 提款回調撤銷()
    {
        var key = "ClLtElIsn0O2VDFYYbTMZg";

        var dict = new Dictionary<string, string?>
        {
            ["status"] = "CANCELLED",
            ["tradeNo"] = "161844",
            ["orderNo"] = "8db32d4885b94041b0de9be071793145",
            ["amount"] = "100000.00",
            ["name"] = "NGUYEN",
            ["bankName"] = "VIETCOMBANK",
            ["bankAccount"] = "testBankAccount",
            ["bankBranch"] = "",
            ["memo"] = "",
            ["mobile"] = "",
            ["fee"] = "0",
            ["extra"] = "",
            ["sign"] = "D593836D2F27542B30B999DEF53B015E",
            ["reason"] = "",
        };
        var expected = "D593836D2F27542B30B999DEF53B015E";

        _ = dict.Remove("sign");

        var excludeKeys = new string[] { "bankBranch", "memo", "appSecret", "reason" };

        var sut = new U2PayHasher(NullLogger<U2PayHasher>.Instance);

        var actual = sut.ComputeHash(key, dict.Where(x => !excludeKeys.Contains(x.Key)));

        Assert.Equal(expected, actual);
    }
}
