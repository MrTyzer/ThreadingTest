using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThreadingTest
{
    /// <summary>
    /// Задача для выполнения в <see cref="FixedThreadPool"/>.
    /// </summary>
    public class Task
    {
        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр задачи для выполнения в <see cref="FixedThreadPool"/>
        /// делегатом тела задачи.
        /// </summary>
        /// <param name="taskBody">
        /// Делегат тела задачи.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Делегат тела задачи не задан.
        /// </exception>
        public Task(Action taskBody)
        {
            TaskBody = taskBody ?? throw new ArgumentNullException("taskBody", "Делегат тела задачи не задан.");
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Начинает выполнение задачи.
        /// </summary>
        public void Execute()
        {
            TaskBody();
        }

        #endregion

        #region Private properties

        /// <summary>
        /// Получает/устанавливает делегат тела задачи.
        /// </summary>
        private Action TaskBody { get; set; }

        #endregion
    }
}