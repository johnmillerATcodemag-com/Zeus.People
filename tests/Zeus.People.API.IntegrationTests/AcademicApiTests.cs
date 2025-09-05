using System.Net;
using FluentAssertions;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Net.Http.Headers;
using Xunit;

namespace Zeus.People.API.IntegrationTests;

public class AcademicApiTests : IClassFixture<IntegrationTestWebApplicationFactory>
{
    private readonly IntegrationTestWebApplicationFactory _factory;
    private const string DevIssuer = "Zeus.People.API.IntegrationTests";
    private const string DevAudience = "Zeus.People.Client.IntegrationTests";
    private const string DevSecret = "integration-test-secret-key-which-is-long-1234567890"; // length > 32

    public AcademicApiTests(IntegrationTestWebApplicationFactory factory) => _factory = factory;

    private static string CreateJwt()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(DevSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "integration-test-user"),
            new Claim(ClaimTypes.Role, "Tester"),
            new Claim("scope", "api.read" )
        };

        var token = new JwtSecurityToken(
            issuer: DevIssuer,
            audience: DevAudience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task Get_Academics_Should_Return_Success()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwt());

        var response = await client.GetAsync("/api/academics");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
