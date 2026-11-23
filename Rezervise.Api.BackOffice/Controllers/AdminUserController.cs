using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Features.Admin.Users.Commands.LockAdminUser;
using Rezervise.Application.Features.Admin.Users.Commands.RevokeUserSessions;
using Rezervise.Application.Features.Admin.Users.Commands.UnlockAdminUser;
using Rezervise.Application.Features.Admin.Users.Queries.GetAdminUserDetail;
using Rezervise.Application.Features.Admin.Users.Queries.GetAdminUsers;
using Rezervise.BackOffice.Api.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.BackOffice.Api.Controllers
{
    /// <summary>
    /// Kullanıcı yönetimi (doküman Modül 11). Okuma UserRead, kilit/açılış ve
    /// oturum iptali UserManage permission'ina bağlıdır. Kritik işlemlerde
    /// gövdede gerekçe (reason) zorunludur.
    /// </summary>
    [Route("api/v1/admin/users")]
    public sealed class AdminUserController : BackOfficeControllerBase
    {
        private readonly IMediator _mediator;

        public AdminUserController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Kullanıcıları filtreleyerek sayfalayarak listeler.</summary>
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

        /// <summary>Tek kullanıcının detayını üyelikleriyle getirir.</summary>
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

        /// <summary>Kullanıcı hesabını kilitler. Gerekçe zorunludur.</summary>
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

        /// <summary>Kilili hesabı tekrar açar. Gerekçe zorunludur.</summary>
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

        /// <summary>Kullanıcının tüm oturumlarını geçersiz kılar. Gerekçe zorunludur.</summary>
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

        /// <summary>Kilit/açılış/oturum iptali isteklerinin ortak gövdesi.</summary>
        public sealed class AdminUserActionRequest
        {
            /// <summary>Yöneticinin işlem gerekçesi; zorunludur.</summary>
            public string? Reason { get; set; }
        }
    }
}
