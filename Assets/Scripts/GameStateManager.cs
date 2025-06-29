using UnityEngine;

/// <summary>
/// ������ ���¸� �����ϴ� �������Դϴ�.
/// Menu: ���� �޴� ����
/// Mode7: �ΰ��� �÷��� ����
/// Ending: ���� ũ���� �Ǵ� ��� ȭ�� ����
/// </summary>
public enum GameState
{
    Menu,
    Mode7,
    Ending
}

/// <summary>
/// ������ ��ü ���¸� �����ϴ� �̱��� Ŭ�����Դϴ�.
/// �� ���¿� �´� UI�� Ȱ��ȭ�ϰ� ���� ��ȯ�� ó���մϴ�.
/// </summary>
public class GameStateManager : MonoBehaviour
{
    // --- �̱��� �ν��Ͻ� ---
    // 'Instance'�� ���� �ٸ� ��ũ��Ʈ���� �� �Ŵ����� ���� ������ �� �ֽ��ϴ�.
    public static GameStateManager Instance { get; private set; }

    // --- ���� ���� ---
    // ���� ������ ���¸� �����մϴ�.
    public GameState CurrentState { get; private set; }

    // --- UI �г� ���� ---
    // ����Ƽ �������� �ν����� â���� �� ���¿� �´� UI �г�(GameObject)�� �����ؾ� �մϴ�.
    [Header("State UI Panels")]
    [Tooltip("�޴� ������ �� Ȱ��ȭ�� UI �г�")]
    public GameObject menuPanel;

    [Tooltip("Mode7(�ΰ���) ������ �� Ȱ��ȭ�� UI �г�")]
    public GameObject mode7Panel;

    [Tooltip("���� ������ �� Ȱ��ȭ�� UI �г�")]
    public GameObject endingPanel;

    /// <summary>
    /// ���� ���� �� �� �� ȣ��Ǵ� Awake �޼����Դϴ�.
    /// �̱��� �ν��Ͻ��� �����մϴ�.
    /// </summary>
    private void Awake()
    {
        // �̱��� ���� ����
        // ���� �ν��Ͻ��� ���� ���ٸ�, �� ��ü�� �ν��Ͻ��� �����մϴ�.
        if (Instance == null)
        {
            Instance = this;
            // (���� ����) ���� ��ȯ�Ǿ �� ���ӿ�����Ʈ�� �ı����� �ʰ� �մϴ�.
            // DontDestroyOnLoad(gameObject);
        }
        // �ν��Ͻ��� �̹� �����Ѵٸ�, �� ��ü�� �ߺ��̹Ƿ� �ı��մϴ�.
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// ù ������ ������Ʈ ���� ȣ��Ǵ� Start �޼����Դϴ�.
    /// ������ �ʱ� ���¸� 'Menu'�� �����մϴ�.
    /// </summary>
    private void Start()
    {
        // ������ ���۵Ǹ� �⺻������ �޴� ���·� ��ȯ�մϴ�.
        ChangeState(GameState.Menu);
    }

    /// <summary>
    /// ������ ���¸� ���ο� ���·� �����մϴ�.
    /// </summary>
    /// <param name="newState">������ ���ο� ���� ����</param>
    public void ChangeState(GameState newState)
    {
        CurrentState = newState;

        // �켱 ��� �г��� ��Ȱ��ȭ�Ͽ� ����� ���¿��� �����մϴ�.
        // �� �г��� null�� �ƴ��� Ȯ���Ͽ� NullReferenceException�� �����մϴ�.
        if (menuPanel != null) menuPanel.SetActive(false);
        if (mode7Panel != null) mode7Panel.SetActive(false);
        if (endingPanel != null) endingPanel.SetActive(false);

        // ���ο� ���¿� ���� ������ �г��� Ȱ��ȭ�մϴ�.
        switch (newState)
        {
            case GameState.Menu:
                if (menuPanel != null) menuPanel.SetActive(true);
                Debug.Log("���� ����: �޴�");
                // ���⿡ �޴� ���¿� ������ �� �ʿ��� �߰� ������ ���� �� �ֽ��ϴ�. (��: �޴� ������� ���)
                break;

            case GameState.Mode7:
                if (mode7Panel != null) mode7Panel.SetActive(true);
                Debug.Log("���� ����: Mode7");
                // ���⿡ Mode7 ���¿� ������ �� �ʿ��� �߰� ������ ���� �� �ֽ��ϴ�. (��: ���� ����, Ÿ�̸� �۵�)
                break;

            case GameState.Ending:
                if (endingPanel != null) endingPanel.SetActive(true);
                Debug.Log("���� ����: ����");
                // ���⿡ ���� ���¿� ������ �� �ʿ��� �߰� ������ ���� �� �ֽ��ϴ�. (��: ��� ���� ǥ��, ���� BGM ���)
                break;
        }
    }

    // --- UI ��ư ��� ȣ���� �� �ִ� ���� �Լ��� ---
    // ����Ƽ�� ��ư OnClick() �̺�Ʈ�� �� �Լ����� �����Ͽ� ���¸� ���� ��ȯ�� �� �ֽ��ϴ�.

    public void GoToMode7()
    {
        ChangeState(GameState.Mode7);
    }

    public void GoToEnding()
    {
        ChangeState(GameState.Ending);
    }

    public void GoToMenu()
    {
        ChangeState(GameState.Menu);
    }
}
