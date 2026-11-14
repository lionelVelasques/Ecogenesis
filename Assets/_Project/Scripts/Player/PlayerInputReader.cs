using UnityEngine;
using UnityEngine.InputSystem;

namespace EcoGenesis.Player
{
    /// <summary>
    /// Única clase que conoce el Input System. Define las acciones por código (sin asset de Input Actions)
    /// y expone propiedades simples para el resto del juego.
    /// Controles: WASD/flechas mover, Shift correr, Espacio saltar, E interactuar, Q/R cambiar residuo, ratón cámara.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        private InputAction move;
        private InputAction look;
        private InputAction run;
        private InputAction jump;
        private InputAction interact;
        private InputAction next;
        private InputAction previous;

        /// <summary>Poner en false para bloquear el control (diálogos, menús).</summary>
        public bool InputEnabled { get; set; } = true;

        public Vector2 Move => InputEnabled ? move.ReadValue<Vector2>() : Vector2.zero;
        public Vector2 Look => InputEnabled ? look.ReadValue<Vector2>() : Vector2.zero;
        public bool RunHeld => InputEnabled && run.IsPressed();
        public bool JumpPressed => InputEnabled && jump.WasPressedThisFrame();
        public bool InteractPressed => InputEnabled && interact.WasPressedThisFrame();
        public bool NextItemPressed => InputEnabled && next.WasPressedThisFrame();
        public bool PreviousItemPressed => InputEnabled && previous.WasPressedThisFrame();

        private void Awake()
        {
            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");

            look = new InputAction("Look", InputActionType.Value);
            look.AddBinding("<Mouse>/delta");
            look.AddBinding("<Gamepad>/rightStick").WithProcessor("scaleVector2(x=10,y=10)");

            run = new InputAction("Run", InputActionType.Button, "<Keyboard>/leftShift");
            run.AddBinding("<Gamepad>/leftStickPress");

            jump = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
            jump.AddBinding("<Gamepad>/buttonSouth");

            interact = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");
            interact.AddBinding("<Gamepad>/buttonWest");

            next = new InputAction("NextItem", InputActionType.Button, "<Keyboard>/r");
            next.AddBinding("<Gamepad>/rightShoulder");

            previous = new InputAction("PreviousItem", InputActionType.Button, "<Keyboard>/q");
            previous.AddBinding("<Gamepad>/leftShoulder");
        }

        private void OnEnable()
        {
            move.Enable(); look.Enable(); run.Enable(); jump.Enable();
            interact.Enable(); next.Enable(); previous.Enable();
        }

        private void OnDisable()
        {
            move.Disable(); look.Disable(); run.Disable(); jump.Disable();
            interact.Disable(); next.Disable(); previous.Disable();
        }

        private void OnDestroy()
        {
            move.Dispose(); look.Dispose(); run.Dispose(); jump.Dispose();
            interact.Dispose(); next.Dispose(); previous.Dispose();
        }
    }
}
