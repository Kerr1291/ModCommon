//To get the full dumped data you'll need to get the Json .dll and place it in the /Mods directory
//The API will think it's a mod and give you an error on startup (which you can ignore)
//TO use; uncomment the below line and get the .dll from NuGet: Install-Package Newtonsoft.Json
//#define USING_NEWTONSOFT
using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using System.IO;

using Component = UnityEngine.Component;

namespace ModCommon
{
    public class GameInspector
    {
        //For printing an FSM component you'll want 5-7, for printing a game object with an FSM you may want higher? But be prepared for huge files....
        //WARNING: Careful setting this high! This can create some huge output files very fast!
        static int maxReferenceRecursion = 7;

        static Type GetRootType(Type t)
        {
            while(t != null && t.Name != "Object" && t.Name != "object")
            {
                t = t.BaseType;
            }
            return t;
        }

        static List<string> parentObject = new List<string>();
        static bool isInFSM = false;
        static BindingFlags bflags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        static int depth = -1;
        public static void PrintObject<T>(T thing, string componentHeader = "", StreamWriter file = null, bool writeProperties = false )
        {
            depth++;
            if(thing == null)
            {
                depth--;
                return;
            }

            //if it's null, just set the value from the reserved word null, and return
            if(thing == null)
            {
                depth--;
                return;
            }

            Type type = thing.GetType();

            if(thing as Component || thing as MonoBehaviour)
            {
                string name = "";
                if(thing as Component)
                {
                    name = (thing as Component).gameObject.name;
                    parentObject.Add( name );
                }
                else
                {
                    name = (thing as MonoBehaviour).gameObject.name;
                    parentObject.Add( name );
                }
                PrintDebugLine(thing.GetType().Name+" ________________________________ ", name, componentHeader, file);
            }
            else if(thing as GameObject)
            {
                parentObject.Add( ( thing as GameObject ).name );
                PrintDebugLine("GameObject Name: ", (thing as GameObject).name, componentHeader, file);
            }
            else
            {
                parentObject.Add( System.Guid.NewGuid().ToString() );
                PrintDebugLine("Object TypeName: ", thing.GetType().Name, componentHeader, file);
                if( thing.GetType().Name.StartsWith( "Fsm" ) )
                    writeProperties = true;
            }

            if(depth > maxReferenceRecursion)
            {
                parentObject.RemoveAt( parentObject.Count - 1 );
                //PrintDebugLine("maxDepth REACHED. CANCELING RECURSIVE DUMP: ", depth.ToString(), componentHeader, file);
                depth--;
                return;
            }

            do
            {
                if(type.IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                {
                    type = Nullable.GetUnderlyingType(type);
                }

                foreach(FieldInfo info in type.GetFields(bflags))
                {
                    FieldInfo fi = info;

                    if(fi == null)
                        continue;

                    //just skip this....
                    if( info.Name == "fsmList"
                         || ( info.Name == "<BecameInvisible>k__BackingField" )
                         || ( info.Name == "<BecameVisible>k__BackingField" )
                         || ( info.Name == "<CollisionEnter>k__BackingField" )
                         || ( info.Name == "<CollisionExit>k__BackingField" )
                         || ( info.Name == "<CollisionStay>k__BackingField" )
                         || ( info.Name == "<CollisionEnter2D>k__BackingField" )
                         || ( info.Name == "<CollisionExit2D>k__BackingField" )
                         || ( info.Name == "<CollisionStay>k__BackingField" )
                         || ( info.Name == "<CollisionStay2D>k__BackingField" )
                         || ( info.Name == "<ControllerColliderHit>k__BackingField" )
                         || ( info.Name == "<Finished>k__BackingField" )
                         || ( info.Name == "<LevelLoaded>k__BackingField" )
                         || ( info.Name == "<MouseDown>k__BackingField" )
                         || ( info.Name == "<MouseDrag>k__BackingField" )
                         || ( info.Name == "<MouseEnter>k__BackingField" )
                         || ( info.Name == "<MouseExit>k__BackingField" )
                         || ( info.Name == "<MouseOver>k__BackingField" )
                         || ( info.Name == "<MouseUp>k__BackingField" )
                         || ( info.Name == "<MouseUpAsButton>k__BackingField" )
                         || ( info.Name == "<TriggerEnter>k__BackingField" )
                         || ( info.Name == "<TriggerExit>k__BackingField" )
                         || ( info.Name == "<TriggerStay>k__BackingField" )
                         || ( info.Name == "<TriggerEnter2D>k__BackingField" )
                         || ( info.Name == "<TriggerExit2D>k__BackingField" )
                         || ( info.Name == "<TriggerStay2D>k__BackingField" )
                         || ( info.Name == "<ApplicationFocus>k__BackingField" )
                         || ( info.Name == "<ApplicationPause>k__BackingField" )
                         || ( info.Name == "<ApplicationQuit>k__BackingField" )
                         || ( info.Name == "<ParticleCollision>k__BackingField" )
                         || ( info.Name == "<JointBreak>k__BackingField" )
                         || ( info.Name == "<JointBreak2D>k__BackingField" )
                         || ( info.Name == "<PlayerConnected>k__BackingField" )
                         || ( info.Name == "<ServerInitialized>k__BackingField" )
                         || ( info.Name == "<ConnectedToServer>k__BackingField" )
                         || ( info.Name == "<PlayerDisconnected>k__BackingField" )
                         || ( info.Name == "<DisconnectedFromServer>k__BackingField" )
                         || ( info.Name == "<FailedToConnect>k__BackingField" )
                         || ( info.Name == "<FailedToConnectToMasterServer>k__BackingField" )
                         || ( info.Name == "<MasterServerEvent>k__BackingField" )
                         || ( info.Name == "<NetworkInstantiate>k__BackingField" ) )
                        continue;

                    bool forcePrintThis = false;

                    if( fi.Name == "byteData" )
                        forcePrintThis = true;
                    if( fi.Name.Contains("fsm") && fi.Name.Contains( "Params" ) )
                        forcePrintThis = true;
                    if( fi.FieldType.Name.Contains( "FsmTransition") )
                        forcePrintThis = true;

                    if(!forcePrintThis && fi.FieldType.IsArray)
                    {
                        PrintDebugLine( "Field", info.Name, componentHeader, file );
                        //Array data = fi.ReflectedType.GetProperty("SyncRoot").GetValue(thing, null) as Array;
                        Array data = fi.GetValue(thing) as Array;
                        foreach(var value in data)
                        {
                            PrintObject(value, componentHeader + "-|--", file);
                        }
                    }
                    else
                    {
                        string output = string.Empty;

                        string rootType = GetRootType(fi.FieldType).Name;
                        string typeName = fi.FieldType.Name;

                        try
                        {
                            object innerObject = fi.ReflectedType.GetField(fi.Name, bflags).GetValue(thing);

                            if( innerObject as MonoBehaviour != null && parentObject.Contains( ( innerObject as MonoBehaviour ).name ) )
                                forcePrintThis = true;
                            else if( innerObject as Component != null && parentObject.Contains( ( innerObject as Component ).name ) )
                                forcePrintThis = true;
                            else if( innerObject as GameObject != null && parentObject.Contains( ( innerObject as GameObject ).name ) )
                                forcePrintThis = true;
                        }
                        catch( Exception ) { }

                        if((typeName == "string" )
                         || ( forcePrintThis )
                         || ( typeName == "HeroController" )
                         || ( typeName == "PlayerData" )
                         || ( typeName == "UIManager" )
                         || ( typeName == "GameManager" )
                         || (typeName == "Transform")
                         || (typeName == "Array" )
                         || (typeName == "List`1")
                         || (typeName == "Int32")
                         || (typeName == "Boolean")
                         || (typeName == "String")
                         || (typeName == "Float")
                         || (typeName == "FloatValue")
                         || (typeName == "Single")
                         || (typeName == "Double")
                         || (typeName == "Decimal")
                         || (typeName == "Vector2")
                         || (typeName == "Vector3" )
                         || ( info.Name == "byteData" )
                         || ( info.Name == "colorIndex" )
                         || ( info.Name == "<BecameInvisible>k__BackingField" )
                         || ( info.Name == "<BecameVisible>k__BackingField" )
                         || ( info.Name == "<CollisionEnter>k__BackingField" )
                         || ( info.Name == "<CollisionExit>k__BackingField" )
                         || ( info.Name == "<CollisionStay>k__BackingField" )
                         || ( info.Name == "<CollisionEnter2D>k__BackingField" )
                         || ( info.Name == "<CollisionExit2D>k__BackingField" )
                         || ( info.Name == "<CollisionStay>k__BackingField" )
                         || ( info.Name == "<CollisionStay2D>k__BackingField" )
                         || ( info.Name == "<ControllerColliderHit>k__BackingField" )
                         || ( info.Name == "<Finished>k__BackingField" )
                         || ( info.Name == "<LevelLoaded>k__BackingField" )
                         || ( info.Name == "<MouseDown>k__BackingField" )
                         || ( info.Name == "<MouseDrag>k__BackingField" )
                         || ( info.Name == "<MouseEnter>k__BackingField" )
                         || ( info.Name == "<MouseExit>k__BackingField" )
                         || ( info.Name == "<MouseOver>k__BackingField" )
                         || ( info.Name == "<MouseUp>k__BackingField" )
                         || ( info.Name == "<MouseUpAsButton>k__BackingField" )
                         || ( info.Name == "<TriggerEnter>k__BackingField" )
                         || ( info.Name == "<TriggerExit>k__BackingField" )
                         || ( info.Name == "<TriggerStay>k__BackingField" )
                         || ( info.Name == "<TriggerEnter2D>k__BackingField" )
                         || ( info.Name == "<TriggerExit2D>k__BackingField" )
                         || ( info.Name == "<TriggerStay2D>k__BackingField" )
                         || ( info.Name == "<ApplicationFocus>k__BackingField" )
                         || ( info.Name == "<ApplicationPause>k__BackingField" )
                         || ( info.Name == "<ApplicationQuit>k__BackingField" )
                         || ( info.Name == "<ParticleCollision>k__BackingField" )
                         || ( info.Name == "<JointBreak>k__BackingField" )
                         || ( info.Name == "<JointBreak2D>k__BackingField" )
                         || ( info.Name == "<PlayerConnected>k__BackingField" )
                         || ( info.Name == "<ServerInitialized>k__BackingField" )
                         || ( info.Name == "<ConnectedToServer>k__BackingField" )
                         || ( info.Name == "<PlayerDisconnected>k__BackingField" )
                         || ( info.Name == "<DisconnectedFromServer>k__BackingField" )
                         || ( info.Name == "<FailedToConnect>k__BackingField" )
                         || ( info.Name == "<FailedToConnectToMasterServer>k__BackingField" )
                         || ( info.Name == "<MasterServerEvent>k__BackingField" )
                         || ( info.Name == "<NetworkInstantiate>k__BackingField" )
                         || ( info.Name == "playerData" )
                         || ( info.Name == "ui" )
                         || ( info.Name == "gm" )
                         || ( info.Name == "mesh" )
                         || ( info.Name == "NetworkMessageInfo" )
                         || ( info.Name == "actionData" )
                         || ( info.Name == "StateColors" )
                         || ( info.Name == "subFsmList" )
                         || ( isInFSM && info.Name == "fsm" )
                         || (typeName == "Vector4")
                         || (typeName == "Quaternion")
                         || (typeName == "Rect")
                         || (typeName == "Matrix4x4")
                         || (fi.FieldType.IsEnum)
                         || (info.Name.Contains("transform"))
                         || (typeName == "GameObject"))
                        {
                            output = PrintField(thing, fi);
                            PrintDebugLine("Field: " + info.Name + " ", output, componentHeader, file);
                        }
                        else
                        {
                            if((rootType == "Object" || rootType == "object")
                                && (typeName != "object" && typeName != "Object"))
                            {
                                try
                                {
                                    if( info.Name == "fsm" )
                                        isInFSM = true;

                                    PrintDebugLine( "Field: " , info.Name, componentHeader, file );
                                    object innerObject = fi.ReflectedType.GetField(fi.Name, bflags).GetValue(thing);
                                    PrintObject(innerObject, componentHeader + "-|--", file);

                                    if( info.Name == "fsm" )
                                        isInFSM = false;
                                }
                                catch(Exception) { }
                            }
                            else
                            {
                                output = PrintField(thing, fi);
                                PrintDebugLine("Field: " + info.Name + " ", output, componentHeader, file);
                            }
                        }
                    }
                }

                if( writeProperties )
                {
                    foreach( PropertyInfo info in type.GetProperties( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance ) )
                    {
                        //don't print vector properties....
                        if( thing.GetType().Name == "Vector2"
                            || thing.GetType().Name == "Vector3"
                            || thing.GetType().Name == "Bounds"
                            || thing.GetType().Name == "Vector4" )
                            continue;

                        PropertyInfo pi = info;

                        if( pi == null )
                            continue;
                        
                        //just skip this....
                        if( info.Name == "fsmList"
                             || ( info.Name == "BecameInvisible" )
                             || ( info.Name == "BecameVisible" )
                             || ( info.Name == "CollisionEnter" )
                             || ( info.Name == "CollisionExit" )
                             || ( info.Name == "CollisionStay" )
                             || ( info.Name == "CollisionEnter2D" )
                             || ( info.Name == "CollisionExit2D" )
                             || ( info.Name == "CollisionStay" )
                             || ( info.Name == "CollisionStay2D" )
                             || ( info.Name == "ControllerColliderHit" )
                             || ( info.Name == "Finished" )
                             || ( info.Name == "LevelLoaded" )
                             || ( info.Name == "MouseDown" )
                             || ( info.Name == "MouseDrag" )
                             || ( info.Name == "MouseEnter" )
                             || ( info.Name == "MouseExit" )
                             || ( info.Name == "MouseOver" )
                             || ( info.Name == "MouseUp" )
                             || ( info.Name == "MouseUpAsButton" )
                             || ( info.Name == "TriggerEnter" )
                             || ( info.Name == "TriggerExit" )
                             || ( info.Name == "TriggerStay" )
                             || ( info.Name == "TriggerEnter2D" )
                             || ( info.Name == "TriggerExit2D" )
                             || ( info.Name == "TriggerStay2D" )
                             || ( info.Name == "ApplicationFocus" )
                             || ( info.Name == "ApplicationPause" )
                             || ( info.Name == "ApplicationQuit" )
                             || ( info.Name == "ParticleCollision" )
                             || ( info.Name == "JointBreak" )
                             || ( info.Name == "JointBreak2D" )
                             || ( info.Name == "PlayerConnected" )
                             || ( info.Name == "ServerInitialized" )
                             || ( info.Name == "ConnectedToServer" )
                             || ( info.Name == "PlayerDisconnected" )
                             || ( info.Name == "DisconnectedFromServer" )
                             || ( info.Name == "FailedToConnect" )
                             || ( info.Name == "FailedToConnectToMasterServer" )
                             || ( info.Name == "MasterServerEvent" )
                             || ( info.Name == "NetworkInstantiate" ) )
                            continue;

                        string output = string.Empty;
                        if( pi != null )
                        {
                            string rootType = GetRootType(pi.PropertyType).Name;
                            string typeName = pi.PropertyType.Name;

                            bool forcePrintThis = false;

                            try
                            {
                                object innerObject = pi.ReflectedType.GetProperty(pi.Name, bflags).GetValue(thing, null);

                                if( innerObject as MonoBehaviour != null && parentObject.Contains( ( innerObject as MonoBehaviour ).name ) )
                                    forcePrintThis = true;
                                else if( innerObject as Component != null && parentObject.Contains( ( innerObject as Component ).name ) )
                                    forcePrintThis = true;
                                else if( innerObject as GameObject != null && parentObject.Contains( ( innerObject as GameObject ).name ) )
                                    forcePrintThis = true;
                            }
                            catch( Exception ) { }

                            if( ( typeName == "string" )
                             || ( forcePrintThis )
                             || ( typeName == "Transform" )
                             || ( typeName == "Array" )
                             || ( typeName == "List`1" )
                             || ( typeName == "Int32" )
                             || ( typeName == "Boolean" )
                             || ( typeName == "LongLength" )
                             || ( info.Name == "LongLength" )
                             || ( info.Name == "ActionData" )
                             || ( typeName == "String" )
                             || ( typeName == "Float" )
                             || ( typeName == "FloatValue" )
                             || ( typeName == "Single" )
                             || ( typeName == "Double" )
                             || ( typeName == "Decimal" )
                             || ( typeName == "Vector2" )
                             || ( typeName == "Vector3" )
                             || ( typeName == "Vector4" )
                             || ( typeName == "Quaternion" )
                             || ( typeName == "Rect" )
                             || ( typeName == "Matrix4x4" )
                             || ( pi.PropertyType.IsEnum )
                             || ( info.Name.Contains( "transform" ) )
                             || ( typeName == "GameObject" ) )
                            {
                                output = PrintProperty( thing, pi );
                                PrintDebugLine( "Prope: " + info.Name + " ", output, componentHeader, file );
                            }
                            else
                            {
                                if( ( rootType == "Object" || rootType == "object" )
                                    && ( typeName != "object" && typeName != "Object" ) )
                                {
                                    try
                                    {
                                        PrintDebugLine( "Prope", info.Name, componentHeader, file );
                                        object innerObject = pi.ReflectedType.GetProperty(pi.Name, bflags).GetValue(thing, null);
                                        PrintObject( innerObject, componentHeader + "-|--", file );
                                    }
                                    catch( Exception ) { }
                                }
                                else
                                {
                                    output = PrintProperty( thing, pi );
                                    PrintDebugLine( "Prope: " + info.Name + " ", output, componentHeader, file );
                                }
                            }
                        }

                        if( pi.Name == "SyncRoot" )
                        {
                            PrintDebugLine( "Prope: ", info.Name, componentHeader, file );
                            Array data = pi.ReflectedType.GetProperty("SyncRoot").GetValue(thing, null) as Array;
                            foreach( var value in data )
                            {
                                PrintObject( value, componentHeader + "-|--", file );
                            }
                        }
                    }
                }

                type = type.BaseType;
            } while(type != null && type.Name != "Object" && type.Name != "object");

            if(thing as GameObject)
            {
                GameObject go = thing as GameObject;
                PrintDebugLine(componentHeader, "-------------------------", componentHeader, file);
                PrintDebugLine(componentHeader, go.name + " Components:", componentHeader, file);
                foreach(Component c in go.GetComponents<Component>())
                {
                    PrintObject(c, componentHeader + "----", file);
                }
                PrintDebugLine(componentHeader, "-------------------------", componentHeader, file);
            }



            parentObject.RemoveAt( parentObject.Count - 1 );
            depth--;
        }

        static string PrintProperty(object source, PropertyInfo p)
        {
            if(p == null)
                return "null";

            object[] pi = new object[p.GetIndexParameters().Length];
            object v = null;

            try
            {
                v = p.GetValue(source, pi);
            }
            catch(Exception e)
            {
                //Debug.Log(e.Message);
                return "null";
            }

            if(v == null)
                return "null"; 

            return JsonPrintObject(v);
        }

        static string JsonPrintObject(object v)
        {
            try
            {
                //TODO: look up a way to check for this assembly/use this call
#if USING_NEWTONSOFT
                string text = Newtonsoft.Json.JsonConvert.SerializeObject(v);
                if( string.IsNullOrEmpty( text ) )
                {
                    text = JsonUtility.ToJson( v );
                }
#else
                string text = JsonUtility.ToJson( v );
#endif
                return text;
            }
            catch(Exception e)
            {
                return v.ToString();
            }
        }

        static string PrintField(object source, FieldInfo fi)
        {
            if(fi == null)
                return "null";

            object v = null;

            try
            {
                v = fi.GetValue(source);
            }
            catch(Exception)
            {
                return "null";
            }

            if(v == null)
                return "null";

            return JsonPrintObject(v);
        }
        
        static void PrintDebugLine(string label, string line, string componentHeader = "", StreamWriter file = null)
        {
            if(file != null)
            {
                file.WriteLine(componentHeader + @" \"+ label + @": " + line);
            }
            else
            {
                Dev.Log(componentHeader + @" \" + label + @": " + line);
            }
        }
    }
}
