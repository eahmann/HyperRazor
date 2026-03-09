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
        return View<AccessRequestsPage>(new
        {
            CompletedRequestId = completed
        }, cancellationToken);
    }

    [HttpGet("/access-requests/{requestId:int}/review")]
    public Task<IResult> ReviewAccessRequest(int requestId, CancellationToken cancellationToken)
    {
        return View<ReviewAccessRequestPage>(new
        {
            RequestId = requestId
        }, cancellationToken);
    }

    [HttpGet("/incidents")]
    public Task<IResult> Incidents(CancellationToken cancellationToken)
    {
        return View<IncidentsPage>(cancellationToken: cancellationToken);
    }

    [HttpGet("/incidents/{incidentId:int}/triage")]
    public Task<IResult> IncidentTriage(int incidentId, CancellationToken cancellationToken)
    {
        return View<IncidentTriagePage>(new
        {
            IncidentId = incidentId
        }, cancellationToken);
    }
}
