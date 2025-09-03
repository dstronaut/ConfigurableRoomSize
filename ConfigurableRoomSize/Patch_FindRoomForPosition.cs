using HarmonyLib;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;


namespace ConfigurableRoomSize.Patches;

[HarmonyPatch(typeof(RoomRegistry), "FindRoomForPosition")]
class Patch_FindRoomForPosition
{
  // ----------- Prefix replaces the whole method -----------------
  static bool Prefix(RoomRegistry __instance, BlockPos pos, ChunkRooms otherRooms, ref Room __result)
  {
    __result = CustomFindRoomForPosition(__instance, pos, otherRooms);
    return false;        //  skip vanilla
  }

  // ----------- Replace the whole method -----------------
  private static Room CustomFindRoomForPosition(RoomRegistry self, BlockPos pos, ChunkRooms otherRooms)
  {
    // ----------- Private fields in RoomRegistry -----------
    //var methods = self.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    var bf_blockAccess = self.GetType().GetMethod("get_blockAccess", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    var blockAccess = (ICachingBlockAccessor)bf_blockAccess.Invoke(self, null);
    var bf_currentVisited = Traverse.Create(self).Field("currentVisited");
    var bf_skyLightXZChecked = Traverse.Create(self).Field("skyLightXZChecked");
    var bf_iteration = Traverse.Create(self).Field("iteration");
    var bf_api = Traverse.Create(self).Field("api");

    int[] currentVisited = (int[])bf_currentVisited.GetValue();
    int[] skyLightXZChecked = (int[])bf_skyLightXZChecked.GetValue();
    int iteration = bf_iteration.GetValue<int>() + 1;
    bf_iteration.SetValue(iteration);
    ICoreAPI api = bf_api.GetValue<ICoreAPI>();

    // ----------- Custom configuration -----------
    int ARRAYSIZE = 29;
    int MAXROOMSIZE = RoomSizeConfig.cfg.MaxRoomSize;
    int MAXCELLARSIZE = RoomSizeConfig.cfg.MaxCellarSize;
    int ALTMAXCELLARSIZE = RoomSizeConfig.cfg.AltMaxCellarSize;
    int ALTMAXCELLARVOLUME = RoomSizeConfig.cfg.AltMaxCellarVolume;

        // ----------- Oiriginal method -----------
        QueueOfInt bfsQueue = new QueueOfInt();

        int halfSize = (ARRAYSIZE - 1) / 2;
        int maxSize = halfSize + halfSize;
        bfsQueue.Enqueue(halfSize << 10 | halfSize << 5 | halfSize);

        int visitedIndex = (halfSize * ARRAYSIZE + halfSize) * ARRAYSIZE + halfSize; // Center node
        //int iteration = ++this.iteration;
        currentVisited[visitedIndex] = iteration;

        int coolingWallCount = 0;
        int nonCoolingWallCount = 0;

        int skyLightCount = 0;
        int nonSkyLightCount = 0;
        int exitCount = 0;

        blockAccess.Begin();

        bool allChunksLoaded = true;

        int minx = halfSize, miny = halfSize, minz = halfSize, maxx = halfSize, maxy = halfSize, maxz = halfSize;
        int posX = pos.X - halfSize;
        int posY = pos.Y - halfSize;
        int posZ = pos.Z - halfSize;
        BlockPos npos = new BlockPos();
        BlockPos bpos = new BlockPos();
        int dx, dy, dz;

        while (bfsQueue.Count > 0)
        {
            int compressedPos = bfsQueue.Dequeue();
            dx = compressedPos >> 10;
            dy = (compressedPos >> 5) & 0x1f;
            dz = compressedPos & 0x1f;
            npos.Set(posX + dx, posY + dy, posZ + dz);
            bpos.Set(npos);

            if (dx < minx) minx = dx;
            else if (dx > maxx) maxx = dx;

            if (dy < miny) miny = dy;
            else if (dy > maxy) maxy = dy;

            if (dz < minz) minz = dz;
            else if (dz > maxz) maxz = dz;

            Block bBlock = blockAccess.GetBlock(bpos);

            foreach (BlockFacing facing in BlockFacing.ALLFACES)
            {
                facing.IterateThruFacingOffsets(npos);  // This must be the first command in the loop, to ensure all facings will be properly looped through regardless of any 'continue;' statements
                int heatRetention = bBlock.GetRetention(bpos, facing, EnumRetentionType.Heat);

                // We cannot exit current block, if the facing is heat retaining (e.g. chiselled block with solid side)
                if (bBlock.Id != 0 && heatRetention != 0)
                {
                    if (heatRetention < 0) coolingWallCount -= heatRetention;
                    else nonCoolingWallCount += heatRetention;

                    continue;
                }

                if (!blockAccess.IsValidPos(npos))
                {
                    nonCoolingWallCount++;
                    continue;
                }

                Block nBlock = blockAccess.GetBlock(npos);
                allChunksLoaded &= blockAccess.LastChunkLoaded;
                heatRetention = nBlock.GetRetention(npos, facing.Opposite, EnumRetentionType.Heat);

                // We hit a wall, no need to scan further
                if (heatRetention != 0)
                {
                    if (heatRetention < 0) coolingWallCount -= heatRetention;
                    else nonCoolingWallCount += heatRetention;

                    continue;
                }

                // Compute the new dx, dy, dz offsets for npos
                dx = npos.X - posX;
                dy = npos.Y - posY;
                dz = npos.Z - posZ;

                // Only traverse within maxSize range, and overall room size must not exceed MAXROOMSIZE
                //   If outside that, count as an exit and don't continue searching in this direction
                //   Note: for performance, this switch statement ensures only one conditional check in each case on the dimension which has actually changed, instead of 6 conditionals or more
                bool outsideCube = false;
                switch (facing.Index)
                {
                    case 0: // North
                        if (dz < minz) outsideCube = dz < 0 || maxz - minz + 1 >= MAXROOMSIZE;
                        break;
                    case 1: // East
                        if (dx > maxx) outsideCube = dx > maxSize || maxx - minx + 1 >= MAXROOMSIZE;
                        break;
                    case 2: // South
                        if (dz > maxz) outsideCube = dz > maxSize || maxz - minz + 1 >= MAXROOMSIZE;
                        break;
                    case 3: // West
                        if (dx < minx) outsideCube = dx < 0 || maxx - minx + 1 >= MAXROOMSIZE;
                        break;
                    case 4: // Up
                        if (dy > maxy) outsideCube = dy > maxSize || maxy - miny + 1 >= MAXROOMSIZE;
                        break;
                    case 5: // Down
                        if (dy < miny) outsideCube = dy < 0 || maxy - miny + 1 >= MAXROOMSIZE;
                        break;
                }
                if (outsideCube)
                {
                    exitCount++;
                    continue;
                }


                visitedIndex = (dx * ARRAYSIZE + dy) * ARRAYSIZE + dz;
                if (currentVisited[visitedIndex] == iteration) continue;   // continue if block position was already visited
                currentVisited[visitedIndex] = iteration;

                // We only need to check the skylight if it's a block position not already visited ...
                int skyLightIndex = dx * ARRAYSIZE + dz;
                if (skyLightXZChecked[skyLightIndex] < iteration)
                {
                    skyLightXZChecked[skyLightIndex] = iteration;
                    int light = blockAccess.GetLightLevel(npos, EnumLightLevelType.OnlySunLight);

                    if (light >= api.World.SunBrightness - 1)
                    {
                        skyLightCount++;
                    }
                    else
                    {
                        nonSkyLightCount++;
                    }
                }

                bfsQueue.Enqueue(dx << 10 | dy << 5 | dz);
            }
        }



        int sizex = maxx - minx + 1;
        int sizey = maxy - miny + 1;
        int sizez = maxz - minz + 1;

        byte[] posInRoom = new byte[(sizex * sizey * sizez + 7) / 8];

        int volumeCount = 0;
        for (dx = 0; dx < sizex; dx++)
        {
            for (dy = 0; dy < sizey; dy++)
            {
                visitedIndex = ((dx + minx) * ARRAYSIZE + (dy + miny)) * ARRAYSIZE + minz;
                for (dz = 0; dz < sizez; dz++)
                {
                    if (currentVisited[visitedIndex + dz] == iteration)
                    {
                        int index = (dy * sizez + dz) * sizex + dx;

                        posInRoom[index / 8] = (byte)(posInRoom[index / 8] | (1 << (index % 8)));
                        volumeCount++;
                    }
                }
            }
        }

        bool isCellar = sizex <= MAXCELLARSIZE && sizey <= MAXCELLARSIZE && sizez <= MAXCELLARSIZE;
        if (!isCellar && volumeCount <= ALTMAXCELLARVOLUME)
        {
            isCellar = sizex <= ALTMAXCELLARSIZE && sizey <= MAXCELLARSIZE && sizez <= MAXCELLARSIZE
                || sizex <= MAXCELLARSIZE && sizey <= ALTMAXCELLARSIZE && sizez <= MAXCELLARSIZE
                || sizex <= MAXCELLARSIZE && sizey <= MAXCELLARSIZE && sizez <= ALTMAXCELLARSIZE;
        }


        return new Room()
        {
            CoolingWallCount = coolingWallCount,
            NonCoolingWallCount = nonCoolingWallCount,
            SkylightCount = skyLightCount,
            NonSkylightCount = nonSkyLightCount,
            ExitCount = exitCount,
            AnyChunkUnloaded = allChunksLoaded ? 0 : 1,
            Location = new Cuboidi(posX + minx, posY + miny, posZ + minz, posX + maxx, posY + maxy, posZ + maxz),
            PosInRoom = posInRoom,
            IsSmallRoom = isCellar && exitCount == 0
        };
    }
}
