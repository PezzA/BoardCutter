using Microsoft.AspNetCore.SignalR;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

public class TestHubComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // first we are going to add signalR to the serviceCollection if no hubs have been added yet
        // this is just in case Umbraco ever decides to use a different technology
        if (!builder.Services.Any(x => x.ServiceType == typeof(IHubContext<>)))
        {
            builder.Services.AddSignalR();
        }

        // next is adding the routes we defined earlier
        builder.Services.AddSingleton<TestHubRoutes>();
        builder.Services.Configure<UmbracoPipelineOptions>(options =>
        {
            options.AddFilter(new UmbracoPipelineFilter(
                "test",
                 endpoints: applicationBuilder =>
                 {
                     applicationBuilder.UseEndpoints(e =>
                     {
                         var hubRoutes = applicationBuilder.ApplicationServices.GetRequiredService<TestHubRoutes>();
                         hubRoutes.CreateRoutes(e);
                     });
                 }
            ));
        });
    }
}