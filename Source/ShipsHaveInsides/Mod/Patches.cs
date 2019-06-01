using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;

namespace ShipsHaveInsides.Mod
{


    [HarmonyPatch(typeof(IncidentWorker_TraderCaravanArrival), "CanFireNowSub")]
    public static class IncidentWorker_TraderCaravanArrival_CanFireNowSub
    {
        [HarmonyPostfix]
        public static void CanSpawnTrader(IncidentParms parms, ref bool __result)
        {
            TerrainDef def = TerrainDef.Named("HardVacuum");

            Map map = (Map)parms.target;

            ShipInteriorMod.Log("Trader trying to spawn?");

            if (map.IsSpace())
            {
                __result = true;
                //return false;
            }
        }
    }

    [HarmonyPatch(typeof(IncidentWorker_Infestation), "CanFireNowSub")]
    public static class IncidentWorker_Infestation_CanFireNowSub
    {
        [HarmonyPostfix]
        public static void CanInfestation(IncidentParms parms, ref bool __result)
        {
            TerrainDef def = TerrainDef.Named("HardVacuum");

            Map map = (Map)parms.target;

            if (map.IsSpace())
            {
                __result = true;
                //return false;
            }
        }
    }

    [HarmonyPatch(typeof(GameConditionManager), "RegisterCondition")]
    public static class GameConditionManager_RegisterCondition_CanFireNowSub
    {
        [HarmonyPostfix]
        public static void CanRegisterCondition(ref GameCondition cond)
        {
            foreach(Map map in cond.AffectedMaps)
            {
                if(map.IsSpace())
                {
                    cond.AffectedMaps.Remove(map);
                }
            }

        }
    }

    [HarmonyPatch(typeof(WildAnimalSpawner), "SpawnRandomWildAnimalAt")]
    public static class WildAnimalSpawner_SpawnRandomWildAnimalAt
    {

        public static FieldInfo mapField = typeof(WildAnimalSpawner).GetField("map", BindingFlags.Instance | BindingFlags.NonPublic);
        public static Func<WildAnimalSpawner, Map> getMap = wd => mapField.GetValue(wd) as Map;

        [HarmonyPrefix]
        public static bool CanPawnKindAllowed(IntVec3 loc, ref WildAnimalSpawner __instance, ref bool __result)
        {
            //        public static FieldInfo mapField = typeof(WeatherDecider).GetField("map", BindingFlags.Instance | BindingFlags.NonPublic);

            Map map = getMap(__instance);

            if (map.IsSpace())
            {
                __result = false;
                return false;
            }

            return true;
        }
    }


    [HarmonyPatch(typeof(WeatherDecider), "StartInitialWeather", null)]
    public static class WeatherDecider_StartInitialWeather
    {
        public static FieldInfo mapField = typeof(WeatherDecider).GetField("map", BindingFlags.Instance | BindingFlags.NonPublic);
        public static Func<WeatherDecider, Map> getMap = wd => mapField.GetValue(wd) as Map;

        [HarmonyPostfix]
        public static void ChooseSpaceWeather(WeatherDecider __instance)
        {
            if (getMap(__instance).terrainGrid.TerrainAt(IntVec3.Zero)?.defName == "HardVacuum" || ShipInteriorMod.noSpaceWeather)
            {
                //No space weather
                getMap(__instance).weatherManager.lastWeather = WeatherDef.Named("NoneSpace");
                getMap(__instance).weatherManager.curWeather = WeatherDef.Named("NoneSpace");
            }
        }
    }

    [HarmonyPatch(typeof(WeatherDecider), "StartNextWeather", null)]
    public static class WeatherDecider_StartNextWeather
    {
        public static FieldInfo mapField = typeof(WeatherDecider).GetField("map", BindingFlags.Instance | BindingFlags.NonPublic);
        public static Func<WeatherDecider, Map> getMap = wd => mapField.GetValue(wd) as Map;

        [HarmonyPostfix]
        public static void ChooseSpaceWeather(WeatherDecider __instance)
        {
            if (getMap(__instance).terrainGrid.TerrainAt(IntVec3.Zero)?.defName == "HardVacuum")
            {
                //No space weather
                getMap(__instance).weatherManager.lastWeather = WeatherDef.Named("NoneSpace");
                getMap(__instance).weatherManager.curWeather = WeatherDef.Named("NoneSpace");
            }
        }
    }

    [HarmonyPatch(typeof(MapTemperature), "get_OutdoorTemp", null)]
    public static class MapTemperature_OutdoorTemp
    {
        public static FieldInfo mapField = typeof(MapTemperature).GetField("map", BindingFlags.Instance | BindingFlags.NonPublic);
        public static Func<MapTemperature, Map> getMap = wd => mapField.GetValue(wd) as Map;

        [HarmonyPostfix]
        public static void ChooseSpaceWeather(MapTemperature __instance, ref float __result)
        {
            if (getMap(__instance).terrainGrid.TerrainAt(IntVec3.Zero)?.defName == "HardVacuum")
            {
                //really cold. This is not accurate, just for gameplay.
                __result = -100f;
            }
        }
    }

    [HarmonyPatch(typeof(MapTemperature), "get_SeasonalTemp", null)]
    public static class MapTemperature_SeasonalTemp
    {
        public static FieldInfo mapField = typeof(MapTemperature).GetField("map", BindingFlags.Instance | BindingFlags.NonPublic);
        public static Func<MapTemperature, Map> getMap = wd => mapField.GetValue(wd) as Map;

        [HarmonyPostfix]
        public static void ChooseSpaceWeather(MapTemperature __instance, ref float __result)
        {
            if (getMap(__instance).terrainGrid.TerrainAt(IntVec3.Zero)?.defName == "HardVacuum")
            {
                //really cold. This is not accurate, just for gameplay.
                __result = -100f;
            }
        }
    }
}
