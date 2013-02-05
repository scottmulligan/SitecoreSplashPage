/*
 * Version: 0.5
 * 
 * Homepage: http://blog.scottymulligan.com
 * GitHub: https://github.com/scottmulligan/SitecoreSplashPage
 * Twitter: @scottmulligan
 * 
 * LEGAL:
 * Sitecore Adaptive Images by Scott Mulligan is licensed under a Creative Commons Attribution 3.0 Unported License.
 */

using System;
using System.Web;
using Sitecore.Links;
using Sitecore.Data.Items;

namespace Sitecore.Sharedsource
{

    public class SplashPageResolver
    {

        /// <summary>
        /// Gets or sets a value indicating whether [set language from the browser preferences].
        /// </summary>
        public bool SetLangFromBrowserPreferences { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [always show splash page for base URL].
        /// </summary>
        public bool AlwaysShowForBaseUrl { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [set the culture].
        /// </summary>
        public bool SetCulture { get; set; }

        /// <summary>
        /// Gets or sets the splash page GUID.
        /// </summary>
        public string SplashPageGuid { get; set; }

        /// <summary>
        /// Processes the specified args.
        /// </summary>
        /// <param name="args">The HttpRequestArgs.</param>
        public void Process(Sitecore.Pipelines.HttpRequest.HttpRequestArgs args)
        {
            if (Sitecore.Context.Site == null)
                return;

            Sitecore.Data.Database db = Sitecore.Context.Database;

            // Set as the requested item
            Item itemRequested = Sitecore.Context.Item;

            // Set as the splash page item
            Item splashPageItem = db.GetItem(SplashPageGuid);

            // Set as the "Home Page" item for the site
            Item startItem = Sitecore.Context.Database.GetItem(Sitecore.Context.Site.StartPath);

            // Set the urlOptions to never include the embedded language in the URL 
            var urlOptions = LinkManager.GetDefaultUrlOptions();
            urlOptions.LanguageEmbedding = LanguageEmbedding.Never;

            // Set the URL for the Home Page item and the raw URL requested in the browser
            string startItemUrl = LinkManager.GetItemUrl(startItem, urlOptions).ToLower();
            string rawUrl = Sitecore.Web.WebUtil.GetRawUrl().ToLower();

            // If the requested item is the Home Page item and the AlwaysShowForBaseUrl setting is TRUE, then display the splash page
            if (AlwaysShowForBaseUrl && startItemUrl.Equals(rawUrl) && splashPageItem != null)
            {
                Sitecore.Context.Item = splashPageItem;
                return;
            }

            // Return if the requested item is null OR is not under the Home Page for the site OR the language cookie is set
            if (itemRequested == null ||
                !itemRequested.Paths.FullPath.ToLower().StartsWith(Sitecore.Context.Site.StartPath.ToLower()) ||
                Sitecore.Web.WebUtil.GetCookieValue(Sitecore.Context.Site.GetCookieKey("lang")) != "")
                return;

            // If the SetLangFromBrowserPreferences setting is TRUE, then set the context language and language cookie (and the culture info if that setting is activated)
            if (SetLangFromBrowserPreferences)
            {
                bool languageSet = SetLanguageFromBrowserPreferences();

                if (languageSet)
                {
                    SetCultureInfo();
                    return;
                }
            }

            // If the splash page item is not null then display the splash page and redirect to the requested item afterwards
            if (splashPageItem != null)
            {
                Sitecore.Context.Item = splashPageItem;
                Sitecore.Context.Items.Add("requestedItem", itemRequested);
            }
        }

        /// <summary>
        /// Sets the language from browser preferences.
        /// </summary>
        private bool SetLanguageFromBrowserPreferences()
        {
            string langs = HttpContext.Current.Request.ServerVariables["HTTP_ACCEPT_LANGUAGE"];

            if (!String.IsNullOrEmpty(langs))
            {
                foreach (string lang in Sitecore.StringUtil.Split(langs, ',', true))
                {
                    string langName = lang;

                    if (lang.IndexOf(';') > -1)
                    {
                        langName = lang.Substring(0, lang.IndexOf(';'));
                    }

                    if (SetLanguage(langName))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Sets the language.
        /// </summary>
        /// <param name="languageName">Name of the language.</param>
        private bool SetLanguage(string languageName)
        {
            if (!String.IsNullOrEmpty(languageName))
            {
                foreach (Sitecore.Globalization.Language compare in Sitecore.Context.Item.Database.Languages)
                {
                    if (languageName.Equals(compare.Name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (HasVersionInLanguage(Sitecore.Context.Item, compare))
                        {
                            SetContextLanguage(compare);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Determines whether [has version in language] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="language">The language.</param>
        /// <returns>
        ///   <c>true</c> if [has version in language] [the specified item]; otherwise, <c>false</c>.
        /// </returns>
        private static bool HasVersionInLanguage(Item item, Sitecore.Globalization.Language language)
        {
            Item langItem = Sitecore.Context.Item.Database.GetItem(Sitecore.Context.Item.ID, language);
            return langItem.Versions.Count > 0;
        }

        /// <summary>
        /// Sets the context language.
        /// </summary>
        /// <param name="language">The language.</param>
        private void SetContextLanguage(Sitecore.Globalization.Language language)
        {
            Sitecore.Context.SetLanguage(language, true);
            Sitecore.Context.Item = Sitecore.Context.Item.Database.GetItem(Sitecore.Context.Item.ID, language);

            string cookieName = Sitecore.Context.Site.GetCookieKey("lang");
            Sitecore.Web.WebUtil.SetCookieValue(cookieName, language.Name, DateTime.MaxValue);
        }

        /// <summary>
        /// Sets the culture info.
        /// </summary>
        private void SetCultureInfo()
        {
            if (SetCulture)
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture =
                    new System.Globalization.CultureInfo(Sitecore.Context.Language.Name);
                System.Threading.Thread.CurrentThread.CurrentCulture =
                    System.Globalization.CultureInfo.CreateSpecificCulture(Sitecore.Context.Language.Name);
            }
        }

    }

}