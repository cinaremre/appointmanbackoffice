using Rezervise.Application.Features.Admin.Auth.Commands.AdminLogin;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.BackOffice.Api.Controllers
{
    /// <summary>
    /// Backoffice giris ucu. Token uretimi mevcut ITokenService uzerinden yapilir;
    /// audience "Rezervise-Backoffice" olarak yapilandirmadan gelir.
    /// </summary>
    [ApiController]
    [Route("api/v1/admin/auth")]
    public sealed class AdminAuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AdminAuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Yonetici girisi. Basarili yanit access token doner; hatalar
        /// ProblemDetails olarak eslenir.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("login")]
        [ProducesResponseType(typeof(AdminLoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] AdminLoginCommand command)
        {
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
                return Ok(result.Value);

            return result.Error!.Code switch
            {
                "UNAUTHORIZED" => Unauthorized(new ProblemDetails
                {
                    Type = "https://api.rezervise.com/problems/invalid-credentials",
                    Title = "Giris basarisiz.",
                    Status = StatusCodes.Status401Unauthorized,
                    Detail = "E-posta veya sifre hatali, ya da bu hesap backoffice erisimine sahip degil."
                }),
                _ => BadRequest(new ProblemDetails
                {
                    Type = "https://api.rezervise.com/problems/validation",
                    Title = "Giris basarisiz.",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = result.Error.Message
                })
            };
        }
    }
}
