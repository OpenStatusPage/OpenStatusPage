using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenStatusPage.Server.Application.Configuration;
using OpenStatusPage.Server.Application.StatusPages.Commands;
using OpenStatusPage.Shared.Requests.Credentials;
using OpenStatusPage.Shared.Utilities;
using System.ComponentModel.DataAnnotations;

namespace OpenStatusPage.Server.Controllers;

[Route("auth/v1/[controller]")]
[ApiController]
[AllowAnonymous]
public class CredentialsValidationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly EnvironmentSettings _environmentSettings;

    public CredentialsValidationController(IMediator mediator, EnvironmentSettings environmentSettings)
    {
        _mediator = mediator;
        _environmentSettings = environmentSettings;
    }

    [HttpPost]
    public async Task<ActionResult<CredentialsValidationRequest.Response>> ValidateCredentialsAsync([FromBody, Required] CredentialsValidationRequest request)
    {
        try
        {
            var response = new CredentialsValidationRequest.Response()
            {
                ValidStatusPageCredentials = new()
            };

            if (request.DashboardCredentials != null && request.DashboardCredentials.ApiKey.Equals(_environmentSettings.ApiKey, StringComparison.OrdinalIgnoreCase))
            {
                response.ValidDashboardCredentials = request.DashboardCredentials;
            }

            if (request.StatusPageCredentials != null)
            {
                var statusPageIds = request.StatusPageCredentials.Select(y => y.StatusPageId).ToList();

                var statusPages = (await _mediator.Send(new StatusPagesQuery
                {
                    Query = new(query => query.Where(x => statusPageIds.Contains(x.Id)))
                }))?.StatusPages;

                if (statusPages != null)
                {
                    foreach (var statusPage in statusPages)
                    {
                        var credentials = request.StatusPageCredentials.First(x => x.StatusPageId == statusPage.Id);

                        if (!string.IsNullOrWhiteSpace(statusPage.Password) && (credentials.PasswordHash == SHA256Hash.Create(statusPage.Password)))
                        {
                            response.ValidStatusPageCredentials.Add(credentials);
                        }
                    }
                }
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            return Problem();
        }
    }
}
