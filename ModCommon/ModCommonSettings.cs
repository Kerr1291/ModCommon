using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Modding;

namespace ModCommon
{
    public class ModCommonSettingsVars
    {
        // change when ModCommon is updated so mods that rely on it can ensure they have the proper functionality to run.
        // This variable exists to be read and used implicitly by external mods. Either through reflection, or directly.
        public const int ABIVersion = 5;
        
        // change when you update the abi version so ModLog.txt dumps / screenshots are more useful.
        public const string ModCommonVersion = "0.0.5";
        // change when the global settings are updated to force a recreation of the global settings
        public const string GlobalSettingsVersion = "0.0.2";
    }

    //Global (non-player specific) settings
    public class ModCommonSettings : IModSettings
    {
        public void Reset()
        {
            BoolValues.Clear();
            StringValues.Clear();
            IntValues.Clear();
            FloatValues.Clear();
        }

        public string SettingsVersion {
            get => GetString( "0.0.0" );
            set => SetString( value );
        }       
    }

    //Player specific settings
    public class ModCommonSaveSettings : IModSettings
    {
    }
}
