/*
Copyright (c) 2019 Sebastian Lague
Released under the MIT license
https://github.com/SebLague/Boids/blob/master/LICENSE
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boid : MonoBehaviour {

    BoidSettings settings;//スピードとか重さとか魚のデータを定義
    Enemy enemy;

    // State
    [HideInInspector]
    public Vector3 position;
    [HideInInspector]
    public Vector3 forward;
    Vector3 velocity;
    public int type;

    // To update:
    Vector3 acceleration;
    [HideInInspector]
    public Vector3 avgFlockHeading;
    [HideInInspector]
    public Vector3 avgAvoidanceHeading;
    [HideInInspector]
    public Vector3 centreOfFlockmates;
    [HideInInspector]
    public int numPerceivedFlockmates;
    [HideInInspector]
    public Vector3 centre0fEnemy;
    [HideInInspector]
    public Vector3 avgEnemyHeading;  //敵から逃げる向き
    [HideInInspector]
    public int numPerceivedEnemy;

    // Cached
    Material material;
    public Material blueMat;
    Transform cachedTransform;         //transformへのアクセスは重いのでキャッシュする

    void Awake () {
        cachedTransform = transform;
    }

    public void Initialize (BoidSettings settings,int type) {
        this.settings = settings;
        this.type=type;
        if(type==1){
            transform.GetChild(1).GetComponent<SkinnedMeshRenderer>().material=blueMat;    //if two species exist, change color
        }
        

        position = cachedTransform.position;
        forward = cachedTransform.forward;

        float startSpeed = (settings.minSpeed + settings.maxSpeed) / 2;
        velocity = transform.forward * startSpeed;
    }

    public void UpdateBoid () {
        Vector3 acceleration = Vector3.zero;

        if (numPerceivedFlockmates != 0) {
            centreOfFlockmates /= numPerceivedFlockmates;               //自分の周りにいる魚の重心を求める

            Vector3 offsetToFlockmatesCentre = (centreOfFlockmates - position);      //重心へのベクトル

            var alignmentForce = SteerTowards (avgFlockHeading) * settings.alignWeight;             //近くの魚が向かう方向に向かう力
            var cohesionForce = SteerTowards (offsetToFlockmatesCentre) * settings.cohesionWeight;  //近くの魚の重心へ向かう力
            var seperationForce = SteerTowards (avgAvoidanceHeading) * settings.seperateWeight;     //近づきすぎるのを避ける力
            acceleration += alignmentForce;
            acceleration += cohesionForce;
            acceleration += seperationForce;
        }

        if (IsHeadingForCollision ()) {
            Vector3 collisionAvoidDir = ObstacleRays ();         //障害物を避ける方向を取得
            Vector3 collisionAvoidForce = SteerTowards (collisionAvoidDir) * settings.avoidCollisionWeight;   //障害物を避ける力
            acceleration += collisionAvoidForce;
        }else if (numPerceivedEnemy != 0) {
            //自分の周りにいる敵
            centre0fEnemy /= numPerceivedEnemy;

            Vector3 offsetToEnemyCentre = (centre0fEnemy-position);
            var moveToEnemyForce = SteerTowards (offsetToEnemyCentre) * settings.avoidEnemyWeight;

            acceleration -= moveToEnemyForce;//反対方向に向かう
        }

        velocity += acceleration * Time.deltaTime;        //加速度を用いて速度を変更する。
        float speed = velocity.magnitude;
        Vector3 dir = velocity / speed;
        speed = Mathf.Clamp (speed, settings.minSpeed, settings.maxSpeed);      //速度のスカラが範囲内に収まるようにする
        velocity = dir * speed;

        cachedTransform.position += velocity * Time.deltaTime;
        cachedTransform.forward = dir;
        position = cachedTransform.position;
        forward = dir;
    }

    bool IsHeadingForCollision () {         //障害物が進む先にあるかどうかを判定
        RaycastHit hit;
        if (Physics.SphereCast (position, settings.boundsRadius, forward, out hit, settings.collisionAvoidDst, settings.obstacleMask)) {
            return true;
        } else { }
        return false;
    }

    Vector3 ObstacleRays () {                                //障害物がない方向ベクトルを取得
        Vector3[] rayDirections = BoidHelper.directions;     //ここに方向ベクトルの候補が格納される

        for (int i = 0; i < rayDirections.Length; i++) {
            Vector3 dir = cachedTransform.TransformDirection (rayDirections[i]);
            Ray ray = new Ray (position, dir);
            if (!Physics.SphereCast (ray, settings.boundsRadius, settings.collisionAvoidDst, settings.obstacleMask)) {
                return dir;          //rayの先に障害物がなかったらその方向を返す。
            }
        }

        return forward;
    }

    Vector3 SteerTowards (Vector3 vector) {                             //力が大きくなりすぎないように上から抑える
        Vector3 v = vector.normalized * settings.maxSpeed - velocity;
        return Vector3.ClampMagnitude (v, settings.maxSteerForce);
    }

}