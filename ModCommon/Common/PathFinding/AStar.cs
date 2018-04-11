using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

namespace ModCommon
{

    //my implementation of https://en.wikipedia.org/wiki/A*_search_algorithm
    //using a SortedDictionary
    public class AStar
    {
        public List<Vector2> result;
        public List<Vector2> debugPath;

        public int throttle = 100;

        public IEnumerator FindPath<T>( ArrayGrid<T> map, Vector2 start, Vector2 end, bool searchDiagonal = false, bool debug = false )
        {
            result = null;

            //we'll use var here for now so we can change the type easier later
            // The set of nodes already evaluated
            var closed = new Dictionary<int, List<int>>();

            // The set of currently discovered nodes that are not evaluated yet.
            // Initially, only the start node is known.
            //var open = new List<Vector2>() { start };
            var open = new SortedDictionary<HeapKey, Vector2>();
            var openPositions = new Dictionary<int, List<int>>();

            // For each node, which node it can most efficiently be reached from.
            // If a node can be reached from many nodes, cameFrom will eventually contain the
            // most efficient previous step.
            ArrayGrid<Vector2> cameFrom = new ArrayGrid<Vector2>(map.Size);

            // For each node, the cost of getting from the start node to that node.
            ArrayGrid<float> gScore = new ArrayGrid<float>(map.Size, float.MaxValue);

            // The cost of going from start to start is zero.
            gScore[ start ] = 0f;

            // For each node, the total cost of getting from the start node to the goal
            // by passing by that node. That value is partly known, partly heuristic.
            ArrayGrid<float> fScore = new ArrayGrid<float>(map.Size, float.MaxValue);

            // For the first node, that value is completely heuristic.
            fScore[ start ] = GetCostEstimate( map, start, end );

            AddToOpen( open, openPositions, fScore[ start ], start );

            int throttleCounter = 0;
            while( !IsEmpty( open ) )
            {
                throttleCounter++;
                if( throttleCounter % throttle == 0 )
                    yield return new WaitForEndOfFrame();

                KeyValuePair<HeapKey,Vector2> current = open.First();

                if( IsGoal( current.Value, end ) )
                {
                    result = ReconstructPath( cameFrom, start, current.Value );
                    yield break;//done
                }

                open.Remove( current.Key );
                RemoveFromSet( openPositions, (int)current.Value.x, (int)current.Value.y );

                if( debug )
                {
                    if( debugPath == null )
                        debugPath = new List<Vector2>();

                    debugPath.Add( current.Value );
                }

                AddToSet( closed, (int)current.Value.x, (int)current.Value.y );

                List<Vector2> neighbors = map.GetAdjacentPositions(current.Value, searchDiagonal);

                for( int i = 0; i < neighbors.Count; ++i )
                {
                    if( IsInSet( closed, (int)neighbors[ i ].x, (int)neighbors[ i ].y ) )
                    {
                        continue;
                    }

                    bool addToOpen = false;

                    if( !IsInSet( openPositions, (int)neighbors[ i ].x, (int)neighbors[ i ].y ) )
                    {
                        addToOpen = true;
                    }

                    // The distance from start to a neighbor
                    float gScoreTemp = gScore[current.Value] + GetCost(map, current.Value, neighbors[i]);
                    if( gScoreTemp >= gScore[ neighbors[ i ] ] )
                    {
                        if( addToOpen )
                            AddToOpen( open, openPositions, float.MaxValue, neighbors[ i ] );
                        continue;
                    }

                    // This path is the best until now. Record it!
                    cameFrom[ neighbors[ i ] ] = current.Value;
                    gScore[ neighbors[ i ] ] = gScoreTemp;
                    fScore[ neighbors[ i ] ] = gScore[ neighbors[ i ] ] + GetCostEstimate( map, neighbors[ i ], end );

                    if( addToOpen )
                        AddToOpen( open, openPositions, fScore[ neighbors[ i ] ], neighbors[ i ] );
                }
            }

            //TODO: notify failure?

            yield break;
        }

        List<Vector2> ReconstructPath( ArrayGrid<Vector2> cameFrom, Vector2 start, Vector2 reconstructionStartPoint )
        {
            List<Vector2> path = new List<Vector2>();
            path.Add( reconstructionStartPoint );
            Vector2 current = reconstructionStartPoint;
            while( !IsGoal( current, start ) )
            {
                current = cameFrom[ current ];
                path.Add( current );
            }
            return path;
        }

        float GetCostEstimate<T>( ArrayGrid<T> map, Vector2 from, Vector2 to )
        {
            return ( to - from ).magnitude;
        }

        float GetCost<T>( ArrayGrid<T> map, Vector2 from, Vector2 to )
        {
            if( !map.IsPositionEmpty( to ) )
            {
                return float.MaxValue;
            }

            return ( to - from ).magnitude;
        }

        void AddToOpen( SortedDictionary<HeapKey, Vector2> openSorted, Dictionary<int, List<int>> openLookup, float fScore, Vector2 position )
        {
            openSorted.Add( new HeapKey( Guid.NewGuid(), fScore ), position );
            AddToSet( openLookup, (int)position.x, (int)position.y );
        }

        void AddToSet( Dictionary<int, List<int>> set, int x, int y )
        {
            if( set.ContainsKey( y ) )
            {
                set[ y ].Add( x );
            }
            else
            {
                set.Add( y, new List<int>() { x } );
            }
        }

        void RemoveFromSet( Dictionary<int, List<int>> set, int x, int y )
        {
            if( set.ContainsKey( y ) )
            {
                set[ y ].Remove( x );
            }

            if( set[ y ].Count <= 0 )
                set.Remove( y );
        }

        bool IsInSet( Dictionary<int, List<int>> set, int x, int y )
        {
            if( set.ContainsKey( y ) )
            {
                return set[ y ].Contains( x );
            }
            return false;
        }

        static bool IsGoal( Vector2 start, Vector2 end )
        {
            return ( ( end - start ).SqrMagnitude() < Mathf.Epsilon );
        }

        //taken from: https://stackoverflow.com/questions/41319/checking-if-a-list-is-empty-with-linq
        static bool IsEmpty<T>( IEnumerable<T> list )
        {
            if( list == null )
            {
                throw new ArgumentNullException( "list" );
            }

            var genericCollection = list as ICollection<T>;
            if( genericCollection != null )
            {
                return genericCollection.Count == 0;
            }

            var nonGenericCollection = list as ICollection;
            if( nonGenericCollection != null )
            {
                return nonGenericCollection.Count == 0;
            }

            return !list.Any();
        }

        class HeapKey : IComparable<HeapKey>
        {
            public HeapKey( Guid id, float value )
            {
                Id = id;
                Value = value;
            }

            public Guid Id { get; private set; }
            public float Value { get; private set; }

            public int CompareTo( HeapKey other )
            {
                if( other == null )
                {
                    throw new ArgumentNullException( "other" );
                }

                var result = Value.CompareTo(other.Value);

                return result == 0 ? Id.CompareTo( other.Id ) : result;
            }
        }
    }

}