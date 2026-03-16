using HyperRazor.Demo.Mvc.Components.Pages.Tasks;
using HyperRazor.Demo.Mvc.Components.Pages.Workbench;
using HyperRazor.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace HyperRazor.Demo.Mvc.Controllers;

[ApiController]
public sealed class OperationsController : HrController
{
    [HttpGet("/access-requests")]
    public Task<IResult> AccessRequests([FromQuery] int? completed, CancellationToken cancellationToken)
    {
        return Page<AccessRequestsPage>(new
        {
            CompletedRequestId = completed
        }, cancellationToken);
    }

    [HttpGet("/access-requests/{requestId:int}/review")]
    public Task<IResult> ReviewAccessRequest(int requestId, CancellationToken cancellationToken)
    {
        return Page<ReviewAccessRequestPage>(new
        {
            RequestId = requestId
        }, cancellationToken);
    }

    [HttpGet("/incidents")]
    public Task<IResult> Incidents(CancellationToken cancellationToken)
    {
        return Page<IncidentsPage>(cancellationToken: cancellationToken);
    }

    [HttpGet("/incidents/{incidentId:int}/triage")]
    public Task<IResult> IncidentTriage(int incidentId, CancellationToken cancellationToken)
    {
        return Page<IncidentTriagePage>(new
        {
            IncidentId = incidentId
        }, cancellationToken);
    }
}
