using Rezervise.Application.Common.DTOs;
using Rezervise.Application.Common.Results;
using Rezervise.Domain.Entities;
using Rezervise.Domain.Interfaces;
using MediatR;

namespace Rezervise.Application.Features.Users.Queries.GetUserById
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
                // 1. Kullanıcıyı ID'ye göre getir
                var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

                // 2. Kullanıcı bulunamadıysa hata döndür
                if (user is null)
                    return Result<UserDto>.Failure(Error.NotFound("Kullanıcı", request.UserId.ToString()));

                // 3. Verileri DTO'ya dönüştür
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
                    CityId = user.CityId,
                    DistrictId = user.DistrictId,
                    IsEmailVerified = user.IsEmailVerified,
                    IsPhoneVerified = user.IsPhoneVerified,
                    CreatedAt = user.CreatedAt
                };

                // 4. Başarılı sonucu döndür
                return Result<UserDto>.Success(dto);
            }
            catch (Exception ex)
            {
                return Result<UserDto>.Failure(Error.Internal("Kullanıcı bilgileri getirilirken bir hata oluştu.", ex.Message));
            }
        }
    }
}