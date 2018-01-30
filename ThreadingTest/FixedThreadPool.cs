using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ThreadingTest
{
    /// <summary>
    /// Пул потоков, имеющий фиксированный размер.
    /// </summary>
    public sealed class FixedThreadPool
    {
        #region Nested type: PriorityQueue

        /// <summary>
        /// Вложенный класс очереди с приоритетами.
        /// </summary> 
        class PriorityQueue
        {
            #region Constructors

            /// <summary>
            /// Конструктор очереди задач.
            /// </summary>
            /// <param name="parent">
            /// Ссылка на экземпляр родительского класса.
            /// </param>
            public PriorityQueue(FixedThreadPool parent)
            {
                _parent = parent;
            }

            #endregion

            #region Public methods

            /// <summary>
            /// Добавляет задачу <paramref name="task"/> 
            /// в конец очереди с учетом ее приоритета <paramref name="priority"/>.
            /// </summary>
            /// <param name="task">
            /// Задача для поставки в очередь.
            /// </param>
            /// <param name="priority">
            /// Приоритет задачи.
            /// </param>
            public void Enqueue(Task task, Priority priority)
            {
                lock (_parent._queuedTasksLock)
                {
                    //Определяем задачу в нужную очередь в соответствии с ее приоритетом.
                    switch (priority)
                    {
                        case Priority.LOW:
                            _lowQueue.Enqueue(task);
                            break;
                        case Priority.NORMAL:
                            _normalQueue.Enqueue(task);
                            break;
                        case Priority.HIGH:
                            _highQueue.Enqueue(task);
                            break;
                    }
                    //Подаем сигнал потокам о наличии нового элемента в очереди.
                    _parent._queueNotEmpty.Set();
                }
            }

            /// <summary>
            /// Удаляет задачу из начала очереди и возвращает ее.
            /// </summary>
            public Task Dequeue()
            {
                Task result = null;
                lock (_parent._queuedTasksLock)
                {
                    if (!_highQueue.Any() && !_normalQueue.Any() && !_lowQueue.Any())
                    {
                        //Подаем сигнал потокам об отсутствии элементов в очереди.
                        _parent._queueNotEmpty.Reset();
                        result = null;
                        if (_parent._isStopped)
                        {
                            //Подаем сигнал методу Stop() о завершении всех задач в очереди.
                            _parent._poolStoppedGate.Set();
                            //Обновляем счетчик для задач с приоритетом NORMAL.
                            _highPriorityTaskCounter = 0;
                        }
                    }
                    else if (!_highQueue.Any() && !_normalQueue.Any())
                    {
                        //При отсутствии задач с приоритетами HIGH и NORMAL
                        //поток выполняет задачу с приоритетом LOW.
                        result = _lowQueue.Dequeue();
                    }
                    else if (_normalQueue.Any() && _highPriorityTaskCounter >= _highPriorityTaskFactor)
                    {
                        //При заданных условиях поток выполняет задачу с приоритетом NORMAL.
                        _highPriorityTaskCounter = 0;
                        result = _normalQueue.Dequeue();
                    }
                    else if (!_highQueue.Any())
                    {
                        //При отсутствии задач с приоритетом HIGH
                        //поток выполняет задачу с приоритетом NORMAL.
                        _highPriorityTaskCounter = 0;
                        result = _normalQueue.Dequeue();
                    }
                    else
                    {
                        //Поток выполняет задачу с приоритетом HIGH.
                        _highPriorityTaskCounter++;
                        result = _highQueue.Dequeue();
                    }
                }
                return result;
            }

            #endregion

            #region Private data

            /// <summary>
            /// Счётчик задач с высоким приоритетом, запущенных на выполнение. Каждая запущенная задача
            /// с высоким приоритетом увеличивает это значение на единицу, каждая запущенная задача с
            /// обычным приоритетом уменьшает это значение на <see cref="_highPriorityTaskFactor"/>.
            /// </summary>
            private int _highPriorityTaskCounter = 0;

            /// <summary>
            /// Ссылка на экземпляр родительского класса.
            /// </summary>
            private FixedThreadPool _parent;

            /// <summary>
            /// Очередь для задач с приоритетом HIGH.
            /// </summary>
            private Queue<Task> _highQueue = new Queue<Task>();

            /// <summary>
            /// Очередь для задач с приоритетом NORMAL.
            /// </summary>
            private Queue<Task> _normalQueue = new Queue<Task>();

            /// <summary>
            /// Очередь для задач с приоритетом LOW.
            /// </summary>
            private Queue<Task> _lowQueue = new Queue<Task>();

            #endregion

        }

        #endregion

        #region Constructors

        /// <summary>
        /// Инициализирует новый экземпляр пул потоков максимальным количеством одновременно
        /// выполняемых задач.
        /// </summary>
        /// <param name="poolSize">
        /// Количество потоков в пуле.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Неправильно заданное количество потоков.
        /// </exception>
        public FixedThreadPool(int poolSize)
        {
            if (poolSize <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    "poolSize",
                    "Размер пула потоков должен быть больше нуля.");
            }
            _taskQueue = new PriorityQueue(this);
            for (int threadIndex = 0; threadIndex < poolSize; threadIndex++)
            {
                string threadName = string.Format("Thread #{0}", threadIndex);
                Thread taskThread = new Thread(new ThreadStart(ThreadEntryPoint));
                taskThread.Name = threadName;
                taskThread.Start();
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Ставит задачу <paramref name="task"/> в очередь на выполнение с приоритетом 
        /// <paramref name="priority"/>.
        /// </summary>
        /// <param name="task">
        /// Задача для постановки в очередь на выполнение.
        /// </param>
        /// <param name="priority">
        /// Приоритет задачи.
        /// </param>
        /// <returns>
        /// <see langword="true"/> - задача поставлена в очередь на выполнение. 
        /// <see langword="false"/> - задача не была поставлена в очередь на выполнение, так как
        /// работа пула потоков была остановлена.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Задача для постановки в очередь на выполнения не задана.
        /// </exception>
        public bool Execute(Task task, Priority priority)
        {
            if (task == null)
            {
                throw new ArgumentNullException(
                    "task", "Задача для постановки в очередь не задана.");
            }
            if (_isStopped)
            {
                // Запрошена остановка.
                // Отклонять новые задачи.
                return false;
            }
            else
            {
                // Добавить задачу в очередь.
                _taskQueue.Enqueue(task, priority);
                return true;
            }
        }

        /// <summary>
        /// Останавливает добавление задач в очередь пула потоков, без очищения очереди. Возвращает
        /// выполнение только после окончания всех имеющихся задач в очереди.
        /// </summary>
        public void Stop()
        {
            // Выставить признак окончания работы пула.
            _isStopped = true;
            //Запустить все потоки, если они были остановлены в связи с отсутствием задач в очереди.
            //Это позволит завершить метод Stop в случае, если он был вызван после опустошения очереди.
            _queueNotEmpty.Set();
            // Дождаться окончания выполнения всех задач, оставшихся в очереди.
            _poolStoppedGate.WaitOne();
        }

        /// <summary>
        /// Возобновляет работу очереди, позволяя добавлять в нее новые задачи.
        /// </summary>
        public void Go()
        {
            _isStopped = false;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Точка входа для создаваемых потоков.
        /// </summary>
        private void ThreadEntryPoint()
        {
            Task currentTask = null;
            while (true)
            {
                //Поток ждет сигнал о поступлении задачи в очередь.
                _queueNotEmpty.WaitOne();
                lock (_currentTaskLock)
                {
                    currentTask = _taskQueue.Dequeue();
                }
                if (currentTask == null)
                {
                    //Если очередь задач пуста, Dequeue() возвращает null 
                    //и поток возвращается в режим ожидания.
                    continue;
                }
                else
                {
                    currentTask.Execute();
                }
            }
        }

        #endregion

        #region Private data

        /// <summary>
        /// Получает/устанавливает признак того, остановлена ли работа пула.
        /// </summary>
        /// <value>
        /// <see langword="true"/> - работа пула остановлена, дальнейшее добавление задач в очередь
        /// не возможно. <see langword="false"/> - работа пула продолжается.
        /// </value>
        private volatile bool _isStopped = false;

        /// <summary>
        /// Количество задач с высоким приоритетом, которое должно быть поставлено в очередь до
        /// того, как можно будет поставить задачу с обычным приоритетом, если в очереди имеются
        /// задачи с высоким приоритетом.
        /// </summary>
        private const int _highPriorityTaskFactor = 3; 

        /// <summary>
        /// Барьер остановки работы пула. Поднимается, когда запрошена остановка и все задачи
        /// завершили своё выполнение.
        /// </summary>
        private readonly ManualResetEvent _poolStoppedGate = new ManualResetEvent(false);

        /// <summary>
        /// Событие, контролирующее работу потоков.
        /// Останавливает работу потоков при отсутствии задач в очереди.
        /// </summary>
        private readonly ManualResetEvent _queueNotEmpty = new ManualResetEvent(false);

        /// <summary>
        /// Список задач, поставленных в очередь на выполнение.
        /// </summary>
        private readonly PriorityQueue _taskQueue;

        /// <summary>
        /// Объект синхронизации доступа к списку задач <see cref="_taskQueue"/>.
        /// </summary>
        private readonly object _queuedTasksLock = new object();

        /// <summary>
        /// Объект синхронизации доступа к локальной переменной <see cref="currentTask"/>.
        /// </summary>
        private readonly object _currentTaskLock = new object();

        #endregion
    }
}