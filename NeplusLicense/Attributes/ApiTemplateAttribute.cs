using System;
using Microsoft.AspNetCore.Mvc.Routing;

namespace NeplusLicense.Attributes
{
    public class ApiRouteTemplateAttribute : Attribute, IRouteTemplateProvider
    {
        public string Template => "{area:exists}/api/v{version:apiVersion}/[controller]/[action]";
        public int?   Order    { get; set; } = 0;
        public string Name     { get; set; }
    }
}