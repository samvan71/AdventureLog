using System.Web;
using System.Web.Optimization;

namespace AdventureLog
{
    public class BundleConfig
    {
        // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at https://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.js"));

            bundles.Add(new ScriptBundle("~/bundles/autocomplete").Include(
                      "~/Scripts/jquery.auto-complete.js"));

            bundles.Add(new StyleBundle("~/Content/site").Include(
                      "~/Content/scss/custom.min.css",
                      "~/Content/summernote-bs4.css",
                      "~/Content/font-awesome.css",
                      "~/Content/font-awesome-all.css",
                      "~/Content/jquery.auto-complete.css",
                      "~/Content/Site.css"));

        }
    }
}
