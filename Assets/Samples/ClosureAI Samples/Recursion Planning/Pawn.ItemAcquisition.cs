#if UNITASK_INSTALLED
using System;
using UnityEngine;
using static ClosureAI.AI;

namespace ClosureAI.Samples.Shared
{
    public partial class Pawn
    {
        /// <summary>
        /// Creates a leaf node that moves the agent to a specified target position with a default stopping distance.
        /// </summary>
        /// <param name="getTargetPosition">A function that returns the target position.</param>
        /// <param name="lifecycle">An optional action to invoke for lifecycle events.</param>
        /// <returns>A leaf node for movement.</returns>
        public Node MoveTo(Func<Vector3> getTargetPosition, Action lifecycle = null)
        {
            return MoveTo(() => (getTargetPosition(), 1f, 0.1f), lifecycle);
        }

        public Node MoveTo(Func<(Vector3 position, float stoppingDistance)> getParams, Action lifecycle = null)
        {
            return MoveTo(() =>
            {
                var (pos, stoppingDistance) = getParams();
                return (pos, stoppingDistance, stoppingDistance);
            },
            lifecycle);
        }

        /// <summary>
        /// Creates a selector node that attempts to acquire a specified item.
        /// This is the central planning method that uses recursion via Yield() to create a plan.
        /// It will try to acquire an item in the following order:
        /// 1. Check if the item is already in the inventory.
        /// 2. Find and collect the item in the world.
        /// 3. Craft the item using a recipe.
        /// 4. Harvest the item from a resource.
        /// </summary>
        /// <param name="getItemID">A function that returns the ID of the item to acquire.</param>
        /// <param name="lifecycle">An optional action to invoke for lifecycle events.</param>
        /// <returns>A selector node for acquiring an item.</returns>
        public Node AcquireItem(Func<string> getItemID, Action lifecycle = null) => Selector("Acquire Item", () =>
        {
            var itemID = Variable(getItemID); // We add these so they are visible in the inspector
            var item = Variable(() => Item.FindWithID(getItemID()));
            var recipe = Variable(() => ItemDatabase.GetFromID(itemID.Value));
            Condition("Already Have", () => Inventory.Contains(itemID.Value));

            //---------------------------------------------------

            D.Condition("Item In World", () => item.Value);
            YieldSimpleCached(() => CollectItem(getItemID, item));

            //----------------------------------------------------

            D.Condition("Craftable", () => recipe.Value?.Craftable ?? false);
            YieldSimpleCached(() => CraftItem(getItemID, recipe));

            //----------------------------------------------------

            YieldSimpleCached(() => HarvestItem(itemID));

            //----------------------------------------------------O

            lifecycle?.Invoke();
        });

        /// <summary>
        /// Creates a sequence node to collect an item that exists in the world.
        /// </summary>
        /// <param name="getItemID">A function that returns the ID of the item to collect.</param>
        /// <param name="getItem">A function that returns the item instance from the world.</param>
        /// <param name="lifecycle">An optional action to invoke for lifecycle events.</param>
        /// <returns>A sequence node for collecting an item.</returns>
        private Node CollectItem(Func<string> getItemID, Func<Item> getItem, Action lifecycle = null) => Sequence("Collect", () =>
        {
            var itemID = Variable(getItemID);
            var item = Variable(getItem);

            MoveTo(() => item.Value.transform.position);
            Wait("Collect", 1, () => // Fake waiting 1 second to collect item
            {
                OnSuccess(() =>
                {
                    Inventory.Add(itemID.Value);
                    Destroy(item.Value.gameObject);
                });
            });

            lifecycle?.Invoke();
        });

        /// <summary>
        /// Creates a sequence node to craft an item from a recipe.
        /// This method will recursively call AcquireItem for required items and Build for required buildings.
        /// </summary>
        /// <param name="getItemID">A function that returns the ID of the item to craft.</param>
        /// <param name="getRecipe">A function that returns the recipe for the item.</param>
        /// <returns>A sequence node for crafting an item.</returns>
        private Node CraftItem(Func<string> getItemID, Func<ItemDatabaseEntry> getRecipe) => SequenceAlways("Craft", () =>
        {
            var itemID = Variable(getItemID);
            var recipe = Variable(getRecipe);

            D.Condition("Does Not Have Building", () => !Building.FindByID(recipe.Value.RequiredBuilding));
            D.Condition("Has Recipe For Building", () => !string.IsNullOrEmpty(recipe.Value.RequiredBuilding));
            YieldSimpleCached(() => Build(() => recipe.Value.RequiredBuilding));

            D.ForEach(() => recipe.Value.RequiredItems, out var requiredItemID);
            YieldSimpleCached(() => AcquireItem(requiredItemID)); // Perform recursion!

            D.Condition(() => !string.IsNullOrEmpty(recipe.Value.RequiredBuilding));
            MoveTo(() => (Building.FindByID(recipe.Value.RequiredBuilding).transform.position, 2f));

            Wait("Perform Crafting", () => recipe.Value.TimeToCraft, () =>
            {
                OnSuccess(() =>
                {
                    foreach (var removeItemID in recipe.Value.RequiredItems)
                        Inventory.Remove(removeItemID);

                    Inventory.Add(itemID.Value);
                });
            });
        });

        /// <summary>
        /// Creates a sequence node to harvest an item from a harvestable resource.
        /// This method may recursively call AcquireItem if a tool is required for harvesting.
        /// </summary>
        /// <param name="getItemID">A function that returns the ID of the item to harvest.</param>
        /// <param name="lifecycle">An optional action to invoke for lifecycle events.</param>
        /// <returns>A sequence node for harvesting an item.</returns>
        private Node HarvestItem(Func<string> getItemID, Action lifecycle = null) => SequenceAlways("Harvest", () =>
        {
            var harvestable = Variable(() => Harvestable.FindByID(getItemID()));

            Condition("Has Harvestable", () => harvestable.Value);

            D.Condition(() => !string.IsNullOrEmpty(harvestable.Value.RequiredItem));
            YieldSimpleCached(() => AcquireItem(() => harvestable.Value.RequiredItem));

            MoveTo(() => (harvestable.Value.transform.position, 1.5f));

            Wait("Perform Harvest", 1.25f, () =>
            {
                OnSuccess(() =>
                {
                    Inventory.Add(harvestable.Value.ItemID, harvestable.Value.Amount);
                    Destroy(harvestable.Value.gameObject);
                });
            });

            lifecycle?.Invoke();
        });

        /// <summary>
        /// Creates a sequence node to build a structure at a predefined placement location.
        /// This method will recursively call AcquireItem to get the necessary buildable item.
        /// </summary>
        /// <param name="getBuildableID">A function that returns the ID of the buildable item.</param>
        /// <param name="lifecycle">An optional action to invoke for lifecycle events.</param>
        /// <returns>A sequence node for building.</returns>
        public Node Build(Func<string> getBuildableID, Action lifecycle = null) => Sequence("Build", () =>
        {
            var buildableID = Variable(getBuildableID);
            var placement = Variable(() => Placement.FindByID(buildableID.Value));

            Condition("Has Placement", () =>
            {
                if (!placement.Value)
                {
                    if (Building.FindByID(buildableID.Value))
                        Debug.LogWarning($"Building {buildableID.Value} already exists in the world!");

                    throw new InvalidOperationException($"No placement found for {buildableID.Value}!");
                }

                return placement.Value;
            });

            YieldSimpleCached(() => AcquireItem(() => buildableID.Value));
            MoveTo(() => (placement.Value.transform.position, 2f));

            Wait("Build", 0.5f, () =>
            {
                OnSuccess(() =>
                {
                    Inventory.Remove(buildableID.Value);
                    placement.Value.NotifyPlacementSuccess(); // Enable build GameObject into scene
                });
            });

            lifecycle?.Invoke();
        });
    }
}
#endif
