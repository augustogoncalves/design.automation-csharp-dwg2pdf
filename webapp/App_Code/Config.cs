using Autodesk.Forge;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Configuration;

namespace webapp
{
  public class Config
  {
    /// <summary>
    /// The client ID of this app
    /// </summary>
    internal static string FORGE_CLIENT_ID { get { return GetAppSetting("FORGE_CLIENT_ID"); } }

    /// <summary>
    /// The client secret of this app
    /// </summary>
    internal static string FORGE_CLIENT_SECRET { get { return GetAppSetting("FORGE_CLIENT_SECRET"); } }

    /// <summary>
    /// List of scopes for Design Automation
    /// </summary>
    internal static Scope[] FORGE_SCOPE_DESIGN_AUTOMATION
    {
      get
      {
        return new Scope[] { Scope.CodeAll };
      }
    }

    /// <summary>
    /// Read settings from web.config.
    /// See appSettings section for more details.
    /// </summary>
    /// <param name="settingKey"></param>
    /// <returns></returns>
    private static string GetAppSetting(string settingKey)
    {
      return WebConfigurationManager.AppSettings[settingKey];
    }
  }
}