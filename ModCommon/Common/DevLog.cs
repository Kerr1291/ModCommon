//TODO: move define into precompiler option
#define ENABLE_COLOR //<-uncomment to enable color hex codes on output in the debug logs
#define DISABLE_EDITOR_DEBUG //<-uncomment to test non-editor debugging in the editor
#define USE_MODLOG
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Diagnostics;
using System;

#if USE_MODLOG
//change to anything with an Instance.Log function
using DevLoggingOutput = ModCommon.ModCommon;
#endif

//disable the unreachable code detected warning for this file
#pragma warning disable 0162

namespace ModCommon
{
#if USE_MODLOG
    public class DevLog
    {
        static public DevLog Logger { get {
                DevLogObject d = DevLogObject.Instance;
                return DevLogObject.Logger;
            } }


        public class DevLogObject : GameSingleton<DevLogObject>
        {
            static public DevLog Logger;
            public static new DevLogObject Instance {
                get {
                    DevLogObject logObject = GameSingleton<DevLogObject>.Instance;
                    if( Logger == null )
                    {
                        Logger = new DevLog();
                        Logger.Setup();
                    }
                    return logObject;
                }
            }
        }
#else
    public class DevLog : GameSingleton<DevLog>
    {
        public static new DevLog Instance {
            get {
                DevLog log = GameSingleton<DevLog>.Instance;
                if( log.logRoot == null)
                    log.Setup();
                return log;
            }
        }
#endif

        struct LogString
        {
            public string text;
            public GameObject obj;
        }

        Queue<LogString> content = new Queue<LogString>();

        [SerializeField]
        GameObject logRoot = null;

        [SerializeField]
        GameObject logWindow = null;

        [SerializeField]
        GameObject logTextPrefab = null;
        
        Vector2 logWindowSize
        {
            get
            {
                CanvasRenderer canvas = logWindow.AddComponent<CanvasRenderer>();
                return canvas.gameObject.GetOrAddComponent<RectTransform>().rect.size;
            }
            set
            {
                CanvasRenderer canvas = logWindow.AddComponent<CanvasRenderer>();
                Rect currentRect = canvas.gameObject.GetOrAddComponent<RectTransform>().rect;
                canvas.gameObject.GetOrAddComponent<RectTransform>().sizeDelta = value;
            }
        }

        public int maxLines = 10;

        public void SetupPrefabs()
        {
            if( logRoot == null )
            {
                logRoot = DevLogObject.Instance.gameObject;
                //logRoot = new GameObject("DebugLogRoot");
                Canvas canvas = logRoot.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.gameObject.GetOrAddComponent<RectTransform>().sizeDelta = new Vector2( 1920f * .5f, 1080f * 20f );
                CanvasScaler canvasScaler = logRoot.AddComponent<CanvasScaler>();
                canvasScaler.referenceResolution = new Vector2( 1920f, 1080f );
            }
            if(logTextPrefab == null)
            {
                logTextPrefab = new GameObject("DebugLogTextElement");
                logTextPrefab.transform.SetParent(logRoot.transform);
                Text text = logTextPrefab.AddComponent<Text>();
                text.color = Color.red;
                text.font = Font.CreateDynamicFontFromOSFont("Arial", 14);
                text.fontSize = 12;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                text.alignment = TextAnchor.MiddleLeft;
                ContentSizeFitter csf = logTextPrefab.AddComponent<ContentSizeFitter>();
                csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                logTextPrefab.gameObject.GetOrAddComponent<RectTransform>().anchorMax = new Vector2(0f, 1f);
                logTextPrefab.gameObject.GetOrAddComponent<RectTransform>().anchorMin = new Vector2(0f, 1f);
                //logTextPrefab.gameObject.GetOrAddComponent<RectTransform>().sizeDelta = Vector2.zero;
                logTextPrefab.gameObject.GetOrAddComponent<RectTransform>().anchoredPosition = new Vector2(0f,0f);
                logTextPrefab.gameObject.GetOrAddComponent<RectTransform>().pivot = new Vector2(0f, 1f);

                logTextPrefab.SetActive(false);
            }
            if( logWindow == null )
            {
                logWindow = new GameObject( "DebugLogWindow" );
                logWindow.transform.SetParent( logRoot.transform );
                CanvasRenderer canvas = logWindow.AddComponent<CanvasRenderer>();

                //create a window that fills its parent
                canvas.gameObject.GetOrAddComponent<RectTransform>().anchorMax = Vector2.one;
                canvas.gameObject.GetOrAddComponent<RectTransform>().anchorMin = Vector2.zero;
                canvas.gameObject.GetOrAddComponent<RectTransform>().sizeDelta = Vector2.zero;
                canvas.gameObject.GetOrAddComponent<RectTransform>().anchoredPosition = Vector2.zero;

                //add background image
                Image bg = logWindow.AddComponent<Image>();

                //mostly black/dark grey transparent background
                bg.color = new Color( .1f, .1f, .1f, .4f );
            }
            GameObject.DontDestroyOnLoad( logTextPrefab );
            GameObject.DontDestroyOnLoad( logRoot );
            GameObject.DontDestroyOnLoad( logWindow );
        }

        public void Hide()
        {
            logRoot.SetActive( false );
        }

        public void Show( bool show = true )
        {
            logRoot.SetActive( show );
        }

        float LineSize()
        {
            return (float)logTextPrefab.GetComponent<Text>().fontSize + logTextPrefab.GetComponent<Text>().lineSpacing;
        }

        void UpdateLog()
        {
            float line_size = LineSize();
            float total_size = content.Count * line_size;
            float max_size = logWindow.GetComponent<RectTransform>().rect.height;
            while( total_size > max_size )
            {
                LogString lString = content.Dequeue();
                GameObject.Destroy( lString.obj.gameObject );
                total_size -= line_size;
            }
            while(content.Count > maxLines)
            {
                LogString lString = content.Dequeue();
                GameObject.Destroy(lString.obj.gameObject);
            }

            UpdatePositions();
        }

        void UpdatePositions()
        {
            float size = LineSize();
            int index = 0;
            foreach(LogString lstring in content)
            {
                lstring.obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -size * index);
                ++index;
            }
        }

        public void Log( string s )
        {
            if( logTextPrefab == null )
                return;
            if( logWindow == null )
                return;
            if( logRoot == null )
                return;

            LogString str = new LogString() { text = s, obj = GameObject.Instantiate( logTextPrefab, logWindow.transform ) as GameObject };
            str.obj.SetActive( true );
            str.obj.transform.localScale = Vector3.one;
            str.obj.GetComponent<Text>().text = s;
            content.Enqueue( str );
            UpdateLog();
        }

        void Setup()
        {
            //logRoot = new GameObject();
            //Dev.GetOrAddComponent<Canvas>( logRoot );
            //Dev.GetOrAddComponent<RectTransform>( logRoot ).sizeDelta = new Vector2( 1024, 680 ); ;

            SetupPrefabs();
        }
    }


    /// <summary>
    /// Collection of tools, debug or otherwise, to improve the quality of life
    /// </summary>
    public partial class Dev
    {
#region Internal

        static string GetFunctionHeader( int frame = 0 )
        {
            //get stacktrace info
            StackTrace stackTrace = new StackTrace();
            string class_name = stackTrace.GetFrame( BaseFunctionHeader + frame ).GetMethod().ReflectedType.Name;

            //build parameters string
            System.Reflection.ParameterInfo[] parameters = stackTrace.GetFrame( 3 + frame ).GetMethod().GetParameters();
            string parameters_name = "";
            bool add_comma = false;
            foreach( System.Reflection.ParameterInfo parameter in parameters )
            {
                if( add_comma )
                {
                    parameters_name += ", ";
                }

                parameters_name += Dev.Colorize( parameter.ParameterType.Name, _param_color );
                parameters_name += " ";
                parameters_name += Dev.Colorize( parameter.Name, _log_color );

                add_comma = true;
            }

            //build function header
            string function_name = stackTrace.GetFrame( BaseFunctionHeader + frame ).GetMethod().Name + "(" + parameters_name + ")";
            return class_name + "." + function_name;
        }

        static string Colorize( string text, string colorhex )
        {
#if ENABLE_COLOR
            string str = "<color=#" + colorhex + ">" + "<b>" + text + "</b>" + "</color>";
#else
            string str = text;
#endif
            return str;
        }

        static string FunctionHeader( int frame = 0 )
        {
            return Dev.Colorize( Dev.GetFunctionHeader( frame ), Dev._method_color ) + " :::: ";
        }

#endregion

#region Settings
        
        public static int BaseFunctionHeader = 3;

        static string _method_color = Dev.ColorToHex( Color.cyan );
        static string _log_color = Dev.ColorToHex( Color.white );
        static string _param_color = Dev.ColorToHex( Color.green );

        public class Settings
        {

            public static void SetMethodColor( int r, int g, int b ) { Dev._method_color = ColorStr( r, g, b ); }
            public static void SetMethodColor( float r, float g, float b ) { Dev._method_color = ColorStr( r, g, b ); }
            public static void SetMethodColor( Color c ) { Dev._method_color = Dev.ColorToHex( c ); }

            public static void SetLogColor( int r, int g, int b ) { Dev._log_color = ColorStr( r, g, b ); }
            public static void SetLogColor( float r, float g, float b ) { Dev._log_color = ColorStr( r, g, b ); }
            public static void SetLogColor( Color c ) { Dev._log_color = Dev.ColorToHex( c ); }

            public static void SetParamColor( int r, int g, int b ) { Dev._param_color = ColorStr( r, g, b ); }
            public static void SetParamColor( float r, float g, float b ) { Dev._param_color = ColorStr( r, g, b ); }
            public static void SetParamColor( Color c ) { Dev._param_color = Dev.ColorToHex( c ); }

        }
#endregion

#region Logging


        public static void Where()
        {
#if UNITY_EDITOR && !DISABLE_EDITOR_DEBUG
            UnityEngine.Debug.Log( " :::: " + Dev.FunctionHeader() );
#else
            DevLoggingOutput.Instance.Log( " :::: " + Dev.FunctionHeader() );
#endif
        }

        public static void LogError( string text )
        {
#if UNITY_EDITOR && !DISABLE_EDITOR_DEBUG
            UnityEngine.Debug.LogError( Dev.FunctionHeader() + Dev.Colorize( text, ColorToHex( Color.red ) ) );
#else
            DevLoggingOutput.Instance.Log( Dev.FunctionHeader() + Dev.Colorize( text, ColorToHex(Color.red) ) );
#endif
        }

        public static void LogWarning( string text )
        {
#if UNITY_EDITOR && !DISABLE_EDITOR_DEBUG
            UnityEngine.Debug.LogWarning( Dev.FunctionHeader() + Dev.Colorize( text, ColorToHex(Color.yellow) ) );
#else
            DevLoggingOutput.Instance.Log( Dev.FunctionHeader() + Dev.Colorize( text, ColorToHex(Color.yellow) ) );
#endif
        }

        public static void Log( string text )
        {
#if UNITY_EDITOR && !DISABLE_EDITOR_DEBUG
            UnityEngine.Debug.Log( Dev.FunctionHeader() + Dev.Colorize( text, _log_color ) );
#else
            DevLoggingOutput.Instance.Log( Dev.FunctionHeader() + Dev.Colorize( text, _log_color ) );
#endif
        }

        public static void Log( string text, int r, int g, int b )
        {
#if UNITY_EDITOR && !DISABLE_EDITOR_DEBUG
            UnityEngine.Debug.Log( Dev.FunctionHeader() + Dev.Colorize( text, Dev.ColorStr( r, g, b ) ) );
#else
            DevLoggingOutput.Instance.Log( ( Dev.FunctionHeader() + Dev.Colorize( text, Dev.ColorStr( r, g, b ) ) ) );
#endif
        }
        public static void Log( string text, float r, float g, float b )
        {
#if UNITY_EDITOR && !DISABLE_EDITOR_DEBUG
            UnityEngine.Debug.Log( Dev.FunctionHeader() + Dev.Colorize( text, Dev.ColorStr( r, g, b ) ) );
#else
            DevLoggingOutput.Instance.Log( Dev.FunctionHeader() + Dev.Colorize( text, Dev.ColorStr( r, g, b ) ) );
#endif
        }

        public static void Log( string text, Color color )
        {
#if UNITY_EDITOR && !DISABLE_EDITOR_DEBUG
            UnityEngine.Debug.Log( Dev.FunctionHeader() + Dev.Colorize( text, Dev.ColorToHex( color ) ) );
#else
            DevLoggingOutput.Instance.Log( Dev.FunctionHeader() + Dev.Colorize( text, Dev.ColorToHex( color ) ) );
#endif
        }

        /// <summary>
        /// Print the value of the variable in a simple and clean way... 
        /// ONLY USE FOR QUICK AND TEMPORARY DEBUGGING (will not work outside editor)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="var"></param>
        public static void LogVar<T>( T var )
        {
#if UNITY_EDITOR && !DISABLE_EDITOR_DEBUG
            string var_name = GetVarName(var);// var.GetType().
            string var_value = Convert.ToString( var );
            UnityEngine.Debug.Log( Dev.FunctionHeader() + Dev.Colorize( var_name, _param_color ) + " = " + Dev.Colorize( var_value, _log_color ) );
#else
            string var_name = var == null ? "Null" : var.GetType().Name;
            string var_value = Convert.ToString( var );
            DevLoggingOutput.Instance.Log( Dev.FunctionHeader() + Dev.Colorize( var_name, _param_color ) + " = " + Dev.Colorize( var_value, _log_color ) );
#endif
        }

        /// <summary>
        /// Print the value of the variable in a simple and clean way
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="var"></param>
        public static void LogVar<T>( string label, T var )
        {
            string var_name = label;
            string var_value = Convert.ToString( var );
#if UNITY_EDITOR && !DISABLE_EDITOR_DEBUG
            UnityEngine.Debug.Log( Dev.FunctionHeader() + Dev.Colorize( var_name, _param_color ) + " = " + Dev.Colorize( var_value, _log_color ) );
#else
            DevLoggingOutput.Instance.Log( Dev.FunctionHeader() + Dev.Colorize( var_name, _param_color ) + " = " + Dev.Colorize( var_value, _log_color ) );
#endif
        }

        /// <summary>
        /// Print the content of the array passed in
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        public static void LogVarArray<T>( string label, IList<T> array )
        {
#if UNITY_EDITOR && !DISABLE_EDITOR_DEBUG
            int size = array.Count;
            for( int i = 0; i < size; ++i )
            {
                string vname = label + "[" + Dev.Colorize( Convert.ToString( i ), _log_color ) +"]";
                UnityEngine.Debug.Log( Dev.FunctionHeader() + Dev.Colorize( vname, _param_color ) + " = " + Dev.Colorize( Convert.ToString( array[ i ] ), _log_color ) );
            }
#else
            int size = array.Count;
            for( int i = 0; i < size; ++i )
            {
                string vname = label + "[" + Dev.Colorize( Convert.ToString( i ), _log_color ) +"]";
                DevLoggingOutput.Instance.Log( Dev.FunctionHeader() + Dev.Colorize( vname, _param_color ) + " = " + Dev.Colorize( Convert.ToString( array[ i ] ), _log_color ) );
            }
#endif
        }

        public static void LogVarOnlyThis<T>( string label, T var, string input_name, string this_name )
        {
#if UNITY_EDITOR && !DISABLE_EDITOR_DEBUG
            if( this_name != input_name )
                return;

            string var_name = label;
            string var_value = Convert.ToString( var );
            UnityEngine.Debug.Log( Dev.FunctionHeader() + Dev.Colorize( var_name, _param_color ) + " = " + Dev.Colorize( var_value, _log_color ) );

#else

            if( this_name != input_name )
                return;

            string var_name = label;
            string var_value = Convert.ToString( var );
            DevLoggingOutput.Instance.Log( Dev.FunctionHeader() + Dev.Colorize( var_name, _param_color ) + " = " + Dev.Colorize( var_value, _log_color ) );            
#endif
        }
#endregion

#region Helpers

        public static string ColorString( string input, Color color )
        {
            return Dev.Colorize( input, Dev.ColorToHex( color ) );
        }

        public static void PrintHideFlagsInChildren( GameObject parent, bool print_nones = false )
        {
#if UNITY_EDITOR && !DISABLE_EDITOR_DEBUG
            bool showed_where = false;

            if( print_nones )
            {
                Dev.Where();
                showed_where = true;
            }

            foreach( Transform child in parent.GetComponentsInChildren<Transform>() )
            {
                if( print_nones && child.gameObject.hideFlags == HideFlags.None )
                    UnityEngine.Debug.Log( Dev.Colorize( child.gameObject.name, Dev.ColorToHex( Color.white ) ) + ".hideflags = " + Dev.Colorize( Convert.ToString( child.gameObject.hideFlags ), _param_color ) );
                else if( child.gameObject.hideFlags != HideFlags.None )
                {
                    if( !showed_where )
                    {
                        Dev.Where();
                        showed_where = true;
                    }
                    UnityEngine.Debug.Log( Dev.Colorize( child.gameObject.name, Dev.ColorToHex( Color.white ) ) + ".hideflags = " + Dev.Colorize( Convert.ToString( child.gameObject.hideFlags ), _param_color ) );
                }
            }
#else
            bool showed_where = false;

            if( print_nones )
            {
                Dev.Where();
                showed_where = true;
            }

            foreach( Transform child in parent.GetComponentsInChildren<Transform>() )
            {
                if( print_nones && child.gameObject.hideFlags == HideFlags.None )
                    DevLoggingOutput.Instance.Log( Dev.Colorize( child.gameObject.name, Dev.ColorToHex( Color.white ) ) + ".hideflags = " + Dev.Colorize( Convert.ToString( child.gameObject.hideFlags ), _param_color ) );
                else if( child.gameObject.hideFlags != HideFlags.None )
                {
                    if( !showed_where )
                    {
                        Dev.Where();
                        showed_where = true;
                    }
                    DevLoggingOutput.Instance.Log( Dev.Colorize( child.gameObject.name, Dev.ColorToHex( Color.white ) ) + ".hideflags = " + Dev.Colorize( Convert.ToString( child.gameObject.hideFlags ), _param_color ) );
                }
            }
#endif
        }

        public static void ClearHideFlagsInChildren( GameObject parent )
        {
            foreach( Transform child in parent.GetComponentsInChildren<Transform>() )
            {
                child.gameObject.hideFlags = HideFlags.None;
            }
        }

#if UNITY_EDITOR && !DISABLE_EDITOR_DEBUG
        class GetVarNameHelper
        {
            public static Dictionary<string, string> _cached_name = new Dictionary<string, string>();
        }

        static string GetVarName( object obj )
        {
            StackFrame stackFrame = new StackTrace(true).GetFrame(2);
            string fileName = stackFrame.GetFileName();
            int lineNumber = stackFrame.GetFileLineNumber();
            string uniqueId = fileName + lineNumber;
            if( GetVarNameHelper._cached_name.ContainsKey( uniqueId ) )
                return GetVarNameHelper._cached_name[ uniqueId ];
            else
            {
                System.IO.StreamReader file = new System.IO.StreamReader(fileName);
                for( int i = 0; i < lineNumber - 1; i++ )
                    file.ReadLine();
                string varName = file.ReadLine().Split(new char[] { '(', ')' })[1];
                GetVarNameHelper._cached_name.Add( uniqueId, varName );
                return varName;
            }
        }
#else
        class GetVarNameHelper
        {
            public static Dictionary<string, string> _cached_name = new Dictionary<string, string>();
        }

        static string GetVarName( object obj )
        {
            return obj == null ? "Null" : obj.GetType().Name;
        }
#endif
#endregion
    }

}

#pragma warning restore 0162
