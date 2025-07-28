using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace GravshipInfo
{
	public enum LaunchStatus : byte
	{
		Ready,
		Cooldown,
		Warning,
		Error,
		NotFound,
		Disabled
	}

	public enum AlertDisplayMode : byte
	{
		Always,
		OnShipFound,
		Disabled
	}

	public enum AlertColorMode : byte
	{
		Default,
		None,
		// CustomColors
	}

	public class GravshipInfoMod : Mod
	{
		private static bool devLogEnabled = false;

		static string logPath = Path.Combine(GenFilePaths.SaveDataFolderPath, "GravshipInfoLog.txt");


		public static GravshipInfoSettings Settings;

		public AlertColorMode AlertColors
		{
			get => Settings?.AlertColors ?? AlertColorMode.Default;
		}

		internal static GravshipInfoMod Instance { get; private set; }

		private LaunchStatus _launchStatus = LaunchStatus.Disabled;

		public int TicksUntilLaunch { get; set; } = 0;

		public bool HasMissingComponents { get; set; } = false;

		public bool HasBlockedThruster { get; set; } = false;

		public bool SubstructureTooBig { get; set; } = false;

		public bool EngineIsUninstalled { get; set; } = false;

		public bool NotEnoughFuel { get; set; } = false;

		public Building_GravEngine GravEngine { get; set; } = null;

		public Map Map { get; set; } = null;

		public LaunchStatus LaunchStatus
		{
			get
			{
				UpdateInfo();
				return _launchStatus;
			}
			set => _launchStatus = value;
		}


		public static bool PlayerHasLocalGravEngine = false;

		public static bool IsEnabled = true;

		public GravshipInfoMod(ModContentPack content) : base(content)
		{
			devLogEnabled = File.Exists(logPath);
			DevLog($"### GravshipInfo v{GetModVersionString()} performance {DateTime.Now}\r\n");
			DevLog($"### XXX_ms are max milliseconds per 30 ticks\r\n");
			DevLog($"### [Timestamp yyyyMMddHHmmss.ffff]\r\n");


			Settings = GetSettings<GravshipInfoSettings>();
			Settings.ExposeData();

			Instance = this;

			var harmony = new Harmony("epicduck.rimworld.gravshipinfo");
			harmony.PatchAll();

			UpdateInfo(true);
		}

		public static string GetModVersionString()
		{
			var assembly = Assembly.GetAssembly(typeof(GravshipInfoMod));
			var t_attribute = typeof(AssemblyFileVersionAttribute);
			var attribute = (AssemblyFileVersionAttribute)Attribute.GetCustomAttribute(assembly, t_attribute, false);
			return attribute.Version;
		}

		public void DevLog(string message)
		{
			if (devLogEnabled)
			{
				File.AppendAllText(logPath, $"[{DateTime.Now:yyyyMMddHHmmss.ffff}] {message}\r\n");
			}
		}

		public void UpdateInfo(bool force = false)
		{
			if (Settings.ShowInfoAlert == AlertDisplayMode.Disabled)
			{
				LaunchStatus = LaunchStatus.Disabled;
				return;
			}
			// if (Time.frameCount % 10 != 0)
			//   return;

			LaunchStatus = LaunchStatus.Ready;
			TicksUntilLaunch = 0;
			GravEngine = null;
			HasMissingComponents = false;
			HasBlockedThruster = false;
			SubstructureTooBig = false;
			EngineIsUninstalled = false;
			NotEnoughFuel = false;

			Map = Find.CurrentMap;
			if (Map == null)
			{
				LaunchStatus = LaunchStatus.Disabled;
				return;
			}


			var ship = GravshipUtility.GetPlayerGravEngine(Map);
			if (ship == null)
			{
				LaunchStatus = LaunchStatus.NotFound;
				if (Settings.ShowInfoAlert == AlertDisplayMode.OnShipFound)
					LaunchStatus = LaunchStatus.Disabled;
				return;
			}

			var engine = ship as Building_GravEngine;
			if (engine == null)
			{
				Log.Error($"GravshipInfo: Found ship {ship} but it is not a Building_GravEngine");
				LaunchStatus = LaunchStatus.NotFound;
				if (Settings.ShowInfoAlert == AlertDisplayMode.OnShipFound)
					LaunchStatus = LaunchStatus.Disabled;
				return;
			}

			GravEngine = engine;

			if (GenTicks.TicksGame < engine.cooldownCompleteTick)
			{
				LaunchStatus = LaunchStatus.Cooldown;
				TicksUntilLaunch = (int)(engine.cooldownCompleteTick - GenTicks.TicksGame);
			}

			if (engine.MissingComponents.Count > 0)
			{
				LaunchStatus = LaunchStatus.Warning;
				HasMissingComponents = true;
			}

			foreach (CompGravshipFacility gravshipComponent in engine.GravshipComponents)
			{
				if (gravshipComponent is CompGravshipThruster gravshipThruster && gravshipThruster.Blocked)
				{
					LaunchStatus = LaunchStatus.Warning;
					HasBlockedThruster = true;
					break;
				}
			}

			if (engine.AllConnectedSubstructure.Count > engine.GetStatValue(StatDefOf.SubstructureSupport))
			{
				DevLog(
					$"[{DateTime.Now:yyyyMMddHHmmss.ffff}] [UpdateInfo] Substructure too big: [{engine.AllConnectedSubstructure.Count} / {engine.GetStatValue(StatDefOf.SubstructureSupport)}]\r\n");
				LaunchStatus = LaunchStatus.Warning;
				SubstructureTooBig = true;
			}

			if (engine.TotalFuel < 10)
			{
				LaunchStatus = LaunchStatus.Warning;
				NotEnoughFuel = true;
			}

			if (!engine.Spawned)
			{
				LaunchStatus = LaunchStatus.Warning;
				EngineIsUninstalled = true;
			}
		}

		public void Reset()
		{
			LaunchStatus = LaunchStatus.Disabled;
		}

		public override void DoSettingsWindowContents(Rect inRect) => Settings.DoWindowContents(inRect);
		public override string SettingsCategory() => "GravshipInfoLabel".Translate();

		public int GetMaxLaunchDistance(PlanetLayer layer)
		{
			int? maxLaunchDistance = this.GravEngine?.MaxLaunchDistance;
			float? nullable = maxLaunchDistance.HasValue ? new float?((float) maxLaunchDistance.GetValueOrDefault()) : new float?();
			float rangeDistanceFactor = layer.Def.rangeDistanceFactor;
			return Mathf.FloorToInt((nullable.HasValue ? new float?(nullable.GetValueOrDefault() / rangeDistanceFactor) : new float?()).GetValueOrDefault());
		}
	}
}
