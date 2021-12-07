using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour {

    const int threadGroupSize = 1;

    public EnemySettings settings;
    public ComputeShader compute;
    Enemy[] enemies;
    Boid[] boids;
    public bool existTwoSpecies;

    void Start () {
        enemies = FindObjectsOfType<Enemy> ();
        boids = FindObjectsOfType<Boid> ();
        foreach (Enemy b in enemies) {
            int type=existTwoSpecies ? (int)Mathf.Round(Random.value) : 0;     //2つの種族がいる場合は50:50になるように設定
            b.Initialize (settings,type);
        }

    }

    void Update () {
        if (enemies != null) {

            int numEnemies = enemies.Length;
            var enemyData = new EnemyData[numEnemies];
            int numBoids = boids.Length;
            var boidData = new BoidData[numBoids];

            for (int i = 0; i < enemies.Length; i++) {      //compute shader用のデータを格納
                enemyData[i].position = enemies[i].position;
                enemyData[i].direction = enemies[i].forward;
                enemyData[i].type=enemies[i].type;
            }
            for (int i=0; i < boids.Length; i++) {
                boidData[i].position = boids[i].position;
                boidData[i].direction = boids[i].forward;
                boidData[i].type = boids[i].type;
            }

            var enemyBuffer = new ComputeBuffer (numEnemies, EnemyData.Size);
            enemyBuffer.SetData (enemyData);
            var boidBuffer = new ComputeBuffer (numBoids, BoidData.Size);
            boidBuffer.SetData (boidData);

            compute.SetBuffer (0, "enemies", enemyBuffer);
            compute.SetBuffer (0, "boids", boidBuffer);
            compute.SetInt ("numEnemies", enemies.Length);
            compute.SetInt ("numBoids", boids.Length);
            compute.SetFloat ("viewRadius", settings.perceptionRadius);
            compute.SetFloat ("viewBoidRadius", settings.detectboidRange);
            compute.SetFloat ("avoidRadius", settings.avoidanceRadius);

            int threadGroups = Mathf.CeilToInt (numEnemies / (float) threadGroupSize);
            compute.Dispatch (0, threadGroups, 1, 1);     //コンピュートシェーダーを実行

            enemyBuffer.GetData (enemyData);

            for (int i = 0; i < enemies.Length; i++) {                
                enemies[i].avgFlockHeading = enemyData[i].flockHeading;
                enemies[i].centreOfFlockmates = enemyData[i].flockCentre;
                enemies[i].avgAvoidanceHeading = enemyData[i].avoidanceHeading;
                enemies[i].numPerceivedFlockmates = enemyData[i].numFlockmates;

                enemies[i].numPerceivedBoid = enemyData[i].numBoid;
                enemies[i].centre0fBoid = enemyData[i].boidCentre;
                enemies[i].avgBoidHeading = enemyData[i].boidHeading;

                enemies[i].UpdateEnemy ();
            }

            enemyBuffer.Release ();
            boidBuffer.Release();
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

        public Vector3 boidHeading;
        public Vector3 boidCentre;
        public int numBoid;

        public static int Size {
            get {
                return sizeof (float) * 3 * 7 + sizeof (int)*3;
            }
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

        public static int Size {
            get {
                return sizeof (float) * 3 * 5 + sizeof (int)*2;
            }
        }
    }
}