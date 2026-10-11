using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Quran
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Route for concordance files: /concordance/<word>.html
            routes.MapRoute(
                name: "Concordance",
                url: "concordance/{id}.html",
                defaults: new { controller = "Concordance", action = "Word" }
            );

            // Route for chapter files: /chapter/<id>.html
            routes.MapRoute(
                name: "Chapter",
                url: "chapter/{id}.html",
                defaults: new { controller = "Concordance", action = "Chapter" }
            );

            // Default MVC route
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}