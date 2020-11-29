using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FormFlow
{
    public delegate IActionResult MissingInstanceHandler(FlowDescriptor flowDescriptor, HttpContext httpContext);
}
