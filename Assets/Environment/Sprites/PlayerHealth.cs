using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int health = 2;

    public void TakeDamage(int damage)
    {
        health -= damage;
        Debug.Log("Player hit! Current health: " + health);
        if (health <= 0)
        {
            Debug.Log("Player Died!");
        }
    }
}