﻿namespace Ambermoon.Data
{
    public class Item
    {
        public uint GraphicIndex { get; set; }
        public ItemType Type { get; set; }
        public byte Unknown1 { get; set; }
        public byte Unknown2 { get; set; }
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
        public byte[] Unknown3 { get; set; }
        public int MagicArmorLevel { get; set; } // M-B-R
        public int MagicAttackLevel { get; set; } // M-B-W
        public ItemFlags Flags { get; set; }
        public byte Unknown4 { get; set; }
        public ClassFlag Classes { get; set; }
        public uint Price { get; set; }
        public uint Weight { get; set; }
        public string Name { get; set; }

        float GetPriceFactor(PartyMember character) => 2.92f + character.Attributes[Data.Attribute.Charisma].TotalCurrentValue * 0.16f / 100.0f;
        public uint GetBuyPrice(PartyMember buyer) => (uint)Util.Round(Price / GetPriceFactor(buyer));
        public uint GetSellPrice(PartyMember seller) => (uint)Util.Round(0.5f * Price * GetPriceFactor(seller));

        public static Item Load(IItemReader itemReader, IDataReader dataReader)
        {
            var item = new Item();

            itemReader.ReadItem(item, dataReader);

            return item;
        }
    }
}
