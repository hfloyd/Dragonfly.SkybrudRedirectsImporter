﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dragonfly.SkybrudRedirectsImporter.Utilities
{
    using Umbraco.Core.Models;
    using Umbraco.Core.Models.PublishedContent;
    using Umbraco.Web;

    //TODO: Move to Dragonfly.Umbraco8?
    public static class NodesHelper
    {
        public static IEnumerable<IPublishedContent> AllContentNodes(UmbracoHelper UmbHelper, int OnlyDescendantsOfNodeId)
        {
            var allContent = new List<IPublishedContent>();
            var root = UmbHelper.Content(OnlyDescendantsOfNodeId);
            allContent.AddRange(GetRecursiveNodes(root));
            return allContent;
        }
        public static IEnumerable<IPublishedContent> AllContentNodes(UmbracoHelper UmbHelper)
        {
            var allContent = new List<IPublishedContent>();
            var roots = UmbHelper.ContentAtRoot();
            foreach (var c in roots)
            {
                allContent.AddRange(GetRecursiveNodes(c));
            }

            return allContent;
        }

        public static IEnumerable<IPublishedContent> AllMediaNodes(UmbracoHelper UmbHelper)
        {
            var allMedia = new List<IPublishedContent>();
            var roots = UmbHelper.MediaAtRoot();
            foreach (var m in roots)
            {
                allMedia.AddRange(GetRecursiveNodes(m));
            }

            return allMedia;
        }

        private static IEnumerable<IPublishedContent> GetRecursiveNodes(IPublishedContent Content)
        {
            var allContent = new List<IPublishedContent>();
            if (Content != null)
            {
                allContent.Add(Content);

                if (Content.Children.Any())
                {
                    foreach (var child in Content.Children)
                    {
                        allContent.AddRange(GetRecursiveNodes(child));
                    }
                }
            }

            return allContent;
        }

       
    }
}
