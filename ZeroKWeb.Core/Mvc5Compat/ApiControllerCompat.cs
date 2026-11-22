namespace System.Web.Http
{
    /// <summary>
    /// MVC 5's Web API base class, so <c>Zero-K.info/ApiControllers/WhrController.cs</c> links
    /// into the port instead of being left behind.
    ///
    /// On .NET Framework, Web API is a SECOND pipeline next to MVC: its own
    /// <c>HttpConfiguration</c>, its own route table (<c>WebApiConfig.Register</c>), its own
    /// controller base. ASP.NET Core merged the two - one controller model, one routing system -
    /// so the base class carries no behaviour here and exists to let the `: ApiController` in a
    /// linked file resolve.
    ///
    /// <see cref="Microsoft.AspNetCore.Mvc.ControllerBase"/> rather than <c>Controller</c>, which
    /// is the closer twin: neither has view support, and an action returning a model gets
    /// serialized by the framework in both.
    ///
    /// [ApiController] is deliberately NOT applied. It changes model binding and turns a
    /// validation failure into an automatic 400 with a ProblemDetails body - useful, and not what
    /// MVC 5 does, so applying it would be a behaviour change dressed up as a port.
    /// </summary>
    public abstract class ApiController : Microsoft.AspNetCore.Mvc.ControllerBase
    {
    }
}
