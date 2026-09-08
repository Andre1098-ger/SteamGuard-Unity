using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    GameBootstrap game;
    List<Vector3> waypoints;
    int wpIndex = 0;
    float speed;
    int reward;

    public float hp;
    public float maxHp;
    public float PathProgress { get; private set; }
    public bool Dead { get; private set; }

    public void Init(GameBootstrap game, List<Vector3> waypoints, float hp, float speed, int reward)
    {
        this.game = game;
        this.waypoints = waypoints;
        this.hp = hp;
        this.maxHp = hp;
        this.speed = speed;
        this.reward = reward;
    }

    void Update()
    {
        if (Dead) return;
        if (wpIndex + 1 >= waypoints.Count)
        {
            Dead = true;
            game.OnEnemyReachedEnd(this);
            Destroy(gameObject);
            return;
        }

        Vector3 targetPos = waypoints[wpIndex + 1] + Vector3.up * 0.2f;
        Vector3 toTarget = targetPos - transform.position;
        float dist = toTarget.magnitude;
        float step = speed * Time.deltaTime;

        if (dist <= step)
        {
            wpIndex++;
            PathProgress = wpIndex;
        }
        else
        {
            transform.position += toTarget.normalized * step;
            if (toTarget.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(toTarget);
            PathProgress = wpIndex + (1f - dist / 1f);
        }
    }

    public void TakeDamage(float dmg)
    {
        if (Dead) return;
        hp -= dmg;
        if (hp <= 0f)
        {
            Dead = true;
            game.OnEnemyDied(this, reward);
            Destroy(gameObject);
        }
    }
}
