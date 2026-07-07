using System.Collections.Generic;
using UnityEngine;

namespace Ui
{
    /// <summary>
    /// Окна итога боя (победа/поражение) и рестарт игры.
    /// Показывает нужную панель по событиям EventsSO и озвучивает её.
    /// Кнопка Restart на панелях вызывает Restart() через Button.onClick.
    /// </summary>
    public class EndGameUI : MonoBehaviour
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
        }

        private void ShowLose()
        {
            var winForm = formsData.Find(x => x.formType == FormType.Lose);
       
            if(winForm == null) return;
       
            winForm.form.SetActive(true);

            AudioManager.Instance?.PlayLoseUi();
        }

        /// <summary>
        /// Кнопка Restart в окне итога. Сам рестарт выполняет GameManager
        /// (подписан на EventsSO.OnRestart) — событийная схема, как и остальная логика.
        /// </summary>
        public void Restart()
        {
            formsData.ForEach(x => x.form.SetActive(false));
            events?.RaiseRestart();
        }
    }
}
