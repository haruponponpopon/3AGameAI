using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour {

    const int threadGroupSize = 1;

    public EnemySettings settings;
    public ComputeShader compute;
    Enemy[] enemies;
    public bool existTwoSpecies;

    void Start () {
        enemies = FindObjectsOfType<Enemy> ();
        foreach (Enemy b in enemies) {
            int type=existTwoSpecies ? (int)Mathf.Round(Random.value) : 0;     //2つの種族がいる場合は50:50になるように設定
            b.Initialize (settings,type);
        }

    }

    void Update () {
        if (enemies != null) {

            int numEnemies = enemies.Length;
            var enemyData = new EnemyData[numEnemies];

            for (int i = 0; i < enemies.Length; i++) {      //compute shader用のデータを格納
                enemyData[i].position = enemies[i].position;
                enemyData[i].direction = enemies[i].forward;
                enemyData[i].type=enemies[i].type;
            }

            var enemyBuffer = new ComputeBuffer (numEnemies, EnemyData.Size);
            enemyBuffer.SetData (enemyData);

            compute.SetBuffer (0, "enemies", enemyBuffer);
            compute.SetInt ("numEnemies", enemies.Length);
            compute.SetFloat ("viewRadius", settings.perceptionRadius);
            compute.SetFloat ("avoidRadius", settings.avoidanceRadius);

            int threadGroups = Mathf.CeilToInt (numEnemies / (float) threadGroupSize);
            compute.Dispatch (0, threadGroups, 1, 1);     //コンピュートシェーダーを実行

            enemyBuffer.GetData (enemyData);

            for (int i = 0; i < enemies.Length; i++) {                
                enemies[i].avgFlockHeading = enemyData[i].flockHeading;
                enemies[i].centreOfFlockmates = enemyData[i].flockCentre;
                enemies[i].avgAvoidanceHeading = enemyData[i].avoidanceHeading;
                enemies[i].numPerceivedFlockmates = enemyData[i].numFlockmates;

                enemies[i].UpdateEnemy ();
            }

            enemyBuffer.Release ();
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