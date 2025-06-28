using UnityEngine;

public class speartile : MonoBehaviour
{
    public int damage = 1;
    public float knockbackforce = 5f;

    private void CollisionResolve(Collision2D other_object)
    {
        if (other_object.gameObject.CompareTag("Player"))
        {
            PlayerHealth playerhealth = other_object.gameObject.GetComponent<PlayerHealth>();
            if (playerhealth != null)
            {
                playerhealth.TakeDamage(damage);
            }
            Rigidbody2D rb = other_object.gameObject.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 knockbackDir = (other_object.transform.position - transform.position).normalized;
                rb.AddForce(knockbackDir * knockbackforce, ForceMode2D.Impulse);
            }
        }
    }
}
