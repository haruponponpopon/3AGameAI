/*
Copyright (c) 2019 Sebastian Lague
Released under the MIT license
https://github.com/SebLague/Boids/blob/master/LICENSE
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidManager : MonoBehaviour {

    const int threadGroupSize = 1024;

    public BoidSettings settings;
    public ComputeShader compute;
    Boid[] boids;
    Enemy[] enemies;
    public bool existTwoSpecies;

    void Start () {
        boids = FindObjectsOfType<Boid> ();
        enemies = FindObjectsOfType<Enemy> ();
        foreach (Boid b in boids) {
            int type=existTwoSpecies ? (int)Mathf.Round(Random.value) : 0;     //2つの種族がいる場合は50:50になるように設定
            b.Initialize (settings,type);
        }

    }

    void Update () {
        if (boids != null) {

            int numBoids = boids.Length;
            var boidData = new BoidData[numBoids];
            int numEnemies = enemies.Length;
            var enemyData = new EnemyData[numEnemies];

            for (int i = 0; i < boids.Length; i++) {      //compute shader用のデータを格納
                boidData[i].position = boids[i].position;
                boidData[i].direction = boids[i].forward;
                boidData[i].type=boids[i].type;
            }
            for (int i=0; i < enemies.Length; i++) {
                enemyData[i].position = enemies[i].position;
                enemyData[i].direction = enemies[i].forward;
                enemyData[i].type=enemies[i].type;
            }

            var enemyBuffer = new ComputeBuffer (numEnemies, EnemyData.Size);
            enemyBuffer.SetData (enemyData);
            var boidBuffer = new ComputeBuffer (numBoids, BoidData.Size);
            boidBuffer.SetData (boidData);

            compute.SetBuffer (0, "boids", boidBuffer);
            compute.SetBuffer (0, "enemies", enemyBuffer);
            compute.SetInt ("numBoids", boids.Length);
            compute.SetInt ("numEnemies", enemies.Length);
            compute.SetFloat ("viewRadius", settings.perceptionRadius);
            compute.SetFloat ("viewEnemyRadius", settings.avoidEnemyrange);
            compute.SetFloat ("avoidRadius", settings.avoidanceRadius);

            int threadGroups = Mathf.CeilToInt (numBoids / (float) threadGroupSize);
            compute.Dispatch (0, threadGroups, 1, 1);     //コンピュートシェーダーを実行

            boidBuffer.GetData (boidData);

            for (int i = 0; i < boids.Length; i++) {                
                boids[i].avgFlockHeading = boidData[i].flockHeading;
                boids[i].centreOfFlockmates = boidData[i].flockCentre;
                boids[i].avgAvoidanceHeading = boidData[i].avoidanceHeading;
                boids[i].numPerceivedFlockmates = boidData[i].numFlockmates;

                boids[i].numPerceivedEnemy = boidData[i].numEnemy;
                boids[i].centre0fEnemy = boidData[i].enemyCentre;
                boids[i].avgEnemyHeading = boidData[i].enemyHeading;
                
                boids[i].UpdateBoid ();
            }

            boidBuffer.Release ();
            enemyBuffer.Release ();
        }
    }

    public struct BoidData {
        public Vector3 position;
        public Vector3 direction;
        public int type;

        public Vector3 flockHeading;
        public Vector3 flockCentre;
        public Vector3 avoidanceHeading;
        public int numFlockmates;

        public Vector3 enemyHeading;
        public Vector3 enemyCentre;
        public int numEnemy;

        public static int Size {
            get {
                return sizeof (float) * 3 * 7 + sizeof (int)*3;
            }
        }
    }
    public struct EnemyData {
        public Vector3 position;
        public Vector3 direction;
        public int type;

        public Vector3 flockHeading;
        public Vector3 flockCentre;
        public Vector3 avoidanceHeading;
        public int numFlockmates;

        public static int Size {
            get {
                return sizeof (float) * 3 * 5 + sizeof (int)*2;
            }
        }
    }
}