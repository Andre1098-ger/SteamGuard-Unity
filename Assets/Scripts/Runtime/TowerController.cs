using UnityEngine;

public class TowerController : MonoBehaviour
{
    GameBootstrap game;
    Transform turretHead;
    Vector3 muzzleOffsetLocal;

    public float damage = 8f;
    public float range = 3.4f;
    public float fireRate = 2.2f;
    float cooldown = 0f;

    public void Init(GameBootstrap game, Transform turretHead, Vector3 muzzleOffsetLocal)
    {
        this.game = game;
        this.turretHead = turretHead;
        this.muzzleOffsetLocal = muzzleOffsetLocal;
    }

    void Update()
    {
        cooldown -= Time.deltaTime;
        var target = FindTarget();
        if (target == null) return;

        Vector3 dir = target.transform.position - turretHead.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
            turretHead.rotation = Quaternion.LookRotation(dir);

        if (cooldown <= 0f)
        {
            Fire(target);
            cooldown = 1f / fireRate;
        }
    }

    EnemyController FindTarget()
    {
        EnemyController best = null;
        float bestProgress = -1f;
        foreach (var e in game.Enemies)
        {
            float d = Vector3.Distance(e.transform.position, transform.position);
            if (d <= range && e.PathProgress > bestProgress)
            {
                bestProgress = e.PathProgress;
                best = e;
            }
        }
        return best;
    }

    void Fire(EnemyController target)
    {
        Vector3 muzzleWorld = turretHead.TransformPoint(muzzleOffsetLocal);
        Projectile.Spawn(muzzleWorld, target, damage);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.3f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
