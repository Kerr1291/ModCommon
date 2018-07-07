using System;
using System.Collections;
using System.Collections.Generic;
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

            public float DefaultAnimationSpeed { get; private set; }

            public void SetDefaultAnimationSpeed(float defaultAnimSpeed)
            {
                DefaultAnimationSpeed = defaultAnimSpeed;
            }
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

            public float DefaultWaitTime { get; private set; }

            public void SetDefaultWaitTime(float defaultWaitTime)
            {
                DefaultWaitTime = defaultWaitTime;
            }
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
        public HealthManager cachedHealthManager { get; protected set; }

        protected const float epsilon = 0.0001f;

        private bool waitingForLoad = false;

        public int damageDone { get; protected set; } = 0;
        protected int maxHP = 0;
        
        // Update records the damage you have done to the enemy, which may be useful to other classes.
        // For example, consider a custom radiance fight. After 400 damage, you may wish to set the enemy state to
        // progress the fight. This lets you do that.
        private void Update()
        {
            if (!active)
                return;
            
            if (cachedHealthManager == null)
            {
                cachedHealthManager = gameObject.GetComponent<HealthManager>();
                if (cachedHealthManager == null)
                {
                    SetActive(false);
                    throw new NullReferenceException("Unable to load health manager." +
                                                     " Please set manually, or add a HealthManager to this gameobject." +
                                                     " Setting CustomEnemy to inactive.");
                }

                maxHP = cachedHealthManager.hp;
            }
            damageDone = maxHP - cachedHealthManager.hp;
            
        }
        
        // This directly sets the damage done to a new value.
        // It also reduces the enemy health such that this damage done stat is accurate
        // So long as enemies cannot heal this behavior makes sense.
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
        
        // This sets the enemy's maximum health. Their actual health will be based on the damage that you have
        // done to them.
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
        
        // If the health manager is not on the base game object, call this class before doing anything else to
        // set the health manager. This is required for this class to operate normally, so if no health manager exists
        // just add one to the gameobject.
        public void SetHealthManager(HealthManager h)
        {
            cachedHealthManager = h;
            maxHP = h.hp;
        }
        
        // Sets the new dance speed. This affects how fast the animations are played. A value of 1.0 plays animations
        // at normal speed. A value of 2.0 uses your modded speeds. A value of 3.0 uses twice the difference between
        // your modded speeds and the normal speeds, plus the normal speeds. A graph and detailed explanation of this
        // behavior is available in Hollow Knight modding documentation.
        
        // The default dance speed is 2.0 which means that it will use all your animation and wait time factors
        // as they are written without any changes.
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
        
        // Calling this function enables the damage tracking functionality in the form of damageDone.
        // If you are using InfiniteEnemy it also will make the enemy infinite without changing their speeds.
        public void SetActive(bool activeState = true)
        {
            active = activeState;
            if (!activeState)
            {
                RestoreOriginalSpeed();
            }
        }
        
        // Call this function when you wish to apply all of the speeds you've added.
        public void StartSpeedMod()
        {
            SetActive();
            speedModActive = true;
            StartCoroutine(_StartUpdateSpeeds());
        }

        // Call this function when you wish to restore all of the speeds to their default values. Or at least
        // the values that were loaded when this component was added to the game object in question.
        public void RestoreOriginalSpeed()
        {
            speedModActive = false;
            StartCoroutine(_StartUpdateSpeeds());
        }
        
        // Waits for speeds to not be null before updating.
        private IEnumerator _StartUpdateSpeeds()
        {
            while (waitingForLoad)
            {
                yield return null;
            }
            
            _UpdateAnimations();
            _UpdateWaits();
        }

        // Removes the animation from the list, if it exists. Returns true if it found and removed it.
        public bool RemoveAnimationData(AnimationData inputData)
        {
            if (!speedModifyAnimations.Contains(inputData)) return false;
            
            _UpdateSingleAnimation(inputData, true);
            speedModifyAnimations.Remove(inputData);
            return true;
        }
        
        // Adds an animation to the list, stored in the struct format AnimationData. You need to assign all variables
        // in this struct except DefaultAnimationSpeed. If you assign DefaultAnimationSpeed it will be ignored.
        // To make this clear, DefaultAnimationSpeed can only be directly set through a function or reflection.
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

            inputData.SetDefaultAnimationSpeed(a.fps);
            
            speedModifyAnimations.Add(inputData);

            if (active && speedModActive)
            {
                _UpdateSingleAnimation(inputData);
            }

        }
        
        // Removes the Wait from the list, if it exists. Returns true if it found and removed it.
        public bool RemoveWaitData(WaitData inputData)
        {
            if (!speedModifyWaits.Contains(inputData)) return false;
            
            _UpdateSingleWait(inputData, true);
            speedModifyWaits.Remove(inputData);
            return true;
        }

        // Adds a wait to the list, stored in the struct format AnimationData. You need to assign all variables
        // in this struct except DefaultWaitTime. If you assign DefaultWaitTime it will be ignored.
        // To make this clear, DefaultWaitTime can only be directly set through a function or reflection.
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
                inputData.SetDefaultWaitTime(tVal);
                speedModifyWaits.Add(inputData);
                
                if (active && speedModActive)
                {
                    _UpdateSingleWait(inputData);
                }
            }
        }
        
        // The game doesn't load FsmFloats at the same time as gameobjects so sometimes you have to wait a little bit
        // to get the float values.
        private IEnumerator _WaitForWaitTimeToBeLoaded(WaitData inputData)
        {
            while (_getOrCacheFSMWait(inputData.FSMStateName, inputData.FSMName).time.Value <= epsilon)
            {
                yield return null;
            }

            inputData.SetDefaultWaitTime(_getOrCacheFSMWait(inputData.FSMStateName, inputData.FSMName).time.Value);
            
            speedModifyWaits.Add(inputData);
                
            if (active && speedModActive)
            {
                _UpdateSingleWait(inputData);
            }

        }

        private int _UpdateSingleWait(WaitData inputData, bool restoreOriginal = false)
        {
            int errorCode = 0;
            Wait waitState = _getOrCacheFSMWait(inputData.FSMStateName, inputData.FSMName);
            if (waitState == null)
            {
                throw new System.NullReferenceException("No Wait Action found on the FSM "
                                                       + inputData.FSMName + " in state " + inputData.FSMStateName);
            }
            float realFactor = (float) (((danceSpeed - 1.0) * (inputData.WaitTimeInverseFactor - 1.0)) + 1.0);
            if (!active || !speedModActive || restoreOriginal)
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
        
        private void _UpdateSingleAnimation(AnimationData inputData, bool restoreOriginal = false)
        {
            tk2dSpriteAnimationClip clipState = _getOrCacheAnimClip(inputData.AnimationName);
            if (clipState == null)
            {
                throw new NullReferenceException("Unable to load a clip named " + inputData.AnimationName + " on "
                                                 + "the game object " + cachedAnimator.gameObject.name + ". " +
                                                 "This clip probably does not exist.");
            }
            
            float realFactor = (float) (((danceSpeed - 1.0) * (inputData.AnimationSpeedFactor - 1.0)) + 1.0);
            if (!active || !speedModActive || restoreOriginal)
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

        // This is probably faster than accessing the FSMs and animations directly. Since I'm using a dictionary
        // which is O(1), and the game is using a list which is O(n).
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