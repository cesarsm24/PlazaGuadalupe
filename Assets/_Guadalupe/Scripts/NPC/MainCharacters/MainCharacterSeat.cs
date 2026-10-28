using System.Collections.Generic;
using UnityEngine;

namespace Guadalupe.NPC
{
    /// <summary>
    /// Silla de un personaje que está sentado. Reserva su mesa para que no se siente ningún turista y, mientras la mesa
    /// no está (en Navidad el árbol quita las de alrededor), le cambia a la silla de repuesto con su bebida.
    /// </summary>
    internal sealed class MainCharacterSeat
    {
        private const string ChairPrefix = "Silla";

        // Metros por segundo al acomodarse sobre el asiento.
        private const float LiftSpeed = 0.3f;

        private readonly Transform _body;
        private readonly Transform _hips;
        private readonly Transform _chair;
        private readonly Transform _spareChair;
        private readonly MainCharacterDrink _drinking;
        private readonly List<Transform> _reserved = new List<Transform>();
        private Vector3 _base;
        private float _height;
        private float _lift;

        public MainCharacterSeat(Transform body, Transform hips, Transform chair, Transform spareChair, MainCharacterDrink drinking)
        {
            _body = body;
            _hips = hips;
            _chair = chair;
            _spareChair = spareChair;
            _drinking = drinking;
            ReserveTableOf(chair);
        }

        /// <summary>Silla en la que está sentado ahora (la suya o la de repuesto).</summary>
        public Transform Current { get; private set; }

        private Transform Wanted => _spareChair != null && !_chair.gameObject.activeInHierarchy ? _spareChair : _chair;

        public void SitDown()
        {
            Current = _chair;
            SitAt(Wanted);
        }

        /// <summary>Se cambia de silla si el escenario ha quitado o devuelto su mesa.</summary>
        public void FollowTable()
        {
            Transform wanted = Wanted;
            if (wanted != Current)
            {
                SitAt(wanted);
            }
        }

        /// <summary>Eleva el cuerpo lo justo para que la cadera quede sobre el asiento, como los turistas.</summary>
        public void Lift()
        {
            float target = Seats.Lift(_hips.position.y - _lift, _height + Seats.HipClearance, Seats.MaxLift);
            _lift = Mathf.MoveTowards(_lift, target, LiftSpeed * Time.deltaTime);
            _body.position = _base + Vector3.up * _lift;
        }

        public void Release()
        {
            foreach (Transform spot in _reserved)
            {
                ReservedSpots.Release(spot);
            }

            _reserved.Clear();
        }

        private void SitAt(Transform chair)
        {
            if (chair != Current)
            {
                // La bebida se muda con él y queda igual respecto a la silla.
                if (_drinking != null && _drinking.Group != null)
                {
                    Transform drinks = _drinking.Group;
                    Quaternion turn = chair.rotation * Quaternion.Inverse(Current.rotation);
                    drinks.SetPositionAndRotation(chair.TransformPoint(Current.InverseTransformPoint(drinks.position)), turn * drinks.rotation);
                    _drinking.RememberRest();
                }

                ReserveTableOf(chair);
                Current = chair;
            }

            _body.SetPositionAndRotation(chair.position, Quaternion.Euler(0f, chair.eulerAngles.y, 0f));
            _base = _body.position;
            _lift = 0f;
            _height = Seats.HeightOf(chair, _base.y);
        }

        private void ReserveTableOf(Transform chair)
        {
            Release();
            foreach (Transform piece in chair.parent)
            {
                if (piece.name.StartsWith(ChairPrefix))
                {
                    ReservedSpots.Reserve(piece);
                    _reserved.Add(piece);
                }
            }
        }
    }
}
