﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

namespace ModCommon
{
    public static class SceneExtensions
    {
        public static GameObject FindGameObject( this Scene scene, string name )
        {
            if( !scene.IsValid() )
                return null;
            
            GameObject[] rootGameObjects = scene.GetRootGameObjects();

            try
            {
                foreach( GameObject go in rootGameObjects )
                {
                    if( go == null )
                    {
                        break;
                    }

                    GameObject found = go.FindGameObjectInChildren(name);
                    if( found != null )
                        return found;
                }
            }
            catch( Exception e )
            {
                Dev.Log( "Exception: " + e.Message );
            }

            return null;
        }

        public static void PrintHierarchy( this Scene scene, int localIndex = -1, Bounds? sceneBounds = null, List<string> randomizerEnemyTypes = null, string outputFileName = "" )
        {
            if( !scene.IsValid() )
                return;

            System.IO.StreamWriter file = null;
            if( !string.IsNullOrEmpty( outputFileName ) )
            {
                try
                {
                    file = new System.IO.StreamWriter( Application.dataPath + "/Managed/Mods/" + outputFileName );
                }
                catch(Exception e)
                {
                    Dev.Log( "Exception!: " + e.Message );
                    file = null;
                }
            }

            if( file != null )
            {
                file.WriteLine( "START =====================================================" );
                file.WriteLine( "Printing full hierarchy for scene: " + scene.name + " [Build index: " + scene.buildIndex + "]" );

                if( localIndex >= 0 )
                    file.WriteLine( "Local scene index: " + localIndex );
            }
            else
            {
                Dev.Log( "START =====================================================" );
                Dev.Log( "Printing full hierarchy for scene: " + scene.name + " [Build index: " + scene.buildIndex + "]" );

                if( localIndex >= 0 )
                    Dev.Log( "Local scene index: " + localIndex );
            }

            GameObject[] rootGameObjects = scene.GetRootGameObjects();

            try
            {
                foreach( GameObject go in rootGameObjects )
                {
                    if( go == null )
                    {
                        if( file != null )
                        {
                            file.WriteLine( "Scene " + scene.name + " has a null root game object! Skipping debug print scene..." );
                        }
                        else
                        {
                            Dev.Log( "Scene " + scene.name + " has a null root game object! Skipping debug print scene..." );
                        }
                        break;
                    }

                    if( string.IsNullOrEmpty( outputFileName ) )
                    {
                        go.PrintSceneHierarchyTree( true );
                    }
                    else
                    {
                        go.PrintSceneHierarchyTree( true, file );
                    }
                }
            }
            catch(Exception e)
            {
                Dev.Log( "Exception: " + e.Message );
            }

            if( file != null )
            {
                file.WriteLine( "END +++++++++++++++++++++++++++++++++++++++++++++++++++++++" );
                file.Close();
            }
            else
            {
                Dev.Log( "END +++++++++++++++++++++++++++++++++++++++++++++++++++++++" );
            }
        }
    }
}
