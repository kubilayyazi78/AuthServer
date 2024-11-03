using AuthServer.Core.Configuration;
using AuthServer.Core.Dtos;
using AuthServer.Core.Models;
using AuthServer.Core.Repositories;
using AuthServer.Core.Services;
using AuthServer.Core.UnitOfWork;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SharedLibrary.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthServer.Service.Services
{

    public class AuthenticationService : IAuthenticationService
    {
        private readonly List<Client> _clients;
        private readonly ITokenService _tokenService;
        private readonly UserManager<UserApp> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepository<UserRefreshToken> _servisGeneric;

        public AuthenticationService(IOptions<List<Client>> options, ITokenService tokenService, UserManager<UserApp> userManager, IUnitOfWork unitOfWork, IGenericRepository<UserRefreshToken> genericRepository)
        {
            _clients = options.Value;
            _tokenService = tokenService;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _servisGeneric = genericRepository;
        }

        public async Task<Response<TokenDto>> CreateTokenAsync(LoginDto loginDto)
        {
            if (loginDto == null)
            {
                throw new ArgumentNullException(nameof(loginDto));
            }
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
            {
                return Response<TokenDto>.Fail("Email or Password is wrong", 400, true);
            }
            if ((await _userManager.CheckPasswordAsync(user, loginDto.Password)) == false)
            {
                return Response<TokenDto>.Fail("Email or Password is wrong", 400, true);
            }

            var token = _tokenService.CreateToken(user);

            var userRefreshToken = await _servisGeneric.Where(x => x.UserId == user.Id).SingleOrDefaultAsync();
            if (userRefreshToken == null)
            {
                await _servisGeneric.AddAsync(new UserRefreshToken
                {
                    UserId = user.Id,
                    Code = token.RefreshToken,
                    Expiration = token.RefreshTokenExpiration
                });
            }
            else
            {
                userRefreshToken.Code = token.RefreshToken;
                userRefreshToken.Expiration = token.RefreshTokenExpiration;
            }

            await _unitOfWork.CommitAsync();
            return Response<TokenDto>.Success(token, 200);
        }

        public Response<ClientTokenDto> CreateTokenByClient(ClientLoginDto clientLoginDto)
        {
            var client = _clients.SingleOrDefault(x => x.Id == clientLoginDto.ClientId && x.Secret == clientLoginDto.ClientSecret);

            if (client == null)
            {
                return Response<ClientTokenDto>.Fail("ClientId or ClientSecret not found", 404, true);
            }
            var token = _tokenService.CreateTokenByClient(client);

            return Response<ClientTokenDto>.Success(token, 200);

        }

        public async Task<Response<TokenDto>> CreateTokenByRefreshToken(string refreshToken)
        {
            throw new NotImplementedException();
        }

        public async Task<Response<NoDataDto>> RevokeRefreshToken(string refreshToken)
        {
            throw new NotImplementedException();
        }
    }
}
