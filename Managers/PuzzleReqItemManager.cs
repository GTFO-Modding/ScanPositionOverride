﻿using System.Collections.Generic;
using System.Linq;
using ChainedPuzzles;
using GTFO.API;
using Il2CppSystem.Text;

namespace ScanPosOverride.Managers
{
    internal class PuzzleReqItemManager
    {
        public static readonly PuzzleReqItemManager Current;

        private Dictionary<int, CarryItemPickup_Core> BigPickupItemsInLevel = new();

        private int itemIndexCounter = 1;

        private List<(CP_Bioscan_Core, List<int>)> bioscanCoresToAddReqItems = new();
        private List<(CP_Cluster_Core, List<int>)> clusterCoresToAddReqItems = new();

        // core.PlayerScanner -> core
        private Dictionary<System.IntPtr, CP_Bioscan_Core> movableScansWithReqItems = new();

        public int Register(CarryItemPickup_Core item)
        {
            int allotedIndex = itemIndexCounter;
            itemIndexCounter += 1;

            BigPickupItemsInLevel.Add(allotedIndex, item);
            return allotedIndex;
        }

        public void RegisterForAddingReqItems(CP_Bioscan_Core core, List<int> itemsIndices) => bioscanCoresToAddReqItems.Add((core, itemsIndices));

        public void RegisterForAddingReqItems(CP_Cluster_Core core, List<int> itemsIndices) => clusterCoresToAddReqItems.Add((core, itemsIndices));

        public CP_Bioscan_Core GetMovableCoreWithReqItem(CP_PlayerScanner scanner) => movableScansWithReqItems.ContainsKey(scanner.Pointer) ? movableScansWithReqItems[scanner.Pointer] : null; 

        private void AddReqItems(CP_Bioscan_Core puzzle, List<int> itemsIndices)
        {
            if (puzzle == null || itemsIndices == null || itemsIndices.Count < 1) return;

            bool addedReqItem = false;

            foreach (int itemIndex in itemsIndices.ToHashSet())
            {
                if (!BigPickupItemsInLevel.ContainsKey(itemIndex))
                {
                    Logger.Error($"Unregistered BigPickup Item with index {itemIndex}");
                    continue;
                }

                CarryItemPickup_Core carryItemPickup_Core = BigPickupItemsInLevel[itemIndex];
                puzzle.AddRequiredItems(new iWardenObjectiveItem[1] { new iWardenObjectiveItem(carryItemPickup_Core.Pointer) });

                addedReqItem = true;
            }

            if(puzzle.IsMovable && addedReqItem)
            {
                movableScansWithReqItems.Add(puzzle.m_playerScanner.Pointer, puzzle);
            }
        }

        private void AddRegisteredReqItems()
        {
            foreach (var tuple in bioscanCoresToAddReqItems)
            {
                CP_Bioscan_Core core = tuple.Item1;
                List<int> itemsIndices = tuple.Item2;

                AddReqItems(core, itemsIndices);
            }

            foreach (var tuple in clusterCoresToAddReqItems)
            {
                CP_Cluster_Core core = tuple.Item1;
                List<int> itemsIndices = tuple.Item2;

                foreach (var childCore in core.m_childCores)
                {
                    CP_Bioscan_Core bioscan_Core = childCore.TryCast<CP_Bioscan_Core>();
                    if (bioscan_Core == null)
                    {
                        Logger.Error("Failed to cast child core to CP_Bioscan_Core");
                        continue;
                    }

                    AddReqItems(bioscan_Core, itemsIndices);
                }
            }
        }

        private void OutputLevelBigPickupInfo()
        {
            StringBuilder info = new();
            info.AppendLine();
            List<CarryItemPickup_Core> allBigPickups = new(BigPickupItemsInLevel.Values);

            allBigPickups.Sort((b1, b2) => {
                var n1 = b1.SpawnNode;
                var n2 = b2.SpawnNode;

                if (n1.m_dimension.DimensionIndex != n2.m_dimension.DimensionIndex)
                    return (int)n1.m_dimension.DimensionIndex <= (int)n2.m_dimension.DimensionIndex ? -1 : 1;

                if(n1.LayerType != n2.LayerType) 
                    return (int)n1.LayerType < (int)n2.LayerType ? -1 : 1;

                if(n1.m_zone.LocalIndex != n2.m_zone.LocalIndex) 
                    return (int)n1.m_zone.LocalIndex < (int)n2.m_zone.LocalIndex ? -1 : 1;

                return 0;
            });

            Dictionary<CarryItemPickup_Core, int> itemIndicesInLevel = new();
            foreach(int itemIndex in BigPickupItemsInLevel.Keys)
                itemIndicesInLevel.Add(BigPickupItemsInLevel[itemIndex], itemIndex);
            

            foreach (CarryItemPickup_Core item in allBigPickups)
            {
                info.AppendLine($"Item Name: {item.ItemDataBlock.publicName}");
                info.AppendLine($"Zone {item.SpawnNode.m_zone.Alias}, Layer {item.SpawnNode.LayerType}, Dim {item.SpawnNode.m_dimension.DimensionIndex}");
                info.AppendLine($"Item Index: {itemIndicesInLevel[item]}");
                info.AppendLine();
            }

            Logger.Debug(info.ToString());
        }

        internal void OnEnterLevel()
        {
            AddRegisteredReqItems();
            OutputLevelBigPickupInfo();
        }

        public void Clear()
        {
            BigPickupItemsInLevel.Clear();
            itemIndexCounter = 1;
            bioscanCoresToAddReqItems.Clear();
            clusterCoresToAddReqItems.Clear();
            movableScansWithReqItems.Clear();
        }

        static PuzzleReqItemManager()
        {
            Current = new();
            LevelAPI.OnLevelCleanup += Current.Clear;
            LevelAPI.OnEnterLevel += Current.OnEnterLevel;
        }

        private PuzzleReqItemManager() { }
    }
}