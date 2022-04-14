using System;
using BepInEx;
using R2API.Utils;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace SoCRebalancePlugin
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    public class SoCRebalancePlugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Xatha";
        public const string PluginName = "SoCRebalancePlugin";
        public const string PluginVersion = "1.1.0";

        private const string itemRarityCommon = "common";
        private const string itemRarityRare = "rare";
        private const string itemRarityLegendary = "legendary";
        private const string itemRarityEquipment = "equipment";

        private readonly PickupDropTable dropTable = null;
        private int concurrentFailureCount = 0;

        //The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            //Init logger.
            Log.Init(Logger);

            //Init configs.
            SoC_Config.Init(Config);

            //Init Hooks.
            InitHooks();

            Log.LogInfo(nameof(Awake) + " is Done. SoC has succesfully hooked into functions.");
        }

        private void InitHooks()
        {
            On.RoR2.ShrineChanceBehavior.AddShrineStack += ShrineChanceBehaviour_AddShrineStack;
        }

        private void ShrineChanceBehaviour_AddShrineStack(On.RoR2.ShrineChanceBehavior.orig_AddShrineStack orig, ShrineChanceBehavior self, Interactor activator)
        {
            //Sanity check.
            if (!NetworkServer.active)
            {
                Debug.LogWarning("[Server] function 'System.Void RoR2.ShrineChanceBehavior::AddShrineStack(RoR2.Interactor)' called on client");
                return;
            }

            //Apply config values.
            SoC_Config.ApplyConfigs(self);

            //Getting fields from object. 
            var rng = self.GetFieldValue<Xoroshiro128Plus>("rng");

            var failureChance = self.GetFieldValue<float>("failureChance");
            var failureWeight = self.GetFieldValue<float>("failureWeight");
            var equipmentWeight = self.GetFieldValue<float>("equipmentWeight");
            var tier1Weight = self.GetFieldValue<float>("tier1Weight");
            var tier2Weight = self.GetFieldValue<float>("tier2Weight");
            var tier3Weight = self.GetFieldValue<float>("tier3Weight");

            var dropletOrigin = self.GetFieldValue<Transform>("dropletOrigin");
            var isBeaconHackingAllowed = SoC_Config.isBeaconHackingAllowed.Value;

            //Getting purchase interaction for interaction cost.
            var purchaseInteraction = self.GetFieldValue<PurchaseInteraction>("purchaseInteraction");

            PickupIndex itemDrop = PickupIndex.none;
            if (this.dropTable)
            {
                if (rng.nextNormalizedFloat > failureChance)
                {
                    itemDrop = this.dropTable.GenerateDrop(rng);
                }
            }
            else if ((isBeaconHackingAllowed == false && purchaseInteraction.Networkcost != 0) || (isBeaconHackingAllowed == true))
            {
                switch (this.concurrentFailureCount)
                {
                    case 0:
                        itemDrop = CalculateDrop(self, rng, failureWeight, tier1Weight, tier2Weight, tier3Weight, equipmentWeight);
                        SpawnEffect(self, itemDrop);
                        Log.LogDebug("0");
                        break;

                    case 1:
                        itemDrop = CalculateDrop(self, rng, 10.5f, 7f, 4f, 0.4f, 2f);
                        Log.LogDebug("1");
                        break;

                    case 2:
                        itemDrop = CalculateDrop(self, rng, 15.5f, 2f, 12f, 2f, 4f);
                        Log.LogDebug("2");
                        break;

                    case 3:
                        itemDrop = CalculateDrop(self, rng, 20f, 2f, 15f, 5f, 4f);
                        Log.LogDebug("3");
                        break;

                    case 4:
                        itemDrop = CalculateDrop(self, rng, 20f, 0.5f, 17f, 7f, 2f);
                        Log.LogDebug("4");
                        break;

                    case 5:
                        itemDrop = CalculateDrop(self, rng, 10.1f, 0f, 6f, 7f, 0f);
                        Log.LogDebug("5");
                        break;

                    case 6:
                        itemDrop = rng.NextElementUniform<PickupIndex>(Run.instance.availableTier3DropList);

                        //If legendary is dropped, deactivate shrine.
                        self.SetFieldValue<int>("successfulPurchaseCount", self.maxPurchaseCount);

                        Log.LogDebug("6");
                        break;
                }
            }
            else
            {
                itemDrop = CalculateDrop(self, rng, failureWeight, tier1Weight, tier2Weight, tier3Weight, equipmentWeight);
                Log.LogDebug("c");
            }

            //Spawns the effect on interaction.
            SpawnEffect(self, itemDrop);

            bool flag = itemDrop == PickupIndex.none;
            string baseToken;

            if (flag)
            {
                baseToken = "SHRINE_CHANCE_FAIL_MESSAGE";

                this.concurrentFailureCount++;
            }
            else
            {
                baseToken = "SHRINE_CHANCE_SUCCESS_MESSAGE";

                int newSuccessfulPurchaseCount = (self.GetFieldValue<int>("successfulPurchaseCount") + 1);
                self.SetFieldValue<int>("successfulPurchaseCount", newSuccessfulPurchaseCount);

                this.concurrentFailureCount = 0;

                //Creates drop object.
                PickupDropletController.CreatePickupDroplet(itemDrop, dropletOrigin.position, dropletOrigin.forward * 20f);
            }

            Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
            {
                subjectAsCharacterBody = activator.GetComponent<CharacterBody>(),
                baseToken = baseToken
            });

            Action<bool, Interactor> action = self.GetFieldValue<Action<bool, Interactor>>("onShrineChancePurchaseGlobal");
            action?.Invoke(flag, activator);

            self.SetFieldValue<bool>("waitingForRefresh", true);

            if (self.GetFieldValue<int>("successfulPurchaseCount") >= self.maxPurchaseCount)
            {
                self.symbolTransform.gameObject.SetActive(false);
            }
        }

        //Handles the effects and sounds with conditional behaviour.
        private void SpawnEffect(ShrineChanceBehavior self, PickupIndex item)
        {
            //Get the items rarity, since the rarity of the drop determines the effects color.
            string itemRarity = GetItemRarity(item);

            switch (itemRarity)
            {
                case itemRarityCommon:
                    Effect(self, Color.black);
                    Effect(self, Color.gray);
                    break;

                case itemRarityRare:
                    Effect(self, Color.green);
                    Effect(self, Color.green);
                    break;

                case itemRarityLegendary:
                    Effect(self, Color.red);
                    Effect(self, Color.red);
                    break;

                case itemRarityEquipment:
                    Effect(self, Color.yellow);
                    Effect(self, Color.yellow);
                    break;

                default:
                    Effect(self, Color.gray);
                    Effect(self, Color.yellow);
                    break;
            }
        }

        //The effect itself. Technically this is the function that spawns the effect.
        private void Effect(ShrineChanceBehavior self, Color _color)
        {
            EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ShrineUseEffect"), new EffectData
            {
                origin = self.transform.localPosition,
                rotation = Quaternion.identity,
                scale = 1f,
                color = _color
            }, true);
        }

        private static PickupIndex CalculateDrop(ShrineChanceBehavior self, Xoroshiro128Plus rng, float failureWeight, float tier1Weight, float tier2Weight, float tier3Weight, float equipmentWeight)
        {
            var stopIfRedItemDrops = SoC_Config.deactivateIfRedItemDrops.Value;
            var maxPurchaseCount = SoC_Config.maxPurchaseCount.Value;

            PickupIndex none = PickupIndex.none;
            PickupIndex tier1Value;
            PickupIndex tier2Value;
            PickupIndex tier3Value;
            PickupIndex equipmentValue;
            PickupIndex itemDrop;
            WeightedSelection<PickupIndex> weightedSelection;

            tier1Value = rng.NextElementUniform<PickupIndex>(Run.instance.availableTier1DropList);
            tier2Value = rng.NextElementUniform<PickupIndex>(Run.instance.availableTier2DropList);
            tier3Value = rng.NextElementUniform<PickupIndex>(Run.instance.availableTier3DropList);
            equipmentValue = rng.NextElementUniform<PickupIndex>(Run.instance.availableEquipmentDropList);

            weightedSelection = new WeightedSelection<PickupIndex>(8);
            weightedSelection.AddChoice(none, failureWeight);
            weightedSelection.AddChoice(tier1Value, tier1Weight);
            weightedSelection.AddChoice(tier2Value, tier2Weight);
            weightedSelection.AddChoice(tier3Value, tier3Weight);
            weightedSelection.AddChoice(equipmentValue, equipmentWeight);
            itemDrop = weightedSelection.Evaluate(rng.nextNormalizedFloat);

            //If legendary is dropped, deactivate shrine.
            if (tier3Value.Equals(itemDrop) && stopIfRedItemDrops == true)
            {
                self.SetFieldValue<int>("successfulPurchaseCount", maxPurchaseCount);
            }
            return itemDrop;
        }

        private static string GetItemRarity(PickupIndex item)
        {
            //Guard Clause
            if (item == null)
            {
                Debug.LogError("In function: " + nameof(GetItemRarity) + " could not execute correctly. Item is null");
                return null;
            }

            //Lazy and probably bad way to do it. 
            //Loop through every rarity list. If our item is on that list, we return it's corresponding rarity as a simple string.
            if (item == PickupIndex.none)
            {
                return "none";
            }

            for (int i = 0; i < Run.instance.availableTier1DropList.Count; i++)
            {
                if (item == Run.instance.availableTier1DropList[i])
                {
                    return "common";
                }
            }

            for (int i = 0; i < Run.instance.availableTier2DropList.Count; i++)
            {
                if (item == Run.instance.availableTier2DropList[i])
                {
                    return "rare";
                }
            }

            for (int i = 0; i < Run.instance.availableTier3DropList.Count; i++)
            {
                if (item == Run.instance.availableTier3DropList[i])
                {
                    return "legendary";
                }
            }

            for (int i = 0; i < Run.instance.availableEquipmentDropList.Count; i++)
            {
                if (item == Run.instance.availableEquipmentDropList[i])
                {
                    return "equipment";
                }
            }

            //If item is not any of these, log warning and return null.
            Log.LogWarning("Function: " + nameof(GetItemRarity) + " could not resolve the item's rarity. This might cause problems.");
            return null;
        }
    }
}