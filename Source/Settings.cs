using System;
using System.Reflection;
using UnityEngine;
using Verse;

namespace GravshipInfo
{
	public class GravshipInfoSettings : ModSettings
	{
		public static int ModSettingsVersion = 1; //current min settings version
		public int SettingsVersion = 0;

		public AlertDisplayMode ShowInfoAlert = AlertDisplayMode.Always;

		public AlertColorMode AlertColors = AlertColorMode.Default;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref SettingsVersion, "SettingsVersion", 0);
			Scribe_Values.Look(ref ShowInfoAlert, "ShowInfoAlert", AlertDisplayMode.OnShipFound);

			if (Scribe.mode == LoadSaveMode.LoadingVars && SettingsVersion < ModSettingsVersion)
			{
				//OnShipFound is new default since v0.5.2.0
				if (ShowInfoAlert == AlertDisplayMode.Always)
				{
					ShowInfoAlert = AlertDisplayMode.OnShipFound;
				}
				SettingsVersion = ModSettingsVersion;
			}


		}

		public void DoWindowContents(Rect inRect)
		{
			if (Find.WindowStack.currentlyDrawnWindow.draggable == false)
				Find.WindowStack.currentlyDrawnWindow.draggable = true;

			var listing = new Listing_Standard { ColumnWidth = (inRect.width - 34f) / 2f };

			listing.Begin(inRect);

			listing.Gap();
			listing.Label($"<b>{"ShowInfoAlertLabel".Translate()}</b>");
			// Radio button for Always
			if (listing.RadioButton("ShowInfoAlertAlways".Translate(), ShowInfoAlert == AlertDisplayMode.Always))
			{
				ShowInfoAlert = AlertDisplayMode.Always;
			}
			// Radio button for OnShipFound
			if (listing.RadioButton("ShowInfoAlertOnShipFound".Translate(), ShowInfoAlert == AlertDisplayMode.OnShipFound))
			{
				ShowInfoAlert = AlertDisplayMode.OnShipFound;
			}
			// Radio button for Disabled
			if (listing.RadioButton("ShowInfoAlertDisabled".Translate(), ShowInfoAlert == AlertDisplayMode.Disabled))
			{
				ShowInfoAlert = AlertDisplayMode.Disabled;
			}
			listing.Gap();
			listing.Label($"<b>{"AlertColorsLabel".Translate()}</b>");
			// Radio button for Colored (Default)
			if (listing.RadioButton("AlertColorsEnabled".Translate(), AlertColors == AlertColorMode.Default))
			{
				AlertColors = AlertColorMode.Default;
			}
			// Radio button for No Colors
			if (listing.RadioButton("AlertColorsDisabled".Translate(), AlertColors == AlertColorMode.None))
			{
				AlertColors = AlertColorMode.None;
			}
			listing.Gap();

			listing.Gap();
			listing.Gap();
			listing.Label("MissingSettingsInfoText".Translate());
			listing.End();
		}

		public void GenerateColors(Rect inRect)
		{
			var listing = new Listing_Standard();
			listing.Begin(inRect);

			listing.Label("ColorLibrary Colors:");
			listing.Gap();

			// Get all static Color fields from ColorLibrary
			var colorFields = typeof(ColorLibrary).GetFields(BindingFlags.Public | BindingFlags.Static);

			float rectWidth = 100f;
			float rectHeight = 22f;
			float spacing = 5f;
			float columnSpacing = 20f;
			int maxEntriesPerColumn = 20;

			int currentColumn = 0;
			int entryInColumn = 0;
			float startX = inRect.x;
			float startY = listing.CurHeight;

			foreach (var field in colorFields)
			{
				if (field.FieldType == typeof(Color))
				{
					var color = (Color)field.GetValue(null);

					// Calculate position
					float x = startX + currentColumn * (rectWidth * 2 + columnSpacing + spacing);
					float y = startY + entryInColumn * (rectHeight + spacing);

					// First rect - color background with white text
					var whiteTextRect = new Rect(x, y, rectWidth, rectHeight);
					Widgets.DrawBoxSolid(whiteTextRect, color);
					Widgets.DrawBox(whiteTextRect, 1);

					var prevColor = GUI.color;
					GUI.color = Color.white;
					Widgets.Label(whiteTextRect, field.Name);
					GUI.color = prevColor;

					// Second rect - color background with black text
					var blackTextRect = new Rect(x + rectWidth + spacing, y, rectWidth, rectHeight);
					Widgets.DrawBoxSolid(blackTextRect, color);
					Widgets.DrawBox(blackTextRect, 1);

					GUI.color = Color.black;
					Widgets.Label(blackTextRect, field.Name);
					GUI.color = prevColor;

					entryInColumn++;

					// Start new column after 20 entries
					if (entryInColumn >= maxEntriesPerColumn)
					{
						currentColumn++;
						entryInColumn = 0;
					}
				}
			}

			listing.End();
		}
	}
}
