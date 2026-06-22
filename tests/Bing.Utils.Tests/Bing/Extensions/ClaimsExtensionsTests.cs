using System.Security.Claims;
using Bing.Extensions;

namespace Bing.Utils.Tests.Bing.Extensions;

/// <summary>
/// <see cref="ClaimsExtensions"/> 单元测试
/// </summary>
public class ClaimsExtensionsTests
{
    // ─────────────────────────────────────────────────────────────────
    // TryAddClaim — guard cases (no-op)
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void TryAddClaim_NullType_DoesNotAddClaim()
    {
        var claims = new List<Claim>();
        claims.TryAddClaim(null!, "value");
        claims.ShouldBeEmpty();
    }

    [Fact]
    public void TryAddClaim_EmptyType_DoesNotAddClaim()
    {
        var claims = new List<Claim>();
        claims.TryAddClaim("", "value");
        claims.ShouldBeEmpty();
    }

    [Fact]
    public void TryAddClaim_WhitespaceType_DoesNotAddClaim()
    {
        var claims = new List<Claim>();
        claims.TryAddClaim("   ", "value");
        claims.ShouldBeEmpty();
    }

    [Fact]
    public void TryAddClaim_NullValue_DoesNotAddClaim()
    {
        var claims = new List<Claim>();
        claims.TryAddClaim("role", null!);
        claims.ShouldBeEmpty();
    }

    [Fact]
    public void TryAddClaim_EmptyValue_DoesNotAddClaim()
    {
        var claims = new List<Claim>();
        claims.TryAddClaim("role", "");
        claims.ShouldBeEmpty();
    }

    [Fact]
    public void TryAddClaim_WhitespaceValue_DoesNotAddClaim()
    {
        var claims = new List<Claim>();
        claims.TryAddClaim("role", "   ");
        claims.ShouldBeEmpty();
    }

    // ─────────────────────────────────────────────────────────────────
    // TryAddClaim — duplicate prevention
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void TryAddClaim_DuplicateType_SameCase_DoesNotAddSecondClaim()
    {
        var claims = new List<Claim> { new Claim("role", "admin") };
        claims.TryAddClaim("role", "user");
        claims.Count.ShouldBe(1);
        claims[0].Value.ShouldBe("admin");
    }

    [Fact]
    public void TryAddClaim_DuplicateType_DifferentCase_DoesNotAddSecondClaim()
    {
        var claims = new List<Claim> { new Claim("Role", "admin") };
        claims.TryAddClaim("role", "user");
        claims.Count.ShouldBe(1);
    }

    [Fact]
    public void TryAddClaim_DuplicateType_UpperCase_DoesNotAddSecondClaim()
    {
        var claims = new List<Claim> { new Claim("role", "admin") };
        claims.TryAddClaim("ROLE", "user");
        claims.Count.ShouldBe(1);
    }

    // ─────────────────────────────────────────────────────────────────
    // TryAddClaim — success cases
    // ─────────────────────────────────────────────────────────────────

    [Fact]
    public void TryAddClaim_NewClaim_AddsToList()
    {
        var claims = new List<Claim>();
        claims.TryAddClaim("role", "admin");
        claims.Count.ShouldBe(1);
        claims[0].Type.ShouldBe("role");
        claims[0].Value.ShouldBe("admin");
    }

    [Fact]
    public void TryAddClaim_DefaultValueType_IsClaimValueTypesString()
    {
        var claims = new List<Claim>();
        claims.TryAddClaim("role", "admin");
        claims[0].ValueType.ShouldBe(ClaimValueTypes.String);
    }

    [Fact]
    public void TryAddClaim_CustomValueType_IsStoredCorrectly()
    {
        var claims = new List<Claim>();
        claims.TryAddClaim("age", "30", ClaimValueTypes.Integer);
        claims[0].ValueType.ShouldBe(ClaimValueTypes.Integer);
        claims[0].Value.ShouldBe("30");
    }

    [Fact]
    public void TryAddClaim_DifferentTypes_BothAdded()
    {
        var claims = new List<Claim>();
        claims.TryAddClaim("role", "admin");
        claims.TryAddClaim("name", "Alice");
        claims.Count.ShouldBe(2);
    }

    [Fact]
    public void TryAddClaim_EmptyList_AddsFirstClaim()
    {
        var claims = new List<Claim>();
        claims.TryAddClaim("sub", "user-id-123");
        claims.ShouldHaveSingleItem();
        claims[0].Type.ShouldBe("sub");
        claims[0].Value.ShouldBe("user-id-123");
    }
}
