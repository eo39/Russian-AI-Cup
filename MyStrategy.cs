using System;
using System.Linq;
using AiCup2019.Model;

namespace AiCup2019
{
    public class MyStrategy
    {
        static double DistanceSqr(Vec2Double a, Vec2Double b)
        {
            return (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y);
        }

        public UnitAction GetAction(Unit unit, Game game, Debug debug)
        {
            Unit? nearestEnemy = game.Units
                .Where(otherUnit => otherUnit.PlayerId != unit.PlayerId)
                .OrderBy(otherUnit => DistanceSqr(unit.Position, otherUnit.Position))
                .Select(otherUnit => (Unit?) otherUnit)
                .FirstOrDefault();

            bool swapWeapon = false;
            LootBox? nearestWeapon = null;
            foreach (var lootBox in game.LootBoxes)
            {
                if (lootBox.Item is Item.HealthPack && unit.Health != 100)
                {
                    if (!nearestWeapon.HasValue || DistanceSqr(unit.Position, lootBox.Position) <
                        DistanceSqr(unit.Position, nearestWeapon.Value.Position))
                    {
                        nearestWeapon = lootBox;
                    }
                }

                if (!unit.Weapon.HasValue)
                {
                    if (lootBox.Item is Item.Weapon)
                    {
                        if (!nearestWeapon.HasValue ||
                            DistanceSqr(unit.Position, lootBox.Position) <
                            DistanceSqr(unit.Position, nearestWeapon.Value.Position))
                            nearestWeapon = lootBox;
                    }
                }
                else
                {
                    if (unit.Weapon.Value.Typ == WeaponType.Pistol && lootBox.Item is Item.Weapon)
                        swapWeapon = true;
                }

            }

            Vec2Double targetPos = unit.Position;
            if (!unit.Weapon.HasValue && nearestWeapon.HasValue)
            {
                targetPos = nearestWeapon.Value.Position;
            }
            else if (nearestEnemy.HasValue)
            {
                targetPos = nearestEnemy.Value.Position;
            }
            debug.Draw(new CustomData.Log("Target pos: " + targetPos));

            Vec2Double aim = new Vec2Double(0, 0);
            if (nearestEnemy.HasValue)
            {
                aim = new Vec2Double(nearestEnemy.Value.Position.X - unit.Position.X,
                    nearestEnemy.Value.Position.Y - unit.Position.Y);
            }

            bool jump = 
                        targetPos.X > unit.Position.X &&
                        game.Level.Tiles[(int)(unit.Position.X + 1)][(int)unit.Position.Y] == Tile.Wall ||
                        targetPos.X < unit.Position.X &&
                        game.Level.Tiles[(int)(unit.Position.X - 1)][(int)(unit.Position.Y)] == Tile.Wall;

            bool shoot = true;
            bool reload = false;

            double velocity = targetPos.X - unit.Position.X;

            if (nearestEnemy?.Weapon != null)
            {
                if (nearestEnemy.Value.Weapon.Value.WasShooting && nearestEnemy.Value.Weapon.Value.Typ == WeaponType.RocketLauncher)
                {
                    if (unit.Position.Y == nearestEnemy.Value.Position.Y)
                        jump = true;
                    else
                        velocity = nearestEnemy.Value.Position.X - unit.Position.X;
                }
            }

            if (unit.Weapon.HasValue)
            {
                if (unit.Weapon.Value.Magazine == 0)
                    reload = true;

                if (unit.Weapon.Value.Typ == WeaponType.RocketLauncher)
                {
                    int minY, maxY;
                    if (targetPos.Y >= unit.Position.Y)
                    {
                        minY = (int) (unit.Position.Y);
                        maxY = (int) (targetPos.Y);
                    }
                    else
                    {
                        minY = (int)(targetPos.Y);
                        maxY = (int)(unit.Position.Y);
                    }

                    int minX, maxX;
                    if (targetPos.X >= unit.Position.X)
                    {
                        minX = (int)(unit.Position.X);
                        maxX = (int)(targetPos.X);
                    }
                    else
                    {
                        minX = (int)(targetPos.X);
                        maxX = (int)(unit.Position.X);
                    }

                    for (int i = minX; i < maxX; i++)
                        for (int j = minY; j < maxY; j++)
                            if (game.Level.Tiles[i][j] == Tile.Wall)
                                shoot = false;
                }

                switch (unit.Weapon.Value.Typ)
                {
                    case WeaponType.AssaultRifle:
                    case WeaponType.Pistol:
                    {
                        if (Math.Abs(nearestEnemy.Value.Position.X - unit.Position.X) < 5)
                        {
                            velocity = (targetPos.X - unit.Position.X);
                            velocity -= Math.Sign(velocity) * 15;
                        }
                        else
                            velocity = targetPos.X - unit.Position.X;

                        break;
                    }

                    case WeaponType.RocketLauncher:

                        if (Math.Abs(nearestEnemy.Value.Position.X - unit.Position.X) < 7 ||
                            DistanceSqr(nearestEnemy.Value.Position, unit.Position) < 10)
                        {
                            velocity = (targetPos.X - unit.Position.X);
                            velocity -= Math.Sign(velocity) * 15;

                            for (int i = 0; i < 5; i++)
                            {
                                if (game.Level.Tiles[(int)unit.Position.X - i * Math.Sign(velocity)][(int)unit.Position.Y + 1] == Tile.Wall)
                                    jump = true;
                            }

                        }
                        else
                            velocity = targetPos.X - unit.Position.X;

                        break;
                }

            }
            else
            {
                velocity = nearestWeapon.Value.Position.X - unit.Position.X;
                velocity += Math.Sign(velocity) * 10;

                if (nearestWeapon.Value.Position.X == unit.Position.X && nearestWeapon.Value.Position.Y != unit.Position.Y)
                    velocity = unit.Position.X + 10;
            }

            if (unit.Health != 100 && nearestWeapon != null)
            {
                velocity = nearestWeapon.Value.Position.X - unit.Position.X;
                velocity += Math.Sign(velocity) * 15;

                if (nearestEnemy.Value.Weapon != null &&
                    (unit.Health > nearestEnemy.Value.Health &&
                     unit.Weapon.Value.Magazine > nearestEnemy.Value.Weapon.Value.Magazine))
                    shoot = true;
            }

            UnitAction action = new UnitAction
            {
                Velocity = velocity,
                Jump = jump,
                JumpDown = !jump,
                Aim = aim,
                Shoot = shoot,
                Reload = reload,
                SwapWeapon = swapWeapon,
                PlantMine = false
            };

            return action;
        }
    }
}