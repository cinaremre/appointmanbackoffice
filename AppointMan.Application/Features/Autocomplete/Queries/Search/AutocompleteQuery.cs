using AppointMan.Application.Common.DTOs;
using AppointMan.Application.Common.Results;
using MediatR;

namespace AppointMan.Application.Features.Autocomplete.Queries.Search
{
    /// <summary>
    /// Unified autocomplete query spanning the address hierarchy and the
    /// business/service category trees.
    /// </summary>
    public class AutocompleteQuery : IRequest<Result<List<AutocompleteItemDto>>>
    {
        /// <summary>
        /// Source discriminator: city, district, locality, neighborhood,
        /// businessCategory or serviceCategory.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>Search term. At least 3 characters are required.</summary>
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// Optional parent identifier. When supplied, results are scoped to
        /// that parent; when omitted the search runs globally.
        /// </summary>
        public string? ParentId { get; set; }

        /// <summary>Maximum number of returned items (1-25).</summary>
        public int Limit { get; set; } = 10;
    }
}
