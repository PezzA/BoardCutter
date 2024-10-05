using Microsoft.Extensions.Options;

using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common.Routing;

public class TestHubRoutes : IAreaRoutes
{
    private readonly IRuntimeState _runtimeState;
    private readonly string _umbracoPathSegment;

    public TestHubRoutes(
        IOptions<GlobalSettings> globalSettings,
        Umbraco.Cms.Core.Hosting.IHostingEnvironment hostingEnvironment,
        IRuntimeState runtimeState)
    {
        _runtimeState = runtimeState;
        _umbracoPathSegment = globalSettings.Value.GetUmbracoMvcArea(hostingEnvironment);
    }

    public void CreateRoutes(IEndpointRouteBuilder endpoints)
    {
        switch (_runtimeState.Level)
        {
            case Umbraco.Cms.Core.RuntimeLevel.Run:
                endpoints.MapHub<TestHub>(GetTestHubRoute());
                break;
        }

    }

    public string GetTestHubRoute()
    {
        return $"/{_umbracoPathSegment}/{nameof(TestHub)}";
    }
}