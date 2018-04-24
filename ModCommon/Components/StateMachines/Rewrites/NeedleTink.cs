﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ModCommon
{
    public class NeedleTink : MonoBehaviour
    {
        private void Awake()
        {
            gameObject.GetComponent<Collider2D>().offset = new Vector2( 0f, -.3f );
        }

        public void SetParent( Transform t )
        {
            //if deparenting, hide the parent
            if( t == null )
            {
                gameObject.GetComponent<Collider2D>().enabled = false;
                if(transform.parent != null)
                    transform.parent.gameObject.SetActive( false );
            }
            else
            {
                gameObject.GetComponent<Collider2D>().enabled = true;
            }

            gameObject.transform.SetParent( t );
            gameObject.transform.localPosition = Vector2.zero;
        }
    }
}
