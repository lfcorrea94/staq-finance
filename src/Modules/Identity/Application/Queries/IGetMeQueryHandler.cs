using StaqFinance.BuildingBlocks;
using StaqFinance.Modules.Identity.Application.DTOs;

namespace StaqFinance.Modules.Identity.Application.Queries;

public interface IGetMeQueryHandler
{
    Task<Result<MeResponse>> HandleAsync(
        GetMeQuery query,
        CancellationToken cancellationToken = default);
}
