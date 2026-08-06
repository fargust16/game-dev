using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameDev.World.Town
{
    /// <summary>
    /// Semantic tag for a socket. Append-only — C1 and content bind to roles, not cell indices.
    /// </summary>
    public enum BuildingSocketRole : byte
    {
        None = 0,
        Generic = 1,
        Occupant = 2,
        Keeper = 3,
        Counter = 4,
        Seating = 5,
        Notice = 6,
        Guard = 7,
        Clergy = 8,
        Scholar = 9,
        Storage = 10
    }

    /// <summary>
    /// Local socket definition on a template (authoring / future explicit tables).
    /// When a template leaves <see cref="BuildingTemplate.ExplicitSockets"/> empty,
    /// sockets are inferred by scanning painted cells.
    /// </summary>
    [Serializable]
    public struct TemplateSocketDef
    {
        public int LocalX;
        public int LocalY;
        public BuildingInteriorCellKind CellKind;
        /// <summary>If not <see cref="BuildingSocketRole.None"/>, skips inference.</summary>
        public BuildingSocketRole RoleOverride;
    }

    /// <summary>Placed socket on a building instance after template assignment.</summary>
    [Serializable]
    public sealed class PlacedBuildingSocket
    {
        /// <summary>Stable index within <see cref="Building.Sockets"/> (insertion order).</summary>
        public int Index;
        public BuildingInteriorCellKind CellKind;
        public BuildingSocketRole Role;
        public int LocalX;
        public int LocalY;
        /// <summary>World XZ metres (cell centre).</summary>
        public Vector2 WorldPos;
    }

    /// <summary>Town-wide reference used by <see cref="TownSocketQuery"/> for C1 iteration.</summary>
    public readonly struct TownSocketRef
    {
        public readonly int BuildingIndex;
        public readonly int SocketIndex;
        public readonly BuildingSocketRole Role;
        public readonly BuildingInteriorCellKind CellKind;
        public readonly Vector2 WorldPos;

        public TownSocketRef(
            int buildingIndex,
            int socketIndex,
            BuildingSocketRole role,
            BuildingInteriorCellKind cellKind,
            Vector2 worldPos)
        {
            BuildingIndex = buildingIndex;
            SocketIndex = socketIndex;
            Role = role;
            CellKind = cellKind;
            WorldPos = worldPos;
        }
    }

    /// <summary>
    /// Extract, infer roles, and place sockets. Keeps ASCII scan + future explicit tables in one place.
    /// </summary>
    public static class BuildingTemplateSocketUtil
    {
        private static readonly List<TemplateSocketDef> Scratch = new List<TemplateSocketDef>(32);

        public static void CollectTemplateSockets(BuildingTemplate template, List<TemplateSocketDef> into)
        {
            into.Clear();
            if (template == null)
            {
                return;
            }

            if (template.ExplicitSockets != null && template.ExplicitSockets.Count > 0)
            {
                for (var i = 0; i < template.ExplicitSockets.Count; i++)
                {
                    into.Add(template.ExplicitSockets[i]);
                }

                return;
            }

            if (template.Cells == null)
            {
                return;
            }

            // Deterministic: row (local Z) then column (local X).
            for (var y = 0; y < template.Height; y++)
            {
                for (var x = 0; x < template.Width; x++)
                {
                    var kind = template.GetCell(x, y);
                    if (kind != BuildingInteriorCellKind.PropSocket &&
                        kind != BuildingInteriorCellKind.NpcSocket)
                    {
                        continue;
                    }

                    into.Add(new TemplateSocketDef
                    {
                        LocalX = x,
                        LocalY = y,
                        CellKind = kind,
                        RoleOverride = BuildingSocketRole.None
                    });
                }
            }
        }

        public static void PlaceSockets(Building building, BuildingTemplate template)
        {
            if (building == null)
            {
                return;
            }

            if (building.Sockets == null)
            {
                building.Sockets = new List<PlacedBuildingSocket>(8);
            }
            else
            {
                building.Sockets.Clear();
            }

            if (template == null || !building.HasTemplate)
            {
                return;
            }

            CollectTemplateSockets(template, Scratch);
            var npcOrdinal = 0;
            var propOrdinal = 0;
            for (var i = 0; i < Scratch.Count; i++)
            {
                var def = Scratch[i];
                var ordinal = def.CellKind == BuildingInteriorCellKind.NpcSocket
                    ? npcOrdinal++
                    : propOrdinal++;

                var role = def.RoleOverride != BuildingSocketRole.None
                    ? def.RoleOverride
                    : InferRole(template.Id, def.CellKind, ordinal);

                var world = TownFootprintUtil.CellCenterWorld(
                    building, template, def.LocalX, def.LocalY);

                building.Sockets.Add(new PlacedBuildingSocket
                {
                    Index = building.Sockets.Count,
                    CellKind = def.CellKind,
                    Role = role,
                    LocalX = def.LocalX,
                    LocalY = def.LocalY,
                    WorldPos = world
                });
            }
        }

        /// <summary>
        /// V1 inference table. Extend with new template ids / roles; avoid reordering existing ordinals.
        /// </summary>
        public static BuildingSocketRole InferRole(
            BuildingTemplateId templateId,
            BuildingInteriorCellKind cellKind,
            int ordinalAmongKind)
        {
            if (cellKind == BuildingInteriorCellKind.NpcSocket)
            {
                switch (templateId)
                {
                    case BuildingTemplateId.Tavern:
                    case BuildingTemplateId.Shop:
                    case BuildingTemplateId.AlleyShop:
                    case BuildingTemplateId.Booth:
                    case BuildingTemplateId.Apothecary:
                    case BuildingTemplateId.MarketHall:
                        return ordinalAmongKind == 0 ? BuildingSocketRole.Keeper : BuildingSocketRole.Occupant;
                    case BuildingTemplateId.Garrison:
                    case BuildingTemplateId.Station:
                        return BuildingSocketRole.Guard;
                    case BuildingTemplateId.SmallChurch:
                    case BuildingTemplateId.DeadHouse:
                        return BuildingSocketRole.Clergy;
                    case BuildingTemplateId.School:
                    case BuildingTemplateId.University:
                        return BuildingSocketRole.Scholar;
                    default:
                        return BuildingSocketRole.Occupant;
                }
            }

            if (cellKind == BuildingInteriorCellKind.PropSocket)
            {
                switch (templateId)
                {
                    case BuildingTemplateId.Tavern:
                        return ordinalAmongKind == 0 ? BuildingSocketRole.Counter : BuildingSocketRole.Seating;
                    case BuildingTemplateId.Shop:
                    case BuildingTemplateId.AlleyShop:
                    case BuildingTemplateId.Booth:
                    case BuildingTemplateId.Apothecary:
                    case BuildingTemplateId.MarketHall:
                        return ordinalAmongKind == 0 ? BuildingSocketRole.Counter : BuildingSocketRole.Storage;
                    case BuildingTemplateId.Station:
                        return ordinalAmongKind == 0 ? BuildingSocketRole.Notice : BuildingSocketRole.Storage;
                    default:
                        return BuildingSocketRole.Storage;
                }
            }

            return BuildingSocketRole.Generic;
        }
    }

    /// <summary>
    /// Read-only town-wide socket queries for future C1 / tools.
    /// Does not spawn content — only enumerates placed sockets.
    /// </summary>
    public static class TownSocketQuery
    {
        public static void CollectAll(TownModel model, List<TownSocketRef> into)
        {
            into.Clear();
            if (model == null)
            {
                return;
            }

            for (var bi = 0; bi < model.Buildings.Count; bi++)
            {
                AppendBuilding(model, bi, BuildingSocketRole.None, into);
            }
        }

        public static void CollectByRole(TownModel model, BuildingSocketRole role, List<TownSocketRef> into)
        {
            into.Clear();
            if (model == null || role == BuildingSocketRole.None)
            {
                return;
            }

            for (var bi = 0; bi < model.Buildings.Count; bi++)
            {
                AppendBuilding(model, bi, role, into);
            }
        }

        public static int CountRole(TownModel model, BuildingSocketRole role)
        {
            if (model == null)
            {
                return 0;
            }

            var n = 0;
            for (var bi = 0; bi < model.Buildings.Count; bi++)
            {
                var sockets = model.Buildings[bi].Sockets;
                if (sockets == null)
                {
                    continue;
                }

                for (var si = 0; si < sockets.Count; si++)
                {
                    if (sockets[si].Role == role)
                    {
                        n++;
                    }
                }
            }

            return n;
        }

        private static void AppendBuilding(
            TownModel model,
            int buildingIndex,
            BuildingSocketRole roleFilter,
            List<TownSocketRef> into)
        {
            var building = model.Buildings[buildingIndex];
            var sockets = building.Sockets;
            if (sockets == null)
            {
                return;
            }

            for (var si = 0; si < sockets.Count; si++)
            {
                var s = sockets[si];
                if (roleFilter != BuildingSocketRole.None && s.Role != roleFilter)
                {
                    continue;
                }

                into.Add(new TownSocketRef(
                    buildingIndex,
                    s.Index,
                    s.Role,
                    s.CellKind,
                    s.WorldPos));
            }
        }
    }
}
