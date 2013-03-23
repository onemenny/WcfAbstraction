using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace WcfAbstraction.Configuration
{
	/// <summary>
	/// Provides methods for handling standard application configuration files editing
	/// </summary>
    public static class AppConfigHandler
    {
        #region ctor
        static AppConfigHandler()
        {
        }
        #endregion

        #region Properties

        /// <summary>
        /// Gets the name of protected configuration provider
        /// </summary>
        public static string ProtectedConfigurationProviderName
        {
            get { return "WcfAbstractionProtectedConfigurationProvider"; }
        }
        
        #endregion

        #region Section Handling Methods

        private static T GetSectionByType<T>(System.Configuration.Configuration config) where T : ConfigurationSection
        {
            T ret = null;
            if (config != null)
            {
                for (int i = 0; i < config.Sections.Count; i++)
                {
                    //if there is an invalid section, we might throw an exception here
                    //this is undesirable and we should just protect ourselves
                    ConfigurationSection section = null;
                    try
                    {
                        section = config.Sections.Get(i);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex);
                    }

                    if (section != null && section is T)
                    {
                        ret = section as T;
                        break;
                    }
                }
            }

            return ret;
        }

        /// <summary>
        /// Gets first configuration section with specified type for current executable or null of no such section is found
        /// </summary>
        /// <typeparam name="T">Type of section to search for</typeparam>
        /// <returns>Section instance found or null</returns>
        /// <remarks>
        /// This methods searches for a first section of type T and returns it, or null if no such section is found
        /// </remarks>
        public static T GetSectionByType<T>() where T : ConfigurationSection
        {
            ////cheking whether System.Web.HttpContext.Currentis null is not enough, but we can only have one possible fallback - that is: web.
            System.Configuration.Configuration config = null;
            try
            {
                config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                return GetSectionByType<T>(config);
            }
            catch (Exception ex)
            {
                throw new Exception("When not in Exe process, turn WcfAbstraction.Configuration.AppConfigHandler.UseExeConfiguration off", ex);
            }
        }

        /// <summary>
        /// Gets first configuration section with specified type in specific config file or null of no such section is found
        /// </summary>
        /// <typeparam name="T">Type of section to search for</typeparam>
        /// <param name="configFilePath">Path to configuration file</param>
        /// <returns>Section instance found or null</returns>
        public static T GetSectionByType<T>(string configFilePath) where T : ConfigurationSection
        {
            ExeConfigurationFileMap map = new ExeConfigurationFileMap { ExeConfigFilename = configFilePath };
            System.Configuration.Configuration config = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
            return GetSectionByType<T>(config);
        }
        
        /// <summary>
        /// Removes configuration section (if present) from target application configuration file
        /// </summary>
        /// <param name="appConfigPath">Path to application configuration file</param>
        /// <param name="sectionName">Section name to delete</param>
        /// <returns>True if configuration file has been changed, false otherwise</returns>
        public static bool RemoveConfigurationSection(string appConfigPath, string sectionName)
        {
            bool ret = false;
            XDocument doc = XDocument.Load(appConfigPath);

            XElement element = (from elem in doc.Root.Elements("configSections").Elements("section")
                                where (string)elem.Attribute("name") == sectionName
                                select elem).FirstOrDefault();
            if (element != null)
            {
                element.Remove();
                ret = true;
            }

            element = doc.Root.Element(sectionName);
            if (element != null)
            {
                element.Remove();
                ret = true;
            }

            if (ret)
            {
                doc.Save(appConfigPath);
            }

            return ret;
        }

        #endregion

        #region Key-Value Methods

        /// <summary>
        /// Tries the get value.
        /// </summary>
        /// <typeparam name="T">Generic T</typeparam>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static bool TryGetValue<T>(string keyName, out T value)
        {
            string setting = ConfigurationManager.AppSettings[keyName];

            if (setting != null)
            {
                try
                {
                    value = (T)Convert.ChangeType(setting, typeof(T));
                    return true;
                }
                catch
                {
                }
            }

            value = default(T);
            return false;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <typeparam name="T">Generic T</typeparam>
        /// <param name="keyName">Name of the key.</param>
        /// <returns></returns>
        public static T GetValue<T>(string keyName)
        {
            return GetValue(keyName, default(T));
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <typeparam name="T">Generic T</typeparam>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns></returns>
        public static T GetValue<T>(string keyName, T defaultValue)
        {
            T value;
            if (TryGetValue<T>(keyName, out value))
            {
                return value;
            }

            return defaultValue;
        }

        /// <summary>
        /// Gets a name-value section as list.
        /// </summary>
        /// <param name="sectionName">Name of the section.</param>
        /// <returns></returns>
        public static List<KeyValuePair<string, string>> GetNameValueSectionAsList(string sectionName)
        {
            NameValueCollection col = (NameValueCollection)ConfigurationManager.GetSection(sectionName);
            List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
            for (int i = 0; i < col.Count; ++i)
            {
                list.Add(new KeyValuePair<string, string>(col.AllKeys[i], col[i]));
            }

            return list;
        }

        #endregion
    }
}
