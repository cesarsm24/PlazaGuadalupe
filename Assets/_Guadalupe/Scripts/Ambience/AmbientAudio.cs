using Guadalupe.Core;
using UnityEngine;

namespace Guadalupe.Ambience
{
    /// <summary>
    /// Sonido ambiente de la plaza: bucle de pájaros (2D) que se apaga de noche y campanadas del reloj del
    /// monasterio (3D, desde la torre) cada cuarto de hora según la hora del <see cref="SimulationClock"/>.
    /// </summary>
    /// <remarks>
    /// En punto suenan tantas campanadas como la hora (1-12); en los cuartos, una, dos o tres. Solo suenan cuando
    /// el cuarto llega durante la visita, no al elegir escenario o franja. El agua de la fuente tiene su propia
    /// <see cref="AudioSource"/>.
    /// </remarks>
    public sealed class AmbientAudio : MonoBehaviour
    {
        [SerializeField] private SessionState _session;
        [SerializeField] private SimulationClock _clock;
        [SerializeField] private SunController _sun;
        [SerializeField] private AudioClip _bell;
        [SerializeField] private AudioSource _bellSource;
        [Tooltip("Fuente 2D en bucle con los pájaros; el volumen lo pone este componente.")]
        [SerializeField] private AudioSource _birdsSource;
        [SerializeField, Range(0f, 1f)] private float _birdsVolume = 0.25f;
        [Tooltip("Segundos entre campanadas consecutivas.")]
        [SerializeField, Min(0.5f)] private float _tollSpacing = 2.6f;

        /// <summary>Cuarto de hora en curso (horas × 4 + cuartos); -1 hasta el primer fotograma.</summary>
        private int _quarter = -1;
        private int _pendingTolls;
        private float _nextToll;
        private float _lastHour;

        /// <summary>Avance máximo de la hora entre dos fotogramas (en minutos) que se considera paso normal del tiempo.</summary>
        private const float MaxStepMinutes = 2f;

        private float _birdsTarget;

        private void OnEnable()
        {
            if (_sun != null)
            {
                _sun.NightChanged += OnNightChanged;
            }
        }

        private void OnDisable()
        {
            if (_sun != null)
            {
                _sun.NightChanged -= OnNightChanged;
            }
        }

        private void Start()
        {
            if (!MissingReferences.Check(this, (_session, "el estado de la sesión"), (_clock, "el reloj")))
            {
                enabled = false;
                return;
            }

            OnNightChanged(_sun != null && _sun.IsNight);
            if (_birdsSource != null && _birdsSource.clip != null)
            {
                // Empieza en un punto aleatorio del bucle para que cada sesión no suene igual.
                _birdsSource.time = Random.Range(0f, _birdsSource.clip.length);
                _birdsSource.volume = _birdsTarget;
                _birdsSource.Play();
            }
        }

        private void OnNightChanged(bool night) => _birdsTarget = night ? 0f : _birdsVolume;

        private void Update()
        {
            // Fundido de unos 3 s al entrar o salir de la noche.
            if (_birdsSource != null)
            {
                _birdsSource.volume = Mathf.MoveTowards(_birdsSource.volume, _birdsTarget, _birdsVolume * Time.deltaTime / 3f);
            }

            if (_bell == null || _bellSource == null)
            {
                return;
            }

            // Solo tocan cuando el reloj pasa un cuarto avanzando durante la visita. Con el menú abierto, o si la hora
            // salta de golpe (al elegir otra franja), el cuarto se actualiza sin campanadas y se cortan las que falten.
            float hour = _clock.Hour;
            int quarter = Mathf.FloorToInt(hour * 4f);
            if (_session.InMenu || IsJump(_lastHour, hour))
            {
                _pendingTolls = 0;
            }
            else if (_quarter >= 0 && quarter != _quarter)
            {
                _pendingTolls = TollsFor(quarter);
                _nextToll = Time.time;
            }

            _quarter = quarter;
            _lastHour = hour;
            if (_pendingTolls > 0 && Time.time >= _nextToll)
            {
                _bellSource.PlayOneShot(_bell);
                _pendingTolls--;
                _nextToll = Time.time + _tollSpacing;
            }
        }

        /// <summary>
        /// Indica si la hora ha saltado de golpe (al elegir otra franja) en lugar de avanzar con el tiempo. La
        /// diferencia se trata como ángulo de un reloj de 24 h (15° por hora) para salvar la medianoche.
        /// </summary>
        internal static bool IsJump(float lastHour, float hour) =>
            Mathf.Abs(Mathf.DeltaAngle(lastHour * 15f, hour * 15f)) > MaxStepMinutes / 60f * 15f;

        /// <summary>Número de campanadas: la hora (1-12) en punto y el número de cuarto en los demás.</summary>
        /// <param name="quarterOfDay">Cuarto de hora del día (0-95).</param>
        internal static int TollsFor(int quarterOfDay)
        {
            int hour = quarterOfDay / 4 % 12;
            int quarter = quarterOfDay % 4;
            return quarter == 0 ? (hour == 0 ? 12 : hour) : quarter;
        }
    }
}
