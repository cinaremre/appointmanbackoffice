using AppointMan.Application.Common.Authorization;
using AppointMan.Application.Features.Admin.Users.Commands.LockAdminUser;
using AppointMan.Application.Features.Admin.Users.Commands.RevokeUserSessions;
using AppointMan.Application.Features.Admin.Users.Commands.UnlockAdminUser;
using AppointMan.Application.Features.Admin.Users.Queries.GetAdminUserDetail;
using AppointMan.Application.Features.Admin.Users.Queries.GetAdminUsers;
using AppointMan.BackOffice.Api.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointMan.BackOffice.Api.Controllers
{
    /// <summary>
    /// Kullanici yonetimi (dokuman Modul 11). Okuma UserRead, kilit/acilis ve
    /// oturum iptali UserManage permission'ina baglidir. Kritik islemlerde
    /// govdede gerekce (reason) zorunludur.
    /// </summary>
    [Route("api/v1/admin/users")]
    public sealed class AdminUserController : BackOfficeControllerBase
    {
        private readonly IMediator _mediator;

        public AdminUserController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Kullanicilari filtreleyerek sayfalayarak listeler.</summary>
        [HttpGet]
        [Authorize(Policy = AdminPermissions.UserRead)]
        [ProducesResponseType(typeof(AdminResponse<IReadOnlyList<AdminUserListItemDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetList([FromQuery] GetAdminUsersQuery query)
        {
            var result = await _mediator.Send(query);
            if (result.IsFailure)
                return AdminErrorMapper.ToProblem(result.Error);

            return OkList(result.Value.Items, result.Value.Page, result.Value.PageSize, result.Value.TotalCount);
        }

        /// <summary>Tek kullanicinin detayini uyelikleriyle getirir.</summary>
        [HttpGet("{id:guid}")]
        [Authorize(Policy = AdminPermissions.UserRead)]
        [ProducesResponseType(typeof(AdminResponse<AdminUserDetailDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDetail(Guid id)
        {
            var result = await _mediator.Send(new GetAdminUserDetailQuery { UserId = id });
            if (result.IsFailure)
                return AdminErrorMapper.ToProblem(result.Error);

            return OkEnvelope(result.Value);
        }

        /// <summary>Kullanici hesabini kilitler. Gerekce zorunludur.</summary>
        [HttpPost("{id:guid}/lock")]
        [Authorize(Policy = AdminPermissions.UserManage)]
        public async Task<IActionResult> Lock(Guid id, [FromBody] AdminUserActionRequest? body)
        {
            var result = await _mediator.Send(new LockAdminUserCommand
            {
                UserId = id,
                Reason = body?.Reason
            });

            if (result.IsFailure)
                return AdminErrorMapper.ToProblem(result.Error);

            return OkEnvelope(new { id, status = "locked" });
        }

        /// <summary>Kilili hesabi tekrar acar. Gerekce zorunludur.</summary>
        [HttpPost("{id:guid}/unlock")]
        [Authorize(Policy = AdminPermissions.UserManage)]
        public async Task<IActionResult> Unlock(Guid id, [FromBody] AdminUserActionRequest? body)
        {
            var result = await _mediator.Send(new UnlockAdminUserCommand
            {
                UserId = id,
                Reason = body?.Reason
            });

            if (result.IsFailure)
                return AdminErrorMapper.ToProblem(result.Error);

            return OkEnvelope(new { id, status = "active" });
        }

        /// <summary>Kullanicinin tum oturumlarini gecersiz kilar. Gerekce zorunludur.</summary>
        [HttpPost("{id:guid}/revoke-sessions")]
        [Authorize(Policy = AdminPermissions.UserManage)]
        public async Task<IActionResult> RevokeSessions(Guid id, [FromBody] AdminUserActionRequest? body)
        {
            var result = await _mediator.Send(new RevokeUserSessionsCommand
            {
                UserId = id,
                Reason = body?.Reason
            });

            if (result.IsFailure)
                return AdminErrorMapper.ToProblem(result.Error);

            return OkEnvelope(new { id, sessionsRevoked = true });
        }

        /// <summary>Kilit/acilis/oturum iptali isteklerinin ortak govdesi.</summary>
        public sealed class AdminUserActionRequest
        {
            /// <summary>Yoneticinin islem gerekcesi; zorunludur.</summary>
            public string? Reason { get; set; }
        }
    }
}
