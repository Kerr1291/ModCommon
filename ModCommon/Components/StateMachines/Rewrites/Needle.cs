using System;
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
    public class Needle : MonoBehaviour
    {
        public tk2dSpriteAnimator tk2dAnimator;
        public PolygonCollider2D bodyCollider;
        public Rigidbody2D body;
        public MeshRenderer meshRenderer;

        public GameObject owner;
        public GameObject thread;
        IEnumerator currentState = null;

        public bool isAnimating = false;
        protected LayerMask collisionLayer = 8;

        public bool canHitWalls = false;
        public bool HitWall { get; private set; }

        float startDelay;
        float throwMaxTravelTime;
        Ray throwRay;
        float throwDistance;
        float needleYOffset = -.35f;
        Vector3 startPos;

        void Awake()
        {
            bodyCollider = gameObject.GetComponent<PolygonCollider2D>();
            bodyCollider.offset = new Vector2( 0f, -.3f );
        }

        public void Play( GameObject owner, float startDelay, float throwMaxTravelTime, Ray throwRay, float throwDistance )
        {
            this.owner = owner;
            tk2dAnimator = gameObject.GetComponent<tk2dSpriteAnimator>();
            bodyCollider = gameObject.GetComponent<PolygonCollider2D>();
            body = gameObject.GetComponent<Rigidbody2D>();
            meshRenderer = gameObject.GetComponent<MeshRenderer>();

            meshRenderer.enabled = false;
            startPos = throwRay.origin + new Vector3( 0f, needleYOffset, 0f );
            transform.position = startPos;
            gameObject.SetActive( true );

            thread = gameObject.FindGameObjectInChildren( "Thread" );

            isAnimating = true;
            HitWall = false;

            this.startDelay = startDelay;
            this.throwMaxTravelTime = throwMaxTravelTime;
            this.throwRay = throwRay;
            this.throwDistance = throwDistance;

            StartCoroutine( MainAILoop() );
        }

        public void Stop()
        {
            if( meshRenderer != null )
                meshRenderer.enabled = false;
            isAnimating = false;
            gameObject.SetActive( false );
        }

        IEnumerator MainAILoop()
        {
            Dev.Where();
            currentState = Out();
            //StartCoroutine( Debug() );

            for(; ; )
            {
                if( owner == null )
                    yield break;

                yield return currentState;
            }
        }

        IEnumerator Out()
        {
            Dev.Where();

            yield return new WaitForSeconds( startDelay );

            meshRenderer.enabled = true;

            transform.localRotation = Quaternion.identity;

            Vector2 throwTarget = throwRay.direction * throwDistance;

            //needle requires a 180 flip to orient properly
            float angleToTarget = GetAngleToTarget(startPos, throwTarget, 0f, -.5f);
            transform.rotation = Quaternion.AngleAxis(angleToTarget + 180f, Vector3.forward);

            //Vector3 throwDirection = ((Vector3)throwTarget + throwRay.origin) - transform.position;
            //if( throwDirection != Vector3.zero )
            //{
            //    float angle = Mathf.Atan2(throwDirection.y, throwDirection.x) * Mathf.Rad2Deg;
            //    transform.rotation = Quaternion.AngleAxis( angle + 180f, Vector3.forward );
            //}

            AnimationCurve throwCurve = new AnimationCurve();
            throwCurve.AddKey( 0f, 0f );
            throwCurve.AddKey( .1f, .2f );
            throwCurve.AddKey( .2f, .4f );
            throwCurve.AddKey( .3f, .6f );
            throwCurve.AddKey( .4f, .75f );
            throwCurve.AddKey( .5f, .85f );
            throwCurve.AddKey( .6f, .92f );
            throwCurve.AddKey( .7f, .95f );
            throwCurve.AddKey( .8f, .97f );
            throwCurve.AddKey( .9f, .98f );
            throwCurve.AddKey( 1f, 1f );

            float throwTime = throwMaxTravelTime;
            float time = 0f;

            while( time < throwTime )
            {
                if(canHitWalls && HitWall)
                {
                    break;
                }

                float t = time/throwTime;

                transform.position = throwCurve.Evaluate( t ) * (Vector3)throwTarget + startPos;

                time += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            if(canHitWalls && HitWall)
            {
                currentState = CompleteFromHitWall();
            }
            else
            {
                currentState = Return();
            }

            yield break;
        }

        IEnumerator CompleteFromHitWall()
        {
            Dev.Where();

            isAnimating = false;

            yield break;
        }

        IEnumerator Return()
        {
            Dev.Where();

            thread.SetActive( true );

            Vector2 returnTarget = startPos;

            float time = 0f;

            float returnTimeRatio = .6f;

            AnimationCurve returnCurve = new AnimationCurve();
            returnCurve.AddKey( 0f, 0f );
            returnCurve.AddKey( .2f, .1f );
            returnCurve.AddKey( .4f, .2f );
            returnCurve.AddKey( .6f, .4f );
            returnCurve.AddKey( .8f, .6f );
            returnCurve.AddKey( 1f, 1f );

            float returnTime = throwMaxTravelTime * returnTimeRatio;
            Vector3 returnStartPos = transform.position;
            Vector3 returnVector = (Vector3)returnTarget - transform.position;

            while( time < returnTime )
            {
                float t = time/returnTime;

                transform.position = returnCurve.Evaluate( t ) * returnVector + returnStartPos;

                time += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            currentState = Complete();

            yield break;
        }

        IEnumerator Complete()
        {
            Dev.Where();

            meshRenderer.enabled = false;
            isAnimating = false;
            gameObject.SetActive( false );

            yield break;
        }

        static protected float GetAngleToTarget(Vector2 origin, Vector2 target, float offsetX, float offsetY)
        {
            float num = target.y + offsetY - origin.y;
            float num2 = target.x + offsetX - origin.x;
            float num3;
            for(num3 = Mathf.Atan2(num, num2) * 57.2957764f; num3 < 0f; num3 += 360f)
            {
            }
            return num3;
        }

        protected virtual void OnCollisionEnter2D(Collision2D collision)
        {
            if(!isAnimating)
                return;

            if(collision.gameObject.layer == collisionLayer)
            {
                HitWall = true;
            }
        }
    }
}
