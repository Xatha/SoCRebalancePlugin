using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using R2API.Utils;
using R2API;
using RoR2;

namespace SoCRebalancePlugin;


[R2APISubmoduleDependency(nameof(CommandHelper))]
internal static class SoC_Config
{
    private const string key_MaxPurchaseCount = "maxPurchaseCount";
    private const string key_RefreshTimer = "refreshTimer";
    private const string key_costMultiplier = "costMultiplier";

    internal static ConfigEntry<int> maxPurchaseCount;
    internal static ConfigEntry<float> refreshTimer;
    internal static ConfigEntry<float> costMultiplier;
    internal static ConfigEntry<bool> isBeaconHackingAllowed;
    internal static ConfigEntry<bool> deactivateIfRedItemDrops;

    private static Dictionary<string, ConfigEntry<int>> intConfigList = new Dictionary<string, ConfigEntry<int>>();
    private static Dictionary<string, ConfigEntry<float>> floatConfigList = new Dictionary<string, ConfigEntry<float>>();

    internal static void Init(ConfigFile configFile)
    {
        //Init our console commands into the game.
        R2API.Utils.CommandHelper.AddToConsoleWhenReady();

        //Configs
        maxPurchaseCount = configFile.Bind("Shrine of Chance: Maximum Purchase Count", "maxPurchaseCount", 2, "Determines the maximum purchase count from a shrine. The higher the value, the more items you can get from the shrine before it deactivates. VALUE MUST NOT BE NEGATIVE.");
        refreshTimer = configFile.Bind("Shrine of Chance: Cooldown", "refreshTimer", 2f, "Determines cooldown between every purchase/interaction. VALUE MUST NOT BE NEGATIVE.");
        costMultiplier = configFile.Bind("Shrine of Chance: Cost Multiplier", "costMultiplier", 1.4f, "Determines the next price of the shrine, by multiplying the previous price with costMultiplier. The value is in percentage as a decimal number. VALUE MUST NOT BE NEGATIVE.");
        isBeaconHackingAllowed = configFile.Bind("Shrine of Chance: Should Hacked Beacons Return To Vanilla Behaviour", "isBeaconHackingAllowed", false, "Normally, hacked beacons revert back to their vanilla behaviour for balancing reasons.");
        deactivateIfRedItemDrops = configFile.Bind("Shrine of Chance: Should Shrine Deactivate After Red Item Drops", "deactivateIfRedItemDrops", true, "Normally, when a red item drops, SoC deactivates for balancing reasons");

        //Makes dictionaries of the configs
        intConfigList = BundleConfigs<int>(AddConfigToBundle(key_MaxPurchaseCount, maxPurchaseCount));
        floatConfigList = BundleConfigs<float>(AddConfigToBundle(key_RefreshTimer, refreshTimer), AddConfigToBundle(key_costMultiplier, costMultiplier));

        CorrectNegativeConfigs(intConfigList, floatConfigList);
    }

    internal static void ApplyConfigs(ShrineChanceBehavior ShrineChanceBehavior)
    {
        ShrineChanceBehavior.SetFieldValue("maxPurchaseCount", SoC_Config.maxPurchaseCount.Value);
        ShrineChanceBehavior.SetFieldValue("costMultiplierPerPurchase", SoC_Config.costMultiplier.Value);
        ShrineChanceBehavior.SetFieldValue("refreshTimer", SoC_Config.refreshTimer.Value);
    }

    private static void Reload()
    {
        maxPurchaseCount.ConfigFile.Reload();
        refreshTimer.ConfigFile.Reload();
        costMultiplier.ConfigFile.Reload();
        isBeaconHackingAllowed.ConfigFile.Reload();
        deactivateIfRedItemDrops.ConfigFile.Reload();

        intConfigList = BundleConfigs<int>(AddConfigToBundle("maxPurchaseCount", maxPurchaseCount));
        floatConfigList = BundleConfigs<float>(AddConfigToBundle("refreshTimer", refreshTimer), AddConfigToBundle("costMultiplier", costMultiplier));

        CorrectNegativeConfigs(intConfigList, floatConfigList);
    }

    //Probably a horribly implemented method. 
    private static Dictionary<string, ConfigEntry<T>> BundleConfigs<T>(params Tuple<string, ConfigEntry<T>>[] configs)
    {
        Dictionary<string, ConfigEntry<T>> bundleConfigs = new Dictionary<string, ConfigEntry<T>>();

        for (int i = 0; i < configs.Length; i++)
        {
            bundleConfigs.Add(configs[i].Item1, configs[i].Item2);
        }
        return bundleConfigs;
    }

    private static Tuple<string, ConfigEntry<T>> AddConfigToBundle<T>(string key, ConfigEntry<T> config)
    {
        //Guard clause.
        if (key == null || config == null)
        {
            Log.LogError("Something went terribly wrong. Configs could not be added to dictionary.");
            return null;
        }

        Tuple<string, ConfigEntry<T>> AddElement = Tuple.Create(key, config);
        return AddElement;
    }

    //If intConfigs are negative, revert back to default value.
    //Cannot check if value type is correct.
    private static void CorrectNegativeConfigs(Dictionary<string, ConfigEntry<int>> intConfigs, Dictionary<string, ConfigEntry<float>> floatConfigs)
    {
        //Guard clause.
        if (intConfigs == null || floatConfigs == null)
        {
            Log.LogError("Something went terribly wrong. Config values could not be read.");
            return;
        }

        foreach (KeyValuePair<string, ConfigEntry<int>> kvp in intConfigList)
        {
            //Unambiguous code. What happens is, were getting the value from our dictionary, which is of type ConfigEntry, which we have to get the int value of.
            if (kvp.Value.Value < 0)
            {
                var key = kvp.Key;
                switch (key)
                {
                    case key_MaxPurchaseCount:
                        maxPurchaseCount.Value = (int)maxPurchaseCount.DefaultValue;
                        Log.LogWarning("maxPurchaseCount reset to default value because of invalid configuration. You cannot use negative values!");
                        break;
                    default:
                        Log.LogError("An error has occured in Class: " + nameof(SoC_Config) + " .. Function: " + nameof(CorrectNegativeConfigs));
                        break;
                }
            }
        }

        foreach (KeyValuePair<string, ConfigEntry<float>> kvp in floatConfigList)
        {
            //Unambiguous code. What happens is, were getting the value from our dictionary, which is of type ConfigEntry, which we have to get the int value of.
            if (kvp.Value.Value < 0)
            {
                var key = kvp.Key;
                switch (key)
                {
                    case key_costMultiplier:
                        costMultiplier.Value = (float)costMultiplier.DefaultValue;
                        Log.LogWarning("costMultiplier reset to default value because of invalid configuration. You cannot use negative values!");
                        break;
                    case key_RefreshTimer:
                        refreshTimer.Value = (float)refreshTimer.DefaultValue;
                        Log.LogWarning("Refresh Timer reset to default value because of invalid configuration. You cannot use negative values!");
                        break;
                    default:
                        Log.LogError("An error has occured in Class: " + nameof(SoC_Config) + " .. Function: " + nameof(CorrectNegativeConfigs));
                        break;
                }
            }
        }
    }

    //Hot reload command.
    [ConCommand(commandName = "SoC_reload_config", flags = ConVarFlags.None, helpText = "Reloads configs")]
    public static void CCReloadConfig(ConCommandArgs args)
    {
        Reload();
        Log.LogInfo("Configs Reloaded");
    }
}