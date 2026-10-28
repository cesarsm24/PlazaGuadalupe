using System;
using System.Collections.Generic;
using Guadalupe.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Guadalupe.UI
{
    /// <summary>
    /// Pantalla de inicio superpuesta a la plaza: elección de escenario y franja horaria, controles, créditos
    /// y salida. Con Esc se vuelve a abrir durante la visita.
    /// </summary>
    /// <remarks>
    /// Escribe la elección en la <see cref="SessionState"/>, que la aplica a la escena al momento, de modo que el
    /// fondo ya muestra cómo empezará la visita. Mientras está abierto, <see cref="SessionState.InMenu"/> bloquea
    /// al jugador. Durante la visita se abre con la acción <c>OpenMenu</c> (Esc o Start); dentro del menú, Esc o
    /// el botón derecho del mando cierran la ventana abierta o el propio menú.
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MainMenuController : MonoBehaviour
    {
        private const string HiddenClass = "hidden";
        private const string SelectedClass = "selected";
        private const string StartText = "Comenzar";
        private const string ContinueText = "Continuar";

        [SerializeField] private SessionState _session;
        [SerializeField] private ScenarioDefinition[] _scenarios;
        [SerializeField] private GameObject _menuCamera;
        [SerializeField] private DayPeriod _defaultPeriod = DayPeriod.Afternoon;
        [Tooltip("Acción que abre el menú durante la visita (mapa Player).")]
        [SerializeField] private InputActionReference _openMenuAction;

        private readonly List<Button> _scenarioCards = new List<Button>();
        private readonly Dictionary<DayPeriod, Button> _periodButtons = new Dictionary<DayPeriod, Button>();
        private readonly Dictionary<Button, Action> _periodHandlers = new Dictionary<Button, Action>();
        private VisualElement _root;
        private VisualElement _screen;
        private VisualElement _controlsPanel;
        private VisualElement _creditsPanel;
        private Button _startButton;
        private Button _controlsButton;
        private Button _creditsButton;
        private Button _quitButton;
        private Button _controlsBackButton;
        private Button _creditsBackButton;

        private void OnEnable()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _root.AddToClassList("menu-root");
            _screen = _root.Q("menuScreen");
            _controlsPanel = _root.Q("controlsPanel");
            _creditsPanel = _root.Q("creditsPanel");
            _startButton = _root.Q<Button>("startButton");
            _controlsButton = _root.Q<Button>("controlsButton");
            _creditsButton = _root.Q<Button>("creditsButton");
            _quitButton = _root.Q<Button>("quitButton");
            _controlsBackButton = _root.Q<Button>("controlsBackButton");
            _creditsBackButton = _root.Q<Button>("creditsBackButton");

            BuildScenarioCards(_root.Q("scenarioList"));
            BindPeriod("morningButton", DayPeriod.Morning);
            BindPeriod("afternoonButton", DayPeriod.Afternoon);
            BindPeriod("nightButton", DayPeriod.Night);

            _startButton.clicked += StartVisit;
            _controlsButton.clicked += ShowControls;
            _creditsButton.clicked += ShowCredits;
            _quitButton.clicked += Quit;
            _controlsBackButton.clicked += OnBack;
            _creditsBackButton.clicked += OnBack;
            _root.RegisterCallback<NavigationCancelEvent>(OnNavigationCancel);
            _openMenuAction.action.performed += OnOpenMenu;
        }

        private void OnDisable()
        {
            _startButton.clicked -= StartVisit;
            _controlsButton.clicked -= ShowControls;
            _creditsButton.clicked -= ShowCredits;
            _quitButton.clicked -= Quit;
            _controlsBackButton.clicked -= OnBack;
            _creditsBackButton.clicked -= OnBack;
            _root.UnregisterCallback<NavigationCancelEvent>(OnNavigationCancel);
            foreach (KeyValuePair<Button, Action> pair in _periodHandlers)
            {
                pair.Key.clicked -= pair.Value;
            }

            _periodHandlers.Clear();
            _openMenuAction.action.performed -= OnOpenMenu;
        }

        private void Start()
        {
            // La franja va antes que el escenario: al aplicar la fecha del escenario se recalcula su hora.
            SelectPeriod(_defaultPeriod);
            SelectScenario(0);
            Open();
        }

        private void BuildScenarioCards(VisualElement list)
        {
            list.Clear();
            _scenarioCards.Clear();
            for (int i = 0; i < _scenarios.Length; i++)
            {
                ScenarioDefinition scenario = _scenarios[i];
                var card = new Button { text = string.Empty };
                card.AddToClassList("scenario-card");

                var header = new VisualElement();
                header.AddToClassList("scenario-card__header");
                header.Add(MakeLabel(scenario.DisplayName, "scenario-card__title"));
                header.Add(MakeLabel(scenario.CrowdLabel, "scenario-card__crowd"));
                card.Add(header);
                card.Add(MakeLabel(scenario.Description, "scenario-card__description"));

                // Las tarjetas se recrean en cada OnEnable, así que su manejador desaparece con ellas.
                int index = i;
                card.clicked += () => SelectScenario(index);
                list.Add(card);
                _scenarioCards.Add(card);
            }
        }

        private static Label MakeLabel(string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList(className);
            return label;
        }

        private void BindPeriod(string buttonName, DayPeriod period)
        {
            Button button = _root.Q<Button>(buttonName);
            button.text = DayPeriods.DisplayName(period);
            Action handler = () => SelectPeriod(period);
            button.clicked += handler;
            _periodHandlers[button] = handler;
            _periodButtons[period] = button;
        }

        private void SelectScenario(int index)
        {
            if (_scenarios.Length == 0)
            {
                return;
            }

            int selected = Mathf.Clamp(index, 0, _scenarios.Length - 1);
            for (int i = 0; i < _scenarioCards.Count; i++)
            {
                _scenarioCards[i].EnableInClassList(SelectedClass, i == selected);
            }

            _session.Scenario = _scenarios[selected];
        }

        private void SelectPeriod(DayPeriod period)
        {
            foreach (KeyValuePair<DayPeriod, Button> pair in _periodButtons)
            {
                pair.Value.EnableInClassList(SelectedClass, pair.Key == period);
            }

            _session.Period = period;
        }

        private void Open()
        {
            HidePanels();
            _startButton.text = _session.Started ? ContinueText : StartText;
            _screen.RemoveFromClassList(HiddenClass);
            if (_menuCamera != null)
            {
                _menuCamera.SetActive(true);
            }

            _session.InMenu = true;
            _startButton.Focus();
        }

        private void Close()
        {
            _screen.AddToClassList(HiddenClass);
            if (_menuCamera != null)
            {
                _menuCamera.SetActive(false);
            }

            _session.InMenu = false;
        }

        private void StartVisit()
        {
            _session.StartVisit();
            Close();
        }

        private void OnOpenMenu(InputAction.CallbackContext context)
        {
            // Durante una conversación Esc la termina (lo atiende el panel del chat).
            if (!_session.InMenu && !_session.InConversation)
            {
                Open();
            }
        }

        private void OnNavigationCancel(NavigationCancelEvent evt) => OnBack();

        /// <summary>Esc o el botón de volver del mando: cierra la ventana abierta o, si no hay, el menú.</summary>
        private void OnBack()
        {
            if (!_controlsPanel.ClassListContains(HiddenClass) || !_creditsPanel.ClassListContains(HiddenClass))
            {
                HidePanels();
                _startButton.Focus();
            }
            else if (_session.Started)
            {
                Close();
            }
        }

        private void ShowControls() => ShowPanel(_controlsPanel);

        private void ShowCredits() => ShowPanel(_creditsPanel);

        private void ShowPanel(VisualElement panel)
        {
            HidePanels();
            panel.RemoveFromClassList(HiddenClass);
            // Con mando, el foco pasa al botón de volver de la ventana.
            panel.Q<Button>(className: "dialog__back")?.Focus();
        }

        private void HidePanels()
        {
            _controlsPanel.AddToClassList(HiddenClass);
            _creditsPanel.AddToClassList(HiddenClass);
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
