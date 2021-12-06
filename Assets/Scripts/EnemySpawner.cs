/*
Copyright (c) 2019 Sebastian Lague
Released under the MIT license
https://github.com/SebLague/Boids/blob/master/LICENSE
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour {

    public enum GizmoType { Never, SelectedOnly, Always }

    public Enemy prefab;
    public float spawnRadius = 10;
    public int spawnCount = 1;
    public Color colour;
    public GizmoType showSpawnRegion;

    void Awake () {
        for (int i = 0; i < spawnCount; i++) {                   //位置と向きをランダムに初期化
            Vector3 pos = transform.position + Random.insideUnitSphere * spawnRadius;
            Enemy enemy = Instantiate (prefab);
            enemy.transform.position = pos;
            enemy.transform.forward = Random.insideUnitSphere;
        }
    }
//グラフィック
//https://www.urablog.xyz/entry/2017/10/21/220046
    private void OnDrawGizmos () {
        if (showSpawnRegion == GizmoType.Always) {
            DrawGizmos ();
        }
    }

    void OnDrawGizmosSelected () {
        if (showSpawnRegion == GizmoType.SelectedOnly) {
            DrawGizmos ();
        }
    }

    void DrawGizmos () {

        Gizmos.color = new Color (colour.r, colour.g, colour.b, 0.3f);
        Gizmos.DrawSphere (transform.position, spawnRadius);
    }

}