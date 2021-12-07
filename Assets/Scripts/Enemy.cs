using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour {

    EnemySettings settings;//スピードとか重さとか魚のデータを定義
    Boid boid;

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
    public Vector3 centre0fBoid;
    [HideInInspector]
    public Vector3 avgBoidHeading;
    [HideInInspector]
    public int numPerceivedBoid;

    // Cached
    Material material;
    public Material blueMat;
    Transform cachedTransform;         //transformへのアクセスは重いのでキャッシュする

    void Awake () {
        cachedTransform = transform;
    }

    public void Initialize (EnemySettings settings,int type) {
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

    public void UpdateEnemy () {
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

        if (numPerceivedBoid != 0) {
            //自分の周りにいる餌
            centre0fBoid /= numPerceivedBoid;

            Vector3 offsetToBoidCentre = (centre0fBoid-position);
            var moveToBoidForce = SteerTowards (offsetToBoidCentre) * settings.movetoboidWeight;

            acceleration += moveToBoidForce;
        }

        if (IsHeadingForCollision ()) {
            Vector3 collisionAvoidDir = ObstacleRays ();         //障害物を避ける方向を取得
            Vector3 collisionAvoidForce = SteerTowards (collisionAvoidDir) * settings.avoidCollisionWeight;   //障害物を避ける力
            acceleration += collisionAvoidForce;
        }

        // if (IsHeadingForBoid ()) {
        //     Vector3 moveToBoidDir = CreateMoveToBoid ();//Boidに向かう方向の取得
        //     Vector3 moveToBoidForce = SteerTowards (moveToBoidDir) * settings.movetoboidWeight;
        //     acceleration += moveToBoidForce;
        // }

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

    // bool IsHeadingForBoid (){
    //     if (boid==null){
    //         Debug.Log("null");
    //         return false;
    //     }
    //     //boidの座標と自分の座標(position)の絶対値が一定値以下だったらtrueを返す
    //     Vector3 dis = boid.centreOfFlockmates - position;
    //     if (dis.magnitude<settings.detectboidRange)return true;
    //     else return false;
    // }

    // Vector3 CreateMoveToBoid (){
    //     Vector3 dis = boid.centreOfFlockmates - position;
    //     return dis;
    // }

    Vector3 SteerTowards (Vector3 vector) {                             //力が大きくなりすぎないように上から抑える
        Vector3 v = vector.normalized * settings.maxSpeed - velocity;
        return Vector3.ClampMagnitude (v, settings.maxSteerForce);
    }

}