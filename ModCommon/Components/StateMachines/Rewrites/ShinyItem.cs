﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ModCommon
{
    public class ShinyItem : GameStateMachine
    {
        public SpriteRenderer spriteRenderer;
        public Animator unityAnimator;
        
        public override bool Running {
            get {
                return gameObject.activeInHierarchy;
            }

            set {
                gameObject.SetActive( value );
            }
        }

        protected override void SetupRequiredReferences()
        {
            base.SetupRequiredReferences();
            spriteRenderer = GetComponent<SpriteRenderer>();
            unityAnimator = GetComponent<Animator>();
        }

        protected override IEnumerator Init()
        {
            yield return base.Init();


        }

        protected override IEnumerator ExtractReferencesFromExternalSources()
        {
            yield return base.ExtractReferencesFromExternalSources();
        }

        protected override void RemoveDeprecatedComponents()
        {
            //TODO: uncomment to remove a lot of stuff on this object and all its children
            //base.RemoveDeprecatedComponents();
        }
    }
}