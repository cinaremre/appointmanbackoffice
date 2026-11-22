using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Users.Queries.GetUserByEmail
{
    public class GetUserByEmailQueryHandler : IRequestHandler<GetUserByEmailQuery, Result<UserDto>>
    {
        private readonly IRepository<User> _userRepository;

        public GetUserByEmailQueryHandler(IRepository<User> userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<UserDto>> Handle(GetUserByEmailQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Email ile kullanici ara
                var users = await _userRepository.FindAsync(
                    u => u.Email == request.Email && !u.IsDeleted,
                    cancellationToken);

                var user = users.FirstOrDefault();

                // 2. Kullanici bulunamadiysa hata dondur
                if (user is null)
                    return Result<UserDto>.Failure(Error.NotFound("Kullanici", request.Email));

                // 3. Verileri DTO'ya donustur
                var dto = new UserDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Role = user.Role,
                    DateOfBirth = user.DateOfBirth,
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    IsEmailVerified = user.IsEmailVerified,
                    IsPhoneVerified = user.IsPhoneVerified,
                    CreatedAt = user.CreatedAt
                };

                // 4. Basarili sonucu dondur
                return Result<UserDto>.Success(dto);
            }
            catch (Exception ex)
            {
                return Result<UserDto>.Failure(Error.Internal("Kullanici bilgileri getirilirken bir hata olustu.", ex.Message));
            }
        }
    }
}