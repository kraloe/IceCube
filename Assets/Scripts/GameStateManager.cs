using UnityEngine;

/// <summary>
/// 게임의 상태를 정의하는 열거형입니다.
/// Menu: 메인 메뉴 상태
/// Mode7: 인게임 플레이 상태
/// Ending: 엔딩 크레딧 또는 결과 화면 상태
/// </summary>
public enum GameState
{
    Menu,
    Mode7,
    Ending
}

/// <summary>
/// 게임의 전체 상태를 관리하는 싱글턴 클래스입니다.
/// 각 상태에 맞는 UI를 활성화하고 상태 전환을 처리합니다.
/// </summary>
public class GameStateManager : MonoBehaviour
{
    // --- 싱글턴 인스턴스 ---
    // 'Instance'를 통해 다른 스크립트에서 이 매니저에 쉽게 접근할 수 있습니다.
    public static GameStateManager Instance { get; private set; }

    // --- 상태 변수 ---
    // 현재 게임의 상태를 저장합니다.
    public GameState CurrentState { get; private set; }

    // --- UI 패널 참조 ---
    // 유니티 에디터의 인스펙터 창에서 각 상태에 맞는 UI 패널(GameObject)을 연결해야 합니다.
    [Header("State UI Panels")]
    [Tooltip("메뉴 상태일 때 활성화될 UI 패널")]
    public GameObject menuPanel;

    [Tooltip("Mode7(인게임) 상태일 때 활성화될 UI 패널")]
    public GameObject mode7Panel;

    [Tooltip("엔딩 상태일 때 활성화될 UI 패널")]
    public GameObject endingPanel;

    /// <summary>
    /// 게임 시작 시 한 번 호출되는 Awake 메서드입니다.
    /// 싱글턴 인스턴스를 설정합니다.
    /// </summary>
    private void Awake()
    {
        // 싱글턴 패턴 구현
        // 만약 인스턴스가 아직 없다면, 이 객체를 인스턴스로 지정합니다.
        if (Instance == null)
        {
            Instance = this;
            // (선택 사항) 씬이 전환되어도 이 게임오브젝트가 파괴되지 않게 합니다.
            // DontDestroyOnLoad(gameObject);
        }
        // 인스턴스가 이미 존재한다면, 이 객체는 중복이므로 파괴합니다.
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 첫 프레임 업데이트 전에 호출되는 Start 메서드입니다.
    /// 게임의 초기 상태를 'Menu'로 설정합니다.
    /// </summary>
    private void Start()
    {
        // 게임이 시작되면 기본적으로 메뉴 상태로 전환합니다.
        ChangeState(GameState.Menu);
    }

    /// <summary>
    /// 게임의 상태를 새로운 상태로 변경합니다.
    /// </summary>
    /// <param name="newState">변경할 새로운 게임 상태</param>
    public void ChangeState(GameState newState)
    {
        CurrentState = newState;

        // 우선 모든 패널을 비활성화하여 깔끔한 상태에서 시작합니다.
        // 각 패널이 null이 아닌지 확인하여 NullReferenceException을 방지합니다.
        if (menuPanel != null) menuPanel.SetActive(false);
        if (mode7Panel != null) mode7Panel.SetActive(false);
        if (endingPanel != null) endingPanel.SetActive(false);

        // 새로운 상태에 따라 적절한 패널을 활성화합니다.
        switch (newState)
        {
            case GameState.Menu:
                if (menuPanel != null) menuPanel.SetActive(true);
                Debug.Log("상태 변경: 메뉴");
                // 여기에 메뉴 상태에 진입할 때 필요한 추가 로직을 넣을 수 있습니다. (예: 메뉴 배경음악 재생)
                break;

            case GameState.Mode7:
                if (mode7Panel != null) mode7Panel.SetActive(true);
                Debug.Log("상태 변경: Mode7");
                // 여기에 Mode7 상태에 진입할 때 필요한 추가 로직을 넣을 수 있습니다. (예: 게임 시작, 타이머 작동)
                break;

            case GameState.Ending:
                if (endingPanel != null) endingPanel.SetActive(true);
                Debug.Log("상태 변경: 엔딩");
                // 여기에 엔딩 상태에 진입할 때 필요한 추가 로직을 넣을 수 있습니다. (예: 결과 점수 표시, 엔딩 BGM 재생)
                break;
        }
    }

    // --- UI 버튼 등에서 호출할 수 있는 헬퍼 함수들 ---
    // 유니티의 버튼 OnClick() 이벤트에 이 함수들을 연결하여 상태를 쉽게 전환할 수 있습니다.

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
