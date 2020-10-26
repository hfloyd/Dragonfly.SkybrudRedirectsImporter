namespace Dragonfly.SkybrudRedirectsImporter
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Mvc;
    using Skybrud.Umbraco.Redirects.Models;

    public static partial class Constants
    {
        #region RedirectType
        public enum RedirectType
        {
            Temporary,
            Permanent
        }

        public static Dictionary<RedirectType, string> RedirectTypesWithDisplayText()
        {
            var dict = new Dictionary<RedirectType, string>();

            dict.Add(RedirectType.Temporary, "Temporary");
            dict.Add(RedirectType.Permanent, "Permanent");

            return dict;
        }

        public static IEnumerable<SelectListItem> RedirectTypesSelectList()
        {
            var dict = RedirectTypesWithDisplayText();
            var options = dict.Select(d => new SelectListItem
            {
                Value = d.Key.ToString(),
                Text = d.Value.ToString()
            });

            //if default option needed... (string DefaultValue, string DefaultText)
            //if (!string.IsNullOrEmpty(DefaultValue))
            //{
            //    var defaultSelect = Enumerable.Repeat(new SelectListItem
            //    {
            //        Value = DefaultId,
            //        Text = DefaultText
            //    }, count: 1);

            //    return defaultSelect.Concat(options);
            //}

            return options;
        }

        #endregion

        #region LinkMode
        public enum LinkMode
        {
            Content,
            Media,
            Url
        }

        public static Dictionary<LinkMode, string> LinkModesWithDisplayText()
        {
            var dict = new Dictionary<LinkMode, string>();

            dict.Add(LinkMode.Content, "Content");
            dict.Add(LinkMode.Media, "Media");
            dict.Add(LinkMode.Url, "Url");

            return dict;
        }

        public static IEnumerable<SelectListItem> LinkModesSelectList()
        {
            var dict = LinkModesWithDisplayText();
            var options = dict.Select(d => new SelectListItem
            {
                Value = d.Key.ToString(),
                Text = d.Value.ToString()
            });

            //if default option needed... (string DefaultValue, string DefaultText)
            //if (!string.IsNullOrEmpty(DefaultValue))
            //{
            //    var defaultSelect = Enumerable.Repeat(new SelectListItem
            //    {
            //        Value = DefaultId,
            //        Text = DefaultText
            //    }, count: 1);

            //    return defaultSelect.Concat(options);
            //}

            return options;
        }

        #endregion

    
    }
}

namespace Dragonfly.SkybrudRedirectsImporter.Models
{
    using System.Collections.Generic;
    using Skybrud.Umbraco.Redirects.Models;

    public class FormInputsImport
    {
        public Constants.RedirectType Type { get; set; }
        public string Filename { get; set; }
        public int SiteRootNode { get; set; }

        public  bool ForwardQueryString { get; set; }

        public IEnumerable<RedirectRootNode> AvailableRootNodes { get; set; }

    }
}
