using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; 
#endif

[RequireComponent(typeof(MazeGenerator))]
[RequireComponent(typeof(MazeBuilder))]
[RequireComponent(typeof(ContentGenerator))]
public class MazeBootstrap : MonoBehaviour
{
    [SerializeField] MazeGenerator generator;
    [SerializeField] MazeBuilder builder;
    [SerializeField] ContentGenerator contentGenerator;

    [Header("Run Options")]
    public bool buildOnStart = true;

    [Header("Hotkey Reroll")]
    public bool enableHotkey = true;
    public KeyCode hotkey = KeyCode.R;

#if ENABLE_INPUT_SYSTEM
    private InputAction rerollAction;
#endif

    void Reset()
    {
        generator = GetComponent<MazeGenerator>();
        builder = GetComponent<MazeBuilder>();
        contentGenerator = GetComponent<ContentGenerator>();
    }

    void Awake()
    {
        if (!generator) generator = GetComponent<MazeGenerator>();
        if (!builder) builder = GetComponent<MazeBuilder>();
        if (!contentGenerator) contentGenerator = GetComponent<ContentGenerator>();
#if ENABLE_INPUT_SYSTEM
        // Bind to "R" 
        rerollAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/r");
        rerollAction.performed += _ => { if (enableHotkey) Rebuild(); };
        rerollAction.Enable();
#endif
    }

    void OnDestroy()
    {
#if ENABLE_INPUT_SYSTEM
        rerollAction?.Disable();
        rerollAction?.Dispose();
#endif
    }

    void Start()
    {
        if (buildOnStart) Rebuild();
    }

    [ContextMenu("Rebuild Now")]
    public void Rebuild()
    {
        if (!generator || !builder) { Debug.LogError("Missing MazeGenerator/MazeBuilder refs"); return; }
        var data = generator.Generate();
        builder.ClearChildren();
        builder.Build(data);
        contentGenerator.Generate(data);
        
    }
}
