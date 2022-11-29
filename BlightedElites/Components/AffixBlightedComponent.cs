﻿using RoR2;
using UnityEngine;
using System;
using System.Collections.Generic;
using R2API;
using UnityEngine.Networking;

namespace BlightedElites.Components
{
    public class AffixBlightedComponent : MonoBehaviour
    {
        public static NetworkSoundEventDef rerollSound;

        public BuffDef buff1;
        public BuffDef buff2;

        public CharacterBody characterBody;

        public bool active = false;

        public void Reroll(Xoroshiro128Plus rng)
        {
            if (!NetworkServer.active) return;
            Deactivate();
            Activate(rng);
        }

        public void Activate(Xoroshiro128Plus rng)
        {
            if (!NetworkServer.active) return;
            if (!characterBody) characterBody = base.GetComponent<CharacterBody>();

            //Reroll affixes whenever Blighted is freshly activated.
            if (!active)
            {
                buff1 = null;
                buff2 = null;

                CombatDirector.EliteTierDef etd = EliteAPI.VanillaFirstTierDef;
                if (etd != null && etd.HasAnyAvailableEliteDefs())
                {
                    EliteDef elite1 = etd.GetRandomAvailableEliteDef(rng);
                    if (elite1 && elite1.eliteEquipmentDef && elite1.eliteEquipmentDef.passiveBuffDef)
                    {
                        buff1 = elite1.eliteEquipmentDef.passiveBuffDef;
                    }

                    //Seems like cumbersome way to do this
                    if (etd.availableDefs.Count > 1)
                    {
                        List<EliteDef> remainingElites = new List<EliteDef>(etd.availableDefs);
                        remainingElites.Remove(elite1);
                        int index = rng.RangeInt(0, remainingElites.Count);
                        EliteDef elite2 = remainingElites[index];
                        if (elite2 && elite2.eliteEquipmentDef && elite2.eliteEquipmentDef.passiveBuffDef)
                        {
                            buff2 = elite2.eliteEquipmentDef.passiveBuffDef;
                        }
                    }
                }

                if (buff1 || buff2)
                {
                    active = true;
                    if (buff1) characterBody.AddBuff(buff1);
                    if (buff2) characterBody.AddBuff(buff2);

                    if (rerollSound)
                    {
                        EffectManager.SimpleSoundEffect(rerollSound.index, base.transform.position, true);
                    }
                }
            }
        }

        //Make sure this doesnt interact weirdly with Wake of Vultures
        public void Deactivate()
        {
            if (!NetworkServer.active) return;
            if (buff1 && characterBody.HasBuff(buff1))
            {
                characterBody.RemoveBuff(buff1);
            }
            if (buff2 && characterBody.HasBuff(buff2))
            {
                characterBody.RemoveBuff(buff2);
            }

            buff1 = null;
            buff2 = null;
            active = false;
        }

        public void FixedUpdate()
        {
            if (characterBody)
            {
                if (active)
                {
                    if (!characterBody.HasBuff(BlightedElitesPlugin.AffixBlightedBuff)) Deactivate();
                }
                else
                {
                    if (characterBody.HasBuff(BlightedElitesPlugin.AffixBlightedBuff) && Run.instance) Activate(Run.instance.spawnRng);
                }
            }
        }
    }
}
