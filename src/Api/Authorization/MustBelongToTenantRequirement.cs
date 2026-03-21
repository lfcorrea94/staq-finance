using Microsoft.AspNetCore.Authorization;

namespace StaqFinance.Api.Authorization;

public sealed class MustBelongToTenantRequirement : IAuthorizationRequirement { }
