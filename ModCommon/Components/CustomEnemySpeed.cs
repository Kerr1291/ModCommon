using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ModCommon.Util;
using UnityEngine;

namespace ModCommon
{
    public class CustomEnemySpeed : MonoBehaviour
    {
        public struct AnimationData
        {
            public string AnimationName;
            public float AnimationSpeedFactor;

            public float DefaultAnimationSpeed;
        }

        public struct WaitData
        {
            public string FSMName;
            public string FSMStateName;
                        
            // The wait time is DIVIDED by this value. Setting it to 0.5 will make enemies HALF as fast as 2.0
            // Why? For one - This makes it work much better with dance speed. Otherwise you could very easily end up
            // with negative waits if you weren't careful. Also it means that a value of 3 here is the same as a
            // value of 3 on AnimationData.AnimationSpeedFactor.
            // Long story short. The math works better this way, so long as you understand this is division and not
            // multiplication.
            //
            // For faster enemies, set a value less than 1, ofc.
            public float WaitTimeInverseFactor;

            public float DefaultWaitTime;
        }




        public bool active { get; private set; } = false;
        public bool speedModActive { get; private set; } = false;
        public double danceSpeed { get; private set; } = 2.0;

        public List<AnimationData> speedModifyAnimations { get; private set; } = new List<AnimationData>();
        public List<WaitData> speedModifyWaits { get; private set; } = new List<WaitData>();
        
        
        private readonly Dictionary<string, PlayMakerFSM> cachedFSMs = new Dictionary<string, PlayMakerFSM>();
        private readonly Dictionary<string, Wait> cachedWaits = new Dictionary<string, Wait>();
        private readonly Dictionary<string, tk2dSpriteAnimationClip> cachedAnimationClips = new Dictionary<string, tk2dSpriteAnimationClip>();

        public tk2dSpriteAnimator cachedAnimator;
        public HealthManager cachedHealthManager;

        private const float epsilon = 0.0001f;

        private bool waitingForLoad = false;

        public int damageDone { get; private set; } = 0;
        private int maxHP = 0;

        private void Update()
        {
            if (!active)
                return;
            
            if (cachedHealthManager == null)
            {
                cachedHealthManager = gameObject.GetComponent<HealthManager>();
                if (cachedHealthManager == null)
                {
                    active = false;
                    throw new NullReferenceException("Unable to load health manager." +
                                                     " Please set manually, or add a HealthManager to this gameobject." +
                                                     " Setting CustomEnemy to inactive.");
                }

                maxHP = cachedHealthManager.hp;
            }
            damageDone = maxHP - cachedHealthManager.hp;
            
        }

        public void OverrideDamageDone(int damage)
        {
            if (cachedHealthManager != null)
            {
                damageDone = damage;
                if (damageDone < maxHP)
                {
                    cachedHealthManager.hp = (maxHP - damageDone);
                }
                else
                {
                    cachedHealthManager.Die(null, AttackTypes.Generic, true);
                }
            }
            else
            {
                cachedHealthManager = gameObject.GetComponent<HealthManager>();
                if (cachedHealthManager == null)
                {
                    throw new NullReferenceException("Unable to load health manager." +
                                                     " Please set manually, or add a HealthManager to this gameobject.");
                }

                maxHP = cachedHealthManager.hp;
                OverrideDamageDone(damage);
            }            
        }

        public void SetEnemyMaxHealth(int health)
        {
            if (cachedHealthManager != null)
            {
                maxHP = health;
                if (damageDone < health)
                {
                    cachedHealthManager.hp = (health - damageDone);
                }
                else
                {
                    cachedHealthManager.Die(null, AttackTypes.Generic, true);
                }
            }
            else
            {
                cachedHealthManager = gameObject.GetComponent<HealthManager>();
                if (cachedHealthManager == null)
                {
                    throw new NullReferenceException("Unable to load health manager." +
                                                     " Please set manually, or add a HealthManager to this gameobject.");
                }
                SetEnemyMaxHealth(health);
            }
        }

        public void SetHealthManager(HealthManager h)
        {
            cachedHealthManager = h;
            maxHP = h.hp;
        }

        public void updateDanceSpeed(double newDanceSpeed)
        {
            if (newDanceSpeed > 0.0)
            {
                danceSpeed = newDanceSpeed;
            }
            else
            {
                throw new Exception("danceSpeed must be greater than 0. It was set to " + danceSpeed);
            }

            if (!active || !speedModActive) return;
            
            StartCoroutine(_StartUpdateSpeeds());
        }
        
        public void SetActive()
        {
            active = true;
        }

        public void StartSpeedMod()
        {
            SetActive();
            speedModActive = true;
            StartCoroutine(_StartUpdateSpeeds());
        }

        public void RestoreOriginalSpeed()
        {
            speedModActive = false;
            StartCoroutine(_StartUpdateSpeeds());
        }
        
        // waits for speeds to not be null before updating.
        private IEnumerator _StartUpdateSpeeds()
        {
            while (waitingForLoad)
            {
                yield return null;
            }
            
            _UpdateAnimations();
            _UpdateWaits();
        }

        public void AddAnimationData(AnimationData inputData)
        {
            if (cachedAnimator == null)
            {
                cachedAnimator = gameObject.GetComponent<tk2dSpriteAnimator>();
            }
            
            tk2dSpriteAnimationClip a = _getOrCacheAnimClip(inputData.AnimationName);
            if (a == null)
            {
                throw new System.NullReferenceException("No Animation found on the cachedAnimator on gameobject " +
                                                        cachedAnimator.gameObject.name + " of name " +
                                                        inputData.AnimationName);
            }

            inputData.DefaultAnimationSpeed = a.fps;
            
            speedModifyAnimations.Add(inputData);

            if (active && speedModActive)
            {
                _UpdateSingleAnimation(inputData);
            }

        }


        public void AddWaitData(WaitData inputData)
        {
            Wait w = _getOrCacheFSMWait(inputData.FSMStateName, inputData.FSMName);
            if (w == null)
            {
                throw new System.NullReferenceException("No Wait Action found on the FSM "
                                                        + inputData.FSMName + " in state " + inputData.FSMStateName);
            }

            float tVal = w.time.Value;

            if (tVal <= epsilon)
            {
                StartCoroutine(_WaitForWaitTimeToBeLoaded(inputData));
            }
            else
            {
                inputData.DefaultWaitTime = tVal;
                speedModifyWaits.Add(inputData);
                
                if (active && speedModActive)
                {
                    _UpdateSingleWait(inputData);
                }
            }
        }

        private IEnumerator _WaitForWaitTimeToBeLoaded(WaitData inputData)
        {
            while (_getOrCacheFSMWait(inputData.FSMStateName, inputData.FSMName).time.Value <= epsilon)
            {
                yield return null;
            }

            inputData.DefaultWaitTime = _getOrCacheFSMWait(inputData.FSMStateName, inputData.FSMName).time.Value;
            
            speedModifyWaits.Add(inputData);
                
            if (active && speedModActive)
            {
                _UpdateSingleWait(inputData);
            }

        }

        private int _UpdateSingleWait(WaitData inputData)
        {
            int errorCode = 0;
            Wait waitState = _getOrCacheFSMWait(inputData.FSMStateName, inputData.FSMName);
            if (waitState == null)
            {
                throw new System.NullReferenceException("No Wait Action found on the FSM "
                                                       + inputData.FSMName + " in state " + inputData.FSMStateName);
            }
            float realFactor = (float) (((danceSpeed - 1.0) * (inputData.WaitTimeInverseFactor - 1.0)) + 1.0);
            if (!active || !speedModActive)
            {
                realFactor = 1.0f;
            }
            // Stop divide by zero.
            else if (realFactor <= epsilon)
            {
                throw new Exception("To prevent Playmaker undefined behavior," +
                                    " your speed factor must be greater than " +
                                    epsilon + ". But a dance speed of " + danceSpeed +
                                    " set it to " + realFactor);
            }

            waitState.time = (inputData.DefaultWaitTime / realFactor);
            return errorCode;
        }

        private void _UpdateWaits()
        {
            foreach (WaitData w in speedModifyWaits)
            {
                _UpdateSingleWait(w);
            }
        }
        
        private void _UpdateSingleAnimation(AnimationData inputData)
        {
            tk2dSpriteAnimationClip clipState = _getOrCacheAnimClip(inputData.AnimationName);
            if (clipState == null)
            {
                throw new NullReferenceException("Unable to load a clip named " + inputData.AnimationName + " on "
                                                 + "the game object " + cachedAnimator.gameObject.name + ". " +
                                                 "This clip probably does not exist.");
            }
            
            float realFactor = (float) (((danceSpeed - 1.0) * (inputData.AnimationSpeedFactor - 1.0)) + 1.0);
            if (!active || !speedModActive)
            {
                realFactor = 1.0f;
            } else if (realFactor <= epsilon)
            {
                throw new Exception("To prevent Playmaker undefined behavior," +
                                               " your speed factor must be greater than " +
                                               epsilon + ". But a dance speed of " + danceSpeed +
                                               " set it to " + realFactor);
            }

            clipState.fps = (inputData.DefaultAnimationSpeed * realFactor);
        }
        
        private void _UpdateAnimations()
        {
            if (cachedAnimator == null)
            {
                throw new NullReferenceException("Unable to load this gameobject's tk2dSpriteAnimator. Please set it " +
                                                 "manually if it exists or avoid using CustomEnemy to manage " +
                                                 "animation speeds.");
            }
            
            foreach (AnimationData anim in speedModifyAnimations)
            {
                _UpdateSingleAnimation(anim);
            }
        }

        private tk2dSpriteAnimationClip _getOrCacheAnimClip(string clipName)
        {
            if (cachedAnimator == null)
            {
                throw new NullReferenceException("Unable to load this gameobject's tk2dSpriteAnimator. Please set it " +
                                                 "manually if it exists or avoid using CustomEnemy to manage " +
                                                 "animation speeds.");
            }

            if (cachedAnimationClips.TryGetValue(clipName, out tk2dSpriteAnimationClip outVal)) return outVal;

            outVal = cachedAnimator.GetClipByName(clipName);

            if (outVal != null)
                cachedAnimationClips[clipName] = outVal;

            return outVal;
        }

        private PlayMakerFSM _getOrCacheFSM(string fsmName)
        {
            if (cachedFSMs.TryGetValue(fsmName, out PlayMakerFSM outVal)) return outVal;
            
            outVal = FSMUtility.LocateFSM(gameObject, fsmName);
            if (outVal != null)
                cachedFSMs[fsmName] = outVal;
            
            return outVal;
        }
                
        private Wait _getOrCacheFSMWait(string stateName, string fsmName)
        {
            if (cachedWaits.TryGetValue(stateName, out Wait outVal)) return outVal;
            PlayMakerFSM myFsm = _getOrCacheFSM(fsmName);
            FsmState myState;
            if (myFsm != null)
                myState = myFsm.GetState(stateName);
            else
                return null;
            if (myState != null)
                outVal = (Wait) myState.Actions.FirstOrDefault(wait => wait is Wait);
            if (outVal != null)
                cachedWaits[stateName] = outVal;
            
            return outVal;
        }
        
        
        
    }
}