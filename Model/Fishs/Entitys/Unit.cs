﻿using ETModel;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Model.Fishs.Entitys
{

    public enum UnitType
    {
        Hero,
        Npc
    }

    [ObjectSystem]
    public class UnitSystem : AwakeSystem<Unit, UnitType>
    {
        public override void Awake(Unit self, UnitType a)
        {
            self.Awake(a);
        }
    }

    public sealed class Unit : Entity
    {
        public UnitType UnitType { get; private set; }

        public Vector3 Position { get; set; }

        public void Awake(UnitType unitType)
        {
            this.UnitType = unitType;
        }

        public override void Dispose()
        {
            if (this.IsDisposed)
            {
                return;
            }

            base.Dispose();
        }
    }
}
