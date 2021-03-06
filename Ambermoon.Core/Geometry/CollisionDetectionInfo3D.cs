﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Ambermoon.Geometry
{
    public interface ICollisionBody
    {
        bool TestCollision(float lastX, float lastZ, float x, float z, float bodyRadius, bool player);
    }

    public class CollisionLine3D : ICollisionBody
    {
        public float X { get; set; }
        public float Z { get; set; }
        public float Length { get; set; }
        public bool Horizontal { get; set; }
        public bool PlayerCanPass { get; set; }

        public bool TestCollision(float lastX, float lastZ, float x, float z, float bodyRadius, bool player)
        {
            if (player && PlayerCanPass)
                return false;

            if (Horizontal)
            {
                if (z < Z == lastZ < Z && Math.Abs(z - Z) >= bodyRadius)
                    return false;

                float left = x - bodyRadius;
                float right = x + bodyRadius;

                return (left > X && left < X + Length) ||
                    (right > X && right < X + Length);
            }
            else
            {
                if (x < X == lastX < X && Math.Abs(x - X) >= bodyRadius)
                    return false;

                float top = z + bodyRadius;
                float bottom = z - bodyRadius;

                return (top < Z && top > Z - Length) ||
                    (bottom < Z && bottom > Z - Length);
            }
        }
    }

    public class CollisionSphere3D : ICollisionBody
    {
        public float CenterX { get; set; }
        public float CenterZ { get; set; }
        public float Radius { get; set; }
        public bool PlayerCanPass { get; set; }

        public bool TestCollision(float lastX, float lastZ, float x, float z, float bodyRadius, bool player)
        {
            if (player && PlayerCanPass)
                return false;

            float xDist = Math.Abs(x - CenterX) - bodyRadius;
            float zDist = Math.Abs(z - CenterZ) - bodyRadius;
            float safeDist = Radius;

            if (xDist >= safeDist ||
                zDist >= safeDist)
                return false;

            if (xDist <= 0.0f || zDist <= 0.0f)
                return true;

            return Math.Sqrt(xDist * xDist + zDist * zDist) < safeDist;
        }
    }

    public class CollisionDetectionInfo3D
    {
        public List<ICollisionBody> CollisionBodies { get; } = new List<ICollisionBody>();
        
        public bool TestCollision(float lastX, float lastZ, float x, float z, float bodyRadius, bool player)
        {
            return CollisionBodies.Any(b => b.TestCollision(lastX, lastZ, x, z, bodyRadius, player));
        }
    }
}
