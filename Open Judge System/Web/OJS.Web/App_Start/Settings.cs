﻿namespace OJS.Web
{
    using System;
    using System.Configuration;

    public static class Settings
    {
        public static string CSharpCompilerPath => GetSetting("CSharpCompilerPath");

        public static string DotNetDisassemblerPath => GetSetting("DotNetDisassemblerPath");

        public static string JavaCompilerPath
        {
            get
            {
                return GetSetting("JavaCompilerPath");
            }
        }

        public static string JavaDisassemblerPath
        {
            get
            {
                return GetSetting("JavaDisassemblerPath");
            }
        }

        private static string GetSetting(string settingName)
        {
            if (ConfigurationManager.AppSettings[settingName] == null)
            {
                throw new Exception($"{settingName} setting not found in App.config file!");
            }

            return ConfigurationManager.AppSettings[settingName];
        }
    }
}
