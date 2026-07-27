using System;
using Bing.Security.Passwords;
using Shouldly;
using Xunit;

namespace Bing.Utils.Security.Tests.Bing.Security.Passwords;

/// <summary>
/// 验证版本化 PBKDF2-HMAC-SHA256 密码哈希行为。
/// </summary>
public class Pbkdf2PasswordHasherTests
{
    /// <summary>
    /// 测试目的：同一密码应使用不同随机盐生成不同记录，且能被正确验证。
    /// </summary>
    [Fact]
    public void Hash_WhenPasswordIsSame_ShouldUseDistinctSaltsAndVerify()
    {
        // Arrange
        var hasher = new Pbkdf2PasswordHasher(new Pbkdf2PasswordHasherOptions { IterationCount = 100000 });

        // Act
        var first = hasher.Hash("中文密码");
        var second = hasher.Hash("中文密码");

        // Assert
        first.ShouldNotBe(second);
        first.ShouldStartWith("BSP1$PBKDF2-SHA256$100000$");
        hasher.Verify("中文密码", first).ShouldBe(PasswordVerificationResult.Success);
        hasher.Verify("错误密码", first).ShouldBe(PasswordVerificationResult.Failed);
    }

    /// <summary>
    /// 测试目的：旧迭代次数的有效记录应要求重新哈希，格式错误应失败。
    /// </summary>
    [Fact]
    public void Verify_WhenHashIsOutdatedOrMalformed_ShouldReturnExpectedResult()
    {
        // Arrange
        var oldHasher = new Pbkdf2PasswordHasher(new Pbkdf2PasswordHasherOptions { IterationCount = 100000 });
        var currentHasher = new Pbkdf2PasswordHasher(new Pbkdf2PasswordHasherOptions { IterationCount = 110000 });
        var hash = oldHasher.Hash("password");

        // Act
        var outdated = currentHasher.Verify("password", hash);
        var malformed = currentHasher.Verify("password", "BSP1$PBKDF2-SHA256$invalid$salt$hash");

        // Assert
        outdated.ShouldBe(PasswordVerificationResult.SuccessRehashNeeded);
        malformed.ShouldBe(PasswordVerificationResult.Failed);
    }

    /// <summary>
    /// 测试目的：密钥派生应拒绝弱参数并对相同输入产生确定结果。
    /// </summary>
    [Fact]
    public void DeriveKey_WhenParametersAreValid_ShouldBeDeterministic()
    {
        // Arrange
        var salt = new byte[16];

        // Act
        var first = Pbkdf2KeyDerivation.DeriveKey("password", salt, 32, 100000);
        var second = Pbkdf2KeyDerivation.DeriveKey("password", salt, 32, 100000);
        var weakSalt = () => Pbkdf2KeyDerivation.DeriveKey("password", new byte[15], 32, 100000);

        // Assert
        first.ShouldBe(second);
        weakSalt.ShouldThrow<ArgumentException>();
    }

    /// <summary>
    /// 测试目的：验证路径必须拒绝超长记录和超出成本上限的记录，且不得执行派生操作。
    /// </summary>
    [Fact]
    public void Verify_WhenRecordExceedsResourceLimits_ShouldReturnFailed()
    {
        // Arrange
        var hasher = new Pbkdf2PasswordHasher();
        var oversizedRecord = new string('a', Pbkdf2PasswordHasherOptions.MaximumEncodedHashLength + 1);
        var expensiveRecord = "BSP1$PBKDF2-SHA256$1000001$AAAAAAAAAAAAAAAAAAAAAA$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

        // Act
        var oversizedResult = hasher.Verify("password", oversizedRecord);
        var expensiveResult = hasher.Verify("password", expensiveRecord);

        // Assert
        oversizedResult.ShouldBe(PasswordVerificationResult.Failed);
        expensiveResult.ShouldBe(PasswordVerificationResult.Failed);
    }

    /// <summary>
    /// 测试目的：创建哈希器和直接派生密钥时必须拒绝超过已声明上限的参数。
    /// </summary>
    [Fact]
    public void CreateOrDerive_WhenParametersExceedLimits_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var oversizedOptions = new Pbkdf2PasswordHasherOptions { IterationCount = Pbkdf2PasswordHasherOptions.MaximumIterationCount + 1 };
        var oversizedOutput = new Action(() => Pbkdf2KeyDerivation.DeriveKey("password", new byte[16], Pbkdf2PasswordHasherOptions.MaximumHashSize + 1, 100000));

        // Act
        var createAction = new Action(() => new Pbkdf2PasswordHasher(oversizedOptions));

        // Assert
        createAction.ShouldThrow<ArgumentOutOfRangeException>();
        oversizedOutput.ShouldThrow<ArgumentOutOfRangeException>();
    }
}