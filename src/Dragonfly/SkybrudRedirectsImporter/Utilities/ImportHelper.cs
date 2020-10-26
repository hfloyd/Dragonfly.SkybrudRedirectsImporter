namespace Dragonfly.SkybrudRedirectsImporter.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web.Mvc;
    using Skybrud.Umbraco.Redirects;
    using Skybrud.Umbraco.Redirects.Models;
    using Umbraco.Core.Models;
    using Umbraco.Core;
    using Umbraco.Core.Services;
    using Umbraco.Core.Services.Implement;

    public static class ImportHelper
    {

        /// <summary>
        /// Gets a list of root nodes based on the domains added to Umbraco. A root node will only be included in the
        /// list once - even if it has been assigned multiple domains.
        /// </summary>
        public static IEnumerable<RedirectRootNode> GetRootNodes(IRedirectsService SkybrudRedirectsService, IContentService CurrentContentService)
        {
            RedirectDomain[] domains = SkybrudRedirectsService.GetDomains();

            List<RedirectRootNode> temp = new List<RedirectRootNode>();

            foreach (RedirectDomain domain in domains.Where(x => x.RootNodeId > 0).DistinctBy(x => x.RootNodeId))
            {
                // Get the root node from the content service
                IContent content = CurrentContentService.GetById(domain.RootNodeId);

                // Skip if not found via the content service
                if (content == null) continue;

                // Skip if the root node is located in the recycle bin
                if (content.Path.StartsWith("-1,-20,")) continue;

                // Append the root node to the result
                temp.Add(RedirectRootNode.GetFromContent(content));

            }

            return temp.OrderBy(x => x.Id);

            //return new
            //{
            //    Total = temp.Count,
            //    Items = temp.OrderBy(x => x.Id)
            //};

        }

        public static IEnumerable<SelectListItem> RootNodesSelectList(IEnumerable<RedirectRootNode> RootNodes, string DefaultValue, string DefaultText)
        {
            var dict = new Dictionary<int, string>();
            foreach (var node in RootNodes)
            {
                dict.Add(node.Id, node.Name);
            }

            var options = dict.Select(d => new SelectListItem
            {
                Value = d.Key.ToString(),
                Text = d.Value.ToString()
            });

            //if default option needed... (string DefaultValue, string DefaultText)
            if (!string.IsNullOrEmpty(DefaultValue))
            {
                var defaultSelect = Enumerable.Repeat(new SelectListItem
                {
                    Value = DefaultValue,
                    Text = DefaultText
                }, count: 1);

                return defaultSelect.Concat(options);
            }

            return options;
        }


        //public static RedirectDestinationType ConvertLinkModeToRedirectDestinationType(Constants.LinkMode Mode)
        //{
        //    switch (Mode)
        //    {
        //        case Constants.LinkMode.Content:
        //            return Skybrud.Umbraco.Redirects.Models.RedirectDestinationType.Content;
        //            break;

        //        case Constants.LinkMode.Media:
        //            return RedirectDestinationType.Media;
        //            break;

        //        case Constants.LinkMode.Url:
        //            return RedirectDestinationType.Url;
        //            break;

        //        default:
        //            throw new ArgumentOutOfRangeException(nameof(Mode), Mode, null);
        //    }

        //}
    }
}
