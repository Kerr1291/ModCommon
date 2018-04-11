﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using nv.Tests;
#endif

namespace ModCommon
{
    public class FlashEffect : MonoBehaviour
    {
        public tk2dSpriteAnimator tk2dAnimator;
        MeshRenderer meshRenderer;

        public GameObject parent;
        IEnumerator currentState = null;

        public bool isAnimating = false;

        public void Play( GameObject parent )
        {
            this.parent = parent;
            tk2dAnimator = GetComponent<tk2dSpriteAnimator>();
            meshRenderer = GetComponent<MeshRenderer>();

            gameObject.SetActive( true );

            isAnimating = true;

            StartCoroutine( MainAILoop() );
        }

        public void Stop()
        {
            isAnimating = false;
            tk2dAnimator.Stop();
        }

        IEnumerator MainAILoop()
        {
            Dev.Where();
            currentState = Init();

            for(; ; )
            {
                if( parent == null )
                    yield break;

                yield return currentState;
            }
        }

        IEnumerator Init()
        {
            Dev.Where();

            meshRenderer.enabled = true;

            transform.localPosition = new Vector3( 0f, 0f, 0f );

            transform.localRotation = Quaternion.identity;

            Vector3 localScale = transform.localScale;
            Vector3 lossyScale = transform.lossyScale;

            transform.SetParent( null );

            transform.localScale = lossyScale;

            yield return PlayFromFrameAndWaitForEndOfAnimation( 0 );

            isAnimating = false;

            transform.SetParent( parent.transform );

            transform.localScale = localScale;

            meshRenderer.enabled = false;

            currentState = null;

            gameObject.SetActive( false );

            yield break;
        }

        IEnumerator PlayFromFrameAndWaitForEndOfAnimation( int frame )
        {
            bool blockingAnimationIsPlaying = true;
            tk2dAnimator.AnimationCompleted = ( tk2dSpriteAnimator sprite, tk2dSpriteAnimationClip clip ) => { blockingAnimationIsPlaying = false; };
            tk2dAnimator.PlayFromFrame( frame );

            while( blockingAnimationIsPlaying )
            {
                yield return new WaitForEndOfFrame();
            }

            yield break;
        }
    }
}
