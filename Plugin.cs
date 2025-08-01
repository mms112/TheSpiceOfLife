using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using LocalizationManager;
using ServerSync;
using UnityEngine;

namespace TheSpiceOfLife
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class TheSpiceOfLifePlugin : BaseUnityPlugin
    {
        internal const string ModName = "TheSpiceOfLife";
        internal const string ModVersion = "1.0.4";
        internal const string Author = "Azumatt";
        internal const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        internal static string ConnectionError = "";
        private readonly Harmony _harmony = new(ModGUID);
        public static readonly ManualLogSource TheSpiceOfLifeLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);
        private static readonly ConfigSync ConfigSync = new(ModGUID) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

        public enum Toggle
        {
            On = 1,
            Off = 0
        }

        public void Awake()
        {
            Localizer.Load();
            _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On, "When enabled, only server administrators can modify the mod's configuration settings. This ensures consistent gameplay experience across all players on a server.");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);
            DiminishingFactor = config("1 - General", "DiminishingFactor", 0.75f, "This value determines how much the benefits of a food item diminish with repeated consumption. For example, a factor of 0.75 means that each time the food is consumed past the threshold, its benefits (like health or stamina restored) are multiplied by 0.75, effectively reducing them by 25%.");
            ConsumptionThreshold = config("1 - General", "ConsumptionThreshold", 3, "This setting defines the number of times a player can consume the same food item before its benefits start diminishing. For example, if set to 3, the food will provide full benefits for the first three times it is eaten, and diminished benefits thereafter.");
            HistoryLength = config("1 - General", "HistoryLength", 5, "This value specifies the maximum number of unique food items tracked in the player's food history. If a food item hasn't been eaten in the last 'n' unique food consumptions (where 'n' is the history length), its consumption counter is reset, allowing it to provide full benefits again.");


            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();
        }

        private void OnDestroy()
        {
            //Config.Save(); Do not save the config, to keep the synced values
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                TheSpiceOfLifeLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                TheSpiceOfLifeLogger.LogError($"There was an issue loading your {ConfigFileName}");
                TheSpiceOfLifeLogger.LogError("Please check your config entries for spelling and format!");
            }
        }


        #region ConfigOptions

        private static ConfigEntry<Toggle> _serverConfigLocked = null!;
        public static ConfigEntry<float> DiminishingFactor = null!;
        public static ConfigEntry<int> ConsumptionThreshold = null!;
        public static ConfigEntry<int> HistoryLength = null!;

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription = new(description.Description + (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"), description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        #endregion
    }
}