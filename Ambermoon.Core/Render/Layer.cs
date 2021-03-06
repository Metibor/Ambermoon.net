﻿namespace Ambermoon
{
    public enum Layer
    {
        None = -1,
        Map3DBackground, // Color floor, sky, etc
        Map3D,
        Billboards3D,
        MapBackground1,
        MapBackground2,
        MapBackground3,
        MapBackground4,
        MapBackground5,
        MapBackground6,
        MapBackground7,
        MapBackground8,
        Characters,
        MapForeground1,
        MapForeground2,
        MapForeground3,
        MapForeground4,
        MapForeground5,
        MapForeground6,
        MapForeground7,
        MapForeground8,
        FOW,
        CombatBackground,
        BattleMonsterRow,
        BattleEffects,
        UI,
        Items,
        Text,
        IntroGraphics,
        IntroText,
        Effects,
        Cursor,
        DrugEffect
    }

    public partial class Global
    {
        public const Layer First2DLayer = Layer.MapBackground1;
        public const Layer Last2DLayer = Layer.MapForeground8;
    }
}
