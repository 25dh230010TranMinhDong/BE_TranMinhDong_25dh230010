using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
namespace MyStore.Web
{
    public class MvcApplication:HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            // Apply the non-destructive ShippingFee upgrade on startup, if permitted.
            // Do not prevent the home page from loading if SQL Server is unavailable.
            try
            {
                Services.OrderSchemaUpdater.EnsureShippingFee();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Trace.TraceError("MyStore ShippingFee upgrade: " + ex.Message);
            }
        }
    }
}
