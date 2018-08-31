using System;
using System.IO;
using Modding;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ModCommon
{
    /*
     * For a nicer building experience, change 
     * SET MOD_DEST="K:\Games\steamapps\common\Hollow Knight\hollow_knight_Data\Managed\Mods"
     * in install_build.bat to point to your hollow knight mods folder...
     * 
     */
    public partial class ModCommon : Mod<VoidModSettings, ModCommonSettings>
    {
        public static ModCommon Instance { get; private set; }

        CommunicationNode comms;

        public override void Initialize()
        {
            if(Instance != null)
            {
                Log("Warning: "+this.GetType().Name+" is a singleton. Trying to create more than one may cause issues!");
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

        void SetupDefaulSettings()
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

        //TODO: update when version checker is fixed in new modding API version
        public override string GetVersion()
        {
            return ModCommonSettingsVars.ModCommonVersion;
        }

        //TODO: update when version checker is fixed in new modding API version
        public override bool IsCurrent()
        {
            return true;
        }

        //Load the mod common first!
        public override int LoadPriority()
        {
            return -100;
        }

        void RegisterCallbacks()
        {
            try
            {
                UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= CheckAndDisableLogicInMenu;
            }
            catch (Exception e)
            {
                LogWarn("Unable to remove old callbacks because: " + e + " This is probably not a bug.");
            }

            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += CheckAndDisableLogicInMenu;
        }

        void CheckAndDisableLogicInMenu( Scene from, Scene to )
        {
            if( to.name == "Menu_Title" )
            {
                Tools.SetNoclip( false );
            }
        }
    }
}
