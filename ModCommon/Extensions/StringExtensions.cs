using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ModCommon
{
    public static class StringExtensions
    {
        public static List<int> AllIndexesOf(this string str, string value)
        {
            if (String.IsNullOrEmpty(value))
                throw new ArgumentException("the string to find may not be empty", "value");
            List<int> indexes = new List<int>();
            for (int index = 0; ; index += value.Length)
            {
                index = str.IndexOf(value, index);
                if (index == -1)
                    return indexes;
                indexes.Add(index);
            }
        }        

        public static string TrimGameObjectName( this string str )
        {
            if( string.IsNullOrEmpty( str ) )
                return string.Empty;

            string trimmedString = str;

            if( trimmedString.Contains( "Spawn Roller v2" ) )
                return "Roller";

            //trim off "(Clone)" from the word, if it's there
            int index = trimmedString.LastIndexOf("(Clone)");
            if( index > 0 )
                trimmedString = trimmedString.Substring( 0, index );

            index = trimmedString.LastIndexOf( " Fixed" );
            if( index > 0 )
                trimmedString = trimmedString.Substring( 0, index );

            int indexOfStartParethesis = trimmedString.IndexOf(" (");
            if( indexOfStartParethesis > 0 )
                trimmedString = trimmedString.Substring( 0, indexOfStartParethesis );

            

            if( trimmedString != "Zombie Spider 1" && trimmedString != "Zombie Spider 2" && trimmedString != "Hornet Boss 1" && trimmedString != "Hornet Boss 2" )
            {
                //trim off " 1" from the word, if it's there
                index = trimmedString.LastIndexOf( " 1" );
                if( index > 0 )
                    trimmedString = trimmedString.Substring( 0, index );

                index = trimmedString.LastIndexOf( " 2" );
                if( index > 0 )
                    trimmedString = trimmedString.Substring( 0, index );

                index = trimmedString.LastIndexOf( " 3" );
                if( index > 0 )
                    trimmedString = trimmedString.Substring( 0, index );

                index = trimmedString.LastIndexOf( " 4" );
                if( index > 0 )
                    trimmedString = trimmedString.Substring( 0, index );

                index = trimmedString.LastIndexOf( " 5" );
                if( index > 0 )
                    trimmedString = trimmedString.Substring( 0, index );

                index = trimmedString.LastIndexOf( " 6" );
                if( index > 0 )
                    trimmedString = trimmedString.Substring( 0, index );

                index = trimmedString.LastIndexOf( " 7" );
                if( index > 0 )
                    trimmedString = trimmedString.Substring( 0, index );

                index = trimmedString.LastIndexOf( " 8" );
                if( index > 0 )
                    trimmedString = trimmedString.Substring( 0, index );

                index = trimmedString.LastIndexOf( " 9" );
                if( index > 0 )
                    trimmedString = trimmedString.Substring( 0, index );
            }

            if( trimmedString.Contains( "Zombie Fungus" ) )
            {
                //trim off " B" from the word, if it's there
                index = trimmedString.LastIndexOf( " B" );
                if( index > 0 )
                    trimmedString = trimmedString.Substring( 0, index );
            }

            //trim off " New" from the word, if it's there
            if( trimmedString.Contains( "Electric Mage" ) )
            {
                index = trimmedString.LastIndexOf( " New" );
                if( index > 0 )
                    trimmedString = trimmedString.Substring( 0, index );
            }

            if( trimmedString.Contains( "Baby Centipede" ) )
            {
                index = trimmedString.LastIndexOf( " Summon" );
                if( index > 0 )
                    trimmedString = trimmedString.Substring( 0, index );
                index = trimmedString.LastIndexOf( " Summoner" );
                if( index > 0 )
                    trimmedString = trimmedString.Substring( 0, index );
                index = trimmedString.LastIndexOf( " Spawner" );
                if( index > 0 )
                    trimmedString = trimmedString.Substring( 0, index );
            }

            if( trimmedString.Contains( "Fluke Fly" ) )
            {
                index = trimmedString.LastIndexOf( " Summon" );
                if( index > 0 )
                    trimmedString = trimmedString.Substring( 0, index );
                index = trimmedString.LastIndexOf( " Summoner" );
                if( index > 0 )
                    trimmedString = trimmedString.Substring( 0, index );
                index = trimmedString.LastIndexOf( " Spawner" );
                if( index > 0 )
                    trimmedString = trimmedString.Substring( 0, index );
            }

            if( trimmedString.Contains( "Balloon" ) )
            {
                index = trimmedString.LastIndexOf( " Summon" );
                if( index > 0 )
                    trimmedString = trimmedString.Substring( 0, index );
                index = trimmedString.LastIndexOf( " Summoner" );
                if( index > 0 )
                    trimmedString = trimmedString.Substring( 0, index );
                index = trimmedString.LastIndexOf( " Spawner" );
                if( index > 0 )
                    trimmedString = trimmedString.Substring( 0, index );
            }

            return trimmedString;
        }
    }
}
