﻿#region Copyright
// DotNetNuke® - http://www.dotnetnuke.com
// Copyright (c) 2002-2016
// by DotNetNuke Corporation
// All Rights Reserved
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Http;
using Dnn.PersonaBar.Library;
using Dnn.PersonaBar.Library.Attributes;
using Dnn.PersonaBar.Seo.Components;
using Dnn.PersonaBar.Seo.Services.Dto;
using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Controllers;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Tabs;
using DotNetNuke.Entities.Urls;
using DotNetNuke.Instrumentation;
using DotNetNuke.Services.Localization;
using DotNetNuke.Services.Url.FriendlyUrl;
using DotNetNuke.Web.Api;

namespace Dnn.PersonaBar.Seo.Services
{
    [ServiceScope(Scope = ServiceScope.Admin)]
    public class SeoController : PersonaBarApiController
    {
        private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof(SeoController));
        //private readonly Components.SeoController _controller = new Components.SeoController();
        private static readonly string LocalResourcesFile = Path.Combine("~/admin/Dnn.PersonaBar/App_LocalResources/Seo.resx");

        /// GET: api/SEO/GetGeneralSettings
        /// <summary>
        /// Gets general SEO settings
        /// </summary>
        /// <returns>General SEO settings</returns>
        [HttpGet]
        public HttpResponseMessage GetGeneralSettings()
        {
            try
            {
                var urlSettings = new FriendlyUrlSettings(PortalId);

                var replacementCharacterList = new List<KeyValuePair<string, string>>();
                replacementCharacterList.Add(new KeyValuePair<string, string>(Localization.GetString("minusCharacter", LocalResourcesFile), "-"));
                replacementCharacterList.Add(new KeyValuePair<string, string>(Localization.GetString("underscoreCharacter", LocalResourcesFile), "_"));

                var deletedPageHandlingTypes = new List<KeyValuePair<string, string>>();
                deletedPageHandlingTypes.Add(new KeyValuePair<string, string>(Localization.GetString("Do404Error", LocalResourcesFile), "Do404Error"));
                deletedPageHandlingTypes.Add(new KeyValuePair<string, string>(Localization.GetString("Do301RedirectToPortalHome", LocalResourcesFile), "Do301RedirectToPortalHome"));

                var response = new
                {
                    Success = true,
                    Settings = new
                    {
                        EnableSystemGeneratedUrls = urlSettings.ReplaceSpaceWith != FriendlyUrlSettings.ReplaceSpaceWithNothing,
                        urlSettings.ReplaceSpaceWith,
                        urlSettings.ForceLowerCase,
                        urlSettings.AutoAsciiConvert,
                        urlSettings.ForcePortalDefaultLanguage,
                        DeletedTabHandlingType = urlSettings.DeletedTabHandlingType.ToString(),
                        urlSettings.RedirectUnfriendly,
                        urlSettings.RedirectWrongCase
                    },
                    ReplacementCharacterList = replacementCharacterList,
                    DeletedPageHandlingTypes = deletedPageHandlingTypes
                };

                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (Exception exc)
            {
                Logger.Error(exc);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exc);
            }
        }

        /// POST: api/SEO/UpdateGeneralSettings
        /// <summary>
        /// Updates SEO general settings
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public HttpResponseMessage UpdateGeneralSettings(UpdateGeneralSettingsRequest request)
        {
            try
            {
                string characterSub = FriendlyUrlSettings.ReplaceSpaceWithNothing;
                if (request.EnableSystemGeneratedUrls)
                {
                    characterSub = request.ReplaceSpaceWith;
                }
                PortalController.UpdatePortalSetting(PortalId, FriendlyUrlSettings.ReplaceSpaceWithSetting, characterSub, false);
                PortalController.UpdatePortalSetting(PortalId, FriendlyUrlSettings.DeletedTabHandlingTypeSetting, request.DeletedTabHandlingType, false);
                PortalController.UpdatePortalSetting(PortalId, FriendlyUrlSettings.ForceLowerCaseSetting, request.ForceLowerCase ? "Y" : "N", false);
                PortalController.UpdatePortalSetting(PortalId, FriendlyUrlSettings.RedirectUnfriendlySetting, request.RedirectUnfriendly ? "Y" : "N", false);
                PortalController.UpdatePortalSetting(PortalId, FriendlyUrlSettings.RedirectMixedCaseSetting, request.RedirectWrongCase.ToString(), false);
                PortalController.UpdatePortalSetting(PortalId, FriendlyUrlSettings.UsePortalDefaultLanguageSetting, request.ForcePortalDefaultLanguage.ToString(), false);
                PortalController.UpdatePortalSetting(PortalId, FriendlyUrlSettings.AutoAsciiConvertSetting, request.AutoAsciiConvert.ToString(), false);

                DataCache.ClearPortalCache(PortalId, false);
                DataCache.ClearTabsCache(PortalId);

                return Request.CreateResponse(HttpStatusCode.OK, new { Success = true });
            }
            catch (Exception exc)
            {
                Logger.Error(exc);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exc);
            }
        }

        /// GET: api/SEO/GetRegexSettings
        /// <summary>
        /// Gets SEO regex settings
        /// </summary>
        /// <returns>General SEO regex settings</returns>
        [HttpGet]
        public HttpResponseMessage GetRegexSettings()
        {
            try
            {
                var urlSettings = new FriendlyUrlSettings(PortalId);

                var response = new
                {
                    Success = true,
                    Settings = new
                    {
                        urlSettings.IgnoreRegex,
                        urlSettings.DoNotRewriteRegex,
                        urlSettings.UseSiteUrlsRegex,
                        urlSettings.DoNotRedirectRegex,
                        urlSettings.DoNotRedirectSecureRegex,
                        urlSettings.ForceLowerCaseRegex,
                        urlSettings.NoFriendlyUrlRegex,
                        urlSettings.DoNotIncludeInPathRegex,
                        urlSettings.ValidExtensionlessUrlsRegex,
                        urlSettings.RegexMatch
                    }
                };

                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (Exception exc)
            {
                Logger.Error(exc);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exc);
            }
        }

        /// POST: api/SEO/UpdateRegexSettings
        /// <summary>
        /// Updates SEO regex settings
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public HttpResponseMessage UpdateRegexSettings(UpdateRegexSettingsRequest request)
        {
            try
            {
                List<KeyValuePair<string, string>> validationErrors = new List<KeyValuePair<string, string>>();
                if (!ValidateRegex(request.IgnoreRegex))
                {
                    validationErrors.Add(new KeyValuePair<string, string>("IgnoreRegex", Localization.GetString("ignoreRegExInvalidPattern", LocalResourcesFile)));
                }
                if (!ValidateRegex(request.DoNotRewriteRegex))
                {
                    validationErrors.Add(new KeyValuePair<string, string>("DoNotRewriteRegex", Localization.GetString("doNotRewriteRegExInvalidPattern", LocalResourcesFile)));
                }
                if (!ValidateRegex(request.UseSiteUrlsRegex))
                {
                    validationErrors.Add(new KeyValuePair<string, string>("UseSiteUrlsRegex", Localization.GetString("siteUrlsOnlyRegExInvalidPattern", LocalResourcesFile)));
                }
                if (!ValidateRegex(request.DoNotRedirectRegex))
                {
                    validationErrors.Add(new KeyValuePair<string, string>("DoNotRedirectRegex", Localization.GetString("doNotRedirectUrlRegExInvalidPattern", LocalResourcesFile)));
                }
                if (!ValidateRegex(request.DoNotRedirectSecureRegex))
                {
                    validationErrors.Add(new KeyValuePair<string, string>("DoNotRedirectSecureRegex", Localization.GetString("doNotRedirectHttpsUrlRegExInvalidPattern", LocalResourcesFile)));
                }
                if (!ValidateRegex(request.ForceLowerCaseRegex))
                {
                    validationErrors.Add(new KeyValuePair<string, string>("ForceLowerCaseRegex", Localization.GetString("preventLowerCaseUrlRegExInvalidPattern", LocalResourcesFile)));
                }
                if (!ValidateRegex(request.NoFriendlyUrlRegex))
                {
                    validationErrors.Add(new KeyValuePair<string, string>("NoFriendlyUrlRegex", Localization.GetString("doNotUseFriendlyUrlsRegExInvalidPattern", LocalResourcesFile)));
                }
                if (!ValidateRegex(request.DoNotIncludeInPathRegex))
                {
                    validationErrors.Add(new KeyValuePair<string, string>("DoNotIncludeInPathRegex", Localization.GetString("keepInQueryStringRegExInvalidPattern", LocalResourcesFile)));
                }
                if (!ValidateRegex(request.ValidExtensionlessUrlsRegex))
                {
                    validationErrors.Add(new KeyValuePair<string, string>("ValidExtensionlessUrlsRegex", Localization.GetString("urlsWithNoExtensionRegExInvalidPattern", LocalResourcesFile)));
                }
                if (!ValidateRegex(request.RegexMatch))
                {
                    validationErrors.Add(new KeyValuePair<string, string>("RegexMatch", Localization.GetString("validFriendlyUrlRegExInvalidPattern", LocalResourcesFile)));
                }

                if (validationErrors.Count > 0)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, new { Success = false, Errors = validationErrors });
                }
                else
                {
                    HostController.Instance.Update(FriendlyUrlSettings.IgnoreRegexSetting, request.IgnoreRegex, false);
                    HostController.Instance.Update(FriendlyUrlSettings.DoNotRewriteRegExSetting,
                        request.DoNotRewriteRegex, false);
                    HostController.Instance.Update(FriendlyUrlSettings.SiteUrlsOnlyRegexSetting,
                        request.UseSiteUrlsRegex, false);
                    HostController.Instance.Update(FriendlyUrlSettings.DoNotRedirectUrlRegexSetting,
                        request.DoNotRedirectRegex, false);
                    HostController.Instance.Update(FriendlyUrlSettings.DoNotRedirectHttpsUrlRegexSetting,
                        request.DoNotRedirectSecureRegex, false);
                    HostController.Instance.Update(FriendlyUrlSettings.PreventLowerCaseUrlRegexSetting,
                        request.ForceLowerCaseRegex, false);
                    HostController.Instance.Update(FriendlyUrlSettings.DoNotUseFriendlyUrlRegexSetting,
                        request.NoFriendlyUrlRegex, false);
                    HostController.Instance.Update(FriendlyUrlSettings.KeepInQueryStringRegexSetting,
                        request.DoNotIncludeInPathRegex, false);
                    HostController.Instance.Update(FriendlyUrlSettings.UrlsWithNoExtensionRegexSetting,
                        request.ValidExtensionlessUrlsRegex, false);
                    HostController.Instance.Update(FriendlyUrlSettings.ValidFriendlyUrlRegexSetting, request.RegexMatch,
                        false);

                    DataCache.ClearHostCache(false);
                    CacheController.FlushPageIndexFromCache();
                    CacheController.FlushFriendlyUrlSettingsFromCache();

                    return Request.CreateResponse(HttpStatusCode.OK, new {Success = true});
                }
            }
            catch (Exception exc)
            {
                Logger.Error(exc);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exc);
            }
        }

        private static bool ValidateRegex(string regexPattern)
        {
            try
            {
                var regex = new Regex(regexPattern);
                return true;
            }
            catch
            {
                //ignore
            }
            return false;
        }

        /// <summary>
        /// Tests the internal URL
        /// </summary>
        /// <returns>Various forms of the URL and any messages when they exist</returns>
        /// <example>
        /// GET /API/PersonaBar/Admin/SEO/TestUrl?pageId=53&amp;queryString=ab%3Dcd&amp;customPageName=test-page
        /// </example>
        [HttpGet]
        public HttpResponseMessage TestUrl(int pageId, string queryString, string customPageName)
        {
            try
            {
                var response = new
                {
                    Success = true,
                    Urls = TestUrlInternal(pageId, queryString, customPageName)
                };
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (Exception exc)
            {
                Logger.Error(exc);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exc);
            }
        }

        private IEnumerable<string> TestUrlInternal(int pageId, string queryString, string customPageName)
        {
            var provider = new DNNFriendlyUrlProvider();
            var tab = TabController.Instance.GetTab(pageId, PortalId, false);
            var pageName = string.IsNullOrEmpty(customPageName) ? Globals.glbDefaultPage : customPageName;
            return PortalAliasController.Instance.GetPortalAliasesByPortalId(PortalId).
                Select(alias => provider.FriendlyUrl(
                    tab, "~/Default.aspx?tabId=" + pageId + "&" + queryString, pageName, alias.HTTPAlias));
        }

        /// GET: api/SEO/TestUrlRewrite
        /// <summary>
        /// Tests the rewritten URL
        /// </summary>
        /// <returns>Rewitten URL and few other information about the URL ( language, redirection result and reason, messages)</returns>
        /// <example>
        /// GET /API/PersonaBar/Admin/SEO/TestUrlRewrite?uri=http%3A%2F%2Fmysite.com%2Ftest-page
        /// </example>
        [HttpGet]
        public HttpResponseMessage TestUrlRewrite(string uri)
        {
            try
            {
                var response = new
                {
                    Success = true,
                    RewritingResult = TestUrlRewritingInternal(uri)
                };
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (Exception exc)
            {
                Logger.Error(exc);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exc);
            }
        }

        private UrlRewritingResult TestUrlRewritingInternal(string uriString)
        {
            var rewritingResult = new UrlRewritingResult();
            try
            {
                var noneText = Localization.GetString("None", Localization.GlobalResourceFile);
                var uri = new Uri(uriString);
                var provider = new AdvancedUrlRewriter();
                var result = new UrlAction(uri.Scheme, uriString, Globals.ApplicationMapPath)
                {
                    RawUrl = uriString
                };
                var httpContext = new HttpContext(HttpContext.Current.Request, new HttpResponse(new StringWriter()));
                provider.ProcessTestRequestWithContext(httpContext, uri, true, result, new FriendlyUrlSettings(PortalId));
                rewritingResult.RewritingResult = string.IsNullOrEmpty(result.RewritePath) ? noneText : result.RewritePath;
                rewritingResult.Culture = string.IsNullOrEmpty(result.CultureCode) ? noneText : result.CultureCode;
                var tab = TabController.Instance.GetTab(result.TabId, result.PortalId, false);
                rewritingResult.IdentifiedPage = (tab != null ? tab.TabName : noneText);
                rewritingResult.RedirectionReason = Localization.GetString(result.Reason.ToString());
                rewritingResult.RedirectionResult = result.FinalUrl;
                var messages = new StringBuilder();
                foreach (var message in result.DebugMessages)
                {
                    messages.AppendLine(message);
                }
                rewritingResult.OperationMessages = messages.ToString();
            }
            catch (Exception ex)
            {
                rewritingResult.OperationMessages = ex.Message;
            }
            return rewritingResult;
        }
    }
}