using UnityEngine;

public class Boss_UI : MonoBehaviour
{
    Animator animator;
    private void Awake()
    {
        animator = GetComponent<Animator>();
    }
    public void Initialize()
    {
        gameObject.SetActive(true);
        animator.Play("Open");
    }

    public void OutInitalize()
    {
        animator.Play("Out");
    }

    public void OutDisable() => gameObject.SetActive(false);
}
