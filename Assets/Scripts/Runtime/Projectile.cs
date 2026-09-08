using UnityEngine;

public class Projectile : MonoBehaviour
{
    EnemyController target;
    float damage;
    float speed = 9.5f;

    public static void Spawn(Vector3 pos, EnemyController target, float damage)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Geschoss";
        go.transform.position = pos;
        go.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
        Destroy(go.GetComponent<Collider>());
        var mat = GameBootstrap.MakeMaterial(new Color(1f, 0.9f, 0.7f), 0.2f, 0.6f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(1.5f, 1.1f, 0.6f));
        go.GetComponent<Renderer>().material = mat;

        var p = go.AddComponent<Projectile>();
        p.target = target;
        p.damage = damage;
    }

    void Update()
    {
        if (target == null || target.Dead)
        {
            Destroy(gameObject);
            return;
        }
        Vector3 targetPos = target.transform.position;
        Vector3 toTarget = targetPos - transform.position;
        float dist = toTarget.magnitude;
        float step = speed * Time.deltaTime;

        if (dist <= step)
        {
            target.TakeDamage(damage);
            Destroy(gameObject);
        }
        else
        {
            transform.position += toTarget.normalized * step;
        }
    }
}
