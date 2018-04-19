using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HutongGames.PlayMaker.Actions;

using ModCommon;

#if UNITY_EDITOR
using nv.Tests;
#endif

namespace ModCommon
{
    public class HornetCorpse : Physics2DSM
    {
        public HornetBoss owner;
        
        public MeshRenderer meshRenderer;
        public tk2dSpriteAnimator tk2dAnimator;
        public ParticleSystem grassEscape;
        public tk2dSpriteAnimator leaveAnim;
        public GameObject startPt;
        public ParticleSystem grass;
        public tk2dSpriteAnimator thread;

        public UnityEngine.Audio.AudioMixerSnapshot audioSnapshot;
        public Dictionary<string, AudioClip> audioClips;
        public Dictionary<string, GameObject> gameObjects;
        public Dictionary<string, ParticleSystem> particleSystems;
        public Dictionary<string, SpawnRandomObjectsV2Data> spawnRandomObjectsV2Data;

        //use for some sound effects
        public AudioSource actorAudioSource;

        string objectToDestroyName;

        public struct SpawnRandomObjectsV2Data
        {
            public GameObject gameObject;
            public GameObject spawnPoint;
            public Vector3? position;
            public int? spawnMin;
            public int? spawnMax;
            public float? speedMin;
            public float? speedMax;
            public float? angleMin;
            public float? angleMax;
            public float? originVariationX;
            public float? originVariationY;
        }

        public override bool Running
        {
            get
            {
                return gameObject.activeInHierarchy;
            }
            set
            {
                gameObject.SetActive(value);
            }
        }

        protected override void SetupRequiredReferences()
        {
            base.SetupRequiredReferences();
            meshRenderer = GetComponent<MeshRenderer>();
            tk2dAnimator = GetComponent<tk2dSpriteAnimator>();
            audioClips = new Dictionary<string, AudioClip>();
            gameObjects = new Dictionary<string, GameObject>();
            particleSystems = new Dictionary<string, ParticleSystem>();
            spawnRandomObjectsV2Data = new Dictionary<string, SpawnRandomObjectsV2Data>();
        }

        protected virtual void OnCollisionStay2D(Collision2D collision)
        {
            if(collision.gameObject.layer == collisionLayer)
            {
                CheckTouching(collisionLayer);
            }
        }        

        protected override IEnumerator Init()
        {
            yield return base.Init();

            nextState = LimitPos;

            yield break;
        }

        protected virtual IEnumerator LimitPos()
        {
            //Not sure what this is for, seems to place hornet's corpse in the right spot on startup?
            nextState = SetPD;
            yield break;
        }

        protected virtual IEnumerator SetPD()
        {
            if(owner.checkPlayerData)
            {
                GameManager.instance.playerData.SetBool("hornet1Defeated", true);
                GameManager.instance.AwardAchievement("HORNET_1");
            }
            yield break;
        }

        protected virtual IEnumerator Blow()
        {
            if(audioSnapshot != null)
            {
                audioSnapshot.TransitionTo(1f);
            }

            PlayOneShot(audioClips["Hornet_Fight_Death_01"]);
            PlayOneShot(audioClips["boss_explode_clean"]);

            GameObject objectToDestroy = GameObject.Find(objectToDestroyName);
            if(objectToDestroy != null)
            {
                Destroy(objectToDestroy);
            }
            else
            {
                Dev.LogWarning(objectToDestroy + " is null! Cannot destroy!");
            }

            //ControlBlowSpawnRandomObjectsV2gameObject
            SpawnRandomObjectsV2Data data0 = spawnRandomObjectsV2Data["ControlBlowSpawnRandomObjectsV2gameObject"];

            //TODO: see "SpawnRandomObjectsV2Data" and implement that functionality here

            //ControlBlowSpawnRandomObjectsV2gameObject1
            SpawnRandomObjectsV2Data data1 = spawnRandomObjectsV2Data["ControlBlowSpawnRandomObjectsV2gameObject1"];

            //TODO: see "SpawnRandomObjectsV2Data" and implement that functionality here
            
            GameObject.Instantiate<GameObject>(gameObjects["White Wave"],transform.position,Quaternion.identity);

            DoCameraEffect("AverageShake");

            nextState = Launch;
            yield break;
        }

        protected virtual IEnumerator Launch()
        {
            nextState = InAir;
            yield break;
        }
        protected virtual IEnumerator InAir()
        {
            nextState = Land;
            yield break;
        }
        protected virtual IEnumerator Land()
        {
            nextState = CheckPos;
            yield break;
        }
        protected virtual IEnumerator CheckPos()
        {
            nextState = R;
            nextState = L;
            yield break;
        }
        protected virtual IEnumerator R()
        {
            nextState = Jump;
            yield break;
        }
        protected virtual IEnumerator L()
        {
            nextState = Jump;
            yield break;
        }
        protected virtual IEnumerator Jump()
        {
            nextState = ThrowStart;
            yield break;
        }
        protected virtual IEnumerator ThrowStart()
        {
            nextState = Throw;
            yield break;
        }
        protected virtual IEnumerator Throw()
        {
            nextState = Yank;
            yield break;
        }
        protected virtual IEnumerator Yank()
        {
            nextState = End;
            yield break;
        }
        protected virtual IEnumerator End()
        {
            //destroy this
            yield break;
        }

        protected virtual void SetFightGates(bool closed)
        {
            if(closed)
            {
                SendEventToFSM("Battle Gate A", "BG Control", "BG CLOSE");
                SendEventToFSM("Battle Gate A (1)", "BG Control", "BG CLOSE");
            }
            else
            {
                SendEventToFSM("Battle Gate A", "BG Control", "BG OPEN");
                SendEventToFSM("Battle Gate A (1)", "BG Control", "BG OPEN");
            }
        }

        protected override IEnumerator ExtractReferencesFromExternalSources()
        {
            yield return base.ExtractReferencesFromExternalSources();

            string bossFSMName = "Control";

            objectToDestroyName = GetValueFromAction<string, FindGameObject>(gameObject, bossFSMName, "Blow", "objectName");

            yield return GetGameObjectFromCreateObjectInFSM(gameObject, bossFSMName, "Blow", SetGameObject, false);//White Wave
            
            yield return GetValueFromAction<SpawnRandomObjectsV2Data, SpawnRandomObjectsV2>(gameObject, bossFSMName, "Blow", "gameObject", SetSpawnRandomObjectsV2DataWithName);   //ControlBlowSpawnRandomObjectsV2gameObject
            yield return GetValueFromAction<SpawnRandomObjectsV2Data, SpawnRandomObjectsV2>(gameObject, bossFSMName, "Blow", "gameObject", SetSpawnRandomObjectsV2DataWithName, 1); //ControlBlowSpawnRandomObjectsV2gameObject1
            //yield return GetValueFromAction<GameObject, SpawnRandomObjectsV2>(gameObject, bossFSMName, "Blow", "gameObject", SetGameObjectWithName);   //ControlBlowSpawnRandomObjectsV2gameObject
            //yield return GetValueFromAction<GameObject, SpawnRandomObjectsV2>(gameObject, bossFSMName, "Blow", "gameObject", SetGameObjectWithName,1); //ControlBlowSpawnRandomObjectsV2gameObject1
            //yield return GetGameObjectsFromSpawnRandomObjectsV2InFSM(gameObject, bossFSMName, "Blow", SetGameObject, 1);////ControlBlowgameObject1
            yield return GetAudioSourceFromAudioPlayerOneShotSingleInFSM(gameObject, bossFSMName, "Blow", SetActorAudioSource);
            yield return GetAudioClipFromAudioPlayerOneShotSingleInFSM(gameObject, bossFSMName, "Blow", SetAudioClip);//Hornet_Fight_Death_01
            yield return GetAudioClipFromAudioPlaySimpleInFSM(gameObject, bossFSMName, "Blow", SetAudioClip);//boss_explode_clean
            yield return GetAudioClipFromAudioPlayerOneShotSingleInFSM(gameObject, bossFSMName, "Jump", SetAudioClip);//Hornet_Fight_Stun_02
            yield return GetAudioClipFromAudioPlayerOneShotSingleInFSM(gameObject, bossFSMName, "Throw", SetAudioClip);//hornet_needle_thow_spin

            //TODO: give this a way to get the 2nd action

            yield return GetValueFromAction<AudioClip, AudioPlayerOneShotSingle>(gameObject, bossFSMName, "Yank", "audioClip", SetAudioClip, 0);//Hornet_Fight_Yell_03
            yield return GetValueFromAction<AudioClip, AudioPlayerOneShotSingle>(gameObject, bossFSMName, "Yank", "audioClip", SetAudioClip, 1);//hornet_dash
            audioSnapshot = GetSnapshotFromTransitionToAudioSnapshotInFSM(gameObject, bossFSMName, "Blow");//Silent

            //load child references
            if(gameObject.FindGameObjectInChildren("Thread") != null)
                thread = gameObject.FindGameObjectInChildren("Thread").GetComponent<tk2dSpriteAnimator>();
            if(gameObject.FindGameObjectInChildren("Grass") != null)
                grass = gameObject.FindGameObjectInChildren("Grass").GetComponent<ParticleSystem>();
            if(gameObject.FindGameObjectInChildren("Start Pt") != null)
                startPt = gameObject.FindGameObjectInChildren("Start Pt");
            if(gameObject.FindGameObjectInChildren("Leave Anim") != null)
                leaveAnim = gameObject.FindGameObjectInChildren("Leave Anim").GetComponent<tk2dSpriteAnimator>();
            if(gameObject.FindGameObjectInChildren("Grass Escape") != null)
                grassEscape = gameObject.FindGameObjectInChildren("Grass Escape").GetComponent<ParticleSystem>();
        }

        protected override void RemoveDeprecatedComponents()
        {
            //base.RemoveDeprecatedComponents();
            //TODO: 
            //PlayMakerFSM fsm = FSMUtility.LocateFSM(gameObject, "Superdash");
            //Destroy(fsm);
        }

        protected virtual void PlayOneShotRandom(List<AudioClip> clips)
        {
            PlayOneShotRandom(actorAudioSource, clips);
        }

        protected virtual void PlayOneShot(AudioClip clip)
        {
            PlayOneShot(actorAudioSource, clip);
        }

        void SetGameObject(GameObject go)
        {
            if(go == null)
            {
                Dev.Log("Warning: prefab is null!");
                return;
            }

            Dev.Log("Added: " + go.name + " to gameObjects!");
            gameObjects.Add(go.name, go);
        }

        void SetGameObjectWithName(GameObject go, string uniqueName)
        {
            if(go == null)
            {
                Dev.Log("Warning: "+ uniqueName + " is null!");
                return;
            }

            Dev.Log("Added: " + uniqueName + " to gameObjects!");
            gameObjects.Add(uniqueName, go);
        }

        void SetSpawnRandomObjectsV2DataWithName(SpawnRandomObjectsV2Data data, string uniqueName)
        {
            if(data.gameObject == null)
            {
                Dev.Log("Warning: " + uniqueName + "'s prefab is null!");
                return;
            }

            Dev.Log("Added: " + uniqueName + " data to spawnRandomObjectsV2Data!");
            spawnRandomObjectsV2Data.Add(uniqueName, data);
        }

        void SetAudioClip(AudioClip clip)
        {
            if(clip == null)
            {
                Dev.Log("Warning: audio clip is null!");
                return;
            }

            Dev.Log("Added: " + clip.name + " to audioClips!");
            audioClips.Add(clip.name, clip);
        }

        void SetActorAudioSource(AudioSource source)
        {
            if(source == null)
            {
                Dev.Log("Warning: Actor AudioSource failed to load and is null!");
                return;
            }

            actorAudioSource = source;
            actorAudioSource.transform.SetParent(gameObject.transform);
            actorAudioSource.transform.localPosition = Vector3.zero;
        }

        protected virtual void SetAudioSource(AudioSource value)
        {
            if(value == null)
            {
                Dev.Log("Warning: SetAudioSource is null!");
                return;
            }
            actorAudioSource = value;
        }

        protected virtual void SetStateMachineValue<T>(T value)
        {
            if(value as AudioClip != null)
            {
                var v = value as AudioClip;
                SetStateMachineValue(audioClips, v.name, v);
            }
            else if(value as ParticleSystem != null)
            {
                var v = value as ParticleSystem;
                SetStateMachineValue(particleSystems, v.name, v);
            }
            else if(value as GameObject != null)
            {
                var v = value as GameObject;
                SetStateMachineValue(gameObjects, v.name, v);
            }
            else
            {
                if(value != null)
                {
                    Dev.Log("Warning: No handler defined for SetStateMachineValue for type " + value.GetType().Name);
                }
                else
                {
                    Dev.Log("Warning: value is null!");
                }
            }
        }

        void SetStateMachineValue<D, T>(D dictionary, string name, T value)
            where D : Dictionary<string, T>
            where T : class
        {
            if(value == null)
            {
                Dev.Log("Warning: " + name + " is null!");
                return;
            }

            Dev.Log("Added: " + name + " to dictionary of " + dictionary.GetType().Name + "!");
            dictionary.Add(name, value);
        }
    }
}