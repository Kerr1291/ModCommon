using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;


namespace ModCommon
{
    public class HornetBossHard : HornetBoss
    {
        //needs some testing
        public int hardMaxHP = 800;
        public int hardMaxHPBonus = 400;
        public float evadeSpeedModifier = 2f;
        public float idleWaitModifier = .1f;
        public float evadeRangeOnTimeMod = .1f;
        public float evadeRangeCooldownMod = .1f;
        public float runSpeedMod = 2f;
        public float runWaitMod = .1f;

        public int needleDamage = 2;

        public float throwAnticMod = 2f;
        public float throwWindUpMod = .1f;
        public float throwMaxTravelTimeMod = .5f;

        public override Dictionary<Func<IEnumerator>, float> DmgResponseChoices
        {
            get
            {
                if(dmgResponseChoices == null)
                {
                    dmgResponseChoices = new Dictionary<Func<IEnumerator>, float>();
                    dmgResponseChoices.Add(EvadeAntic, .455f);
                    dmgResponseChoices.Add(SetJumpOnly, .25f);
                    dmgResponseChoices.Add(MaybeGSphere, .25f);
                    dmgResponseChoices.Add(DmgIdle, .05f);
                }
                return dmgResponseChoices;
            }
        }

        public override void ShowTitle(float hideTime)
        {
            ShowBossTitle(this, areaTitleObject, hideTime, "", "", "", "HORNET", "", "THE GUARDIAN");
        }

        protected override IEnumerator Init()
        {
            yield return base.Init();

            maxHP = hardMaxHP + GameRNG.Rand(0, hardMaxHPBonus);

            tk2dAnimator.GetClipByName("Evade Antic").fps *= evadeSpeedModifier;
            evadeJumpAwayTimeLength /= evadeSpeedModifier;

            idleWaitMin *= idleWaitModifier;
            idleWaitMax *= idleWaitModifier;

            evadeRange.onTimeMin *= evadeRangeOnTimeMod;
            evadeRange.onTimeMax *= evadeRangeOnTimeMod;

            evadeCooldownMin *= evadeRangeCooldownMod;
            evadeCooldownMax *= evadeRangeCooldownMod;

            tk2dAnimator.GetClipByName("Run").fps *= runSpeedMod;
            runSpeed *= runSpeedMod;
            runWaitMin *= runWaitMod;
            runWaitMax *= runWaitMod;
            
            tk2dAnimator.GetClipByName("Throw Antic").fps *= throwAnticMod;
            throwWindUpTime *= throwWindUpMod;
            throwMaxTravelTime *= throwMaxTravelTimeMod;

            needle.GetComponent<DamageHero>().damageDealt = needleDamage;

            stunControl.maxStuns = 12;

            yield break;
        }

        protected override void DoThrowNeedle()
        {
            needle.Play(gameObject, throwWindUpTime, throwMaxTravelTime, throwRay, throwDistance);
        }

        protected override void SelectNextStateFromIdle()
        {
            //nothing hit us, choose the next state with 50/50
            List<Func<IEnumerator>> nextStates = new List<Func<IEnumerator>>()
                {
                    MaybeFlip, MaybeGSphere
                };

            if(sphereRange.ObjectIsInRange)
            {
                ctIdle = 0;
                ctRun += 1;
                nextState = nextStates[1];
            }
            else
            {
                ctIdle += 1;
                ctRun = 0;
                nextState = nextStates[0];
            }
        }

        protected override IEnumerator Escalation()
        {
            Dev.Where();

            //see if we're low on hp and should act faster
            float hpRemainingPercent = (float)healthManager.hp / (float)maxHP;
            if(!escalated && hpRemainingPercent < escalationHPPercentage)
            {
                //TODO: not sure yet what I want to escalate....
            }

            nextState = Idle;

            yield break;
        }

        //change the throw to aim at the hero
        protected override IEnumerator CanThrow()
        {
            Dev.Where();

            HeroController hero = HeroController.instance;
            Vector3 currentPosition = gameObject.transform.position;

            Vector2 throwOrigin = currentPosition;

            float lead = GameRNG.Rand(0f, .2f);

            //aim a bit ahead of our hero
            Vector2 target = hero.GetComponent<Rigidbody2D>().velocity * lead + (Vector2)hero.transform.position;

            //clamp the y prediction so we don't throw into the ground
            if(target.y < throwOrigin.y)
            {
                target.y = throwOrigin.y;
            }

            Vector2 throwDirection = (target - throwOrigin).normalized;
            
            throwRay = new Ray(throwOrigin, throwDirection);
            throwRaycast = Physics2D.Raycast(throwOrigin, throwDirection, throwDistance, 1 << 8);
            if(throwRaycast.collider != null)
            {
                //TODO: alter this code so that we can throw, but make it shorter and/or have hornet grapple
                //there's a wall, we cannot throw!
                nextState = MoveChoiceB;
            }
            else
            {
                //we can throw!
                nextState = MoveChoiceA;
            }

            yield break;
        }
    }
}