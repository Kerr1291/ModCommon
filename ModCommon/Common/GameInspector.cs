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
        static Type GetRootType(Type t)
        {
            while(t != null && t.Name != "Object" && t.Name != "object")
            {
                t = t.BaseType;
            }
            return t;
        }

        static BindingFlags bflags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        static int maxDepth = 7;
        static int depth = -1;
        public static void PrintObject<T>(T thing, string componentHeader = "", StreamWriter file = null)
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
                }
                else
                {
                    name = (thing as MonoBehaviour).gameObject.name;
                }
                
                PrintDebugLine(thing.GetType().Name+" ________________________________ ", name, componentHeader, file);
            }
            else if(thing as GameObject)
            {
                PrintDebugLine("GameObject Name: ", (thing as GameObject).name, componentHeader, file);
            }
            else
            {
                PrintDebugLine("Object TypeName: ", thing.GetType().Name, componentHeader, file);
            }

            if(depth > maxDepth)
            {
                PrintDebugLine("maxDepth REACHED. CANCELING RECURSIVE DUMP: ", depth.ToString(), componentHeader, file);
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
                    
                    if(fi.FieldType.IsArray)
                    {
                        //Array data = fi.ReflectedType.GetProperty("SyncRoot").GetValue(thing, null) as Array;
                        Array data = fi.GetValue(thing) as Array;
                        foreach(var value in data)
                        {
                            PrintObject(value, componentHeader + "----", file);
                        }
                    }
                    else
                    {
                        string output = string.Empty;

                        string rootType = GetRootType(fi.FieldType).Name;
                        string typeName = fi.FieldType.Name;

                        if((typeName == "string")
                         || (typeName == "Transform")
                         || (typeName == "Array")
                         || (typeName == "List")
                         || (typeName == "Int32")
                         || (typeName == "Boolean")
                         || (typeName == "String")
                         || (typeName == "Float")
                         || (typeName == "FloatValue")
                         || (typeName == "Single")
                         || (typeName == "Double")
                         || (typeName == "Decimal")
                         || (typeName == "Vector2")
                         || (typeName == "Vector3")
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
                                    object innerObject = fi.ReflectedType.GetField(fi.Name, bflags).GetValue(thing);
                                    PrintObject(innerObject, componentHeader + "----", file);
                                }
                                catch(Exception) { }
                            }
                            else
                            {
                                output = PrintField(thing, fi);
                                PrintDebugLine("Field: " + info.Name + " ", output, componentHeader, file);
                            }
                        }




                        //string output = string.Empty;
                        //if(fi != null)
                        //{
                        //    output = PrintField(thing, fi);
                        //}
                        //PrintDebugLine("Field: " + info.Name + ": ", output, componentHeader, file);
                    }
                }

                foreach(PropertyInfo info in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                {
                    PropertyInfo pi = info;

                    if(pi == null)
                        continue;

                    string output = string.Empty;
                    if(pi != null)
                    {
                        string rootType = GetRootType(pi.PropertyType).Name;
                        string typeName = pi.PropertyType.Name;

                        if((typeName == "string")
                         || (typeName == "Transform")
                         || (typeName == "Array")
                         || (typeName == "List")
                         || (typeName == "Int32")
                         || (typeName == "Boolean")
                         || (typeName == "String")
                         || (typeName == "Float")
                         || (typeName == "FloatValue")
                         || (typeName == "Single")
                         || (typeName == "Double")
                         || (typeName == "Decimal")
                         || (typeName == "Vector2")
                         || (typeName == "Vector3")
                         || (typeName == "Vector4")
                         || (typeName == "Quaternion")
                         || (typeName == "Rect")
                         || (typeName == "Matrix4x4")
                         || (pi.PropertyType.IsEnum)
                         || (info.Name.Contains("transform"))
                         || (typeName == "GameObject"))
                        {
                            output = PrintProperty(thing, pi);
                            PrintDebugLine("Prope: " + info.Name + " ", output, componentHeader, file);
                        }
                        else
                        {
                            if((rootType == "Object" || rootType == "object")
                                && (typeName != "object" && typeName != "Object"))
                            {
                                try
                                {
                                    object innerObject = pi.ReflectedType.GetProperty(pi.Name, bflags).GetValue(thing, null);
                                    PrintObject(innerObject, componentHeader + "----", file);
                                }
                                catch(Exception) { }
                            }
                            else
                            {
                                output = PrintProperty(thing, pi);
                                PrintDebugLine("Prope: " + info.Name + " ", output, componentHeader, file);
                            }
                        }
                    }

                    if( pi.Name == "SyncRoot" )
                    {
                        Array data = pi.ReflectedType.GetProperty("SyncRoot").GetValue(thing, null) as Array;
                        foreach(var value in data)
                        {
                            PrintObject(value, componentHeader + "----", file);
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
                string text = JsonUtility.ToJson( v );
                return text;
                //string text = Newtonsoft.Json.JsonConvert.SerializeObject(v);
                //if(string.IsNullOrEmpty(text))
                //{
                //    text = JsonUtility.ToJson(v);
                //}
                //return text;
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
        
        public static void PrintDebugLine(string label, string line, string componentHeader = "", StreamWriter file = null)
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
