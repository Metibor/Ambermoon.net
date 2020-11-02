﻿using Ambermoon.Data.Serialization;

namespace Ambermoon.Data
{
    public class Item
    {
        public uint Index { get; set; }
        public uint GraphicIndex { get; set; }
        public ItemType Type { get; set; }
        public EquipmentSlot EquipmentSlot { get; set; }
        public byte BreakChance { get; set; }
        public GenderFlag Genders { get; set; }
        public uint NumberOfHands { get; set; }
        public uint NumberOfFingers { get; set; }
        public int HitPoints{ get; set; }
        public int SpellPoints { get; set; }
        public Attribute? Attribute { get; set; }
        public int AttributeValue { get; set; }
        public Ability? Ability { get; set; }
        public int AbilityValue { get; set; }
        public int Defense { get; set; }
        public int Damage { get; set; }
        /// <summary>
        /// Used if this is a ammunition.
        /// </summary>
        public AmmunitionType AmmunitionType { get; set; }
        /// <summary>
        /// Used if this is a long-ranged weapon with ammunition.
        /// </summary>
        public AmmunitionType UsedAmmunitionType { get; set; }
        public byte[] UnknownBytes17To20 { get; set; } // 4
        /// <summary>
        /// This value is used for:
        /// - Special item purposes like clock, compass, etc (<see cref="SpecialItemPurpose"/>)
        /// - Transportation (<see cref="Transportation"/>)
        /// - Text index of text scrolls (<see cref="TextIndex"/>)
        /// </summary>
        public byte SpecialValue { get; set; } // special item purpose, transportation, etc
        public byte TextSubIndex { get; set; }
        public SpellType SpellType { get; set; }
        public byte SpellIndex { get; set; }
        public byte SpellUsageCount { get; set; } // 255 = infinite
        public byte[] UnknownBytes26To29 { get; set; } // 4
        public int MagicArmorLevel { get; set; } // M-B-R
        public int MagicAttackLevel { get; set; } // M-B-W
        public ItemFlags Flags { get; set; }
        public ItemSlotFlags DefaultSlotFlags { get; set; }
        public ClassFlag Classes { get; set; }
        public uint Price { get; set; }
        public uint Weight { get; set; }
        public string Name { get; set; }

        /// <summary>
        /// Used only for special items.
        /// 
        /// Note that this is the same as <see cref="SpecialValue"/>.
        /// </summary>
        public SpecialItemPurpose SpecialItemPurpose
        {
            get => (SpecialItemPurpose)SpecialValue;
            set => SpecialValue = (byte)value;
        }
        /// <summary>
        /// Used only for transportation items.
        /// 
        /// Note that this is the same as <see cref="SpecialValue"/>.
        /// </summary>
        public Transportation Transportation
        {
            get => (Transportation)SpecialValue;
            set => SpecialValue = (byte)value;
        }
        /// <summary>
        /// Used only for text scrolls.
        /// 
        /// Note that this is the same as <see cref="SpecialValue"/>.
        /// </summary>
        public uint TextIndex
        {
            get => SpecialValue;
            set => SpecialValue = (byte)value;
        }
        public Spell Spell => SpellIndex == 0 ? Spell.None : (Spell)((int)SpellType * 30 + SpellIndex);

        float GetPriceFactor(PartyMember character) => 2.92f + character.Attributes[Data.Attribute.Charisma].TotalCurrentValue * 0.16f / 100.0f;
        public uint GetBuyPrice(PartyMember buyer) => (uint)Util.Round(Price / GetPriceFactor(buyer));
        public uint GetSellPrice(PartyMember seller) => (uint)Util.Round(0.5f * Price * GetPriceFactor(seller));

        public static Item Load(uint index, IItemReader itemReader, IDataReader dataReader)
        {
            var item = new Item { Index = index };

            itemReader.ReadItem(item, dataReader);

            return item;
        }
    }
}
