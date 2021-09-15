namespace Dragonfly.SkybrudRedirectsImporter
{
    public partial class Constants
    {
        public const string AppPluginsPath = "~/App_Plugins/Dragonfly.SkybrudRedirectsImporter/";
        public const string RazorViewsPath = "~/App_Plugins/Dragonfly.SkybrudRedirectsImporter/RazorViews/";
        public const string FileUploadPath = "~/App_Data/TEMP/RedirectsImporter/";
        public const string FileName = "redirects{0}.csv";

        #region NodeType
        public enum NodeType
        {
            Content,
            Media,
            Unknown
        }

        public static NodeType GetNodeType(string TypeString)
        {
            switch (TypeString)
            {
                case "Content":
                    return NodeType.Content;

                case "Media":
                    return NodeType.Media;

                default:
                    return NodeType.Unknown;
            }
        }

        #endregion
    }

}
