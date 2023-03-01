﻿using System.Collections.Generic;
using GameData;
namespace ScanPosOverride.PuzzleOverrideData
{
    internal sealed class PuzzleOverride
    {
        public uint Index { get; set; }

        public Vec3 Position { get; set; } = new Vec3();

        public Vec3 Rotation { get; set; } = new Vec3();

        public bool ConcurrentCluster { get; set; } = false;
        
        public List<Vec3> TPositions { get; set; } = new List<Vec3>();

        public List<int> RequiredItemsIndices { get; set; } = new();

        public List<WardenObjectiveEventData> EventsOnPuzzleSolved { get; set; } = new();
    }
}
