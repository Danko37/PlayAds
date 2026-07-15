using System.Collections.Generic;
using UnityEngine;

namespace Ui
{
    /// <summary>
    /// Окна итога боя (победа/поражение) и рестарт игры.
    /// Показывает нужную панель по событиям EventsSO и озвучивает её.
    /// Кнопка Restart на панелях вызывает Restart() через Button.onClick.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [SerializeField]
        private EventsSO events;
    
        [SerializeField]
        private List<FormData> formsData;

        private void OnEnable()
        {
            if (events == null)
                return;

            events.OnHeroWin += ShowWin;
            events.OnHeroLose += ShowLose;
        }

        private void OnDisable()
        {
            if (events == null)
                return;

            events.OnHeroWin -= ShowWin;
            events.OnHeroLose -= ShowLose;
        }

        private void ShowWin()
        {
            var winForm = formsData.Find(x => x.formType == FormType.Win);

            if(winForm == null) return;

            winForm.form.SetActive(true);

            AudioManager.Instance?.PlayWinUi();

            // Показан end card — сигнализируем сети о завершении игры (требуют Mintegral/Vungle и др.).
            Luna.Unity.LifeCycle.GameEnded();
        }

        private void ShowLose()
        {
            var winForm = formsData.Find(x => x.formType == FormType.Lose);

            if(winForm == null) return;

            winForm.form.SetActive(true);

            AudioManager.Instance?.PlayLoseUi();

            // Показан end card — сигнализируем сети о завершении игры.
            Luna.Unity.LifeCycle.GameEnded();
        }

        /// <summary>
        /// Кнопка Restart в окне итога. Сам рестарт выполняет GameManager
        /// (подписан на EventsSO.OnRestart) — событийная схема, как и остальная логика.
        /// </summary>
        public void Restart()
        {
            // Аналитика Luna: клик по CTA — конверсия (низ воронки).
            Luna.Unity.Analytics.LogEvent("cta_click", 0);

            // CTA "PLAY NOW": уводим пользователя в стор (в редакторе/превью — no-op).
            // Некоторые сети требуют этот вызов, иначе отклоняют креатив (LP3006).
            Luna.Unity.Playable.InstallFullGame();

            formsData.ForEach(x => x.form.SetActive(false));
            events?.RaiseRestart();
        }
    }
}
