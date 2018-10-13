using System;
using System.Collections;
using System.IO;
using HutongGames.PlayMaker;
using JetBrains.Annotations;
using ModCommon.Util;
using Modding;
using UnityEngine;
using UnityEngine.SceneManagement;
using static ModCommon.ModCommon.Spell;
using Logger = Modding.Logger;
using Object = UnityEngine.Object;

namespace ModCommon
{
    /*
     * For a nicer building experience, change 
     * SET MOD_DEST="K:\Games\steamapps\common\Hollow Knight\hollow_knight_Data\Managed\Mods"
     * in install_build.bat to point to your hollow knight mods folder...
     * 
     */
    [UsedImplicitly]
    public class ModCommon : Mod<VoidModSettings, ModCommonSettings>
    {
        public static ModCommon Instance { get; private set; }

        CommunicationNode comms;

        public override void Initialize()
        {
            if(Instance != null)
            {
                Log("Warning: "+ GetType().Name+ " is a singleton. Trying to create more than one may cause issues!");
                return;
            }

            Instance = this;
            comms = new CommunicationNode();
            comms.EnableNode( this );

            Log("Mod Common initializing!");

            SetupDefaulSettings();

            RegisterCallbacks();

            DevLog.Logger.Hide();

            // Setup and prepare the CanvasUtil fonts so that other mods can use them.
            CanvasUtil.CreateFonts();

            Log( "Mod Common is done initializing!" );
        }

        private void SetupDefaulSettings()
        {
            string globalSettingsFilename = Application.persistentDataPath + ModHooks.PathSeperator + GetType().Name + ".GlobalSettings.json";

            bool forceReloadGlobalSettings = false;
            if( GlobalSettings != null && GlobalSettings.SettingsVersion != ModCommonSettingsVars.GlobalSettingsVersion )
            {
                forceReloadGlobalSettings = true;
            }
            else
            {
                Log( "Global settings version match!" );
            }

            if( forceReloadGlobalSettings || !File.Exists( globalSettingsFilename ) )
            {
                if( forceReloadGlobalSettings )
                {
                    Log( "Global settings are outdated! Reloading global settings" );
                }
                else
                {
                    Log( "Global settings file not found, generating new one... File was not found at: " + globalSettingsFilename );
                }

                GlobalSettings.Reset();

                GlobalSettings.SettingsVersion = ModCommonSettingsVars.GlobalSettingsVersion;
            }

            SaveGlobalSettings();
        }

        // TODO: update when version checker is fixed in new modding API version
        public override string GetVersion()
        {
            return ModCommonSettingsVars.ModCommonVersion;
        }

        // TODO: update when version checker is fixed in new modding API version
        public override bool IsCurrent()
        {
            return true;
        }

        // Load ModCommon first!
        public override int LoadPriority()
        {
            return -100;
        }

        private void RegisterCallbacks()
        {
            try
            {
                UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= CheckAndDisableLogicInMenu;
                ModHooks.Instance.AfterSavegameLoadHook -= SpellHook;
                ModHooks.Instance.NewGameHook -= SpellHook;
            }
            catch (Exception e)
            {
                LogWarn("Unable to remove old callbacks because: " + e + " This is probably not a bug.");
            }

            ModHooks.Instance.AfterSavegameLoadHook += SpellHook;
            ModHooks.Instance.NewGameHook += SpellHook;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += CheckAndDisableLogicInMenu;
        }

        private static void SpellHook(SaveGameData data) => SpellHook();

        private static void SpellHook()
        {
            Logger.Log("die1");
            GameManager.instance.StartCoroutine(AddSpellHook());
            Logger.Log("die2");
        }

        public enum Spell
        {
            Fireball,
            Quake,
            Scream
        }
        
        /// <summary>
        /// Called when hero tries to cast a spell
        /// </summary>
        /// <param name="s">Spell type</param>
        public delegate bool OnSpellHandler(Spell s);
        
        public static event OnSpellHandler OnSpellHook
        {
            add => _OnSpell += value;
            remove => _OnSpell -= value;
        }

        private static event OnSpellHandler _OnSpell;

        private static bool OnSpell(Spell s)
        {
            Logger.LogFine( "[ModCommon] - OnSpell Invoked" );

            if( _OnSpell == null ) return true;

            Delegate[] delegates = _OnSpell.GetInvocationList();

            bool cast = true;
            
            foreach( Delegate del in delegates )
            {
                try
                {
                    cast &= (bool) del.DynamicInvoke(s);
                }
                catch( Exception ex )
                {
                    Logger.LogError( "[ModCommon] - " + ex );
                }
            }
            
            return cast;
        }

        private static IEnumerator AddSpellHook()
        {
            while (HeroController.instance == null || GameManager.instance == null)
            {
                yield return null;
            }

            PlayMakerFSM fsm = HeroController.instance.spellControl;
            
            fsm.InsertMethod("Has Quake?", 0, () =>
            {
                if (!OnSpell(Quake))
                {
                    fsm.Fsm.SetState("Inactive");
                }
            });
            
            fsm.InsertMethod("Has Scream?", 0, () =>
            {
                if (!OnSpell(Scream))
                {
                    fsm.Fsm.SetState("Inactive");
                }
            });
            
            fsm.InsertMethod("Has Fireball?", 0, () =>
            {
                if (!OnSpell(Fireball))
                {
                    fsm.Fsm.Event("CANCEL");
                }
            });
        }

        private static void CheckAndDisableLogicInMenu( Scene from, Scene to )
        {
            if( to.name == "Menu_Title" )
            {
                Tools.SetNoclip( false );
            }
        }
    }
}
