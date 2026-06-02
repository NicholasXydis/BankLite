using BankLite.Domain.Entities;

namespace BankLite.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    DateTime GetAccessTokenExpiry();
}