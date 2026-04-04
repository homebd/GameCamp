using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using GameCamp.Game.Data;
using UnityEngine;

namespace GameCamp.Game.Rewards
{
    public static class RewardCsvParsing
    {
        public static List<RewardDefinition> Parse(TextAsset csvAsset)
        {
            if (csvAsset == null)
            {
                throw new InvalidOperationException("Reward CSV TextAsset is null.");
            }

            List<List<string>> table = ParseRows(DecodeCsvText(csvAsset));
            if (table.Count < 2)
            {
                throw new InvalidOperationException("Reward CSV must contain header + at least one data row.");
            }

            Dictionary<string, int> headerIndex = BuildHeaderIndex(table[0]);
            RequireHeader(headerIndex, "id");
            RequireHeader(headerIndex, "name");
            RequireHeader(headerIndex, "desc");
            RequireHeader(headerIndex, "rarity");
            RequireHeader(headerIndex, "scope");
            RequireHeader(headerIndex, "max_count");
            RequireHeader(headerIndex, "duration");

            List<RewardDefinition> results = new();
            HashSet<int> usedIds = new();

            for (int row = 1; row < table.Count; row++)
            {
                List<string> cells = table[row];
                if (IsRowEmpty(cells))
                {
                    continue;
                }

                int rewardId = ParseInt(cells, headerIndex, "id", row);
                if (!usedIds.Add(rewardId))
                {
                    throw new InvalidOperationException($"Duplicate reward id in CSV: {rewardId} (row {row + 1})");
                }

                string name = GetRequiredCell(cells, headerIndex, "name", row);
                string desc = GetRequiredCell(cells, headerIndex, "desc", row);
                RewardRarity rarity = ParseRarity(GetRequiredCell(cells, headerIndex, "rarity", row), row);
                WeaponType scope = ParseWeaponKind(GetRequiredCell(cells, headerIndex, "scope", row), row, "scope");
                int maxCount = ParseInt(cells, headerIndex, "max_count", row);
                float duration = ParseFloat(cells, headerIndex, "duration", row);

                RewardEffectSpec effect = ParseEffect(cells, headerIndex, row, duration);
                results.Add(RewardDefinition.Create(rewardId, name, desc, rarity, scope, maxCount, effect));
            }

            if (results.Count == 0)
            {
                throw new InvalidOperationException("Reward CSV parsed zero rewards.");
            }

            return results;
        }

        private static RewardEffectSpec ParseEffect(IReadOnlyList<string> cells, IReadOnlyDictionary<string, int> headerIndex, int row, float duration)
        {
            string typeColumn = "effect_0_type";
            string valueColumn = "value_0";
            string weaponColumn = "effect_0_weapon";

            RequireHeader(headerIndex, typeColumn);
            RequireHeader(headerIndex, valueColumn);
            RequireHeader(headerIndex, weaponColumn);

            string rawType = GetRequiredCell(cells, headerIndex, typeColumn, row);
            RewardEffectType effectType = ParseEffectType(rawType, row, typeColumn);
            float value = ParseFloat(cells, headerIndex, valueColumn, row);
            WeaponType weapon = ParseWeaponKind(GetRequiredCell(cells, headerIndex, weaponColumn, row), row, weaponColumn);

            if (effectType == RewardEffectType.UnlockWeapon && weapon == WeaponType.Common)
            {
                throw new InvalidOperationException($"Unlock effect requires non-common weapon at row {row + 1}, column '{weaponColumn}'.");
            }

            return new RewardEffectSpec
            {
                EffectType = effectType,
                Value = value,
                DurationSeconds = Mathf.Max(0f, duration),
                WeaponType = weapon,
            };
        }

        private static RewardEffectType ParseEffectType(string value, int row, string column)
        {
            string normalized = Normalize(value);
            return normalized switch
            {
                "damagemultiplier" or "damage" => RewardEffectType.DamageMultiplier,
                "fireratemultiplier" or "attackspeedmultiplier" or "firerate" => RewardEffectType.AttackSpeedMultiplier,
                "scalemultiplier" or "projectilescalemultiplier" => RewardEffectType.ProjectileScaleMultiplier,
                "countadder" or "projectilecount" => RewardEffectType.ProjectileCount,
                "lifetimemultiplier" or "projectilelifetimemultiplier" => RewardEffectType.ProjectileLifetimeMultiplier,
                "pierceadder" or "projectilepierce" => RewardEffectType.ProjectilePierce,
                "unlock" or "unlockweapon" => RewardEffectType.UnlockWeapon,
                _ => throw new InvalidOperationException($"Invalid effect type '{value}' at row {row + 1}, column '{column}'."),
            };
        }

        private static RewardRarity ParseRarity(string value, int row)
        {
            string normalized = Normalize(value);
            return normalized switch
            {
                "common" => RewardRarity.Common,
                "rare" => RewardRarity.Rare,
                "epic" => RewardRarity.Epic,
                "legendary" => RewardRarity.Legendary,
                _ => throw new InvalidOperationException($"Invalid rarity '{value}' at row {row + 1}."),
            };
        }

        private static WeaponType ParseWeaponKind(string value, int row, string column)
        {
            string normalized = Normalize(value);
            return normalized switch
            {
                "common" => WeaponType.Common,
                "rifle" => WeaponType.Rifle,
                "laser" => WeaponType.Laser,
                "missile" => WeaponType.Missile,
                _ => throw new InvalidOperationException($"Invalid weapon kind '{value}' at row {row + 1}, column '{column}'."),
            };
        }

        private static string Normalize(string value)
        {
            return value.Trim().Replace(" ", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
        }

        private static Dictionary<string, int> BuildHeaderIndex(IReadOnlyList<string> headers)
        {
            Dictionary<string, int> map = new(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Count; i++)
            {
                string key = headers[i].Trim();
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                if (map.ContainsKey(key))
                {
                    throw new InvalidOperationException($"Duplicate CSV header: {key}");
                }

                map.Add(key, i);
            }

            return map;
        }

        private static void RequireHeader(IReadOnlyDictionary<string, int> headerIndex, string header)
        {
            if (!headerIndex.ContainsKey(header))
            {
                throw new InvalidOperationException($"Reward CSV missing required header: {header}");
            }
        }

        private static int ParseInt(IReadOnlyList<string> cells, IReadOnlyDictionary<string, int> headerIndex, string column, int row)
        {
            string raw = GetRequiredCell(cells, headerIndex, column, row);
            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                throw new InvalidOperationException($"Invalid int '{raw}' at row {row + 1}, column '{column}'.");
            }

            return value;
        }

        private static float ParseFloat(IReadOnlyList<string> cells, IReadOnlyDictionary<string, int> headerIndex, string column, int row)
        {
            string raw = GetRequiredCell(cells, headerIndex, column, row);
            if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                throw new InvalidOperationException($"Invalid float '{raw}' at row {row + 1}, column '{column}'.");
            }

            return value;
        }

        private static string GetRequiredCell(IReadOnlyList<string> cells, IReadOnlyDictionary<string, int> headerIndex, string column, int row)
        {
            string value = GetCell(cells, headerIndex, column);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Missing required value at row {row + 1}, column '{column}'.");
            }

            return value;
        }

        private static string GetCell(IReadOnlyList<string> cells, IReadOnlyDictionary<string, int> headerIndex, string column)
        {
            if (!headerIndex.TryGetValue(column, out int idx))
            {
                return string.Empty;
            }

            if (idx < 0 || idx >= cells.Count)
            {
                return string.Empty;
            }

            return cells[idx]?.Trim() ?? string.Empty;
        }

        private static bool IsRowEmpty(IReadOnlyList<string> cells)
        {
            for (int i = 0; i < cells.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(cells[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static List<List<string>> ParseRows(string csv)
        {
            List<List<string>> rows = new();
            List<string> currentRow = new();
            StringBuilder cell = new();
            bool inQuotes = false;

            for (int i = 0; i < csv.Length; i++)
            {
                char ch = csv[i];

                if (inQuotes)
                {
                    if (ch == '"')
                    {
                        bool isEscapedQuote = i + 1 < csv.Length && csv[i + 1] == '"';
                        if (isEscapedQuote)
                        {
                            cell.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        cell.Append(ch);
                    }

                    continue;
                }

                if (ch == '"')
                {
                    inQuotes = true;
                    continue;
                }

                if (ch == ',')
                {
                    currentRow.Add(cell.ToString());
                    cell.Clear();
                    continue;
                }

                if (ch == '\r')
                {
                    continue;
                }

                if (ch == '\n')
                {
                    currentRow.Add(cell.ToString());
                    cell.Clear();
                    rows.Add(currentRow);
                    currentRow = new List<string>();
                    continue;
                }

                cell.Append(ch);
            }

            currentRow.Add(cell.ToString());
            rows.Add(currentRow);
            return rows;
        }

        private static string DecodeCsvText(TextAsset csvAsset)
        {
            byte[] bytes = csvAsset.bytes;
            if (bytes == null || bytes.Length == 0)
            {
                return csvAsset.text ?? string.Empty;
            }

            // BOM
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                return new UTF8Encoding(false, true).GetString(bytes, 3, bytes.Length - 3);
            }

            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            {
                return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
            }

            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            {
                return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);
            }

            try
            {
                return new UTF8Encoding(false, true).GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                // Fall through to Korean legacy encodings.
            }

            try
            {
                return Encoding.GetEncoding(949).GetString(bytes); // CP949
            }
            catch
            {
                try
                {
                    return Encoding.GetEncoding("euc-kr").GetString(bytes);
                }
                catch
                {
                    return csvAsset.text ?? string.Empty;
                }
            }
        }
    }
}
