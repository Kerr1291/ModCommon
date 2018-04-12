using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

namespace ModCommon
{
    public class MapNode
    {
        public WorldInfo.SceneInfo scene;
        public Dictionary<string,WorldInfo.SceneInfo> connections;

        /// <summary>
        /// Get the transitions in this scene
        /// </summary>
        public List<WorldInfo.TransitionInfo> GetTransitions()
        {
            return scene.Transitions.ToList();
        }

        /// <summary>
        /// Get the names of the transition doors
        /// </summary>
        public List<string> GetTransitionNames()
        {
            return scene.Transitions.Select(x => x.DoorName).ToList();
        }

        /// <summary>
        /// Get neighboring scenes in node form
        /// </summary>
        public List<MapNode> GetNeighbors(Dictionary<string, MapNode> map)
        {
            return scene.Transitions.Select(x => map[x.DestinationSceneName]).ToList();
        }

        /// <summary>
        /// Filter the list by .Contsins(filter)
        /// </summary>
        public List<WorldInfo.TransitionInfo> GetTransitionsWithNameContaining(string filter)
        {
            return scene.Transitions.Where(x => x.DoorName.Contains(filter)).ToList();
        }

        /// <summary>
        /// Filter the list by .Contsins(filter)
        /// </summary>
        public List<string> GetTransitionsNamesContaining(string filter)
        {
            return scene.Transitions.Select(x => x.DoorName).Where(x => x.Contains(filter)).ToList();
        }

        /// <summary>
        /// Filter the list by .Contsins(filter)
        /// </summary>
        public List<MapNode> GetNeighborsNamesContaining(Dictionary<string, MapNode> map, string filter)
        {
            return scene.Transitions.Select(x => map[x.DestinationSceneName]).Where(x => x.scene.SceneName.Contains(filter)).ToList();
        }


        /// <summary>
        /// Filter the list by !.Contsins(filter)
        /// </summary>
        public List<WorldInfo.TransitionInfo> GetTransitionsWithNameNotContaining(string filter)
        {
            return scene.Transitions.Where(x => !x.DoorName.Contains(filter)).ToList();
        }

        /// <summary>
        /// Filter the list by !.Contsins(filter)
        /// </summary>
        public List<string> GetTransitionsNamesNotContaining(string filter)
        {
            return scene.Transitions.Select(x => x.DoorName).Where(x => !x.Contains(filter)).ToList();
        }

        /// <summary>
        /// Filter the list by !.Contsins(filter)
        /// </summary>
        public List<MapNode> GetNeighborsNamesNotContaining(Dictionary<string, MapNode> map, string filter)
        {
            return scene.Transitions.Select(x => map[x.DestinationSceneName]).Where(x => !x.scene.SceneName.Contains(filter)).ToList();
        }
    }

    public class World
    {
        /// <summary>
        /// The game map, compiled for easy referencing scenes from other scenes
        /// </summary>
        static Dictionary<string, MapNode> map = new Dictionary<string, MapNode>();
        static public Dictionary<string, MapNode> Map
        {
            get
            {
                if(map.Count <= 0)
                    BuildMap();
                return map;
            }
        }

        //Can just call this if you don't want to iterate over all the scenes
        public static void BuildMap()
        {
            //BuildMap(GameManager.instance.WorldInfo);
        }

        Dictionary<string, MapNode> remaining = new Dictionary<string, MapNode>();
        Dictionary<string, MapNode> visited = new Dictionary<string, MapNode>();
        Queue<MapNode> pending = new Queue<MapNode>();


        /// <summary>
        /// Iterate through all the scenes in the game in a breadth-first search style. For each scene, the provided onVisit method will be called.
        /// ------
        /// onVisit is a callback that takes:
        /// MapNode current (node being visited)
        /// MapNode previous (node last visited)
        /// Dictionary<string, MapNode> visited (set of visited nodes)
        /// Dictionary<string, MapNode> map (the entire map)
        /// </summary>
        /// <returns></returns>
        public void Load(MonoBehaviour owner, string startScene, Func<MapNode, MapNode, Dictionary<string, MapNode>, Dictionary<string, MapNode>, IEnumerator> onVisit)
        {
            owner.StartCoroutine(DoLoad(startScene, onVisit));
        }

        /// <summary>
        /// Iterate through all the scenes in the game in a breadth-first search style. For each scene, the provided onVisit method will be called.
        /// ------
        /// onVisit is a callback that takes:
        /// MapNode current (node being visited)
        /// MapNode previous (node last visited)
        /// Dictionary<string, MapNode> visited (set of visited nodes)
        /// Dictionary<string, MapNode> map (the entire map)
        /// </summary>
        /// <returns></returns>
        public IEnumerator DoLoad(string startScene, Func<MapNode, MapNode, Dictionary<string, MapNode>, Dictionary<string, MapNode>, IEnumerator> onVisit)
        {
            if(map.Count <= 0)
            {
                //build the map
                BuildMap();
            }

            if(!map.ContainsKey(startScene))
            {
                Dev.LogError("Scene " + startScene + " not found in game map!");
                yield break;
            }

            MapNode current = null;
            MapNode prev = null;

            pending.Enqueue(map[startScene]);

            do
            {
                prev = current;
                current = pending.Dequeue();
                yield return onVisit(current, prev, visited, map);
                AddNeighborsToQueue(current);
                visited.Add(current.scene.SceneName, current);
            }
            while(pending.Count > 0);
        }


        void BuildMap(WorldInfo worldInfo)
        {
            Dictionary<string, WorldInfo.SceneInfo> scenes = new Dictionary<string, WorldInfo.SceneInfo>();

            foreach(var s in worldInfo.Scenes)
            {
                MapNode node = new MapNode();
                node.scene = s;
                node.connections = new Dictionary<string,WorldInfo.SceneInfo>();
                foreach(var connection in s.Transitions)
                {
                    var w = scenes[connection.DestinationSceneName];
                    node.connections.Add(connection.DoorName, w);
                }
                map.Add(s.SceneName, node);
                remaining.Add(s.SceneName, node);
            }
        }

        void AddNeighborsToQueue(MapNode node)
        {
            foreach(var connection in node.connections)
            {
                if(remaining.ContainsKey(connection.Value.SceneName))
                {
                    pending.Enqueue(map[connection.Value.SceneName]);
                    remaining.Remove(connection.Value.SceneName);
                }
            }
        }
    }
}