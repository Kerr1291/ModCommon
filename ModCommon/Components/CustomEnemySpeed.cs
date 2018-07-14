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
        public const int customEnemyAbiVersion = 2;
        
        #region Structures
        public struct AnimationData
        {
            public AnimationData(float animationSpeedFactor, string animationName, GameObject customGameObject = null)
            {
                this.CustomGameObject = customGameObject;
                this.AnimationName = animationName;
                this.AnimationSpeedFactor = animationSpeedFactor;
                DefaultAnimationSpeed = 0f;
                cachedClip = null;
            }


            public AnimationData(float animationSpeedFactor, tk2dSpriteAnimationClip animationClip)
            {
                cachedClip = animationClip;
                AnimationSpeedFactor = animationSpeedFactor;
                this.CustomGameObject = null;
                this.AnimationName = animationClip.name;
                this.DefaultAnimationSpeed = 0f;
            }
            
            
            public GameObject CustomGameObject;
            public string AnimationName;
            public float AnimationSpeedFactor;
            public float DefaultAnimationSpeed { get; private set; }
            public tk2dSpriteAnimationClip cachedClip;
            

            public void SetDefaultAnimationSpeed(float defaultAnimSpeed)
            {
                DefaultAnimationSpeed = defaultAnimSpeed;
            }
        }

        public struct WaitData
        {
            public GameObject CustomGameObject;
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
            
            public int ElementIndex;

            public Wait CachedWait;

            public WaitData(float waitTimeInverseFactor, string fsmName, string fsmStateName, int elementIndex = -1,
                GameObject customGameObject = null)
            {
                CustomGameObject = customGameObject;
                this.FSMName = fsmName;
                this.FSMStateName = fsmStateName;
                this.WaitTimeInverseFactor = waitTimeInverseFactor;
                DefaultWaitTime = 0f;
                ElementIndex = elementIndex;

                CachedWait = null;
            }

            public WaitData(float waitTimeInverseFactor, Wait cachedWait)
            {
                this.CachedWait = cachedWait;
                WaitTimeInverseFactor = waitTimeInverseFactor;
                CustomGameObject = CachedWait.Owner;
                FSMName = CachedWait.Fsm.Name;
                FSMStateName = CachedWait.State.Name;
                ElementIndex = -1;
                DefaultWaitTime = 0f;

            }

            public float DefaultWaitTime { get; private set; }

            public void SetDefaultWaitTime(float defaultWaitTime)
            {
                DefaultWaitTime = defaultWaitTime;
            }
        }

        public struct SetVelocity2DData
        {
            public GameObject CustomGameObject;
            public string FSMName;
            public string FSMStateName;
            public float MagnitudeFactor;
            public int ElementIndex;
            public SetVelocity2d cachedVelo2D;

            public SetVelocity2DData(float magnitudeFactor, string fsmName, string fsmStateName, int elementIndex = -1,
                GameObject customGameObject = null)
            {
                CustomGameObject = customGameObject;
                FSMName = fsmName;
                FSMStateName = fsmStateName;
                MagnitudeFactor = magnitudeFactor;
                DefaultMagnitude = Vector2.zero;
                this.cachedVelo2D = null;

                this.ElementIndex = elementIndex;
            }

            public SetVelocity2DData(float magnitudeFactor, SetVelocity2d cachedVelo2D)
            {
                MagnitudeFactor = magnitudeFactor;
                this.cachedVelo2D = cachedVelo2D;
                CustomGameObject = cachedVelo2D.Owner;
                FSMName = cachedVelo2D.Fsm.Name;
                FSMStateName = cachedVelo2D.State.Name;
                ElementIndex = -1;
                DefaultMagnitude = Vector2.zero;
                
            }
            
            public Vector2 DefaultMagnitude { get; private set; }

            public void SetDefaultMagnitude(Vector2 magnitude)
            {
                DefaultMagnitude = magnitude;
            }

        }
        #endregion
        
        #region Health And Damage Done
        
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

        #endregion
        
        #region Speed

        // Sets the new dance speed. This affects how fast the animations are played. A value of 1.0 plays animations
        // at normal speed. A value of 2.0 uses your modded speeds. A value of 3.0 uses twice the difference between
        // your modded speeds and the normal speeds, plus the normal speeds. A graph and detailed explanation of this
        // behavior is available in Hollow Knight modding documentation.
        
        // The default dance speed is 2.0 which means that it will use all your animation and wait time factors
        // as they are written without any changes.        
        public void UpdateDanceSpeed(double newDanceSpeed)
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
            _UpdateSetVelocities();
        }

        #endregion
        
        #region Add And Remove
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
            if (inputData.cachedClip == null)
            {
                if (inputData.CustomGameObject == null)
                {
                    inputData.CustomGameObject = gameObject;
                }
                
                inputData.cachedClip = _getOrCacheAnimClip(inputData.AnimationName, inputData.CustomGameObject);
            }

            if (inputData.cachedClip == null)
            {
                throw new System.NullReferenceException("No Animation found on the cachedAnimator on gameobject " +
                                                        inputData.CustomGameObject.name + " of name " +
                                                        inputData.AnimationName);
            }

            inputData.SetDefaultAnimationSpeed(inputData.cachedClip.fps);
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
            if (inputData.CustomGameObject == null)
            {
                inputData.CustomGameObject = gameObject;
            }
            
            if (inputData.CachedWait == null)
            {
                inputData.CachedWait = 
                    _getOrCacheFSMWait(inputData.FSMStateName, inputData.FSMName,
                        inputData.CustomGameObject, inputData.ElementIndex);
            }
            
            if (inputData.CachedWait == null)
            {
                throw new System.NullReferenceException("No Wait Action found on the FSM "
                                                        + inputData.FSMName + " in state " + inputData.FSMStateName);
            }

            float tVal = inputData.CachedWait.time.Value;

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
        
        // Removes the SetVelocity2D from the list, if it exists. Returns true if it found and removed it.
        public bool RemoveSetVelocity2DData(SetVelocity2DData inputData)
        {
            if (!speedModifyVelocity2D.Contains(inputData)) return false;
            
            _UpdateSingleSetVelocity2D(inputData, true);
            speedModifyVelocity2D.Remove(inputData);
            return true;
        }

        // Adds a wait to the list, stored in the struct format AnimationData. You need to assign all variables
        // in this struct except DefaultWaitTime. If you assign DefaultWaitTime it will be ignored.
        // To make this clear, DefaultWaitTime can only be directly set through a function or reflection.
        public void AddSetVelocity2DData(SetVelocity2DData inputData)
        {
            if (inputData.CustomGameObject == null)
            {
                inputData.CustomGameObject = gameObject;
            }
            
            if (inputData.cachedVelo2D == null)
            {
                inputData.cachedVelo2D = 
                    _getOrCacheFSMSetVelocity2D(inputData.FSMStateName, inputData.FSMName,
                        inputData.CustomGameObject, inputData.ElementIndex);
            }
            
            if (inputData.cachedVelo2D == null)
            {
                throw new System.NullReferenceException("No Wait Action found on the FSM "
                                                        + inputData.FSMName + " in state " + inputData.FSMStateName);
            }
            
            Vector2 origVec = new Vector2(inputData.cachedVelo2D.x.Value, inputData.cachedVelo2D.y.Value);
            float mag = origVec.magnitude;
            if (mag <= epsilon)
            {
                StartCoroutine(_WaitForSetVelo2DToBeLoaded(inputData));
            }
            else
            {
                
                inputData.SetDefaultMagnitude(origVec);
                speedModifyVelocity2D.Add(inputData);
                
                if (active && speedModActive)
                {
                    _UpdateSingleSetVelocity2D(inputData);
                }
            }
        }
        #endregion

        #region Private Animation Methods

        private void _UpdateSingleAnimation(AnimationData inputData, bool restoreOriginal = false)
        {
            if (inputData.cachedClip == null && inputData.CustomGameObject != null)
            {
                throw new NullReferenceException("Unable to load a clip named " + inputData.AnimationName + " on "
                                                 + "the game object " + inputData.CustomGameObject.name + ". " +
                                                 "This clip probably does not exist.");
            }

            if (inputData.cachedClip == null)
            {
                throw new NullReferenceException("Unable to load a clip named " + inputData.AnimationName + " on an" +
                                                 " unknown gameobject. " +
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
            inputData.cachedClip.fps = (inputData.DefaultAnimationSpeed * realFactor);
        }
        
        private void _UpdateAnimations()
        {
            foreach (AnimationData anim in speedModifyAnimations)
            {
                _UpdateSingleAnimation(anim);
            }
        }

        #endregion
        
        #region Private Wait Methods


        // The game doesn't load FsmFloats at the same time as gameobjects so sometimes you have to wait a little bit
        // to get the float values.
        private IEnumerator _WaitForWaitTimeToBeLoaded(WaitData inputData)
        {
            while (inputData.CachedWait.time.Value <= epsilon)
            {
                yield return null;
            }
            inputData.SetDefaultWaitTime(inputData.CachedWait.time.Value);
            speedModifyWaits.Add(inputData);
            if (active && speedModActive)
            {
                _UpdateSingleWait(inputData);
            }

        }

        private void _UpdateSingleWait(WaitData inputData, bool restoreOriginal = false)
        {
            if (inputData.CachedWait == null)
            {
                throw new NullReferenceException("No Wait Action found on the FSM "
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

            inputData.CachedWait.time = (inputData.DefaultWaitTime / realFactor);
        }

        private void _UpdateWaits()
        {
            foreach (WaitData w in speedModifyWaits)
            {
                _UpdateSingleWait(w);
            }
        }
        #endregion
        
        #region Private SetVelocity2D Methods
        private IEnumerator _WaitForSetVelo2DToBeLoaded(SetVelocity2DData inputData)
        {
            // after this much time it will assume the set velocity 2d actually in fact has a magnitude of 0
            // at which point it WILL NOT add it to the list. because: simply put, you can't speed up or slow down
            // a zero vector by any amount.
            float maxWaitTime = 5f;
            Vector2 velocityVec = new Vector2(inputData.cachedVelo2D.x.Value, inputData.cachedVelo2D.y.Value);
            while (velocityVec.magnitude <= epsilon && maxWaitTime > 0.0f)
            {
                maxWaitTime -= Time.deltaTime;
                yield return null;
                velocityVec = new Vector2(inputData.cachedVelo2D.x.Value, inputData.cachedVelo2D.y.Value);
            }

            if (maxWaitTime <= 0.0f)
            {
                Modding.Logger.LogError("[ModCommon] Unable to add setvelocity2d to CustomEnemySpeed because the velocity is a " +
                                        "zero vector which cannot have its speed modified.");
                yield break;
            }
            
            Vector2 origVec = new Vector2(inputData.cachedVelo2D.x.Value, inputData.cachedVelo2D.y.Value);
            inputData.SetDefaultMagnitude(origVec);
            speedModifyVelocity2D.Add(inputData);
            if (active && speedModActive)
            {
                _UpdateSingleSetVelocity2D(inputData);
            }
        }

        private void _UpdateSingleSetVelocity2D(SetVelocity2DData inputData, bool restoreOriginal = false)
        {
            if (inputData.cachedVelo2D == null)
            {
                throw new NullReferenceException("No SetVelocity2D Action found on the FSM "
                                                 + inputData.FSMName + " in state " + inputData.FSMStateName);
            }
            float realFactor = (float) (((danceSpeed - 1.0) * (inputData.MagnitudeFactor - 1.0)) + 1.0);
            if (!active || !speedModActive || restoreOriginal)
            {
                realFactor = 1.0f;
            }
            // Stop divide by zero.
            else if (realFactor <= epsilon)
            {
                throw new Exception("To prevent stupid looking, and broken behavior," +
                                    " your relative magnitude must be greater than " +
                                    epsilon + ". But a dance speed of " + danceSpeed +
                                    " set it to " + realFactor);
            }
            
            inputData.cachedVelo2D.x.Value = (inputData.DefaultMagnitude.x * realFactor);
            inputData.cachedVelo2D.y.Value = (inputData.DefaultMagnitude.y * realFactor);
        }

        private void _UpdateSetVelocities()
        {
            foreach (SetVelocity2DData s in speedModifyVelocity2D)
            {
                _UpdateSingleSetVelocity2D(s);
            }
        }
        

        #endregion
        
        #region Get Or Cache

        private static tk2dSpriteAnimationClip _getOrCacheAnimClip(string clipName, GameObject go)
        {
            return go == null ? null : go.GetComponent<tk2dSpriteAnimator>().GetClipByName(clipName);
        }

        private static PlayMakerFSM _getOrCacheFSM(string fsmName, GameObject go)
        {
            return go == null ? null : FSMUtility.LocateFSM(go, fsmName);
        }
                
        private static Wait _getOrCacheFSMWait(string stateName, string fsmName, GameObject go, int index)
        {
            Wait outVal = null;
             
            PlayMakerFSM myFsm = _getOrCacheFSM(fsmName, go);

            if (index < 0)
            {
                FsmState myState;
                if (myFsm != null)
                    myState = myFsm.GetState(stateName);
                else
                    return null;
                if (myState != null)
                    outVal = (Wait) myState.Actions.FirstOrDefault(wait => wait is Wait);
            
                return outVal;
            }
            else
            {
                return FsmUtil.GetAction<Wait>(myFsm, stateName, index);
            }
        }
        
        
        private static SetVelocity2d _getOrCacheFSMSetVelocity2D(string stateName, string fsmName, GameObject go, int index)
        {
            SetVelocity2d outVal = null;
             
            PlayMakerFSM myFsm = _getOrCacheFSM(fsmName, go);

            if (index < 0)
            {
                FsmState myState;
                if (myFsm != null)
                    myState = myFsm.GetState(stateName);
                else
                    return null;
                if (myState != null)
                    outVal = (SetVelocity2d) myState.Actions.FirstOrDefault(setVelo => setVelo is SetVelocity2d);
            
                return outVal;
            }
            else
            {
                return FsmUtil.GetAction<SetVelocity2d>(myFsm, stateName, index);
            }
        }

        #endregion
        
        public bool active { get; private set; } = false;
        public bool speedModActive { get; private set; } = false;
        public double danceSpeed { get; private set; } = 2.0;
        public List<AnimationData> speedModifyAnimations { get; private set; } = new List<AnimationData>();
        public List<WaitData> speedModifyWaits { get; private set; } = new List<WaitData>();
        public List<SetVelocity2DData> speedModifyVelocity2D { get; private set; } = new List<SetVelocity2DData>();
        public HealthManager cachedHealthManager { get; protected set; }
        protected const float epsilon = 0.0001f;
        private bool waitingForLoad = false;
        public int damageDone { get; protected set; } = 0;
        protected int maxHP = 0;
    }
}