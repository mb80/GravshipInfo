using RimWorld;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace GravshipInfo
{
	public class Alert_GravshipInfo : Alert
	{
		public GravshipInfoMod Info;


		public Alert_GravshipInfo()
		{
			Info = GravshipInfoMod.Instance;
			this.defaultLabel = "Gravship Info".Translate();
			this.defaultPriority = AlertPriority.Medium;
		}

		//TODO change to: public if using Krafs.Rimworld.Ref, protected of using rimworld game assemblies
		public override Color BGColor => BgColor();


		public Color BgColor()
		{
			Color color = Color.gray;
			switch (Info.LaunchStatus)
			{
				case LaunchStatus.Cooldown:
					color = ColorLibrary.Blue;
					break;
				case LaunchStatus.Warning:
					color = ColorLibrary.Gold;
					break;
				case LaunchStatus.Error:
					color = ColorLibrary.Red;
					break;
				case LaunchStatus.Ready:
					color = ColorLibrary.Green;
					break;
				case LaunchStatus.Disabled:
				case LaunchStatus.NotFound:
				default:
					break;
			}

			color.a = 0.5f;
			return color;
		}

		public override AlertReport GetReport()
		{
			AlertReport status = AlertReport.Active;
			if (Info.LaunchStatus == LaunchStatus.Disabled)
			{
				status = AlertReport.Inactive;
				Info.UpdateInfo();
			}

			return status;
		}

		public override string GetLabel()
		{
			var label = "";
			switch (Info.LaunchStatus)
			{
				case LaunchStatus.Cooldown:
					label = $"{"Gravship".Translate()}: {Info.TicksUntilLaunch.ToStringTicksToPeriod()}";
					break;
				case LaunchStatus.Warning:
					if (Info.TicksUntilLaunch > 0)
						label = $"{"Gravship".Translate()}: {Info.TicksUntilLaunch.ToStringTicksToPeriod()}";
					else
						label = "GravshipReadyLabel".Translate();
					break;
				case LaunchStatus.Ready:
					label = "GravshipReadyLabel".Translate();
					break;
				case LaunchStatus.Disabled:
				case LaunchStatus.NotFound:
				default:
					label = $"{"NoGravshipLabel".Translate()}";
					break;
			}

			return label;
		}

		public override TaggedString GetExplanation()
		{
			if (Info.LaunchStatus == LaunchStatus.Disabled)
				return "GravshipDisabled".Translate();

			if (Info.LaunchStatus == LaunchStatus.NotFound)
				return "NoGravEngineFound".Translate();

			var gravEngine = Info.GravEngine;

			StringBuilder stringBuilder = new StringBuilder();

			string title = "Gravship".Translate();
			title = gravEngine.nameHidden ? title : title + " " + gravEngine.RenamableLabel;

			stringBuilder.AppendLine(title.Colorize(ColoredText.TipSectionTitleColor));

			stringBuilder.AppendInNewLine(gravEngine.GetInspectString());


			stringBuilder.AppendInNewLine((string)"GravshipRange".Translate().CapitalizeFirst()).Append(": ")
				.Append(Info.GetMaxLaunchDistance(Info.Map.Tile.Layer).ToString("F0"));

			stringBuilder.AppendInNewLine((string)"StoredChemfuel".Translate().CapitalizeFirst()).Append(": ")
				.Append(gravEngine.TotalFuel.ToString("F0")).Append(" / ").Append(gravEngine.MaxFuel.ToString("F0"));
			stringBuilder.AppendInNewLine((string)"FuelConsumption".Translate().CapitalizeFirst()).Append(": ").Append(
				(string)"FuelPerTile".Translate((NamedArgument)gravEngine.FuelPerTile.ToString("0.#")).CapitalizeFirst());

			stringBuilder.AppendInNewLine("\n");

			if (GenTicks.TicksGame < gravEngine.cooldownCompleteTick)
				stringBuilder.AppendInNewLine((string)"CannotLaunchOnCooldown"
					.Translate((NamedArgument)(gravEngine.cooldownCompleteTick - GenTicks.TicksGame).ToStringTicksToPeriod())
					.CapitalizeFirst());
			else
			{
				stringBuilder.AppendInNewLine("ReadyToLaunch".Translate().Colorize(ColorLibrary.Green));
			}

			if (Info.LaunchStatus == LaunchStatus.Warning)
			{
				stringBuilder.AppendInNewLine("\n");
				stringBuilder.AppendInNewLine("DetectedProblems".Translate().Colorize(ColorLibrary.RedReadable));

				if (Info.EngineIsUninstalled)
				{
					stringBuilder.AppendInNewLine("EngineNotInstalled".Translate().Colorize(ColorLibrary.RedReadable));
				}

				if (Info.HasBlockedThruster)
				{
					stringBuilder.AppendInNewLine("ThrustersBlocked".Translate().Colorize(ColorLibrary.RedReadable));
				}

				if (Info.SubstructureTooBig)
				{
					stringBuilder.AppendInNewLine(
						"SubstructureTooLarge".Translate((NamedArgument)gravEngine.AllConnectedSubstructure.Count,
								(NamedArgument)gravEngine.GetStatValue(StatDefOf.SubstructureSupport))
							.Colorize(ColorLibrary.RedReadable));
				}

				if (Info.HasMissingComponents && !Info.EngineIsUninstalled)
				{
					stringBuilder.AppendInNewLine("MissingComponents".Translate().Colorize(ColorLibrary.RedReadable));
				}

				if (Info.NotEnoughFuel && !Info.EngineIsUninstalled)
				{
					stringBuilder.AppendInNewLine("NotEnoughFuel".Translate().Colorize(ColorLibrary.RedReadable));
				}
			}

			return stringBuilder.ToString();
		}
	}
}
