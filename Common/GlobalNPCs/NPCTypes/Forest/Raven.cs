using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaCells.Common.Utilities;
using TerrariaCells.Content.Projectiles;


namespace TerrariaCells.Common.GlobalNPCs.NPCTypes.Forest
{
    public class Raven : GlobalNPC, OnAnyPlayerHit.IGlobal
    {
        public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => entity.type is NPCID.Raven;

        const int Idle = 0;
		const int Orbit = 1;
		const int Charge = 2;

		public override bool PreAI(NPC npc)
		{
            npc.ai[3] = npc.rotation;
			if (!npc.HasValidTarget)
			{
				IdleAI(npc);
				return false;
			}

            if (npc.direction == 0)
            {
                npc.direction = 1;
            }

            float oldAI = npc.ai[1];
			switch ((int)npc.ai[1])
			{
				case Idle:
					IdleAI(npc);
					break;
				case Orbit:
					OrbitAI(npc);
					break;
				case Charge:
					ChargeAI(npc);
                    break;
			}
            if (npc.ai[1] != oldAI)
                npc.netUpdate = true;

            npc.spriteDirection = npc.direction;

            return false;
		}

        //Used when raven is sitting
		private void IdleAI(NPC npc)
		{
            npc.noGravity = false;
            npc.noTileCollide = false;
            npc.TargetClosest(false);
            CombatNPC.ToggleContactDamage(npc, false);
			if (npc.TargetInAggroRange(420))
			{
				npc.ai[2] = Main.rand.Next(new int[] { -1, 1 });
				npc.ai[1] = Orbit;
                npc.netUpdate = true;
                npc.noTileCollide = true;
				return;
            }
		}

		private void OrbitAI(NPC npc)
		{
			int timer = (int)npc.ai[0];

            CombatNPC.ToggleContactDamage(npc, false);
            npc.noGravity = true;
			Player target = Main.player[npc.target];
			Vector2 orbitPeak = target.Center + new Vector2(0, -(16 * 12));
			orbitPeak.X += 24 * MathF.Sin(npc.whoAmI);
			orbitPeak.Y += 32 * MathF.Cos(npc.whoAmI);

			float xModifier = MathF.Sin(timer * 0.0125f * npc.ai[2]);
			float yModifier = (1 - MathF.Cos(timer * 0.025f)) * 0.5f;
			Vector2 movePos = orbitPeak + new Vector2(xModifier * (4 * 16), yModifier * (5 * 16));

			int moveDirX = MathF.Sign(movePos.X - npc.position.X);
			int moveDirY = MathF.Sign(movePos.Y - npc.position.Y);
			if(npc.velocity.X * moveDirX < 3.6f)
				npc.velocity.X += moveDirX * 0.1f;
			if(npc.velocity.Y * moveDirY < 3.6f)
				npc.velocity.Y += moveDirY * 0.075f;

			if (npc.velocity.Y < 0 && npc.position.Y < movePos.Y - 4 * 16) npc.velocity.Y += 0.125f;
			if (npc.velocity.Y > 0 && npc.position.Y > movePos.Y + 1 * 16) npc.velocity.Y -= 0.175f;

            //Raven only attacks if it is above the player
            if (npc.Center.Y < target.Center.Y - 12)
            {
                //Insane amount of randomness, because this is run every tick, and will be weighted towards lower numbers
                if(timer > 200 + Main.rand.Next(400))
                {
                    npc.ai[1] = Charge;
                    npc.ai[0] = 0;
                    npc.netUpdate = true;
                }
            }
            //Timer only starts counting when raven is above player
            else
            {
                npc.ai[0] = 0;
                npc.netUpdate = true;
			}

			npc.ai[0]++;
		}

		private void ChargeAI(NPC npc)
		{
            npc.noGravity = true;

			CombatNPC.ToggleContactDamage(npc, true);
			Player target = Main.player[npc.target];

			int timer = (int)npc.ai[0];

			if (timer < 15)
			{
				npc.velocity *= 0.95f;
				npc.rotation = npc.DirectionTo(target.Center).ToRotation() - MathHelper.PiOver2;
			}
			else if (timer < 30)
			{
				if(timer == 15)
					npc.DoAttackWarning();
				npc.velocity = Vector2.Lerp(npc.velocity, npc.DirectionFrom(target.Center) * 3, 0.1f);
				npc.rotation = npc.DirectionTo(target.Center).ToRotation() - MathHelper.PiOver2;
			}
			else
			{
				if (npc.ai[2] != 0)
				{
					npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitX.RotatedBy(npc.rotation + MathHelper.PiOver2) * 10f, 0.3f);
				}
			}

            //Raven stops the dive attack once it is a few blocks below the player
            if (npc.Center.Y > target.Center.Y + 8)
            {
				npc.ai[1] = Orbit;
				npc.ai[0] = 0;
                npc.netUpdate = true;
                return;
            }

			npc.ai[0]++;
		}

        public void OnAnyPlayerHit(NPC npc, Player attacker, NPC.HitInfo info, int damage)
        {
            if (info.DamageType.CountsAsClass(DamageClass.Melee))
            {
                if (npc.ai[1] != Charge)
                {
                    npc.ai[0] = Math.Max(npc.ai[0] - 5, 0);
                }
            }
        }
    }
}


/*
namespace TerrariaCells.Common.GlobalNPCs.NPCTypes.Shared
{
    public partial class Fliers
    {
        bool ravenSettled = false;
        public void RavenSpawn(NPC npc, IEntitySource source)
        {
            for (int i = 0; i < 16000; i++)
            {
                if (Collision.IsWorldPointSolid(npc.Center + new Vector2(0, npc.height / 2 + 1)))
                {
                    break;
                }
                npc.position.Y++;
            }
            npc.ai[0] = 0f;
        }

        public void RavenAI(NPC npc)
        {
            const float ravenSpeedFactor = 1.67f;
			const float invRavenSpeedFactor = 1f / ravenSpeedFactor;

            //make sure npc is real
            if (npc == null || !npc.active) return;

            npc.oldVelocity *= invRavenSpeedFactor;
            npc.velocity *= invRavenSpeedFactor;

            VanillaRavenAI(npc);

            npc.oldVelocity *= ravenSpeedFactor;
            npc.velocity *= ravenSpeedFactor;
        }

        public void RavenPostAI(NPC npc)
        {
            if (ravenSettled)
            {
                npc.noTileCollide = npc.ai[0] == 1;
            }
            else
            {
                npc.ai[0] = 0;
                if (npc.collideY)
                {
                    ravenSettled = true;
                }
            }
        }

        //copied and adjusted from tmodloader source code
        void VanillaRavenAI(NPC npc)
        {
            npc.noGravity = true;
            if (npc.ai[0] == 0f)
            {
                npc.noGravity = false;
                npc.TargetClosest();
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (npc.velocity.X != 0f || npc.velocity.Y < 0f || npc.velocity.Y > 0.3) //raven starts flying when moved
                    {
                        npc.ai[0] = 1f;
                        npc.netUpdate = true;
                    }
                    else
                    {
                        Rectangle rectangle = new Rectangle((int)Main.player[npc.target].position.X, (int)Main.player[npc.target].position.Y, Main.player[npc.target].width, Main.player[npc.target].height);
                        if (new Rectangle((int)npc.position.X - 100, (int)npc.position.Y - 100, npc.width + 200, npc.height + 200).Intersects(rectangle) || npc.life < npc.lifeMax) //raven starts flying when damaged or when player is close
                        {
                            npc.ai[0] = 1f;
                            npc.velocity.Y -= 6f;
                            npc.netUpdate = true;
                        }
                    }
                }
            }
            else if (!Main.player[npc.target].dead)
            {
                if (npc.collideX && !npc.noTileCollide)
                {
                    npc.velocity.X = npc.oldVelocity.X * -0.5f;
                    if (npc.direction == -1 && npc.velocity.X > 0f && npc.velocity.X < 2f)
                    {
                        npc.velocity.X = 2f;
                    }
                    if (npc.direction == 1 && npc.velocity.X < 0f && npc.velocity.X > -2f)
                    {
                        npc.velocity.X = -2f;
                    }
                }
                if (npc.collideY && !npc.noTileCollide)
                {
                    npc.velocity.Y = npc.oldVelocity.Y * -0.5f;
                    if (npc.velocity.Y > 0f && npc.velocity.Y < 1f)
                    {
                        npc.velocity.Y = 1f;
                    }
                    if (npc.velocity.Y < 0f && npc.velocity.Y > -1f)
                    {
                        npc.velocity.Y = -1f;
                    }
                }
                npc.TargetClosest();
                if (npc.direction == -1 && npc.velocity.X > -3f)
                {
                    npc.velocity.X -= 0.1f;
                    if (npc.velocity.X > 3f)
                    {
                        npc.velocity.X -= 0.1f;
                    }
                    else if (npc.velocity.X > 0f)
                    {
                        npc.velocity.X -= 0.05f;
                    }
                    if (npc.velocity.X < -3f)
                    {
                        npc.velocity.X = -3f;
                    }
                }
                else if (npc.direction == 1 && npc.velocity.X < 3f)
                {
                    npc.velocity.X += 0.1f;
                    if (npc.velocity.X < -3f)
                    {
                        npc.velocity.X += 0.1f;
                    }
                    else if (npc.velocity.X < 0f)
                    {
                        npc.velocity.X += 0.05f;
                    }
                    if (npc.velocity.X > 3f)
                    {
                        npc.velocity.X = 3f;
                    }
                }
                float num269 = Math.Abs(npc.position.X + (npc.width / 2) - (Main.player[npc.target].position.X + (Main.player[npc.target].width / 2)));
                float targetHeight = Main.player[npc.target].position.Y - (npc.height / 2);
                if (num269 > 50f)
                {
                    targetHeight -= 100f;
                    //Main.NewText(7);
                }
                if (npc.position.Y < targetHeight)
                {
                    npc.velocity.Y += 0.05f;
                    if (npc.velocity.Y < 0f)
                    {
                        npc.velocity.Y += 0.01f;
                    }
                }
                else
                {
                    npc.velocity.Y -= 0.05f;
                    if (npc.velocity.Y > 0f)
                    {
                        npc.velocity.Y -= 0.01f;
                    }
                }
                npc.velocity.Y = Math.Clamp(npc.velocity.Y, -3f, 3f);
            }
            if (npc.wet)
            {
                if (npc.velocity.Y > 0f)
                {
                    npc.velocity.Y *= 0.95f;
                }
                npc.velocity.Y -= 0.5f;
                if (npc.velocity.Y < -4f)
                {
                    npc.velocity.Y = -4f;
                }
                npc.TargetClosest();
            }
        }
    }
}
*/