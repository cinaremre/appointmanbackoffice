using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using AppointMan.Domain.Entities;
using AppointMan.Domain.Interfaces;
using MediatR;

namespace AppointMan.Application.Features.Users.Queries.GetUserById
{
    public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserDto>>
    {
        private readonly IRepository<User> _userRepository;

        public GetUserByIdQueryHandler(IRepository<User> userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Kullaniciyi ID'ye gore getir
                var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

                // 2. Kullanici bulunamadiysa hata dondur
                if (user is null)
                    return Result<UserDto>.Failure(Error.NotFound("Kullanici", request.UserId.ToString()));

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