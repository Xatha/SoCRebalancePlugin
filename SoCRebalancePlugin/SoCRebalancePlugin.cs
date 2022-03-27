using System;
using BepInEx;
using R2API;
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
        //The Plugin GUID should be a unique ID for this plugin, which is human readable (as it is used in places like the config).
        //If we see this PluginGUID as it is on thunderstore, we will deprecate this mod. Change the PluginAuthor and the PluginName !
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Luca";
        public const string PluginName = "SoCRebalancePlugin";
        public const string PluginVersion = "1.0.1";

        //Init values
        private PickupDropTable dropTable;
        private int concurrentFailurePurchaseCount = 0;
        private float refreshTimer = 2f;


        //The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            //Init our logging class so that we can properly log for debugging
            Log.Init(Logger);

            On.RoR2.ShrineChanceBehavior.AddShrineStack += ShrineChanceBehaviour_AddShrineStack;
            Log.LogInfo(nameof(Awake) +" is Done. SoC has succesfully hooked into wanted functions.");
        }


        private void ShrineChanceBehaviour_AddShrineStack(On.RoR2.ShrineChanceBehavior.orig_AddShrineStack orig, ShrineChanceBehavior self, Interactor activator)
        {
            //Sanity check.
            if (!NetworkServer.active)
            {
                Debug.LogWarning("[Server] function 'System.Void RoR2.ShrineChanceBehavior::AddShrineStack(RoR2.Interactor)' called on client");
                return;
            }

            //For testing purposes 
            //self.SetFieldValue<float>("refreshTimer", 0f);
            //self.SetFieldValue<float>("costMultiplierPerPurchase", 1f);
            //self.SetFieldValue<int>("maxPurchaseCount", 999);

            //Getting fields from object. 
            var rng = self.GetFieldValue<Xoroshiro128Plus>("rng");

            var failureChance = self.GetFieldValue<float>("failureChance");

            var failureWeight = self.GetFieldValue<float>("failureWeight");
            var equipmentWeight = self.GetFieldValue<float>("equipmentWeight");
            var tier1Weight = self.GetFieldValue<float>("tier1Weight");
            var tier2Weight = self.GetFieldValue<float>("tier2Weight");
            var tier3Weight = self.GetFieldValue<float>("tier3Weight");
            var shrineColor = self.GetFieldValue<Color>("shrineColor");
            var dropletOrigin = self.GetFieldValue<Transform>("dropletOrigin");

            //Initialising values for the droptable algorithm. 
            PickupIndex none;
            PickupIndex tier1Value;
            PickupIndex tier2Value;
            PickupIndex tier3Value;
            PickupIndex equipmentValue;

            WeightedSelection<PickupIndex> weightedSelection;

            //Getting the purchase cost.
            var purchaseInteraction = self.GetFieldValue<PurchaseInteraction>("purchaseInteraction");

            PickupIndex pickupIndex = PickupIndex.none;
            if (this.dropTable)
            {
                if (rng.nextNormalizedFloat > failureChance)
                {
                    pickupIndex = this.dropTable.GenerateDrop(rng);
                }
            }
            else if (purchaseInteraction.Networkcost != 0)
            {
                switch (this.concurrentFailurePurchaseCount)
                {

                    case 0:
                        none = PickupIndex.none;
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
                        pickupIndex = weightedSelection.Evaluate(rng.nextNormalizedFloat);

                        //If legendary is dropped, deactivate shrine.
                        if (tier3Value.Equals(pickupIndex))
                        {
                            self.SetFieldValue<int>("successfulPurchaseCount", self.maxPurchaseCount);
                        }

                        break;

                    case 1:
                        none = PickupIndex.none;
                        tier1Value = rng.NextElementUniform<PickupIndex>(Run.instance.availableTier1DropList);
                        tier2Value = rng.NextElementUniform<PickupIndex>(Run.instance.availableTier2DropList);
                        tier3Value = rng.NextElementUniform<PickupIndex>(Run.instance.availableTier3DropList);
                        equipmentValue = rng.NextElementUniform<PickupIndex>(Run.instance.availableEquipmentDropList);
                        weightedSelection = new WeightedSelection<PickupIndex>(8);
                        weightedSelection.AddChoice(none, 10.5f);
                        weightedSelection.AddChoice(tier1Value, 7f);
                        weightedSelection.AddChoice(tier2Value, 4f);
                        weightedSelection.AddChoice(tier3Value, 0.4f);
                        weightedSelection.AddChoice(equipmentValue, 2f);
                        pickupIndex = weightedSelection.Evaluate(rng.nextNormalizedFloat);

                        //If legendary is dropped, deactivate shrine.
                        if (tier3Value.Equals(pickupIndex))
                        {
                            self.SetFieldValue<int>("successfulPurchaseCount", self.maxPurchaseCount);
                        }

                        break;

                    case 2:
                        none = PickupIndex.none;
                        tier1Value = rng.NextElementUniform<PickupIndex>(Run.instance.availableTier1DropList);
                        tier2Value = rng.NextElementUniform<PickupIndex>(Run.instance.availableTier2DropList);
                        tier3Value = rng.NextElementUniform<PickupIndex>(Run.instance.availableTier3DropList);
                        equipmentValue = rng.NextElementUniform<PickupIndex>(Run.instance.availableEquipmentDropList);
                        weightedSelection = new WeightedSelection<PickupIndex>(8);
                        weightedSelection.AddChoice(none, 15.5f);
                        weightedSelection.AddChoice(tier1Value, 2f);
                        weightedSelection.AddChoice(tier2Value, 12f);
                        weightedSelection.AddChoice(tier3Value, 2f);
                        weightedSelection.AddChoice(equipmentValue, 4f);
                        pickupIndex = weightedSelection.Evaluate(rng.nextNormalizedFloat);

                        //If legendary is dropped, deactivate shrine.
                        if (tier3Value.Equals(pickupIndex))
                        {
                            self.SetFieldValue<int>("successfulPurchaseCount", self.maxPurchaseCount);
                        }

                        break;

                    case 3:
                        none = PickupIndex.none;
                        tier1Value = rng.NextElementUniform<PickupIndex>(Run.instance.availableTier1DropList);
                        tier2Value = rng.NextElementUniform<PickupIndex>(Run.instance.availableTier2DropList);
                        tier3Value = rng.NextElementUniform<PickupIndex>(Run.instance.availableTier3DropList);
                        equipmentValue = rng.NextElementUniform<PickupIndex>(Run.instance.availableEquipmentDropList);
                        weightedSelection = new WeightedSelection<PickupIndex>(8);
                        weightedSelection.AddChoice(none, 20f);
                        weightedSelection.AddChoice(tier1Value, 2f);
                        weightedSelection.AddChoice(tier2Value, 15f);
                        weightedSelection.AddChoice(tier3Value, 5f);
                        weightedSelection.AddChoice(equipmentValue, 4f);
                        pickupIndex = weightedSelection.Evaluate(rng.nextNormalizedFloat);

                        //If legendary is dropped, deactivate shrine.
                        if (tier3Value.Equals(pickupIndex))
                        {
                            self.SetFieldValue<int>("successfulPurchaseCount", self.maxPurchaseCount);
                        }

                        break;

                    case 4:
                        none = PickupIndex.none;
                        tier1Value = rng.NextElementUniform<PickupIndex>(Run.instance.availableTier1DropList);
                        tier2Value = rng.NextElementUniform<PickupIndex>(Run.instance.availableTier2DropList);
                        tier3Value = rng.NextElementUniform<PickupIndex>(Run.instance.availableTier3DropList);
                        equipmentValue = rng.NextElementUniform<PickupIndex>(Run.instance.availableEquipmentDropList);
                        weightedSelection = new WeightedSelection<PickupIndex>(8);
                        weightedSelection.AddChoice(none, 20f);
                        weightedSelection.AddChoice(tier1Value, 0.5f);
                        weightedSelection.AddChoice(tier2Value, 17f);
                        weightedSelection.AddChoice(tier3Value, 7f);
                        weightedSelection.AddChoice(equipmentValue, 2f);
                        pickupIndex = weightedSelection.Evaluate(rng.nextNormalizedFloat);

                        //If legendary is dropped, deactivate shrine.
                        if (tier3Value.Equals(pickupIndex))
                        {
                            self.SetFieldValue<int>("successfulPurchaseCount", self.maxPurchaseCount);
                        }

                        break;

                    case 5:
                        none = PickupIndex.none;
                        tier1Value = rng.NextElementUniform<PickupIndex>(Run.instance.availableTier1DropList);
                        tier2Value = rng.NextElementUniform<PickupIndex>(Run.instance.availableTier2DropList);
                        tier3Value = rng.NextElementUniform<PickupIndex>(Run.instance.availableTier3DropList);
                        equipmentValue = rng.NextElementUniform<PickupIndex>(Run.instance.availableEquipmentDropList);
                        weightedSelection = new WeightedSelection<PickupIndex>(8);
                        weightedSelection.AddChoice(none, 10.1f);
                        weightedSelection.AddChoice(tier1Value, 0f);
                        weightedSelection.AddChoice(tier2Value, 6f);
                        weightedSelection.AddChoice(tier3Value, 7f);
                        weightedSelection.AddChoice(equipmentValue, 0f);
                        pickupIndex = weightedSelection.Evaluate(rng.nextNormalizedFloat);

                        //If legendary is dropped, deactivate shrine.
                        if (tier3Value.Equals(pickupIndex))
                        {
                            self.SetFieldValue<int>("successfulPurchaseCount", self.maxPurchaseCount);
                        }

                        break;


                    case 6:
                        tier3Value = rng.NextElementUniform<PickupIndex>(Run.instance.availableTier3DropList);
                        weightedSelection = new WeightedSelection<PickupIndex>(8);
                        weightedSelection.AddChoice(tier3Value, tier3Weight);
                        pickupIndex = weightedSelection.Evaluate(rng.nextNormalizedFloat);

                        //If legendary is dropped, deactivate shrine.
                        self.SetFieldValue<int>("successfulPurchaseCount", self.maxPurchaseCount);

                        break;
                }
            } 
            else
            {
                none = PickupIndex.none;
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
                pickupIndex = weightedSelection.Evaluate(rng.nextNormalizedFloat);
            }


            bool flag = pickupIndex == PickupIndex.none;
            string baseToken;
            if (flag)
            {
                baseToken = "SHRINE_CHANCE_FAIL_MESSAGE";

                this.concurrentFailurePurchaseCount++;

                var purchasseInteraction = self.GetFieldValue<PurchaseInteraction>("purchaseInteraction");
                var x = purchasseInteraction.Networkcost;
            }
            else
            {
                int tmpValue = self.GetFieldValue<int>("successfulPurchaseCount") + 1;

                baseToken = "SHRINE_CHANCE_SUCCESS_MESSAGE";

                self.SetFieldValue<int>("successfulPurchaseCount", tmpValue);
                this.concurrentFailurePurchaseCount = 0;

                PickupDropletController.CreatePickupDroplet(pickupIndex, dropletOrigin.position, dropletOrigin.forward * 20f);

                var purchasseInteraction = self.GetFieldValue<PurchaseInteraction>("purchaseInteraction");
                var x = purchasseInteraction.Networkcost;
            }

            Chat.SendBroadcastChat(new Chat.SubjectFormatChatMessage
            {
                subjectAsCharacterBody = activator.GetComponent<CharacterBody>(),
                baseToken = baseToken
            });

            //Gets tier1Value of 'onShrineChancePurchaseGlobal'. It is used to determain if object has been interacted with or not.
            Action<bool, Interactor> action = self.GetFieldValue<Action<bool, Interactor>>("onShrineChancePurchaseGlobal");
            if (action != null)
            {
                action(flag, activator);
            }

            //Analogue to the source code.
            self.SetFieldValue<bool>("waitingForRefresh", true);
            self.SetFieldValue<float>("refreshTimer", this.refreshTimer);

            if (self.GetFieldValue<int>("successfulPurchaseCount") >= self.maxPurchaseCount)
            {
                self.symbolTransform.gameObject.SetActive(false);
            }

        }
    }
}
