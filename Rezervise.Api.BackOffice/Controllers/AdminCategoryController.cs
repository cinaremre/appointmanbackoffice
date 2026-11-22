using Rezervise.Application.Common.Authorization;
using Rezervise.Application.Common.Results;
using Rezervise.Application.Features.Admin.Categories.Commands.ArchiveBusinessCategory;
using Rezervise.Application.Features.Admin.Categories.Commands.CreateBusinessCategoryByAdmin;
using Rezervise.Application.Features.Admin.Categories.Commands.MoveBusinessCategory;
using Rezervise.Application.Features.Admin.Categories.Commands.UpdateBusinessCategoryByAdmin;
using Rezervise.Application.Features.Admin.Categories.Queries.GetBusinessCategoryTree;
using Rezervise.Application.Features.Admin.Categories.Queries.GetBusinessCategoryUsage;
using Rezervise.BackOffice.Api.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Rezervise.BackOffice.Api.Controllers
{
    /// <summary>
    /// Kategori yonetimi (dokuman Modul 9). Tum isletme kategori agaci
    /// uzerinde okuma/olusturma/guncelleme/tasima/arsivleme islemleri.
    /// Not: ASP.NET Core sinif seviyesi route'lari birlesmedigi icin
    /// tam prefix burada tekrar yazilir.
    /// </summary>
    [Authorize]
    [Route("api/v1/admin/categories")]
    public sealed class AdminCategoryController : BackOfficeControllerBase
    {
        private readonly IMediator _mediator;

        public AdminCategoryController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>Tum kategori agaci; duz liste + ParentId (frontend agaci kurar).</summary>
        [HttpGet]
        [Authorize(Policy = AdminPermissions.CategoryRead)]
        public async Task<IActionResult> GetTree(
            [FromQuery] bool includeInactive = true,
            CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(
                new GetBusinessCategoryTreeQuery { IncludeInactive = includeInactive },
                cancellationToken);

            return result.IsSuccess
                ? OkEnvelope(result.Value)
                : result.ToProblem(Response.Headers["X-Correlation-ID"].ToString());
        }

        /// <summary>Yeni kategori olusturur; ayni adli aktif kardes Conflict doner.</summary>
        [HttpPost]
        [Authorize(Policy = AdminPermissions.CategoryCreate)]
        public async Task<IActionResult> Create(
            [FromBody] CreateBusinessCategoryByAdminCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);

            return result.IsSuccess
                ? OkEnvelope(result.Value)
                : result.ToProblem(Response.Headers["X-Correlation-ID"].ToString());
        }

        /// <summary>Kategori ad/sira/durum alanlarini gunceller.</summary>
        [HttpPatch("{id:guid}")]
        [Authorize(Policy = AdminPermissions.CategoryUpdate)]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] UpdateBusinessCategoryByAdminCommand command,
            CancellationToken cancellationToken)
        {
            command.CategoryId = id;

            var result = await _mediator.Send(command, cancellationToken);

            return result.IsSuccess
                ? OkEnvelope(result.Value)
                : result.ToProblem(Response.Headers["X-Correlation-ID"].ToString());
        }

        /// <summary>Kategoriyi baska bir uste tasir; dongu korumasi vardir.</summary>
        [HttpPost("{id:guid}/move")]
        [Authorize(Policy = AdminPermissions.CategoryUpdate)]
        public async Task<IActionResult> Move(
            Guid id,
            [FromBody] MoveBusinessCategoryCommand command,
            CancellationToken cancellationToken)
        {
            command.CategoryId = id;

            var result = await _mediator.Send(command, cancellationToken);

            return result.IsSuccess
                ? OkEnvelope(result.Value)
                : result.ToProblem(Response.Headers["X-Correlation-ID"].ToString());
        }

        /// <summary>Kategoriyi arsivler; reason zorunlu, aktif cocuk varsa Conflict.</summary>
        [HttpPost("{id:guid}/archive")]
        [Authorize(Policy = AdminPermissions.CategoryArchive)]
        public async Task<IActionResult> Archive(
            Guid id,
            [FromBody] ArchiveBusinessCategoryCommand command,
            CancellationToken cancellationToken)
        {
            command.CategoryId = id;

            var result = await _mediator.Send(command, cancellationToken);

            return result.IsSuccess
                ? OkEnvelope(result.Value)
                : result.ToProblem(Response.Headers["X-Correlation-ID"].ToString());
        }

        /// <summary>Kategoriye bagli isletme sayisi; arsiv oncesi etki analizi.</summary>
        [HttpGet("{id:guid}/usage")]
        [Authorize(Policy = AdminPermissions.CategoryRead)]
        public async Task<IActionResult> GetUsage(
            Guid id,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetBusinessCategoryUsageQuery { CategoryId = id },
                cancellationToken);

            return result.IsSuccess
                ? OkEnvelope(result.Value)
                : result.ToProblem(Response.Headers["X-Correlation-ID"].ToString());
        }
    }
}
