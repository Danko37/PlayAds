using System.Collections.Generic;
using UnityEngine;

namespace Characters
{
    public class InteractBase : MonoBehaviour
    {
        // Реестр кликабельных — клик по сущности подбираем экранно, без физ-рейкаста (Luna-safe).
        public static readonly List<InteractBase> Interactables = new();

        public EntityType entityType;

        //когдща кликаем по врагу - идем к этой точке
        [SerializeField]
        public Transform moveTarget;

        protected virtual void OnEnable() => Interactables.Add(this);

        protected virtual void OnDisable() => Interactables.Remove(this);
    }
}
