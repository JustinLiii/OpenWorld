﻿using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace OpenWorld
{
    public class MPCaravan
    {
		public void TakeFundsFromCaravan()
		{
			int q = 0;

			foreach (Thing item in CaravanInventoryUtility.AllInventoryItems(Main._ParametersCache.focusedCaravan).ToList())
			{
				if (q == Main._ParametersCache.silverAmount)
				{
					Main._ParametersCache.silverAmount = 0;
					Main._Injections.thingsToDoInUpdate.Add(Main._MPGame.ForceSave);
					return;
				}

				if (item.def == ThingDefOf.Silver)
				{
					if (q + item.stackCount > Main._ParametersCache.silverAmount)
					{
						int countToTake = Main._ParametersCache.silverAmount - q;

						q += countToTake;
						item.holdingOwner.Take(item, countToTake);
					}

					else
					{
						q += item.stackCount;
						item.holdingOwner.Take(item, item.stackCount);
					}
				}
			}
		}

		public void GiveFundsToCaravan()
        {
			try
			{
				Caravan caravan = Main._ParametersCache.focusedCaravan;

				Thing thing = ThingMaker.MakeThing(ThingDefOf.Silver);
				thing.stackCount = int.Parse(Main._ParametersCache.wantedSilver);

				caravan.AddPawnOrItem(thing, false);

				Main._ParametersCache.wantedSilver = "";
			}

			catch { }
		}

		public void TakeGiftsFromCaravan()
		{
			Action action = delegate
			{
				Main._ParametersCache.transferMode = "Gift";

				if (TradeSession.deal.TryExecute(out bool actuallyTraded))
				{
					SoundDefOf.ExecuteTrade.PlayOneShotOnCamera();
					TradeSession.playerNegotiator.GetCaravan()?.RecacheImmobilizedNow();
					Main._ParametersCache.__MPGift.Close(doCloseSound: false);
				}
			};

			action();

			Event.current.Use();
		}

		public void SendGiftsToSettlement()
		{
			if (Main._ParametersCache.giftedItemsString.Count() > 0) Main._ParametersCache.giftedItemsString = Main._ParametersCache.giftedItemsString.Remove(Main._ParametersCache.giftedItemsString.Count() - 1, 1);
			else return;

			string giftedString = "SendGiftTo│" + Main._ParametersCache.focusedSettlement.Tile + "│" + Main._ParametersCache.giftedItemsString;

			Main._Networking.SendData(giftedString);

			Main._ParametersCache.giftedItemsString = "";

			Main._Injections.thingsToDoInUpdate.Add(Main._MPGame.ForceSave);
		}

		public void SendGiftedPodsToSettlement(List<CompTransporter> podList, Settlement targetSettlement)
		{
			SoundDefOf.ExecuteTrade.PlayOneShotOnCamera();

			string itemDefName;
			int countToTransfer;

			foreach (CompTransporter pod in podList)
			{
				ThingOwner directlyHeldThings = pod.GetDirectlyHeldThings();

				for (int num = directlyHeldThings.Count - 1; num >= 0; num--)
				{
					countToTransfer = directlyHeldThings[num].stackCount;

					if (directlyHeldThings[num] is Pawn)
					{
						Pawn sentPawn = (Pawn)directlyHeldThings[num];
						string pawnData = "pawn┼";
						pawnData += sentPawn.Name + "┼";
						pawnData += sentPawn.ageTracker.AgeBiologicalTicks + "┼";
						pawnData += sentPawn.ageTracker.AgeChronologicalTicks + "┼";
						pawnData += (int)sentPawn.gender + "┼";

						pawnData += "‼";

						try { pawnData += sentPawn.story.childhood.identifier + "┼"; }
						catch { }

						try { pawnData += sentPawn.story.adulthood.identifier + "┼"; }
						catch { }

						pawnData += "‼";

						foreach (SkillRecord s in sentPawn.skills.skills)
						{
							pawnData += s.levelInt + "-";
							pawnData += (int)s.passion + "┼";
						}

						pawnData += "‼";

						foreach (Trait t in sentPawn.story.traits.allTraits)
						{
							pawnData += t.Label + "┼";
						}

						pawnData = pawnData.Remove(pawnData.Count() - 1, 1);

						//Add more

						Main._ParametersCache.giftedItemsString += pawnData + "»";
					}

					else
					{
						QualityCategory qc = QualityCategory.Normal;
						try { directlyHeldThings[num].TryGetQuality(out qc); }
						catch { }

						string stuffDefName = "";
						try { stuffDefName = directlyHeldThings[num].Stuff.defName; }
						catch { }

						if (directlyHeldThings[num].def == ThingDefOf.MinifiedThing || directlyHeldThings[num].def == ThingDefOf.MinifiedTree)
						{
							Thing innerThing = directlyHeldThings[num].GetInnerIfMinified();
							itemDefName = "minified-" + innerThing.def.defName;
						}

						else itemDefName = directlyHeldThings[num].def.defName;

						Main._ParametersCache.giftedItemsString += itemDefName + "┼" + countToTransfer + "┼" + ((int)qc) + "┼" + stuffDefName + "»";
					}
				}
			}

			if (Main._ParametersCache.giftedItemsString.Count() > 0) Main._ParametersCache.giftedItemsString = Main._ParametersCache.giftedItemsString.Remove(Main._ParametersCache.giftedItemsString.Count() - 1, 1);
			else return;

			string giftedString = "SendGiftTo│" + targetSettlement.Tile + "│" + Main._ParametersCache.giftedItemsString + "│" + "Pod";

			Main._Networking.SendData(giftedString);

			Main._ParametersCache.giftedItemsString = "";

			Main._Injections.thingsToDoInUpdate.Add(Main._MPGame.ForceSave);
		}

		public void TakeTradesFromSettlement()
		{
			Action action = delegate
			{
				Main._ParametersCache.transferMode = "Trade";

				if (TradeSession.deal.TryExecute(out bool actuallyTraded))
				{
					try { Main._ParametersCache.__MPTrade.Close(doCloseSound: false); }
					catch { Main._ParametersCache.__MPBarter.Close(doCloseSound: false); }
				}
			};

			action();

			Event.current.Use();
		}

		public void TakeTradesFromCaravan()
        {
			Action action = delegate
			{
				Main._ParametersCache.transferMode = "Trade";

				if (TradeSession.deal.TryExecute(out bool actuallyTraded))
				{
					TradeSession.playerNegotiator.GetCaravan()?.RecacheImmobilizedNow();
					try { Main._ParametersCache.__MPTrade.Close(doCloseSound: false); }
					catch { Main._ParametersCache.__MPBarter.Close(doCloseSound: false); }
				}
			};

			action();

			Event.current.Use();
		}

		public void ReturnTradesToCaravan()
		{
			Caravan caravan = Main._ParametersCache.focusedCaravan;

			string tradedString = Main._ParametersCache.tradedItemString;
			string[] receivedItems = new string[0];
			if (tradedString.Contains('»')) receivedItems = tradedString.Split('»').ToArray();
			else receivedItems = new string[1] { tradedString };

			foreach (string str in receivedItems)
			{
				string itemDefName = str.Split('┼')[0];
				int itemCount = int.Parse(str.Split('┼')[1]);
				QualityCategory qc = (QualityCategory)int.Parse(str.Split('┼')[1]);

				Thing thing = ThingMaker.MakeThing(ThingDefOf.Silver);

				foreach (ThingDef item in DefDatabase<ThingDef>.AllDefs)
				{
					if (item.defName == itemDefName)
					{
						thing = ThingMaker.MakeThing(item);
						thing.stackCount = itemCount;
						break;
					}
				}

				caravan.AddPawnOrItem(thing, false);
			}

			Main._ParametersCache.tradedItemString = "";
		}

		public void ReturnTradesToSettlement()
        {
			Map map = Find.AnyPlayerHomeMap;
			IntVec3 positionToDrop = new IntVec3(map.Center.x, map.Center.y, map.Center.z);

			string tradedString = Main._ParametersCache.tradedItemString;
			string[] receivedItems = new string[0];
			if (tradedString.Contains('»')) receivedItems = tradedString.Split('»').ToArray();
			else receivedItems = new string[1] { tradedString };

			try
			{
				Zone z = map.zoneManager.AllZones.Find(zone => zone.label == "Trading" && zone.GetType() == typeof(Zone_Stockpile));
				positionToDrop = z.Position;
			}
			catch { }

			foreach (string str in receivedItems)
			{
				string itemDefName = str.Split('┼')[0];
				int itemCount = int.Parse(str.Split('┼')[1]);
				Thing thing = ThingMaker.MakeThing(ThingDefOf.Silver);

				foreach (ThingDef item in DefDatabase<ThingDef>.AllDefs)
				{
					if (itemDefName.Contains("minified-") && item.defName == itemDefName.Replace("minified-", ""))
					{
						thing = ThingMaker.MakeThing(item);
						Thing miniThing = thing.TryMakeMinified();
						thing = miniThing;
						break;
					}

					else if (item.defName == itemDefName)
					{
						thing = ThingMaker.MakeThing(item);
						break;
					}
				}

				thing.stackCount = itemCount;

				try { GenPlace.TryPlaceThing(thing, positionToDrop, map, ThingPlaceMode.Near); }
				catch { }
			}

			Main._ParametersCache.awaitingRebarter = false;
		}

		public void SendTradesToSettlement()
        {
			if (Main._ParametersCache.tradedItemString.Count() > 0) Main._ParametersCache.tradedItemString = Main._ParametersCache.tradedItemString.Remove(Main._ParametersCache.tradedItemString.Count() - 1, 1);
			else return;

			string tradedString = "SendTradeTo│" + Main._ParametersCache.focusedSettlement.Tile + "│" + Main._ParametersCache.tradedItemString + "│" + Main._ParametersCache.wantedSilver;

			Main._Networking.SendData(tradedString);
		}

		public void SendBarterToSettlement()
        {
			if (Main._ParametersCache.tradedItemString.Count() > 0) Main._ParametersCache.tradedItemString = Main._ParametersCache.tradedItemString.Remove(Main._ParametersCache.tradedItemString.Count() - 1, 1);
			else return;

			string tradedString = "SendBarterTo│" + Main._ParametersCache.focusedSettlement.Tile + "│" + Main._ParametersCache.tradedItemString;

			Main._Networking.SendData(tradedString);
		}

		public void SendBarterToCaravan()
		{
			if (Main._ParametersCache.tradedItemString.Count() > 0) Main._ParametersCache.tradedItemString = Main._ParametersCache.tradedItemString.Remove(Main._ParametersCache.tradedItemString.Count() - 1, 1);
			else return;

			string tradedString = "BarterStatus│Rebarter│" + Main._ParametersCache.focusedSettlement.Tile + "│" + Main._ParametersCache.tradedItemString;

			Main._Networking.SendData(tradedString);
		}

		public void ReceiveBarterToCaravan(string[] items)
        {
			Caravan caravan = Main._ParametersCache.focusedCaravan;

			foreach (string str in items)
			{
				string itemDefName = str.Split('┼')[0];
				int itemCount = int.Parse(str.Split('┼')[1]);
				int itemQuality = int.Parse(str.Split('┼')[2]);
				string madeFromString = str.Split('┼')[3];

				Thing thing = ThingMaker.MakeThing(ThingDefOf.Silver);

				foreach (ThingDef item in DefDatabase<ThingDef>.AllDefs)
				{
					if (itemDefName.Contains("minified-") && item.defName == itemDefName.Replace("minified-", ""))
					{
						thing = ThingMaker.MakeThing(item);
						Thing miniThing = thing.TryMakeMinified();
						thing = miniThing;

						CompQuality compQuality = thing.TryGetComp<CompQuality>();
						if (compQuality != null)
						{
							QualityCategory q = (QualityCategory)itemQuality;
							compQuality.SetQuality(q, ArtGenerationContext.Colony);
						}

						foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
						{
							if (def.defName == madeFromString)
							{
								thing.SetStuffDirect(def);
							}
						}

						break;
					}

					else if (item.defName == itemDefName)
					{
						thing = ThingMaker.MakeThing(item);

						CompQuality compQuality = thing.TryGetComp<CompQuality>();
						if (compQuality != null)
						{
							QualityCategory q = (QualityCategory)itemQuality;
							compQuality.SetQuality(q, ArtGenerationContext.Colony);
						}

						foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
						{
							if (def.defName == madeFromString)
							{
								thing.SetStuffDirect(def);
							}
						}

						break;
					}
				}

				thing.stackCount = itemCount;

				try { caravan.AddPawnOrItem(thing, false); }
				catch { }
			}

			Main._Injections.thingsToDoInUpdate.Add(Main._MPGame.ForceSave);
		}

		public void ReceiveTradesFromPlayer(string[] items)
        {
			Map map = Find.AnyPlayerHomeMap;
			IntVec3 positionToDrop = new IntVec3(map.Center.x, map.Center.y, map.Center.z);

			try
			{
				Zone z = map.zoneManager.AllZones.Find(zone => zone.label == "Trading" && zone.GetType() == typeof(Zone_Stockpile));
				positionToDrop = z.Position;
			}
			catch { }

			foreach (string str in items)
			{
				string itemDefName = str.Split('┼')[0];

				if (itemDefName == "pawn")
				{
					Pawn newPawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);

					newPawn.ageTracker.AgeBiologicalTicks = int.Parse(str.Split('┼')[2]);
					newPawn.ageTracker.AgeChronologicalTicks = int.Parse(str.Split('┼')[3]);
					newPawn.gender = (Gender)int.Parse(str.Split('┼')[4]);

					foreach (var bs in BackstoryDatabase.allBackstories)
					{
						if (bs.Value.identifier == str.Split('‼')[1].Split('┼')[0])
						{
							newPawn.story.childhood = bs.Value;
							break;
						}
					}

					try
					{
						foreach (var bs in BackstoryDatabase.allBackstories)
						{
							if (bs.Value.identifier == str.Split('‼')[1].Split('┼')[1])
							{
								newPawn.story.adulthood = bs.Value;
								break;
							}
						}
					}
					catch { newPawn.story.adulthood = null; }

					List<string> skillList = str.Split('‼')[2].Split('┼').ToList();
					for (int i = 0; i < 12; i++)
					{
						int level = int.Parse(skillList[i].Split('-')[0]);
						int passion = int.Parse(skillList[i].Split('-')[1]);

						newPawn.skills.skills[i].levelInt = level;
						newPawn.skills.skills[i].passion = (Passion)passion;
					}

                    List<string> traitList = str.Split('‼')[3].Split('┼').ToList();
                    newPawn.story.traits.allTraits.Clear();
                    foreach (string traitS in traitList)
                    {
                        foreach (TraitDef def in DefDatabase<TraitDef>.AllDefs)
                        {
                            Trait trait = new Trait(def);

                            if (trait.Label == traitS)
                            {
                                newPawn.story.traits.GainTrait(trait);
                                break;
                            }
                        }
                    }

                    try { GenPlace.TryPlaceThing(newPawn, positionToDrop, map, ThingPlaceMode.Near); }
					catch { }
				}

				else
				{
					int itemCount = int.Parse(str.Split('┼')[1]);
					int itemQuality = int.Parse(str.Split('┼')[2]);
					string madeFromString = str.Split('┼')[3];

					Thing thing = ThingMaker.MakeThing(ThingDefOf.Silver);

					foreach (ThingDef item in DefDatabase<ThingDef>.AllDefs)
					{
						if (itemDefName.Contains("minified-") && item.defName == itemDefName.Replace("minified-", ""))
						{
							thing = ThingMaker.MakeThing(item);
							Thing miniThing = thing.TryMakeMinified();
							thing = miniThing;

							CompQuality compQuality = thing.TryGetComp<CompQuality>();
							if (compQuality != null)
							{
								QualityCategory q = (QualityCategory)itemQuality;
								compQuality.SetQuality(q, ArtGenerationContext.Colony);
							}

							foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
							{
								if (def.defName == madeFromString)
								{
									thing.SetStuffDirect(def);
								}
							}

							break;
						}

						else if (item.defName == itemDefName)
						{
							thing = ThingMaker.MakeThing(item);

							CompQuality compQuality = thing.TryGetComp<CompQuality>();
							if (compQuality != null)
							{
								QualityCategory q = (QualityCategory)itemQuality;
								compQuality.SetQuality(q, ArtGenerationContext.Colony);
							}

							foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
							{
								if (def.defName == madeFromString)
								{
									thing.SetStuffDirect(def);
								}
							}

							break;
						}
					}

					thing.stackCount = itemCount;

					try { GenPlace.TryPlaceThing(thing, positionToDrop, map, ThingPlaceMode.Near); }
					catch { }
				}
			}

			Main._ParametersCache.letterTitle = "Trade Received";
			Main._ParametersCache.letterDescription = "You have received a trade from another player! \n\nIf you have a zone called 'Trading' check it for the items, if not, search around the center of the map. \n\nTake a look at your objects!Some might have been swapped out by thieves!";
			Main._ParametersCache.letterType = LetterDefOf.PositiveEvent;

			Main._Injections.thingsToDoInUpdate.Add(Main._MPGame.TryGenerateLetter);

			Main._Injections.thingsToDoInUpdate.Add(Main._MPGame.ForceSave);
		}

		public void ReceiveGiftsFromPlayer(string[] items)
		{
			Map map = Find.AnyPlayerHomeMap;
			IntVec3 positionToDrop = new IntVec3(map.Center.x, map.Center.y, map.Center.z);

			try
			{
				Zone z = map.zoneManager.AllZones.Find(zone => zone.label == "Trading" && zone.GetType() == typeof(Zone_Stockpile));
				positionToDrop = z.Position;
			}
			catch { }

			foreach (string str in items)
			{
				string itemDefName = str.Split('┼')[0];

				if (itemDefName == "pawn")
				{
					Pawn newPawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);

					newPawn.ageTracker.AgeBiologicalTicks = int.Parse(str.Split('┼')[2]);
					newPawn.ageTracker.AgeChronologicalTicks = int.Parse(str.Split('┼')[3]);
					newPawn.gender = (Gender)int.Parse(str.Split('┼')[4]);

					foreach (var bs in BackstoryDatabase.allBackstories)
                    {
						if (bs.Value.identifier == str.Split('‼')[1].Split('┼')[0])
                        {
							newPawn.story.childhood = bs.Value;
							break;
                        }
					}

					try
					{
						foreach (var bs in BackstoryDatabase.allBackstories)
						{
							if (bs.Value.identifier == str.Split('‼')[1].Split('┼')[1])
							{
								newPawn.story.adulthood = bs.Value;
								break;
							}
						}
					}
					catch { newPawn.story.adulthood = null; }

					List<string> skillList = str.Split('‼')[2].Split('┼').ToList();
					for (int i = 0; i < 12; i++)
                    {
						int level = int.Parse(skillList[i].Split('-')[0]);
						int passion = int.Parse(skillList[i].Split('-')[1]);

						newPawn.skills.skills[i].levelInt = level;
						newPawn.skills.skills[i].passion = (Passion)passion;
					}

                    List<string> traitList = str.Split('‼')[3].Split('┼').ToList();
                    newPawn.story.traits.allTraits.Clear();
                    foreach (string traitS in traitList)
                    {
                        foreach (TraitDef def in DefDatabase<TraitDef>.AllDefs)
                        {
                            Trait trait = new Trait(def);

                            if (trait.Label == traitS)
                            {
                                newPawn.story.traits.GainTrait(trait);
                                break;
                            }
                        }
                    }

                    try { GenPlace.TryPlaceThing(newPawn, positionToDrop, map, ThingPlaceMode.Near); }
					catch { }
				}

				else
				{
					int itemCount = int.Parse(str.Split('┼')[1]);
					int itemQuality = int.Parse(str.Split('┼')[2]);
					string madeFromString = str.Split('┼')[3];

					Thing thing = ThingMaker.MakeThing(ThingDefOf.Silver);

					foreach (ThingDef item in DefDatabase<ThingDef>.AllDefs)
					{
						if (itemDefName.Contains("minified-") && item.defName == itemDefName.Replace("minified-", ""))
						{
							thing = ThingMaker.MakeThing(item);
							Thing miniThing = thing.TryMakeMinified();
							thing = miniThing;

							CompQuality compQuality = thing.TryGetComp<CompQuality>();
							if (compQuality != null)
							{
								QualityCategory q = (QualityCategory)itemQuality;
								compQuality.SetQuality(q, ArtGenerationContext.Colony);
							}

							foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
							{
								if (def.defName == madeFromString)
								{
									thing.SetStuffDirect(def);
								}
							}

							break;
						}

						else if (item.defName == itemDefName)
						{
							thing = ThingMaker.MakeThing(item);

							CompQuality compQuality = thing.TryGetComp<CompQuality>();
							if (compQuality != null)
							{
								QualityCategory q = (QualityCategory)itemQuality;
								compQuality.SetQuality(q, ArtGenerationContext.Colony);
							}

							foreach(ThingDef def in DefDatabase<ThingDef>.AllDefs)
                            {
								if (def.defName == madeFromString)
                                {
									thing.SetStuffDirect(def);
								}
                            }

							break;
						}
					}

					thing.stackCount = itemCount;

					try { GenPlace.TryPlaceThing(thing, positionToDrop, map, ThingPlaceMode.Near); }
					catch { }
				}
			}

			Main._ParametersCache.letterTitle = "Gift Received";
			Main._ParametersCache.letterDescription = "You have received a gift from another player! \n\nIf you have a zone called 'Trading' check it for the items, if not, search around the center of the map. \n\nTake a look at your objects! Some might have been swapped out by thieves!";
			Main._ParametersCache.letterType = LetterDefOf.PositiveEvent;

			Main._Injections.thingsToDoInUpdate.Add(Main._MPGame.TryGenerateLetter);

			Main._ParametersCache.receiveGiftsData = "";

			Main._Injections.thingsToDoInUpdate.Add(Main._MPGame.ForceSave);
		}
	}
}